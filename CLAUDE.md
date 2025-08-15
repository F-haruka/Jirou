# CLAUDE.md

このファイルは、Claude Code (claude.ai/code) がこのリポジトリで作業する際のガイダンスを提供します。

## 重要な前提条件

**言語について：**
- このプロジェクトのすべてのドキュメントは日本語で記述してください
- Claude Code との会話もすべて日本語で行ってください
- コードのコメントも日本語で記述してください
- 変数名やクラス名は英語でも構いませんが、説明やドキュメントは必ず日本語を使用してください

## プロジェクト概要

「Jirou」は開発初期段階にあるリズムゲームのUnity 3Dゲーム開発プロジェクトです。プロジェクトは`JirouUnity/`サブディレクトリに配置された2D URP（Universal Render Pipeline）Unityプロジェクトとして設定されています。

**プロジェクト種別：** Unity 3D ゲーム（2D リズムゲーム）
**Unity バージョン：** 6000.2.0f1 (Unity 6.0 LTS)
**レンダーパイプライン：** Universal Render Pipeline (URP)

## プロジェクト構造

リポジトリは、ルートレベルでドキュメントを管理し、Unityプロジェクトをサブディレクトリに配置するデュアル構造アプローチを採用しています：

```
Jirou/
├── CLAUDE.md               # このファイル - Claude Code用ガイダンス
├── docs/                   # プロジェクトドキュメント・企画書
│   ├── architectures/      # アーキテクチャ関連ドキュメント
│   ├── design/            # デザイン関連ドキュメント
│   └── plans/
│       ├── setup.md       # 詳細なセットアップガイド（日本語）
│       └── step.md        # 包括的な開発ガイド（日本語）
├── JirouUnity/            # メインUnityプロジェクトディレクトリ
│   ├── Assets/
│   │   ├── Animations/    # アニメーション関連アセット
│   │   ├── Audio/
│   │   │   ├── Music/     # BGM・楽曲ファイル
│   │   │   └── SFX/       # 効果音ファイル
│   │   ├── DefaultVolumeProfile.asset  # URPボリューム設定
│   │   ├── Fonts/         # フォントファイル
│   │   ├── InputSystem_Actions.inputactions  # 入力システム設定
│   │   ├── Materials/     # マテリアルファイル
│   │   ├── Prefabs/
│   │   │   ├── Gameplay/  # ゲームプレイ用プレハブ
│   │   │   └── UI/        # UI用プレハブ
│   │   ├── Scenes/
│   │   │   └── SampleScene.unity  # メインシーン
│   │   ├── Scripts/
│   │   │   ├── Gameplay/  # ゲームプレイ関連スクリプト（現在空）
│   │   │   ├── Managers/  # 各種マネージャースクリプト（現在空）
│   │   │   └── UI/        # UI関連スクリプト（現在空）
│   │   ├── Settings/      # URP設定とシーンテンプレート
│   │   ├── Shaders/       # カスタムシェーダー
│   │   ├── Sprites/
│   │   │   ├── Backgrounds/  # 背景スプライト
│   │   │   ├── Notes/       # ノーツスプライト
│   │   │   └── UI/          # UIスプライト
│   │   ├── Timeline/      # Timelineアセット
│   │   └── UniversalRenderPipelineGlobalSettings.asset
│   ├── Editor/            # エディタ専用スクリプト
│   ├── JirouUnity.sln     # Visual Studioソリューションファイル
│   ├── Library/           # Unity生成キャッシュ（.gitignoreで除外）
│   ├── Logs/              # Unity生成ログ（.gitignoreで除外）
│   ├── Packages/
│   │   ├── manifest.json  # パッケージ依存関係定義
│   │   └── packages-lock.json  # パッケージロックファイル
│   ├── ProjectSettings/   # Unity プロジェクト設定
│   ├── Temp/              # 一時ファイル（.gitignoreで除外）
│   └── UserSettings/      # ユーザー固有設定（.gitignoreで除外）
└── .gitignore             # Unity専用gitignore設定
```

## 開発環境

### Unity設定
- **バージョン管理モード：** Visible Meta Files（プロジェクト設定で設定済み）
- **アセットシリアライゼーション：** Force Text（Git互換性のため）
- **入力システム：** 新しい入力システム（Unity Input System パッケージ）

### 主要Unityパッケージ
- Universal Render Pipeline (URP) 17.2.0
- 2D Animation 12.0.2
- Input System 1.14.1
- Timeline 1.8.7
- TextMeshPro (UGUI 2.0.0)
- Visual Scripting 1.9.7

### 入力システム設定
プロジェクトはUnityの新しい入力システムを使用し、以下のアクションマップが事前定義されています：
- **Player：** 移動、視点操作、攻撃、インタラクト、しゃがみ、ジャンプ、走り、ナビゲーション
- **UI：** 標準的なUI操作制御

対応入力方式：
- キーボード＆マウス
- ゲームパッド
- タッチ
- XRコントローラー

