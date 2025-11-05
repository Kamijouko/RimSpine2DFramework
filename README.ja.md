# RimSpine2DFramework

RimSpine2DFramework は **RimWorld** の mod 向けに Spine 2D アニメーションを導入するフレームワークです。Spine アセットの読み込み、RimWorld のポーンへのバインド、ゲーム内イベントに応じて動作するステートマシン拡張をまとめて提供します。

## 主な特徴

- **Spine 2D との連携:** RimWorld の描画パイプラインと Spine のスケルトン／アニメーションを橋渡しします。
- **動的Pawnアニメーションステートマシン:** ゲーム内のPawn行動に対応するアニメーション状態を割り当て、自然な動きを実現します。
- **拡張可能な XML 定義:** 新しいポーンやオブジェクト、思考ハンドラーを追加しやすい XML と C# のユーティリティを収録しています。
- **ドキュメントとサンプル:** `DynamicObject/docs/` には C# の参照資料や XML サンプル、ステートマシン定義がまとめられています。

## リポジトリ構成

| パス | 内容 |
| --- | --- |
| `DynamicObject/` | ランタイム処理とゲーム連携コードを含むコア C# プロジェクト。 |
| `DynamicObject/Abandoned/` | 参考用に残している旧実験コードと未使用プロトタイプ。 |
| `DynamicObject/Core/` | Mod 起動クラス、オブジェクトマネージャー、共通ランタイムヘルパー。 |
| `DynamicObject/Definitions/` | 動的オブジェクトやプラン、ポーン、ストーリーテラーを定義する XML `Def` クラス。 |
| `DynamicObject/Graphics/` | Spine アニメーションの読み込みと RimWorld テクスチャの紐付けを行う構造体。 |
| `DynamicObject/Harmony/` | RimWorld エンジンに Spine 駆動の挙動を注入する Harmony パッチ。 |
| `DynamicObject/Incidents/` | アニメ付きオリジニウムスラッグなどのカスタムインシデントワーカー。 |
| `DynamicObject/Pawn/` | ゲーム内アニメーションを制御するポーンのコンポーネント、レンダラー、ステートマシン。 |
| `DynamicObject/Properties/` | Mod アセンブリのメタデータ。 |
| `DynamicObject/Spine/` | 各 Spine ランタイムバージョン向けのラッパーとアダプター。 |
| `DynamicObject.sln` | ローカル開発用の Visual Studio ソリューション。 |

## 利用方法

1. Steam ワークショップで [RimSpine2DFramework](https://steamcommunity.com/sharedfiles/filedetails/?id=3010174963) を登録します。
2. RimWorld のランチャーで mod を有効化し、ゲームを開始します。
3. フレームワークを拡張したい mod 作者は、[リポジトリの Wiki](https://github.com/0x7C13/RimSpine2DFramework/wiki) と `DynamicObject/docs/` の資料を参照してください。

## 関連リンク

- Steam ワークショップ: [RimSpine2DFramework](https://steamcommunity.com/sharedfiles/filedetails/?id=3010174963)
- サンプル mod: [ArknightsStoryTellers](https://steamcommunity.com/sharedfiles/filedetails/?id=3010189725)

## ライセンス

本リポジトリのライセンスは作者が Steam ワークショップで示す方針に従います。不明点がある場合はメンテナーへお問い合わせください。
