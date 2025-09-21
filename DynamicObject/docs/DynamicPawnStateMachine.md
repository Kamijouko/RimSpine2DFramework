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
