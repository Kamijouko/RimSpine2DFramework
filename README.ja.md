# RimSpine2DFramework

RimSpine2DFramework は **RimWorld** の mod 向けに Spine 2D アニメーションを導入するフレームワークです。Spine アセットの読み込み、RimWorld のポーンへのバインド、ゲーム内イベントに応じて動作するステートマシン拡張をまとめて提供します。

## 主な特徴

- **Spine 2D との連携:** RimWorld の描画パイプラインと Spine のスケルトン／アニメーションを橋渡しします。
- **動的ポーンアニメーションステートマシン:** ゲーム内のポーン行動に対応するアニメーション状態を割り当て、自然な動きを実現します。
- **拡張可能な XML 定義:** 新しいポーンやオブジェクト、思考ハンドラーを追加しやすい XML と C# のユーティリティを収録しています。
- **ドキュメントとサンプル:** `DynamicObject/docs/` には C# の参照資料や XML サンプル、ステートマシン定義がまとめられています。

## リポジトリ構成

| パス | 内容 |
| --- | --- |
| `DynamicObject/` | フレームワークの中核となる C# ソース（アニメーション処理や Harmony パッチを含む）。 |
| `DynamicObject/docs/` | 参考ドキュメント（C# リファレンス、XML サンプル、ステートマシン定義など）。 |
| `DynamicObject/Spine/` | Spine アニメーションデータを扱うユーティリティ。 |

## 利用方法

1. Steam ワークショップで [RimSpine2DFramework 動態フレームワーク](https://steamcommunity.com/sharedfiles/filedetails/?id=3010067716) を購読します。
2. RimWorld のランチャーで mod を有効化し、ゲームを開始します。
3. フレームワークを拡張したい mod 作者は、[リポジトリの Wiki](https://github.com/0x7C13/RimSpine2DFramework/wiki) と `DynamicObject/docs/` の資料を参照してください。

## 関連リンク

- Steam ワークショップ: [RimSpine2DFramework 動態フレームワーク](https://steamcommunity.com/sharedfiles/filedetails/?id=3010067716)
- サンプル mod: [ArknightsStoryTellers 明日方舟動態語り部](https://steamcommunity.com/sharedfiles/filedetails/?id=3010113041)

## ライセンス

本リポジトリのライセンスは作者が Steam ワークショップで示す方針に従います。不明点がある場合はメンテナーへお問い合わせください。
