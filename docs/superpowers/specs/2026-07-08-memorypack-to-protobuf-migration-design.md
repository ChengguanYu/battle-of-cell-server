# 协议从 MemoryPack 迁移到 Fantasy ProtoBuf — 设计文档

- 日期：2026-07-08
- 范围：`Server/` 游戏服端协议迁移。把当前手写的 MemoryPack 协议改为 Fantasy 框架原生的 protobuf 协议导出管线，迁移现有消息类。
- 状态：待评审

## 1. 背景与目标

### 1.1 起点
提交 `5f6e26a`（"切换为 MemoryPack 序列化，按模块手工维护协议消息"）之后，协议走了**手写 MemoryPack** 路线，绕开 Fantasy 框架原生的协议导出管线。本次目标是撤销这条手写路线，回到框架原生 protobuf 全链路。

当前手写协议的状态：
- `Entity/Protocol/home/EntryHome.cs`：手写 `[MemoryPackable]` + `[MemoryPackOrder]` + 手写 `OpCode()` + 手写 `Dispose()`。
- `Entity/Protocol/OuterOpcode.cs`：手写常量，**只有 `EntryHomeReq`，缺 `EntryHomeRes`，导致 build 报 CS0117**。
- `Entity/Entity.csproj`：含 `MemoryPackGenerator_TypeScriptOutputDirectory` 与 `CleanTypeScriptOutput` MSBuild Target，借 MemoryPack 顺带生成 TS（与本次服务端迁移无关，但属 MemoryPack 残留，一并清除）。

### 1.2 目标
1. 服务端协议从手写 MemoryPack 迁移到 Fantasy 框架原生 protobuf 导出管线（`[ProtoContract]`/`[ProtoMember]` + 框架源码生成器自动登记）。
2. 彻底清除 MemoryPack 相关逻辑：手写的 `[MemoryPackable]`/`[MemoryPackOrder]`/`[MemoryPackIgnore]`、`MemoryPackGenerator_TypeScriptOutputDirectory`、`CleanTypeScriptOutput` Target、手写 `OuterOpcode` 常量与手写 `OpCode()`/`Dispose()`，全部改由导出工具 + 框架源码生成器产出。
3. 修掉 `OuterOpcode.EntryHomeRes` 缺失导致的 CS0117 build 错误（opcode 常量改由导出工具自动补全）。
4. 迁移现有消息类 `EntryHomeReq` / `EntryHomeRes`，字段保持不变。

### 1.3 范围
- **含**：服务端协议迁移、MemoryPack 残留清理、`.proto` 源文件落地、导出工具配置。
- **不含**：web 客户端 / Unity 客户端接入；opcode 表交付客户端；任何客户端侧落地。本轮只做服务端。

## 2. 已验证的框架能力（实地验证，非文档推断）

本设计所有结论均由在当前仓库实跑导出工具得到（验证后已清理测试产物）：

- **工具链全通**：`Tools/ProtocolExportTool/Fantasy.ProtocolExportTool.dll export --silent` 从 `ExporterSettings.json` 读配置，成功加载。当前 `ExporterSettings.json` 三路径已指向本机实际目录：源 `Config/NetworkProtocol`、服务端生成 `Entity/Generate/NetworkProtocol`、客户端生成（暂无独立客户端，与服务端同路径）。
- **`.proto` 语法**：`proto3` + `package` 行 + `// IRequest,<Response名>` 行尾注释 + 字段号从 1 起递增。工具接受该写法。
- **生成产物**（三个文件，落 `Entity/Generate/NetworkProtocol/`）：
  - `OuterOpcode.cs`：`public static partial class OuterOpcode`，含 `EntryHomeReq` 与 `EntryHomeRes` **成对**常量，opcode 由工具确定性算出。
  - `OuterMessage.cs`：消息类，`[Serializable][ProtoContract]`，`partial class EntryHomeReq : AMessage, IRequest`，带对象池 `Create()/Return()/Dispose()`、`OpCode()`、`[ProtoIgnore] ResponseType`、`[ProtoMember(n)]` 业务字段；`EntryHomeRes : AMessage, IResponse` 同构。
  - `NetworkProtocolHelper.cs`：`Session` 扩展方法，提供 RPC 风格调用（`session.EntryHomeReq(token)` 直接拿 `EntryHomeRes`）。
