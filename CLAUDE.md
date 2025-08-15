# CLAUDE.md

このファイルは、Claude Code (claude.ai/code) がこのリポジトリで作業する際のガイダンスを提供します。

## 重要な前提条件

**言語について：**
- このプロジェクトのすべてのドキュメントは日本語で記述してください
- Claude Code との会話もすべて日本語で行ってください
- コードのコメントも日本語で記述してください
- 変数名やクラス名は英語でも構いませんが、説明やドキュメントは必ず日本語を使用してください

## プロジェクト概要

「Jirou」は開発初期段階にある奥行き型リズムゲームのUnity 3Dゲーム開発プロジェクトです。プロジェクトは`JirouUnity/`サブディレクトリに配置された3D URP（Universal Render Pipeline）Unityプロジェクトとして設定されています。

**プロジェクト種別：** Unity 3D ゲーム（奥行き型リズムゲーム）
**Unity バージョン：** 6000.2.0f1 (Unity 6.0 LTS) / または2022.3 LTS（推奨）
**レンダーパイプライン：** Universal Render Pipeline (URP)

### ゲームの特徴

- **奥行き表現**: ノーツが画面奥（Z軸正の値）から手前の判定ライン（Z=0）に向かって流れる3D空間でのリズムゲーム
- **4レーンシステム**: シンプルで分かりやすい4つのレーン
- **2種類のノーツ**: Tap（単押し）とHold（長押し）のみ
- **キーボード入力**: D、F、J、Kキーによる簡単操作
- **基本的な判定システム**: Perfect、Great、Good、Missの4段階
- **台形状レーン**: 奥が狭く、手前が広がる遠近感のあるレーン表示
- **動的スケーリング**: ノーツが奥から手前に来るにつれて大きくなる視覚効果

## プロジェクト構造

リポジトリは、ルートレベルでドキュメントを管理し、Unityプロジェクトをサブディレクトリに配置するデュアル構造アプローチを採用しています：

```
Jirou/
├── CLAUDE.md               # このファイル - Claude Code用ガイダンス
├── docs/                   # プロジェクトドキュメント・企画書
│   ├── architectures/      # アーキテクチャ関連ドキュメント
│   ├── design/            # デザイン関連ドキュメント
│   └── plans/
│       └── 3d-rhythm-game-guide.md  # 奥行き型リズムゲーム実装手順書
├── JirouUnity/            # メインUnityプロジェクトディレクトリ
│   ├── Assets/
│   │   ├── Settings/      # URP設定とシーンテンプレート
│   │   │   ├── SampleSceneProfile.asset
│   │   │   ├── URP-Balanced-Renderer.asset
│   │   │   ├── URP-Balanced.asset
│   │   │   ├── URP-HighFidelity-Renderer.asset
│   │   │   ├── URP-HighFidelity.asset
│   │   │   ├── URP-Performant-Renderer.asset
│   │   │   └── URP-Performant.asset
│   │   └── _Jirou/         # プロジェクト専用アセットフォルダ
│   │       ├── Art/
│   │       │   ├── Materials/   # 3Dマテリアル
│   │       │   ├── Shaders/     # カスタムシェーダー
│   │       │   └── Sprites/
│   │       │       ├── Lanes/   # レーンスプライト
│   │       │       ├── Notes/   # ノーツスプライト
│   │       │       └── UI/      # UIスプライト
│   │       ├── Audio/
│   │       │   ├── Music/       # BGM・楽曲ファイル
│   │       │   └── SFX/         # 効果音ファイル
│   │       ├── Data/
│   │       │   └── Charts/      # 譜面データ（ScriptableObject）
│   │       ├── Prefabs/
│   │       │   ├── Effects/     # エフェクトプレハブ
│   │       │   ├── Notes/       # ノーツプレハブ
│   │       │   └── Stage/       # ステージプレハブ
│   │       ├── Scenes/          # ゲームシーン
│   │       │   └── SampleScene.unity  # メインシーン
│   │       └── Scripts/
│   │           ├── Core/        # Conductor、チャートデータ等
│   │           ├── Gameplay/    # ノート制御、判定等
│   │           ├── UI/          # UIマネージャー等
│   │           └── Visual/      # エフェクト、レーン表示等
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
奥行き型リズムゲーム用の入力設定：
- **D, F, J, Kキー**: 4レーンのノーツ入力
- **キーボード中心**: メインの操作方式
- **単押し/長押し**: Tapノーツ（KeyDown）とHoldノーツ（Key継続）の判定

対応入力方式：
- キーボード＆マウス（メイン）

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

### C#コーディング規約

**重要なルール：**
- **#regionディレクティブは使用しない**: C#コードでは`#region`および`#endregion`を使用しないでください。コードの構造はクラス設計とメソッドの適切な配置で表現します。

