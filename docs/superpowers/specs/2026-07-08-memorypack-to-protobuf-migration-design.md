# 协议从 MemoryPack 迁移到 Fantasy ProtoBuf 导出管线 — 设计文档

- 日期：2026-07-08
- 范围：`Server/` 游戏服端（Fantasy 框架）；web 客户端的 protobuf 接入仅在设计上确认兼容性，不包含实现。
- 状态：待评审

## 1. 背景与现状

### 1.1 当前协议体系（"手写"路线）
当前项目的协议类是**手写**的，绕过了 Fantasy 框架原生的协议导出管线：

- 消息类 `Entity/Protocol/home/EntryHome.cs`：手写 `[MemoryPackable]` + `[MemoryPackOrder]` + 手写 `OpCode()` + 手写 `Dispose()`；并借 MemoryPack 自带的 `[GenerateTypeScript]` 生成 TS。
- `Entity/Protocol/OuterOpcode.cs`：手写常量（`public const uint EntryHomeReq = 285222674;`），**目前缺 `EntryHomeRes` 常量，导致 build 报错 CS0117**。
- `Entity/Entity.csproj` 含 `MemoryPackGenerator_TypeScriptOutputDirectory` 与自定义 `CleanTypeScriptOutput` MSBuild Target（每次编译清空并强制重新生成 `ts-protocol/`）。
- 已删除的旧产物（git status 中）：`ts-protocol/LoginRequest.ts`、`MemoryPackReader.ts`、`MemoryPackWriter.ts` 等。

### 1.2 框架生成器目前是"空的"
验证 `Entity/obj/.../generated/.../NetworkProtocolGenerator/` 下 Fantasy 自带的源码生成器产物：

- `Entity_OpCodeRegistrar.g.cs`
- `Entity_ProtoBufDispatcherRegistrar.g.cs`
- `Entity_ResponseTypeRegistrar.g.cs`
- `Entity_NetworkProtocolRegistrar.g.cs`

全部为 `Array.Empty<...>()`。原因：手写消息类缺少框架生成器要找的标记（`[ProtoContract]` 等），框架并未登记它们。也就是说**当前协议并未真正接入 Fantasy 的协议注册/调度机制**，只是借了 MemoryPack 做序列化格式 + 手工凑出 OpCode。

### 1.3 Fantasy 框架自带的 ProtoBuf 全链路（迁移目标）
框架自带：
- `ProtoBufHelper`（`Fantasy.Serialize`）+ 自研 `LightProto`（`[ProtoContract]/[ProtoMember]`，API 风格同 protobuf-net）。
- `SerializerManager` 按 OpCode 高位 `OpCodeIdStruct.OpCodeProtocolType` 分流：`0=ProtoBuf`，`1=Bson`，`2=MemoryPack`（见 `OpCodeProtocolType.cs`）。
- `Fantasy.ProtocolExportTool`（`Tools/ProtocolExportTool/`，仓库已有编译 dll）：写 `.proto` → 导出带 `[ProtoContract]/[ProtoMember]` 的 C# → Fantasy 源码生成器自动登记 `OpCodeRegistrar`/`ProtoBufDispatcherRegistrar`/`ResponseTypeRegistrar`/`NetworkProtocolRegistrar`。
- 默认 ProtoBuf 分支（`ProtocolSettings.CreateProtoBuf()`）产出：`ClassAttribute=[ProtoContract]`，`MemberAttribute=ProtoMember`，`IgnoreAttribute=[ProtoIgnore]`，`OpCodeType=ProtoBuf(0)`，字段号从 1 起。
- 导出工具**只有 `CSharpExporter`**，不产 TS。

## 2. web 客户端兼容性结论（已核证，本设计只确认、不实现）

- web 客户端（`client/`，Vite + TS）当前：`src/ts-protocol/` 已空；`src/network/GameNetwork.ts` + `Packet.ts` 已实现 20 字节固定头（`MessagePacketLength(4) + ProtocolCode/opcode(4) + RpcId(4) + padding(8)`）与按 `rpcId` / `opcode` 分发；body 解码依赖已删的 MemoryPack TS。
- `LightProto` 的编码原语（`WritingPrimitives`：varint、小端定长、proto3 有符号整数规则）= **标准 protobuf wire format**。故 web 端可用标准工具（`protobufjs` / `protobuf-ts` / `protoc-gen-ts`）从同一份 `.proto` 生成 TS 编解码，与 C# 互通；20 字节头部逻辑无需改动。
- **结论**：本次服务端迁移对 web 客户端**不产生破坏性变更之外的兼容负担**，body 改为标准 protobuf 后用标准 protoc 工具生成即可。web 端接入留作**独立后续任务**，不在本次实现范围。