- **命名空间**：生成代码全部 `namespace Fantasy`（不是当前手写的 `Fantasy.Protocol`）。
- **ErrorCode 自动生成**：`.proto` 里 `EntryHomeRes` 只写业务字段 `ok = 1`，工具自动追加 `[ProtoMember(2)] public uint ErrorCode`（编号为"首个可用号"）。手写版要自己写 `ErrorCode`，导出版不用。
- **序列化**：生成类用 `LightProto`（`[ProtoContract]`/`[ProtoMember]`），即 Fantasy 原生 protobuf 序列化；`using MemoryPack;` 仅借用 `[ProtoIgnore]` 特性别名，不走 MemoryPack 序列化。
- **opcode 示例值**（本次测试值，正式导出同算法、值相同）：`EntryHomeReq = 268445457`、`EntryHomeRes = 402663185`。

## 3. 设计

### 3.1 数据流

```
Config/NetworkProtocol/Outer/EntryHome.proto   （人写：消息结构）
        │  Fantasy.ProtocolExportTool export --silent
        ▼
Entity/Generate/NetworkProtocol/*.cs           （工具生成：消息类+opcode+Helper）
        │  dotnet build → Fantasy 源码生成器
        ▼
Entity/obj/.../NetworkProtocolGenerator/*Registrar.g.cs  （框架自动登记opcode/调度）
```

单一真相源：`.proto`。opcode、消息类、调度登记全部从它派生，无需手写、无需手工对齐。

### 3.2 `.proto` 源文件

新增 `Config/NetworkProtocol/Outer/EntryHome.proto`：

```protobuf
syntax = "proto3";
package BattleOfCell.Message;

/// 客户端进家园请求
message EntryHomeReq // IRequest,EntryHomeRes
{
    string token = 1;
}

/// 客户端进家园响应
message EntryHomeRes // IResponse
{
    bool ok = 1;
    // ErrorCode 由导出工具自动生成，不手写
}
```

要点：
- 消息名沿用无前缀风格（`EntryHomeReq`/`EntryHomeRes`），与近期"移除 C2G_/G2C_ 前缀"提交一致。
- `IRequest` 行尾注释必须填**完整回复消息名**（`EntryHomeRes`）；Request/Response 成对紧邻。这是工具识别响应关系的唯一依据。
- `EntryHomeRes` 的 `ErrorCode` 由工具自动追加（实测落 `ProtoMember(2)`），`.proto` 不写。
- 字段从 1 起递增；`proto3` 语法。
- `package` 行满足 proto3 语法，对生成 C# 命名空间无影响（实测固定 `namespace Fantasy`）。

### 3.3 文件增删清单

**删除**：
- `Entity/Protocol/OuterOpcode.cs`（手写常量）→ 由导出工具产到 `Entity/Generate/NetworkProtocol/OuterOpcode.cs`。
- `Entity/Protocol/home/EntryHome.cs`（手写 MemoryPackable 类）→ 由导出工具产到 `Entity/Generate/NetworkProtocol/OuterMessage.cs`。
- `Entity/Entity.csproj` 中：`MemoryPackGenerator_TypeScriptOutputDirectory` 属性、对应 `CompilerVisibleProperty`、`CleanTypeScriptOutput` MSBuild Target。
- `Entity/Protocol/home/` 目录（变空后删除）。

**新增**：
- `Config/NetworkProtocol/Outer/EntryHome.proto`（3.2）。

**生成（不手写、不提交到 git 由人维护；是否纳入 git 见 3.5）**：
- `Entity/Generate/NetworkProtocol/OuterOpcode.cs`
- `Entity/Generate/NetworkProtocol/OuterMessage.cs`
- `Entity/Generate/NetworkProtocol/NetworkProtocolHelper.cs`

