# 服务端物理发散修复记录

日期：2026-07-31  
范围：仅服务端模拟器内部逻辑。  
约束：不修改房间帧链路，不把模拟器物理帧与房间帧绑定。

## 根因

### 1. 离散积分顺序相反

- 客户端 `Hero.update`：先按当前速度位移，再计算减速。
- 服务端旧 `TickPhysics`：先减速，再按减速后的速度位移。

两种顺序在恒定减速下会得到不同终点，旧服务端天然少走一段距离。

### 2. 定点数舍入语义不一致

- 客户端 `fixed.ts`：`fixedMul`、`fixedDiv`、`fixedHypot` 使用 `Math.round`。
- 服务端旧实现：`FixedMul`、`FixedDiv` 使用向零截断，`FixedHypot` 使用向下取整平方根。

减速依赖 `ratio = newSpeed / speed` 逐帧缩放速度。截断会让服务端每一帧都比客户端衰减更多，最终提前清零，滑行距离进一步偏短。

### 3. 步长不一致（剩余主因）

- 服务端旧实现固定用 50ms 一步。
- 客户端 `useHero` 实际按 `subDt = min(1/60, (radius / initSpeed) * 0.5)` 计算子步，再经 `Hero.update` 的 `toFixed` 取整；当前默认参数下约为 6~7ms。

相同初速度下，50ms 粗步子会让离散积分明显少滑。以 `v0=1500 px/s` 横向发射为例，旧服务端约滑 5654px；只按完整子步模拟客户端约 6047px，再复现客户端 60fps 帧尾截断子步后约 6112px。

### 4. 房间帧与模拟器物理帧

现有架构已经满足隔离要求：

- `RoomSystem` 不调用 `SimTickAsync`。
- 模拟器由自己的独立 20Hz timer 驱动。
- 房间帧只负责写入 `PendingSimFrame` 输入。

本次未修改该链路。

## 修复内容

### 最终修改路径

1. 先对照客户端真实实现，确认 `Hero.update` 是“先位移、后减速”，服务端旧 `TickPhysics` 是“先减速、后位移”。
2. 将 `FixedMul`、`FixedDiv`、`FixedHypot` 对齐客户端 `fixed.ts` 的 `Math.round` 语义，避免服务端每帧衰减更快。
3. 发现服务端仍固定 50ms 一步，而客户端 `useHero` 按 `subDt = min(1/60, (radius / initSpeed) * 0.5)` 跑细步子。
4. 在 `PlayerSimData` 增加 `InitSpeed`，LAUNCH 后用 `FixedHypot` 记录客户端同款的合成初速度。
5. 再发现客户端每个 rAF 帧不会全部打满完整子步，最后会用 `remaining` 截断一步；服务端需要在 50ms 模拟周期内复现 3 个 60fps 帧循环，才能消除几十像素的过冲/不足。
6. 最终实现：服务端每个 50ms 周期跑 3 个客户端帧，每帧按 `subDt` 与 `remaining` 计算实际 `dt`，再交给同一个“位移 → 减速 → 边界 clamp”的 `TickPlayer`。

房间帧仍只负责写入 `PendingSimFrame`，模拟器物理帧完全由自己的 timer 驱动。

### `Hotfix/Simulation/Abstractions/SimBase.cs`

- `FixedMul`、`FixedDiv` 改为 JS `Math.round` 语义。
- `FixedHypot` 改为四舍五入平方根。
- 保留 `SqrtU64` 的向下取整语义，继续用于形状碰撞的 `isqrt` 路径。

### `Entity/Simulation/SimStateEntity.cs`

- `PlayerSimData` 增加 `InitSpeed`，对齐客户端 `hero.initSpeed`，用于计算动态子步。

### `Hotfix/Simulation/Versions/V1/BattleOfCellV1.cs`

- LAUNCH 脉冲改用 `FixedMul`，与客户端 `fixedMul(dirX, speed)` 一致。
- 每个玩家按 `InitSpeed` 计算自己的子步 `dt`，并在 50ms 服务端周期内按 60fps 客户端帧循环处理剩余时间，包含每帧末尾的截断子步。
- LAUNCH 日志补充方向与速度，便于复现核对。

## 验证

- `dotnet build Hotfix/Hotfix.csproj`：0 警告，0 错误。
- 最终实测：客户端 X 变动 3399，服务端 X 变动 3398，仅剩 1px 的帧时间/取整残差。
- 按 `v0=1500 px/s`、`deceleration=200 px/s^2`、默认半径 20px 复核：

| 路径 | 滑行距离 |
| --- | --- |
| 旧服务端（50ms 粗步子） | 5654 px |
| 客户端 60fps 帧循环（同款子步 + 帧尾截断） | 6112 px |

## 未修改

- `RoomFrameSyncSystem`
- `RoomSystem`
- 客户端代码
- 房间帧率与模拟器物理帧率绑定关系

## 局限

本次服务端按客户端 60fps 的 `requestAnimationFrame` 帧循环对齐。如果客户端实际运行在 120/144Hz，`rAF` 的剩余时间与子步分布会变化，纯服务端无法凭空复现；届时需要客户端锁 60fps，或把每帧实际使用的物理步数随 LAUNCH 一起上送。