## 3. opcode / rpcId 关联机制（web 客户端分发依据）

### 3.1 框架在响应包里填了什么
- 偏移 4 ×4 字节 `ProtocolCode`（opcode）= **响应类自身的 opcode**（`response.OpCode()`，如 `EntryHomeRes` 的 opcode，不是请求 `EntryHomeReq`）。
- 偏移 8 ×4 字节 `RpcId` = **请求时 rpcId 原样回填**。`MessageRPC.Handle` 拿到的 `rpcId`，`Reply()` 里 `session.Send(response, rpcId, …)` 再写回包头。框架保证回填。

### 3.2 `.proto` 与 opcode 的分工（纠正一个常见误解）
- `.proto` 定义消息结构与字段 tag（protobuf 字段号 ≠ opcode），**天然不含 opcode**，标准 protoc 工具链也不关心 opcode。
- opcode 是 Fantasy 路由编号（消息→uint），由导出工具 `OpCodeGenerator` 按类型+计数器确定性算出，产在 `OuterOpcode.cs`。
- 因此：web 客户端**两样都要**——`.proto` 给 body 编解码，opcode 表给路由分发；二者来源不同、互不替代。

### 3.3 web 客户端分发逻辑（迁移后照旧有效）
收包后：①先按 `rpcId` 关联 pending 请求（与消息类型无关、框架保证回填，最可靠）→ 命中即 resolve；②按 opcode 区分语义（Response 走 rpcId 命中；服务器主动推送的 `IMessage` 走 opcode→handler 注册表）。当前 `GameNetwork.ts` 的三步分发在 protobuf 迁移后原样有效，因为头部协议与序列化格式无关。

## 4. opcode 表交付 web 客户端（关键设计决策）

opcode 须与 body 同源、不漂移。方案选定（已与用户确认）：

- **不改上游导出工具逻辑**：`Tools/ProtocolExportTool/Fantasy.ProtocolExportTool.dll` 照常产 `OuterOpcode.cs`（C#，服务端用）。
- **在仓库内新增一个极小的自有翻译步骤**（归 `Tools/` 下，命名暂定 `OpcodeTableExporter`，实现语言待定——.NET 小工具或 node 脚本均可）：
  - 输入：导出工具产出的 `OuterOpcode.cs`（`public const uint Name = Value;` 行）。
  - 输出：web 客户端目录下的 `OuterOpcode.json`（及可选 `.ts`），形如 `{ "EntryHomeReq": 285222674, "EntryHomeRes": ... }`。
  - 性质：**纯下游字符串翻译**，不解析 `.proto`、不复现 opcode 算法、不碰框架/上游工具，杜绝与服务端数值漂移。
- 单一真相源：导出工具一次性算出 opcode 值；翻译步骤只做格式转码。
- 该翻译步骤与本次服务端迁移**同一 PR 落地**，但它本身是独立工具、无业务逻辑；web 客户端 import 该文件属后续接入范畴。

## 5. 目标与范围

### 5.1 目标
1. 服务端协议从手写 MemoryPack 迁移到 **Fantasy 框架原生 ProtoBuf 导出管线**。
2. 项目内**彻底清除 MemoryPack 相关逻辑**：`[MemoryPackable]`/`[MemoryPackOrder]`/`[MemoryPackIgnore]`、`MemoryPackGenerator_TypeScriptOutputDirectory`、`CleanTypeScriptOutput` MSBuild Target、手写 `OuterOpcode` 常量与手写 `OpCode()`/`Dispose()` 全部移除，转由导出工具 + Fantasy 源码生成器产出。
3. 修掉当前 build 已坏的 `OuterOpcode.EntryHomeRes` 缺失错误（CS0117）；清理仓库根目录误生成的 `nul` 文件。
4. 提供 opcode 表交付 web 客户端的能力（第 4 节翻译步骤）。

### 5.2 范围
- **含**：`Server/` 服务端协议迁移、MemoryPack 残留清理、导出工具配置、opcode 翻译工具落地。
- **不含**：web 客户端接入 protobuf（body 用标准 protoc 生成 TS + import opcode JSON 的客户端侧落地），留作独立后续任务。