**改动**：
- `Hotfix/Scene/Gate/Handler/home/EntryHome.cs`：`EntryHomeHandler : Message<EntryHomeReq>` 泛型与 `Run(Session, EntryHomeReq)` 签名不变；因消息命名空间由 `Fantasy.Protocol` 变为 `Fantasy`，调整 `using`（去掉 `using Fantasy.Protocol;`，消息在 `Fantasy` 命名空间可见）。Handler 体内 `JwtHelper.GetUserIdFromToken(message.token)` 不变。

### 3.4 命名空间影响
导出工具产物置于 `namespace Fantasy`（实测），而非当前手写的 `Fantasy.Protocol`。受影响：
- `EntryHomeHandler` 的 `using`（去 `using Fantasy.Protocol;`）。
- `OuterOpcode` 引用方式：由 `Fantasy.Protocol.OuterOpcode` 变为 `Fantasy.OuterOpcode`（`using Fantasy;` 后直接 `OuterOpcode.X`）。
- 删手写 `Fantasy.Protocol.OuterOpcode`，避免与导出版的 `Fantasy.OuterOpcode` 同名不同命名空间造成歧义。

### 3.5 生成代码是否纳入 git
导出工具产物在 `Entity/Generate/NetworkProtocol/`。两种做法：
- **纳入 git**：把生成文件提交，CI/他人 clone 即可编译，不依赖本地跑导出工具。生成文件头注明"请勿手改"。
- **不纳入 git、构建时生成**：`Entity/Generate/` 加入 `.gitignore`，靠 MSBuild Target 在编译前自动跑导出工具。

**推荐纳入 git**：当前 `ExporterSettings.json` 用本机绝对路径（`C:/Users/...`），换机器/CI 会失效；生成文件纳入 git 可避免他人必须先改路径再跑工具。代价是改 `.proto` 后要手动重跑工具并提交生成产物（与"生成文件勿手改"不冲突——重跑即覆盖）。

> 此项待你拍板（见第 5 节待确认项）。

## 4. 验证

- **编译**：`dotnet build Server.sln` 应 0 error、无 CS0117。（实测通过，唯一警告为 `Session._lastHeartbeat` 未使用，与本次迁移无关。）
- **消息类纳入编译**：临时移走 `Entity/Generate/` 会使 build 报 CS0246"未能找到 EntryHomeReq"，证明生成类确实被编译进 `Entity.dll`。（实测确认。）
- **opcode 成对**：生成 `OuterOpcode.cs` 同时含 `EntryHomeReq` 与 `EntryHomeRes`。
- **框架调度登记接入**：Fantasy 源码生成器不落 `*.g.cs` 文件到 `obj/`，而是 in-memory emit 进编译。验证方法为在 `Entity.dll` 二进制中检索类型名——实测含 `NetworkProtocolRegistrar`、`OpCodeRegistrar`、`ProtoBufDispatcherRegistrar`，确认协议已登记进框架调度。
- **传输自洽**：触发 `EntryHomeReq`→`EntryHomeRes` 链路，服务端日志确认 `JwtHelper.GetUserIdFromToken` 正常。
- **opcode 稳定**：同一 `.proto` 多次导出，opcode 值不变（确定性算法）。

## 5. 风险与待确认

- **`ExporterSettings.json` 用本机绝对路径**：换机器失效。若 3.5 选"纳入 git"，此文件本身仍带本机路径——需评估是否改为相对路径（工具是否支持相对路径未验证，保守起见本轮不动，保持本机绝对路径，靠"纳入 git 的生成产物"绕开他人跑工具的需求）。
- **生成代码纳入 git 与否**（3.5）：待你确认。
- **命名空间迁移破坏引用**：清单 3.3/3.4 已列受影响点，编译即暴露遗漏。
- **`OuterOpcode` partial 冲突**：删手写 `Fantasy.Protocol.OuterOpcode`，避免与导出版 `Fantasy.OuterOpcode` 歧义。

## 6. 决策记录
- 迁移路径：走 Fantasy 框架 protobuf 导出工具管线（撤销 `5f6e26a` 的手写 MemoryPack 路线）。
- 消息命名：沿用无前缀风格，不改名。
- 范围：只迁服务端，不碰客户端。
- 生成代码纳入 git：待确认（默认推荐纳入）。