## 開発コマンド

これはUnityプロジェクトのため、開発は主にUnityエディタを通じて行われます。ただし、一般的な操作には以下があります：

### プロジェクトビルド
- Unityエディタを開く → File → Build Settings
- ビルドにシーンが追加されていることを確認
- ターゲットプラットフォームを選択してビルド

### バージョン管理
プロジェクトはUnity専用の.gitignore設定でGitを使用します：
```bash
# 一般的なGit操作
git status                  # リポジトリの状態確認
git add .                  # すべての変更をステージング
git commit -m "メッセージ"    # 変更をコミット
```

**重要：** プロジェクトはUnity開発用に以下が設定されています：
- Library/フォルダは除外
- Temp/およびUserSettings/フォルダは除外
- .metaファイルは追跡（Unityには必須）

## コードアーキテクチャ

### 現在の状態
プロジェクトは初期設定段階にあり、以下の状態です：
- 機能別に整理された空のスクリプトディレクトリ（Gameplay/, Managers/, UI/）
- SampleSceneを含む基本シーン構造
- 標準Unity 2D URPテンプレート設定
- 入力システムは設定済みだが未実装

### 計画されたアーキテクチャ（ドキュメントより）
`docs/plans/`の日本語ドキュメントに基づき、以下の4レーン リズムゲームとして計画されています：
- 「Vibe Coding」手法を用いたAI支援開発
- コアマネージャー（Conductor, ScoreManager）のシングルトンパターン
- 楽曲チャート用のScriptableObjectベースのデータ駆動設計
- スポーンと判定メカニクスを持つプレハブベースのノートシステム

### 実装予定の主要システム
1. **Conductorシステム：** `AudioSettings.dspTime`を使用したマスタータイミング/オーディオ同期
2. **ノートスポーン：** チャートデータからのプレハブベースノート生成
3. **入力管理：** 4レーン入力検出と判定
4. **スコアリングシステム：** タイミングベースのスコアリングとコンボ追跡
5. **チャートデータ：** ScriptableObjectベースの楽曲・ノートデータ

## 開発ワークフロー

### AI支援開発（「Vibe Coding」）
ドキュメントによると、このプロジェクトは「Vibe Coding」アプローチに従います：

1. **人間の役割：** アーキテクチャ設計、システム統合、要件定義
2. **AIの役割：** 実装、コード生成、反復作業
3. **プロセス：** プロンプト → コード → レビュー → 改良の反復サイクル

### このプロジェクトのベストプラクティス
- リズムの精度のため`AudioSettings.dspTime`を使用した正確なタイミング
- コアゲームマネージャーのシングルトンパターンに従う
- ScriptableObjectsでデータ駆動設計を実装
- ゲームプレイ、マネージャー、UIの明確な分離を維持
- Unityのコンポーネントベースアーキテクチャに従う

### テスト
- Unityエディタでプレイテスト
- 実装されている場合はUnity Test Frameworkを使用
- ターゲットプラットフォームでビルドしてテスト

## AI開発のための注意事項

1. **タイミング精度：** リズムゲームにはサンプル精度のタイミングが必要 - 音楽同期には常に`AudioSettings.dspTime`を使用し、`Time.time`や`Time.deltaTime`は絶対に使用しないこと

2. **ScriptableObjects：** 楽曲データ、チャートデータ、設定にはScriptableObjectsを使用 - これによりデザイナーフレンドリーな編集が可能になる

3. **プレハブシステム：** ノートは簡単な変更とスポーンのためプレハブベースにすべき

4. **入力システム：** プロジェクトはUnityの新しい入力システムを使用 - `InputSystem_Actions.inputactions`で設定されたアクションマップを参照

5. **2D URP機能：** URPで利用可能な2Dライティング、カスタムシェーダー、ポストプロセッシングを活用

6. **日本語ドキュメント：** `docs/plans/`ディレクトリにはプロジェクトのアーキテクチャと開発アプローチに関する包括的な日本語ドキュメントが含まれています - 詳細な実装ガイダンスについてはこれらを参照してください

## ファイル命名規則

- スクリプトにはPascalCaseを使用：`NoteController.cs`、`ScoreManager.cs`
- 説明的なプレハブ名を使用：`Note_Basic.prefab`、`HitEffect.prefab`
- それぞれのフォルダ内でタイプ別にアセットを整理
- 関連アセットには一貫した命名を使用（例：`Song_Title_Chart.asset`、`Song_Title_Audio.mp3`）

## コミュニケーション指針

**重要：** Claude Codeとの全ての会話は日本語で行ってください。これには以下が含まれます：
- コードの説明と解説
- エラーメッセージの説明
- 実装提案と議論
- プロジェクト計画の検討
- デバッグとトラブルシューティング

この日本語中心のアプローチにより、`docs/plans/`にある既存の日本語ドキュメントとの一貫性を保ち、プロジェクトの全体的な理解を深めることができます。