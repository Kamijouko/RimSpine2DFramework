# DynamicPawnStateMachineDef 触发器说明

`DynamicPawnStateMachineDef` 的 `<triggers>` 元素用于描述动画状态的切换条件。除了原有的 Job、Verb 等来源外，现在还可以使用新的移动状态触发器：

```xml
<trigger source="Movement" isMoving="true" />
```

* `source="Movement"`：表示根据 Pawn 当前是否在移动来判断是否满足触发条件。
* `isMoving`：必填布尔字段，`true` 表示当 Pawn 正在移动时触发，`false` 表示当 Pawn 停止移动时触发。请务必显式写出 `isMoving="true"` 或 `isMoving="false"`，否则会收到加载时的警告，并且默认行为会被视为“未移动”。

例如，当 Pawn 开始行走时切换到移动动画，可以这样定义：

```xml
<triggers>
  <li>
    <source>Movement</source>
    <isMoving>true</isMoving>
  </li>
</triggers>
```

或者使用属性写法：

```xml
<trigger source="Movement" isMoving="false" />
```

这类触发器会读取 `pawn.pather?.Moving` 的当前值，与 `isMoving` 比较来决定是否满足条件。




顶层字段
targetDynamicObject：指向要驱动的 DynamicObjectDef，注册时会按该字段把状态机归类到对应的动态对象下，并在绑定 Pawn 时依据这个键挑选控制器，缺失时会直接报错。

binding：PawnBinding 结构，用来筛选哪些 Pawn 可以使用此状态机（按种族、阵营、是否殖民者、是否允许动物/非人形/非玩家等），Matches 会逐项检查这些限制，TryBind 时只会选中第一个通过 Matches 的定义。

states：PawnAnimationState 列表，是状态机实际可播的动画配置集合；刷新时先按 priority 从高到低遍历，遇到 triggers 全部满足的状态就切换到对应动画，否则回落到默认或标记为 isFallback 的状态。

defaultStateId：当没有任何触发器命中且没有显式强制状态时，会用这个 ID 寻找默认状态作为兜底播放。

interactionStateId：外部调用 TriggerInteraction 时使用的状态 ID，可让交互事件临时切换到指定动画一次。

priority（状态机级）：同一动态对象可能有多份状态机定义，注册时按此值降序排序，绑定时优先尝试高优先级定义以支持更具体的覆盖配置。

skeleton：PawnSkeletonSettings 默认骨架参数，在控制器构造时应用到 DynamicObjectInstance，为 Pawn 专用实例提供默认皮肤、缩放、偏移、旋转与相机距离等基础外观设置。

hideVanillaPawn：布尔开关，默认为 `false`。当设置为 `true` 时，状态机控制器会阻止原版 `PawnRenderer` 绘制该 Pawn，仅保留 Spine 动态模型，适用于完全替换原模型的场景。

blockMouseTargeting：布尔开关，默认为 `false`。当设置为 `true` 时，会给对应的 Pawn 注册一个“鼠标选取拦截器”，阻止 RimWorld 的目标选取系统在鼠标拾取目标时把该 Pawn 当作合法的 `Thing` 目标，从而避免被动弹窗、技能指向等鼠标参数选择操作选中它。设计细节与使用建议参见《DynamicPawnSelectionWrapper 设计说明》文档。

PawnBinding 字段
pawnKinds / factions：限制绑定到特定 PawnKind 或阵营；如果列表为空则不限制，对应条件不满足时 Matches 会返回 false。

colonistOnly：仅允许殖民者使用；配合 allowNonPlayer 可以控制是否只对玩家殖民者生效。

allowAnimals / allowNonHumanlike / allowNonPlayer：控制是否排除动物、非人形或非玩家阵营 Pawn，Matches 会在最前面依据这些布尔值过滤。

PawnAnimationState 字段
stateId：状态标识符，用于与 currentStateId 比较、触发强制状态或匹配 defaultStateId。

animationName：要播放的 Spine 动画名称，ApplyState 会据此调用 SetAnimation/AddAnimation；为空会记录警告并跳过。

skin：切换到的皮肤名，若留空则回退到实例默认皮肤；状态变化时只在皮肤改变时调用适配器切换，避免重复。

loop、useQueue、trackIndex、mixDuration、delay、clearTrack、clearMixDuration：控制动画播放方式（是否循环、排队或直接替换、播放轨道、混合时长、延迟、是否清轨及清轨混合时长）；这些参数直接传给 Spine 的动画状态接口。

forceRestart：即使当前已在同一状态也强制重播；SelectState 会根据该值决定是否再次应用状态。

isFallback：标记为备用状态，遍历时记录第一个符合条件的 fallback，当其他状态都不匹配时作为最终结果。

priority（状态级）：决定同层状态的匹配顺序，值越高越先检查。

triggers：触发器列表；如果列表为空，状态始终可用；否则必须全部通过 MatchesTrigger 才会选中。

PawnStateTrigger 字段
source：触发器类型（作业、动词、需求、状态等），决定调用哪种匹配函数。

def / defName：需要匹配的游戏 Def；ResolveTriggerDefName 会优先取 def.defName，否则回落到字符串字段。

stageIndex / stageName：用于对作业、需求、Hediff、Thought 等多阶段对象进一步筛选；对应匹配函数会检查当前阶段索引或标签。

isMoving：Movement 触发器专用布尔值，缺省时默认要求 Pawn 不在移动，设置后则精确比较当前移动状态。

threshold / thresholdGreaterOrEqual：给需求、Hediff 等带数值的触发器设置阈值与比较方向，用于只在数值高于或低于某个值时生效。

PawnStateTriggerSource 枚举
定义了支持的八种事件来源：作业、动词、需求、Hediff、思绪、职责、精神状态、移动；MatchesTrigger 会根据该枚举路由到对应的匹配函数。

PawnSkeletonSettings
defaultSkin、scale、offset、rotation、cameraDistance：为 Pawn 绑定的骨架提供默认皮肤与局部变换（缩放、平移、旋转）及摄像机距离。控制器在绑定时调用 ApplyPawnSkeletonSettings 保存这些参数，实例再把它们与 storyteller 配置合并成 SkeletonConfiguration，由 Spine 适配器在 ConfigureSkeleton 里设置 transform 并初始化骨架；cameraDistance 额外决定模型沿 Z 轴的偏移。
