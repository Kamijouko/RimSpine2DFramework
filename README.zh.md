# RimSpine2DFramework

RimSpine2DFramework 为 **RimWorld** 模组提供 Spine 2D 动画支持，帮助作者在游戏中实现流畅的骨骼动画。框架封装了资源加载、角色绑定以及状态机扩展等常用逻辑，让自定义角色可以根据游戏事件进行动态表现。

## 主要特性

- **Spine 2D 集成：** 将 RimWorld 的渲染流程与 Spine 骨骼动画衔接。
- **动态角色动画状态机：** 将游戏内的行为与对应动画状态关联，让角色表现更自然。
- **可扩展 XML 定义：** 提供 XML 与 C# 辅助类，便于添加新的角色、物体与思维处理器。

## 仓库结构

| 路径 | 说明 |
| --- | --- |
| `DynamicObject/` | 框架核心 C# 项目，包含运行时逻辑与游戏集成代码。 |
| `DynamicObject/Abandoned/` | 历史实验与未使用的原型，仅供参考。 |
| `DynamicObject/Core/` | 模组入口类、对象管理器与通用运行时辅助功能。 |
| `DynamicObject/Definitions/` | 用于暴露动态物体、计划、角色与讲述者的 XML `Def` 类。 |
| `DynamicObject/Graphics/` | 负责加载 Spine 动画并绑定 RimWorld 贴图的结构。 |
| `DynamicObject/Harmony/` | 向 RimWorld 引擎注入 Spine 行为的 Harmony 补丁。 |
| `DynamicObject/Incidents/` | 自定义事件处理，例如带动画效果的源石虫事件。 |
| `DynamicObject/Pawn/` | 驱动游戏内动画的角色组件、渲染器与状态机。 |
| `DynamicObject/Properties/` | 模组程序集的元数据。 |
| `DynamicObject/Spine/` | 不同 Spine 运行时版本的封装与适配工具。 |
| `DynamicObject.sln` | 用于本地构建与调试的 Visual Studio 解决方案。 |

## 使用方法

1. 前往 Steam 创意工坊订阅 [RimSpine2DFramework 动态框架](https://steamcommunity.com/sharedfiles/filedetails/?id=3010174963)。
2. 在 RimWorld 启动器中启用该模组并开始游戏。
3. 若你是模组作者，希望基于本框架拓展功能，请参考 [仓库 Wiki](https://github.com/Kamijouko/RimSpine2DFramework/wiki) 中的资料。

## 相关链接

- Steam 创意工坊：[RimSpine2DFramework 动态框架](https://steamcommunity.com/sharedfiles/filedetails/?id=3010174963)
- 示例模组：[ArknightsStoryTellers 明日方舟动态叙述者](https://steamcommunity.com/sharedfiles/filedetails/?id=3010189725)

## 许可说明

本仓库遵循原作者在 Steam 创意工坊页面所述的授权方式。如有疑问，请联系维护者获取更多信息。