### 5.3 强约束
- 消息名**不改**：保持 `EntryHomeReq`/`EntryHomeRes`（沿用近期"移除 C2G_/G2C_ 前缀"的风格）。`IRequest` 的行尾注释须写全回复消息名 `EntryHomeRes`。
- 不改上游仓库/导出工具源码；仅在本仓库内新增下游翻译工具与配置。

## 6. 目录与导出配置

### 6.1 目录约定
- `.proto` 源：`Config/NetworkProtocol/{Outer,Inner}/`（Outer/Inner 分目录，Outer 放客户端↔服务端消息）。
- 服务端生成 C#：`Entity/Generate/NetworkProtocol/`（导出工具 `NetworkProtocolServerDirectory` 指向此）。
- 客户端生成 C#：暂无 Unity 客户端，`NetworkProtocolClientDirectory` 本设计仅作占位（与服务端同路径或留空由用户定，导出工具允许仅服务端）。
- opcode 翻译产物落地：web 客户端目录（具体路径由 web 接入任务定；本次工具输出到 `client/src/ts-protocol/OuterOpcode.json` 亦可，`ts-protocol/` 既有目录复用）。

### 6.2 `.proto` 源文件（`Config/NetworkProtocol/Outer/EntryHome.proto`）
按 Fantasy 导出工具语法（`define-outer.md`）：

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
    // ErrorCode 0=成功，非0=错误码（框架自带，不需要手动定义）
}
```

要点：
- 消息名沿用无前缀风格（`EntryHomeReq`/`EntryHomeRes`）。
- `IRequest` 行尾注释必须填**完整回复消息名**（`EntryHomeRes`），且 Request/Response 成对紧邻。
- `IResponse` 的 `ErrorCode` 由导出工具自动生成（`CSharpExporter` `GenerateMessages` 中 `IsResponseType` 分支），**不手写**。
- 字段从 1 起递增；`proto3` 语法。
- `entryHomeRes.ok` 为业务字段，保留。
- **`package` 行被导出工具忽略**：解析器（`ProtocolFileParser`）只认 `// using X` 注释引入命名空间，不解析 `package`（`define.md` 称 "package 名可自定义"，实为语法占位）。生成的 C# 消息类**写死 `namespace Fantasy`**（`CSharpExporter.GenerateOuterMessages` L182）。故 `.proto` 里 `package` 仅满足 proto3 语法，对最终命名空间无影响。
- **ErrorCode 字段号**：`ProtocolFileParser.CalculateErrorCodeIndex` —— 收集消息所有已用业务字段号，从 1 起找第一个可用号。故 `EntryHomeRes` 中 `ok = 1` 已占 1，ErrorCode 落 `ProtoMember(2)`。业务字段勿与该动态分配的号冲突的设计原则：Response 中不要在预期 ErrorCode 之前留空号。

### 6.3 `ExporterSettings.json`（`Tools/ProtocolExportTool/`）
修正三路径为本机本仓库实际绝对路径（替换上游作者的 `/Users/fantasy/...`）：

```json
{
  "Export": {
    "NetworkProtocolDirectory":      { "Value": "<repo>/Config/NetworkProtocol", "Comment": "ProtoBuf文件所在的文件夹位置" },
    "NetworkProtocolServerDirectory":{ "Value": "<repo>/Entity/Generate/NetworkProtocol", "Comment": "ProtoBuf生成到服务端的文件夹位置" },
    "NetworkProtocolClientDirectory":{ "Value": "<repo>/Entity/Generate/NetworkProtocol", "Comment": "ProtoBuf生成到客户端的文件夹位置（暂无独立客户端，与服务端同路径）" }
  }
}
```

`Run.sh` 静默模式 `export --silent` 照旧可用。

## 7. 文件增删清单

### 7.1 删除
- `Entity/Protocol/OuterOpcode.cs`（手写常量）→ 改由导出工具产到 `Entity/Generate/NetworkProtocol/OuterOpcode.cs`。
- `Entity/Protocol/home/EntryHome.cs`（手写 MemoryPackable 类）→ 改由导出工具产到 `Entity/Generate/NetworkProtocol/` 对应消息文件。
- `Entity/Entity.csproj` 中的 `MemoryPackGenerator_TypeScriptOutputDirectory` 属性与 `CompilerVisibleProperty`、`CleanTypeScriptOutput` MSBuild Target。
- 仓库根目录 `nul` 文件（误生成产物）。
- 已 `D` 标记的旧 `LoginRequest.cs`/`LoginResponse.cs`/`LoginRequestHandler.cs`、旧 `ts-protocol/*.ts` 等历史残留（git 已标记删除，本次随迁移一并确认）。