### 現在の状態
プロジェクトは初期設定段階にあり、以下の状態です：
- ドキュメント通りのクリーンなディレクトリ構造が構築済み（Assets/_Jirou/, Assets/Settings/のみ）
- 機能別に整理された空のスクリプトディレクトリ（_Jirou/Scripts/Core/, Gameplay/, Visual/, UI/）
- SampleSceneを_Jirou/Scenes/に配置した基本シーン構造
- 3D URPテンプレート設定（Settings/に各種URP設定ファイル）
- 不要なUnityテンプレートファイル・ディレクトリを削除済み

### 計画されたアーキテクチャ（奥行き型3Dリズムゲーム）
`docs/plans/3d-rhythm-game-guide.md`に基づき、以下の奥行き型4レーンリズムゲームとして計画されています：
- 「Vibe Coding」手法を用いたAI支援開発
- 3D空間でのZ軸移動とタイミング管理
- コアマネージャー（Conductor, ScoreManager）のシングルトンパターン
- 楽曲チャート用のScriptableObjectベースのデータ駆動設計
- Z軸移動を含むプレハブベースのノートシステム
- 台形レーンによる奥行き表現

### 実装予定の主要システム
1. **奥行き対応Conductorシステム：** `AudioSettings.dspTime`を使用したマスタータイミング/オーディオ同期とZ軸位置計算
2. **3D空間ノートスポーン：** チャートデータからの3Dプレハブベースノート生成（Z軸移動）
3. **Z軸判定システム：** 4レーン入力検出とZ座標による奥行き判定
4. **スコアリングシステム：** タイミングベースのスコアリングとコンボ追跡
5. **チャートデータ：** ScriptableObjectベースの楽曲・ノートデータ（3D位置対応）
6. **視覚効果システム：** 台形レーン表示、動的スケーリング、3Dエフェクト
7. **カメラシステム：** 奥行き表現に最適化されたカメラ設定（透視投影、角度調整）

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

## ユニットテスト

### テスト環境構成

プロジェクトには Unity Test Runner を使用した自動テスト環境が構築されています：

```
Assets/Tests/
├── EditMode/               # エディタモードテスト
│   ├── Jirou.EditModeTests.asmdef
│   └── ConductorTests.cs
└── PlayMode/               # プレイモードテスト
    ├── Jirou.PlayModeTests.asmdef
    └── GameplayIntegrationTests.cs
```

### テスト実行方法

**重要：** ユニットテストの実行はUnityエディタを使用してください。

#### Unityエディタでのテスト実行（推奨）

1. **Test Runnerを開く**
   - Unityエディタで `Window > General > Test Runner` を選択

2. **テストを実行**
   - EditModeタブ: エディタ上で動作する単体テスト
   - PlayModeタブ: ゲーム実行環境での統合テスト
   - 各テストまたは「Run All」ボタンをクリック

3. **テスト結果の確認**
   - 緑のチェック: テスト成功
   - 赤のX: テスト失敗（詳細はConsoleウィンドウで確認）


### Unityテストのベストプラクティス

#### 1. テストの分類

**EditModeテスト：**
- ピュアなロジックテスト（計算、アルゴリズム）
- MonoBehaviourを使わないクラスのテスト
- ScriptableObjectのデータ検証
- 実行が高速で、Unityエンジンの機能を必要としないテスト

**PlayModeテスト：**
- GameObject、MonoBehaviourを使用するテスト
- 物理演算、コルーチン、時間経過を含むテスト
- 実際のゲームプレイシナリオの統合テスト
- オーディオ、グラフィックス、入力システムのテスト

#### 2. テスト作成のガイドライン

```csharp
// 良いテストの例
[Test]
public void NoteZPosition_CalculatesCorrectly()
{
    // Arrange - 準備
    float spawnZ = 20f;
    float beatsPassed = 2f;
    float noteSpeed = 5f;
    
    // Act - 実行
    float actualZ = spawnZ - (beatsPassed * noteSpeed);
    
    // Assert - 検証
    Assert.AreEqual(10f, actualZ, 0.001f);
}
```

#### 3. 奥行き型リズムゲーム特有のテストポイント

- **タイミング精度テスト：** `AudioSettings.dspTime`の正確性
- **Z軸移動テスト：** ノーツの奥行き移動計算
- **判定精度テスト：** Perfect/Great/Good判定のタイミングウィンドウ
- **レーン配置テスト：** 4レーンのX座標配置（-3, -1, 1, 3）
- **3D空間判定テスト：** Z座標による判定ライン検出

#### 4. テスト命名規則

