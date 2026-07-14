# Fantasy 跨 Scene 通信指南

## 概述

Fantasy 支持同进程和跨进程两种场景间通信模式。当前项目所有 Scene（Gate / Http / Avatars）配置在同一个 Process 上，因此**同进程**通信是默认路径——不走真正的网络，仅通过 `ProcessSession` 在内存中序列化转发。

本文档基于 "Gate → Avatars 加载玩家" 这个实际案例，整理出标准的跨 Scene 通信步骤。

---

## 架构总览

```
Client ──WS──→ Gate Scene                    Avatars Scene
                  │                              │
                  │  GetComponent<SessionService> │
                  ▼                              │
           SessionService                        │
                  │                              │
                  │  Scene.Call(address, req)     │
                  │  ──────────────────────────→  │
                  │         LoadPlayerReq         │
                  │                              │
                  │                     AddressRPC<FScene, Req, Res>
                  │                         → AvatarsService.LoadPlayer()
                  │                              │
                  │  ←──────────────────────────  │
                  │         LoadPlayerRes         │
```

### 关键约定

| 组件 | 位置 | 职责 |
|------|------|------|
| `*.proto` | `Config/NetworkProtocol/{Inner,Outer}/` | 协议定义（服务端导出） |
| `*.cs` (生成) | `Entity/Generate/NetworkProtocol/` | 协议实体（`IMessage` / `IAddressRequest`） |
| `AddressRPC<Scene, Req, Res>` | `Hotfix/Scene/Xxx/Handler/` | 内网消息 Handler，附着在目标 Scene 上 |
| `Scene Config` | `Fantasy.config` → `SceneConfigData` | 定义各 Scene 的 ID、端口、类型 |
| `Scene.Call(address, request)` | `Scene` 实例方法 | 发起跨 Scene 的 RPC 调用 |
| `SceneConfig.Address` | `SceneConfigData` 中预计算 | 作为 `Scene.Call` 的目标地址 |

---

## 完整实现步骤

### 第一步：定义内网协议

在 `Config/NetworkProtocol/Inner/` 下创建/编辑 `.proto` 文件。

```protobuf
// Config/NetworkProtocol/Inner/Avatars.proto
syntax = "proto3";
package BattleOfCell.Message;

/// Gate -> Avatars 加载玩家地址请求
message LoadPlayerReq // IAddressRequest,LoadPlayerRes
{
    int64 userId = 1;
}

/// Gate -> Avatars 加载玩家地址响应
message LoadPlayerRes // IAddressResponse
{
    uint32 status = 1;
}
```

> ⚠️ **关键**：注释中的 `// IAddressRequest,LoadPlayerRes` 告诉协议生成器：
> - 请求类实现 `IAddressRequest`（而非 `IRequest`），才能被 `Scene.Call()` 接受
> - 响应类实现 `IAddressResponse`
> - 请求类的 ResponseType 为 `LoadPlayerRes`
> - 内网（Inner）协议走 Route/Address 路由体系

### 第二步：导出协议代码

```bash
cd Tools/ProtocolExportTool
dotnet Fantasy.ProtocolExportTool.dll export --silent
```

这会重新生成：
- `Entity/Generate/NetworkProtocol/InnerMessage.cs` — 协议实体类
- `Entity/Generate/NetworkProtocol/InnerOpcode.cs` — OpCode 常量

### 第三步：在目标 Scene 创建 AddressRPC Handler

```csharp
// Hotfix/Scene/Avatars/Handler/LoadPlayerAddressHandler.cs
using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;
using Hotfix.Scene.Avatars.Service;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Avatars.Handler;

public sealed class LoadPlayerAddressHandler : AddressRPC<FScene, LoadPlayerReq, LoadPlayerRes>
{
    protected override async FTask Run(FScene scene, LoadPlayerReq request, LoadPlayerRes response, Action reply)
    {
        // scene 参数 = 当前 Scene（Avatars Scene）
        var srv = scene.GetComponent<AvatarsService>();
        var result = await srv.LoadPlayer(request.userId);

        response.status = result.IsSuccess ? (uint)StatusCode.Ok : (uint)StatusCode.LoadPlayerFailed;
        reply(); // 必须调用，否则调用方会永远等待
    }
}
```

**要点：**
- 泛型参数：`AddressRPC<目标Entity类型, 请求类型, 响应类型>`
- 目标为 Scene 级别时，用 `Fantasy.Scene`（本项目别名 `FScene`）
- `Run` 方法执行在 Avatars Scene 的 `ThreadSynchronizationContext` 上
- **必须调用 `reply()`**，否则调用方（`Scene.Call`）会一直阻塞

### 第四步：在源 Scene 发起调用

```csharp
// Hotfix/Scene/Gate/Service/SessionService.cs

public async FTask<bool> EntryHome(long userId)
{
    // 1. 获取目标 Scene 的配置及其 Address（= RuntimeId）
    var configs = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Avatars);
    if (configs.Count == 0) return false;

    // 2. 构造请求（注意：框架的消息使用池化，必须 Create）
    var req = LoadPlayerReq.Create();
    req.userId = userId;

    // 3. 跨场景 RPC 调用
    var res = (LoadPlayerRes)await Scene.Call(configs[0].Address, req);
    var ok = res.status == (uint)StatusCode.Ok;

    // 4. 释放池化对象
    req.Dispose();
    res.Dispose();
    return ok;
}
```

