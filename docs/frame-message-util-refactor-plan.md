# client_frame 协议逻辑混入业务 — 修复计划表

> 状态：待实施（只抽离固定逻辑，不改业务语义）  
> 日期：2026-07-23  
> 范围：仅 `Server/` 工作区

## 1. 问题定义

「协议逻辑写进业务」指：

| 协议/消息层职责 | 房间业务层职责 |
|----------------|----------------|
| 池化消息 `Create` / 字段深拷贝 / `Dispose` | 是否在房、帧号是否合法 |
| 嵌套 `frame` 生命周期与所有权交接 | 是否入窗、何时延迟广播 |
| `server_frame` 发送包装配 | 开放窗口 / 旧帧覆盖等同步策略 |

当前多处把前者塞进了后者，且**同一套逻辑重复实现**。

## 2. 问题清单（扫描结果）

### P0 — 协议深拷贝重复 + 发送装配混层

| # | 位置 | 问题 |
|---|------|------|
| A1 | `Entity/Runtime/room/RoomFrameWindow.cs` → `CloneFrame` / `ClonePlayer` | `frame→player→vec2d/position2d` 手写拷贝写在窗口层 |
| A2 | `Entity/Runtime/room/RoomFrameSync.cs` → `CopyFrames` / `CloneFrameForSend` | 与 A1 **几乎同一套**，广播路径再写一遍 |
| A3 | `RoomFrameSync.BroadcastFrame` | 业务内 `server_frame.Create` + 填字段 + 拷贝 frames + `session.Send` |

### P1 — 消息所有权 / Dispose 散落各跳

Fantasy 约定：Handler `finally` 会 `message.Dispose()`，生成代码会级联 `Dispose frames`。  
业务为避 UAF 手写「摘 list」，这是**生命周期适配**，不是房间规则。

| # | 位置 | 问题 |
|---|------|------|
| B1 | `Hotfix/.../Gate/Handler/room/ClientFrameHandler.cs` | `var frames = message.frames; message.frames = new List<frame>()` |
| B2 | `Hotfix/.../Avatars/Handler/AvatarClientFrameNotifyHandler.cs` | 同上 |
| B3 | `Hotfix/.../Rooms/Handler/ClientFrame.cs` | 同上 + 入窗后手写 `foreach Dispose` |
| B4 | `Hotfix/.../Gate/Service/SessionService.cs` → `DisposeFrames` | 失败路径释放，与 Avatar 重复 |
| B5 | `Hotfix/.../Avatars/Service/AvatarsService.cs` → `DisposeFrames` | 同上 |

### 不算本轮问题（业务语义 / 另表）

| 项 | 说明 |
|----|------|
| 开放窗口硬拒 / 旧帧可覆盖写 | 帧同步策略，留在 Window/Sync |
| `_currentTickIndex` | 分层缓存，可选后续由 Room 传 tick |
| FrameSync 直接 `SessionManager`/`Send` | 传输分层，另表 |
| 协议 `frames_count` → `repeated frame` | 协议本职；子仓可独立提交 |

## 3. 本轮目标（约束）

1. **不改业务语义**：仍「转发复用引用 + 终点深拷贝入窗」；不改成「边界只深拷贝一次」的架构收口。  
2. **只抽固定逻辑**：字段拷贝、摘 list、Dispose 列表、建发送 `server_frame`。  
3. **用静态模块（或可注入工具类）集中提供方法**，禁止再散落在业务文件私有方法里。  
4. 生成代码 API（`frame.Create()` 等）照旧使用；util 不手改导出流程。

## 4. 目标模块边界

```text
Entity/Utils/FrameMessageUtil.cs  (static，无 Room 依赖)
  ├─ CloneFrame / ClonePlayer
  ├─ CopyFramesTo(server_frame, source)   // 或 CreateServerFrameForSend(src)
  ├─ DetachFrames(message)                // 摘 frames，父消息挂空 list
  └─ DisposeFrames(List<frame>?)

RoomFrameWindow  → 槽语义 + 调 CloneFrame 入窗
RoomFrameSync    → 窗口策略 + 调 util 装配/发送
Gate/Avatar/Rooms Handler/Service → 场景校验 + Detach/Dispose 调 util
```

落点选 **Entity/Utils**：Window 与 Sync 同在 Entity，无需跨到 Hotfix；Hotfix 可直接引用 Entity 工具。

