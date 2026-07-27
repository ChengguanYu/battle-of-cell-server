# TODO / FIXME 追踪表

扫描范围：`Hotfix/`、`Entity/`、`Main/` 源码中的 `TODO` / `FIXME` / `todo`。  
更新时间：2026-07-25  
关联提交：`d951d21`（逻辑人数过滤与 Entry 拦截）

## 追踪表

| ID | 优先级 | 模块 | 位置 | 标记 | 未实现内容 | 现状影响 | 建议处理 | 状态 |
|---|---|---|---|---|---|---|---|---|
| T1 | 高 | Rooms / Match 占位 | `Hotfix/Scene/Rooms/Service/Leave.cs` | TODO | 最后一人离房后按占位 TTL 进入 Holding，延时正式关房并清理 `match:{roomId}:*` | 关房后占位可能残留；`roomId` 回收时新房逻辑人数可能虚高 | 状态机驱动：`Opened→Holding→Closed`；见 [`docs/room-delay-close-plan.md`](room-delay-close-plan.md) | 已完成 |
| T2 | 高 | Match 占位计数 | `Hotfix/Database/MatchResultDao.cs` | FIXME | 同一 user 既在成员列表又有占位 key 时双计，暂不去重 | 逻辑人数偏大，可能误判已满、接口失败（不超加） | `Entry` 成功后删本人占位；计数时对已入房 user 去重 | 已完成 |
| T3 | 中 | Avatar 匹配入口 | `Hotfix/Scene/Avatars/Service/Relay/Relay.cs` | TODO | 非 Lobby 状态时的重连回房逻辑 | 非大厅玩家只能匹配失败，断线重连回房未打通 | 补“重连回原房/恢复匹配态”流程 | 未开始 |
| T4 | 中 | Gate 会话 | `Entity/Managers/SessionManager.cs` | TODO | 完整重连策略（同连接复用等）；当前仅顶号摘旧 | 重连体验粗糙，可能误踢/无法复用连接 | 设计同连接复用 + 超时重连策略 | 未开始 |
| T5 | 低 | 帧转发所有权 | `Hotfix/Scene/Gate/Service/SessionService.cs`、`Hotfix/Scene/Avatars/Service/Relay/Relay.cs` | FIXME | `frames` 依赖上游 Handler 手工转移所有权，待统一收口 | 易漏交接导致帧对象提前 Dispose/泄漏；属工程债 | 边界深拷贝或单所有者 API | 未开始 |
| T6 | 低 | 服务层错误模型 | `Hotfix/Scene/Match/Service/MatchService.cs` | todo | 服务层错误改为抛错传递，不用返回值 | 当前 `InnerResult` 仍可用，风格不统一 | 全服务层统一错误约定后再改，避免半吊子迁移 | 未开始 |

## 规划索引

| ID | 规划文档 | 说明 |
|---|---|---|
| T1 | [`docs/room-delay-close-plan.md`](room-delay-close-plan.md) | 状态机 `Holding` 驱动延时关房；只改 Leave/Entry 必要分支，不侵入 Create |

## 统计

- 总计：4 条未实现标记 + 2 条已完成（T1、T2）
- 高：2 已完成（T1、T2）
- 中：2（T3、T4）
- 低：2（T5、T6）