---

## 关键 API 说明

### `Scene.Call(address, request)`

```csharp
public FTask<IResponse> Call<T>(long address, T request) where T : IAddressRequest
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `address` | `long` | 目标 Scene 的 RuntimeId（可用 `SceneConfig.Address` 获取） |
| `request` | `T : IAddressRequest` | 请求消息，必须由协议生成器导出（带 `IAddressRequest` 标记） |

**返回值：** `IResponse`，可转型为对应的 `*Res` 类型。

**底层路径（同进程）：**

```
Scene.Call(address, req)
  → NetworkMessagingComponent.Call(address, req)
    → Scene.GetSession(address)
        → Process.IsInApplication(sceneId) == true
            → 创建 ProcessSession（内存直连）
    → session.Send(req, rpcId, address)
    → ProcessSession.Scheduler()
        → Process.TryGetScene(sceneId)   // 找到目标 Scene
        → scene.ThreadSynchronizationContext.Post(...)
            → MessageDispatcherComponent.AddressMessageHandler()
                → LoadPlayerAddressHandler.Run()
    → await FTask<IResponse>             // 等待 reply()
```

### `SceneConfig.Address`

```csharp
// SceneConfig 初始化时自动计算
Address = IdFactoryHelper.RuntimeId(isPool: false, 0u, Id, (byte)WorldConfigId, 0u);
```

等同于该 Scene 的 `RuntimeId`，可直接作为 `Scene.Call()` 的 `address` 参数。

### `SceneConfigData.Instance`

| 方法 | 用途 |
|------|------|
| `GetSceneBySceneType(int sceneType)` | 按 `SceneType.*` 查找场景配置列表 |
| `Get(uint id)` | 按 SceneConfigId（如 1003）查找 |
| `GetByProcess(uint processId)` | 按进程 ID 查找 |

---

## 协议类型对照

| 协议标记 | 基类(请求) | 基类(Handler) | 适用场景 |
|----------|-----------|---------------|----------|
| `// IRequest,ResType` | `IRequest` | `MessageRPC<Req, Res>` | 客户端 → 服务端（Outer） |
| `// IResponse` | `IResponse` | — | 客户端响应（Outer） |
| `// IAddressRequest,ResType` | `IAddressRequest` | `AddressRPC<Scene, Req, Res>` | 内网 Scene → Scene（Inner） |
| `// IAddressResponse` | `IAddressResponse` | — | 内网响应（Inner） |
| `// IAddressableRequest,ResType` | `IAddressableRequest` | `AddressableRPC<Entity, Req, Res>` | 按 EntityId 路由（Inner） |

---

## Scene 初始化模板

在 `OnCreateSceneEvent` 中为各 Scene 注册 Service 组件：

```csharp
// Hotfix/OnCreateSceneEvent.cs
protected override async FTask Handler(OnCreateScene self)
{
    switch (self.Scene.SceneType)
    {
        case SceneType.Gate:
            self.Scene.AddComponent<SessionService>();
            break;
        case SceneType.Avatars:
            self.Scene.AddComponent<AvatarsService>();
            break;
        // case SceneType.Http: ...
    }
}
```

Service 本身是一个 `Entity` 子类，通过 `scene.GetComponent<T>()` 获取，保证同一 Scene 内所有 Handler 共享同一实例。

---

## 最佳实践

1. **协议文件位置**：内网通信的协议放在 `Config/NetworkProtocol/Inner/`，客户端通信放在 `Outer/`。
2. **消息池化**：始终用 `LoadPlayerReq.Create()` 而非 `new LoadPlayerReq()`，用完调用 `.Dispose()`。
3. **`reply()` 必不可少**：`AddressRPC.Run` 末尾必须调用 `reply()`，否则调用方永不会返回。
4. **Scene 间不要直接引用 Service**：避免 `scene.Process.GetScene(id).GetComponent<T>()`，这会破坏 Scene 的边界。始终走 `Scene.Call()` + `AddressRPC`。
5. **异常处理**：`AddressRPC` 基类已包含 try-catch，`Run` 中抛异常会自动设置 `ErrorCode` 并 `reply()`，不会死锁。
6. **同进程 vs 跨进程透明**：代码不需要区分目标 Scene 在哪个进程——`Scene.GetSession()` 会自动判断并选择 `ProcessSession` 或网络 `Session`。如果后续将 Avatars 移到其他进程，调用方代码**无需更改**。

---

## 常见问题

**Q: `Scene.Call()` 永远不返回？**

→ 检查目标 Scene 的 `AddressRPC` Handler 是否调用了 `reply()`。

**Q: 提示 "Found Unhandled AddressMessage"？**

→ `LoadPlayerReq` 的 OpCode 没有注册对应的 Handler。重新编译（让 Source Generator 重新扫描 `IAddressMessageHandler` 实现）。

**Q: 协议导出后 `response.Status` 变成 `response.status`？**

→ Proto 字段名是 `status`（小写），导出器按字段原名生成属性。C# 中应该使用 `response.status`。

**Q: `AddressRPC<Scene, Req, Res>` 和 `AddressRPC<其他Entity, Req, Res>` 的区别？**

→ 第一个泛型参数是消息的目标 Entity 类型。`FScene` 表示消息路由到 Scene 自身（`scene.GetEntity(address)` 返回 Scene）。如果是某个具体 Entity（如 `Player`），则路由到该 Entity。