## 5. 修复计划表

| # | 动作 | 落点 | 调用方 | 行为是否变 | 优先级 | 验收 |
|---|------|------|--------|------------|--------|------|
| 1 | 新增 `FrameMessageUtil`，迁入现有 Clone/Dispose 实现 | `Entity/Utils/FrameMessageUtil.cs` | — | 否 | P0 | 编译通过；无第二套字段拷贝 |
| 2 | `TryAppendOps` 改调 `CloneFrame` | `RoomFrameWindow` | 删私有 `CloneFrame`/`ClonePlayer` | 否 | P0 | Window 无协议字段赋值 |
| 3 | `BroadcastFrame` 改调 util 建发送包 | `RoomFrameSync` | 删 `CopyFrames`/`CloneFrameForSend` | 否 | P0 | Sync 无嵌套 Create/手写树拷贝 |
| 4 | 三 Handler 改 `DetachFrames` | Gate/Avatar/Rooms Handler | 一行调用 | 否 | P1 | 无「换空 list」样板代码 |
| 5 | Service/Rooms 改 `DisposeFrames` | Gate/Avatar Service、Rooms Handler | 删私有重复实现 | 否 | P1 | 全仓仅 util 一处 Dispose 实现 |
| 6 | （可选）`AttachFrames` 薄封装 | Forward 成功挂包 | Session/Avatars | 否 | P2 | 可选 |
| 7 | 编译 Entity + Hotfix | — | — | — | — | 通过 |
| 8 | 单独 commit | `refactor(frame): 抽出 FrameMessageUtil...` | 与功能语义提交分离 | — | — | status 干净或仅剩无关改动 |

## 6. 实施顺序

1. 新增 `FrameMessageUtil`（先迁实现，再删调用方私有方法）。  
2. 改 `RoomFrameWindow.TryAppendOps`。  
3. 改 `RoomFrameSync.BroadcastFrame`。  
4. 改三 Handler → `DetachFrames`；Rooms 拷贝后 → `DisposeFrames`。  
5. 改 Session/Avatars，删除私有 `DisposeFrames`。  
6. `rg CloneFrame|ClonePlayer|DisposeFrames|CopyFrames|CloneFrameForSend` 确认无业务侧残留实现。  
7. 编译验证。  
8. 独立 refactor 提交（**不要**与入窗/广播功能语义混在一个「大包」里若用户要求拆分时）。

## 7. 明确不做（本轮）

| 项 | 原因 |
|----|------|
| 改开放窗口 / 硬拒 / 旧帧覆盖规则 | 业务语义 |
| 取消每跳所有权转移、改「边界深拷贝一次」 | 架构策略变更，非抽离 |
| FrameSync 不再碰 Session | 传输分层另表 |
| 协议子仓再改 | 与本 refactor 无关 |

## 8. 风险

- `DetachFrames` 对 `frames == null` 必须与现逻辑一致（挂空 list，避免 Dispose 时 NRE）。  
- util 必须用池化 `*.Create()`，禁止 `new frame()`，否则与 Dispose 回池不一致。  
- 字段拷贝遗漏会导致静默丢 ops/坐标；合并后只改一处，回归时重点看入窗与广播内容。

## 9. 相关路径速查

```
Entity/Runtime/room/RoomFrameWindow.cs
Entity/Runtime/room/RoomFrameSync.cs
Entity/VOs/room/Room.cs
Hotfix/Scene/Gate/Handler/room/ClientFrameHandler.cs
Hotfix/Scene/Gate/Service/SessionService.cs
Hotfix/Scene/Avatars/Handler/AvatarClientFrameNotifyHandler.cs
Hotfix/Scene/Avatars/Service/AvatarsService.cs
Hotfix/Scene/Rooms/Handler/ClientFrame.cs
Hotfix/Scene/Rooms/Service/ClientFrame.cs
Entity/Generate/NetworkProtocol/OuterMessage.cs   # frame / server_frame / player
Entity/Generate/NetworkProtocol/InnerMessage.cs   # *ClientFrameNotify
```

## 10. 后续可选（不在本表实施）

- 边界深拷贝一次，去掉每跳 Detach（真正「收口」所有权）。  
- 广播：业务只出帧快照，发送适配层负责 Create/Send。  
- `_currentTickIndex` 改为 Room 注入 `TickIndex`。
