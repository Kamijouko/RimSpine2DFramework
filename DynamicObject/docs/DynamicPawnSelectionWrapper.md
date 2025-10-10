# DynamicPawnSelectionWrapper 设计说明

为了满足“让某个 Pawn 依然存在于地图上，但在鼠标选取目标时不可被 RimWorld 的参数选择框选中”的需求，我们在框架中新增了 `DynamicPawnSelectionWrapper`。

## 设计思路

1. **按需启用**：
   - 是否需要屏蔽鼠标选取由 `DynamicPawnStateMachineDef` 的新布尔开关 `blockMouseTargeting` 控制。
   - 当状态控制器绑定到 Pawn 时，如果该开关为 `true`，则通过 `DynamicPawnSelectionWrapper.Wrap(pawn)` 获取一个与 Pawn 关联的包装器对象，并把 `BlockMouseTargeting` 标记设为 `true`。
   - 释放控制器 (`Dispose`) 时则调用 `DynamicPawnSelectionWrapper.Clear(pawn)`，确保不会影响之后对 Pawn 的正常操作。

2. **弱引用存储**：
   - 采用 `ConditionalWeakTable<Pawn, DynamicPawnSelectionWrapper>` 保存包装器，以避免显式管理生命周期。只要 Pawn 被 GC 清理或我们手动 `Clear`，记录就会自动消失。

3. **补丁切入点**：
   - RimWorld 内部在执行各种鼠标指向/参数选择操作时都会调用 `TargetingParameters.CanTarget`，不同版本或调用场景可能传入 `Thing`、`LocalTargetInfo` 或 `GlobalTargetInfo`。
   - 我们使用 Harmony 在这些重载方法前插入逻辑：当目标是 Pawn 或者包含 Pawn 的 `Thing`（如 `Corpse`）时，查询包装器的 `ShouldBlock`。
   - 如果包装器标记为拦截，则直接把方法返回值设为 `false` 并跳过原始逻辑，从而阻断所有通过鼠标进行的参数选择。

4. **行为范围**：
   - 只影响通过鼠标拾取目标的流程，不会屏蔽脚本直接把 Pawn 作为参数传递的情况。
   - 由于 Harmony Patch 的位置在 `TargetingParameters` 层级，也适用于 RimWorld 原版和大多数依赖该接口的模组。

## 使用步骤

1. 在对应的 `DynamicPawnStateMachineDef` 里把 `blockMouseTargeting` 设置为 `true`。
2. 当 Pawn 由框架的状态控制器管理时，它将无法被鼠标作为 `TargetingParameters` 的合法目标选中。
3. 当控制器被释放或 Pawn 离开地图/被销毁时，拦截会自动移除。

通过以上机制，我们实现了“存在但不可被鼠标选中”的效果，同时保持对 Pawn 生命周期的安全管理，避免了悬挂引用或持久化副作用。
