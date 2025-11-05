# RimSpine2DFramework

RimSpine2DFramework adds Spine 2D animation support to **RimWorld** mods. It provides reusable systems for loading Spine assets, binding them to RimWorld pawns, and extending the game's state machines so animated characters can react dynamically to in-game events.

## Key features

- **Spine 2D integration:** Bridges RimWorld's rendering pipeline with Spine skeletons and animations.
- **Dynamic pawn animation state machines:** Maps in-game pawn behaviors to matching animation states so characters respond naturally.
- **Extensible XML definitions:** Provides XML and C# helpers for adding new animated pawns, objects, and thought handlers.
- **Documentation and examples:** Includes reference docs in `DynamicObject/docs/` to help mod authors adapt the framework.

## Repository layout

| Path | Description |
| --- | --- |
| `DynamicObject/` | Core C# project containing the runtime logic and game integration code. |
| `DynamicObject/Abandoned/` | Legacy experiments and unused prototypes kept for reference. |
| `DynamicObject/Core/` | Mod bootstrap classes, object managers, and shared runtime helpers. |
| `DynamicObject/Definitions/` | XML-backed `Def` classes that expose dynamic objects, plans, pawns, and storytellers. |
| `DynamicObject/Graphics/` | Structures for loading Spine animations and binding RimWorld textures. |
| `DynamicObject/Harmony/` | Harmony patches that inject Spine-driven behavior into the RimWorld engine. |
| `DynamicObject/Incidents/` | Custom incident workers, such as animated Originium slug events. |
| `DynamicObject/Pawn/` | Pawn components, renderers, and state machines that drive in-game animations. |
| `DynamicObject/Properties/` | Assembly metadata for the compiled mod. |
| `DynamicObject/Spine/` | Runtime wrappers for different Spine runtime versions plus adapter helpers. |
| `DynamicObject.sln` | Visual Studio solution for building and debugging the mod locally. |

## How to use

1. Subscribe to [RimSpine2DFramework 动态框架](https://steamcommunity.com/sharedfiles/filedetails/?id=3010174963) on the Steam Workshop.
2. Enable the mod in RimWorld and launch the game.
3. Mod authors who want to extend the framework can consult the [repository wiki](https://github.com/0x7C13/RimSpine2DFramework/wiki) and the docs in `DynamicObject/docs/`.

## Additional resources

- Steam Workshop: [RimSpine2DFramework 动态框架](https://steamcommunity.com/sharedfiles/filedetails/?id=3010174963)
- Example mod: [ArknightsStoryTellers 明日方舟动态叙述者](https://steamcommunity.com/sharedfiles/filedetails/?id=3010189725)

## License

This repository follows the licensing specified by the original authors. Refer to the Steam Workshop pages or contact the maintainers for clarification.