### 7.2 新增/改动
- `Config/NetworkProtocol/Outer/EntryHome.proto`（新增，6.2）。
- `Entity/Generate/NetworkProtocol/*.cs`（导出工具生成，含 `OuterOpcode` / 各消息类 / `NetworkProtocolHelper`。**生成文件勿手改**）。
- `Tools/ProtocolExportTool/ExporterSettings.json`（6.3 路径修正）。
- 仓库根 `Tools/OpcodeTableExporter/`（自有翻译工具，第 4 节；实现语言待定，纯字符串解析 `OuterOpcode.cs` → `OuterOpcode.json`）。
- `Hotfix/Scene/Gate/Handler/home/EntryHome.cs`：保持 `Message<EntryHomeReq>` 泛型与 `Run(Session, EntryHomeReq)` 不变；若导出后消息命名空间由 `Fantasy.Protocol` 变为 `Fantasy`（导出工具默认 `namespace Fantasy`），同步调整 `using` 命名空间。
- `Entity/VOs/session/Session.cs`：与协议迁移无直接关联，本次不动（其 `_lastHeartbeat` 未使用告警可顺带处理，但非本设计范围）。

### 7.3 命名空间影响
导出工具产出的消息类置于 `namespace Fantasy`（见 `CSharpExporter.GenerateOuterMessages`），而非当前手写的 `Fantasy.Protocol`。需同步调整：
- `EntryHomeHandler` 的 `using`（去掉 `using Fantasy.Protocol;`，消息直接在 `Fantasy` 命名空间可见）。
- `OuterOpcode` 也由导出工具置于 `namespace Fantasy`（`OuterOpcode` 常量类 `partial`，服务端引用方式 `Fantasy.OuterOpcode.X` 或 `using Fantasy;`）。
- 该命名空间变更为"手写→框架导出"的固有结果，属本次迁移预期内变更。

## 8. 验证与错误处理

- **编译**：`dotnet build Server.sln` 应 0 error、无 CS0117（opcode 常量由导出工具补全）。
- **生成器产物转非空**：重建后检查 `Entity/obj/.../NetworkProtocolGenerator/` 下四份 `*Registrar.g.cs` 由 `Array.Empty` 变为含 `EntryHomeReq`/`EntryHomeRes` 的登记——确认协议真正接入框架。
- **opcode 数值稳定**：同一 `.proto` 多次导出，opcode 值应不变（确定性算法）；`OpcodeTableExporter` 产物可 diff 比对。
- **传输自洽**：用 `EntryHomeHandler` 触发 `EntryHomeReq`→`EntryHomeRes` 链路，服务端日志确认 `JwtHelper.GetUserIdFromToken` 正常；rpcId 由框架保证回填（已在 `ProcessSession`/`MessageRPC` 源码核证）。
- **web 兼容**（验证项，非本次实现）：确认 `LightProto` wire format 与标准 protobuf 一致（已核证 `WritingPrimitives`），web 端可解码 body；opcode 经翻译 JSON 与服务端同值。

## 9. 风险与缓解
- **生成器产回归零**：消息类标记缺失或 `.proto` 语法不符会让生成器回落空——靠"生成器产物非空"校验项捕获。
- **opcode 翻译漂移**：唯一真相源为导出工具产出的 `OuterOpcode.cs`；翻译工具每次全量重生成，不做增量/合并。
- **命名空间迁移破坏引用**：清单第 7.3 节已列受影响点，编译即可暴露遗漏。
- **`OuterOpcode` partial 冲突**：导出产 `namespace Fantasy` 的 `OuterOpcode`,须删除手写的 `Fantasy.Protocol.OuterOpcode`,避免两份 partial 同名不同命名空间的歧义。
- **`package` 行无效误解**：导出工具忽略 `package`,产物固定 `namespace Fantasy`;勿误以为 `.proto` 的 `package` 能改 C# 命名空间(已核证解析器只用 `// using`)。

## 10. 开题问答摘要（决策记录）
- 迁移路径：走框架 protobuf 导出工具，项目内不再留 MemoryPack 逻辑。
- `.proto` 与生成 C# 目录：`Config/NetworkProtocol` 与 `Entity/Generate/NetworkProtocol`。
- 消息命名：不恢复前缀，导出当前协议、不改名。
- 客户端：web 端，body 用 `.proto` 走标准 protoc；opcode 表用方案 A 但**不动上游**，加仓库内自有翻译步骤。
- 范围：只迁服务端；web 客户端接入留作独立后续任务。