```
[テスト対象]_[テスト条件]_[期待結果]

例：
- BPMToSeconds_ConvertsBPMCorrectly
- NoteMovement_MovesForwardOverTime
- TimingWindow_PerfectJudgment
```

#### 5. テストデータ管理

```csharp
// テスト用のScriptableObjectを作成
[CreateAssetMenu(fileName = "TestChartData", menuName = "Tests/ChartData")]
public class TestChartData : ScriptableObject
{
    public List<NoteData> notes;
    public float bpm = 120f;
}
```

#### 6. 非同期テストの扱い

```csharp
[UnityTest]
public IEnumerator AudioSync_MaintainsConsistentTiming()
{
    float startTime = AudioSettings.dspTime;
    
    yield return new WaitForSeconds(1f);
    
    float elapsed = (float)(AudioSettings.dspTime - startTime);
    Assert.AreEqual(1f, elapsed, 0.1f);
}
```

#### 7. テストのパフォーマンス基準

- EditModeテスト: 各テスト0.1秒以内で完了
- PlayModeテスト: 各テスト5秒以内で完了
- 全テストスイート: 1分以内で全テスト完了

#### 8. CI/CD統合

プロジェクトのテストは以下のタイミングで実行されるべきです：
- コミット前（ローカル）
- プルリクエスト作成時
- メインブランチへのマージ時

### テスト実行時の注意事項

1. **Test Runnerウィンドウ：** Unityエディタの `Window > General > Test Runner` から実行
2. **結果の確認：** Test Runnerウィンドウとコンソールでテスト結果を確認
3. **エラー時の対処：** Consoleウィンドウでエラーの詳細を確認
4. **メタファイル：** テストファイルには必ず.metaファイルを付与

### テスト駆動開発（TDD）の推奨

新機能実装時は以下の順序で開発することを推奨：

1. テストを先に書く（Red）
2. テストが通る最小限の実装（Green）
3. コードをリファクタリング（Refactor）

```csharp
// 1. まずテストを書く
[Test]
public void ScoreCalculation_PerfectTiming_Returns1000Points()
{
    var scorer = new ScoreCalculator();
    int score = scorer.Calculate(JudgmentType.Perfect);
    Assert.AreEqual(1000, score);
}

// 2. 実装する
public class ScoreCalculator
{
    public int Calculate(JudgmentType judgment)
    {
        return judgment == JudgmentType.Perfect ? 1000 : 0;
    }
}
```

## AI開発のための注意事項

### 奥行き型リズムゲーム特有の要件

1. **3D座標系とZ軸移動：** 
   - Z軸: 奥（正の値）から手前（0）への移動軸
   - X軸: 左右のレーン位置（-3, -1, 1, 3）
   - Y軸: 高さ（基本的に固定、0.5f程度）
   - ノーツは画面奥（Z=20）から判定ライン（Z=0）に向かって移動

2. **タイミング精度とZ軸計算：** 
   - 音楽同期には常に`AudioSettings.dspTime`を使用し、`Time.time`や`Time.deltaTime`は絶対に使用しないこと
   - Z軸位置計算: `spawnZ + (beatsPassed * noteSpeed)`
   - 判定は3D空間のZ座標距離で行う

3. **3Dビジュアル要件：**
   - 透視投影カメラによる奥行き表現
   - 距離に応じた動的スケーリング（奥で小さく、手前で大きく）
   - 台形状レーン表示（奥が狭く、手前が広い）
   - カメラ設定: Position(0, 5, -5), Rotation(30, 0, 0)

4. **ScriptableObjects：** 楽曲データ、チャートデータ、設定にはScriptableObjectsを使用 - これによりデザイナーフレンドリーな編集が可能になる

5. **3Dプレハブシステム：** 
   - ノーツは3D空間での移動とスケーリングに対応したプレハブベースにすべき
   - Tap/Holdノーツの視覚的差別化
   - 3D Colliderによるトリガー判定

6. **入力システム：** 
   - 4レーン専用入力: D、F、J、Kキー
   - 3D空間での判定ゾーン設定
   - Holdノーツの継続入力判定

7. **3D URP機能：** 
   - URPで利用可能な3Dライティング、カスタムシェーダー、ポストプロセッシングを活用
   - 奥行き表現を強調するライティング設定
   - パーティクルエフェクトによる3D視覚効果

8. **パフォーマンス最適化：** 
   - WindowsPC環境での快適な動作を最優先
   - 3D計算の最適化
   - 不要なノーツオブジェクトの適切な破棄

9. **日本語ドキュメント：** `docs/plans/3d-rhythm-game-guide.md`には奥行き型リズムゲームの詳細な実装手順が含まれています - 3D空間での実装ガイダンスについてはこちらを参照してください

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