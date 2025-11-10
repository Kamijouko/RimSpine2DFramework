# 🧩 プロジェクトコードファイル出典リスト
 
- `Spine3.5`、`Spine3.8`、`Spine4.0`、`Spine4.1` ディレクトリは **Spine2D公式ランタイムコード** に由来し、内部ファイルは展開していません。

```text
. /
├── DynamicObject/
│   ├── Core/
│   │   ├── DynamicObjectInstance.cs（自作）
│   │   ├── ImportMode.cs（自作）
│   │   ├── ModDynamicObjectManager.cs（自作）
│   │   ├── ModStaticMethod.cs（自作）
│   │   └── ThisModBase.cs（自作）
│   ├── Definitions/
│   │   ├── DynamicObjectDef.cs（自作）
│   │   ├── DynamicObjectPlanDef.cs（自作）
│   │   ├── DynamicPawnStateMachineDef.cs（自作）
│   │   └── DynamicStoryTellerDef.cs（自作）
│   ├── DynamicObject.csproj（自作）
│   ├── Graphics/
│   │   ├── SpineAnimationType.cs（自作）
│   │   ├── SpineTextAssetData.cs（自作）
│   │   └── TexturePath.cs（自作）
│   ├── Harmony/
│   │   ├── HarmonyMain.cs（自作）
│   │   └── PawnStateHarmonyPatches.cs（Codexの支援を受けて作成・改良）
│   ├── Incidents/
│   │   └── IncidentWorker_AggressiveOriginiumSlug.cs（自作）
│   ├── Pawn/
│   │   ├── Components/
│   │   │   ├── DynamicPawnComp.cs（自作）
│   │   │   └── DynamicPawnComp_Properties.cs（自作）
│   │   ├── Rendering/
│   │   │   └── Chibi_PawnRenderNode_Body.cs（自作）
│   │   └── StateMachine/
│   │       ├── DynamicPawnSelectionWrapper.cs（Codexの支援を受けて作成・改良）
│   │       ├── DynamicPawnStateController.cs（Codexの支援を受けて作成・改良）
│   │       └── DynamicPawnStateRegistry.cs（Codexの支援を受けて作成・改良）
│   ├── Properties/
│   │   └── AssemblyInfo.cs（自作）
│   └── Spine/
│       ├── Spine3.5/（Spine2D公式ランタイムコードより）
│       ├── Spine3.8/（Spine2D公式ランタイムコードより）
│       ├── Spine4.0/（Spine2D公式ランタイムコードより）
│       ├── Spine4.1/（Spine2D公式ランタイムコードより）
│       └── SpineAdapters/
│           ├── ISpineRuntimeAdapter.cs（Codexの支援を受けて作成・改良）
│           ├── Spine35RuntimeAdapter.cs（Codexの支援を受けて作成・改良）
│           ├── Spine38RuntimeAdapter.cs（Codexの支援を受けて作成・改良）
│           ├── Spine40RuntimeAdapter.cs（Codexの支援を受けて作成・改良）
│           └── Spine41RuntimeAdapter.cs（Codexの支援を受けて作成・改良）
└── DynamicObject.sln（自作）
```
