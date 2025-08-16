# NoteController 実装計画書

## 概要

本ドキュメントは、奥行き型リズムゲーム「Jirou」のNoteControllerコンポーネントの実装計画を定義します。既存実装を拡張し、要求仕様に基づいた機能追加を段階的に実施します。

## 現状分析

### 既存実装の確認

現在のNoteController.csには以下の機能が実装済み：
- 基本的な初期化処理（Initialize）
- Z座標の更新処理（Update）
- ヒット処理（OnHit）
- リセット処理（Reset）
- 完了状態の管理（IsCompleted）

### 不足している機能

要求仕様に対して以下の機能が不足：
1. 距離に応じた動的スケーリング
2. タイミング判定システム（CheckHitTiming）
3. Holdノーツ用のTrail表示
4. ミス処理の詳細実装
5. プライベートフィールドの追加

## 実装フェーズ

### フェーズ1: 基本構造の拡張（即座に実装）

#### 1.1 フィールドの追加
```csharp
// パブリックフィールド
public float moveSpeed = 10.0f;

// プライベートフィールド  
private float targetBeat;
private bool hasBeenHit;
private Vector3 initialScale;
private MeshRenderer renderer;
private TrailRenderer trailRenderer;
```

#### 1.2 判定タイプの定義
新規ファイル: `JirouUnity/Assets/_Jirou/Scripts/Core/JudgmentType.cs`
```csharp
namespace Jirou.Core
{
    public enum JudgmentType
    {
        Perfect,
        Great,
        Good,
        Miss
    }
}
```

### フェーズ2: コア機能の実装（即座に実装）

#### 2.1 Start メソッドの実装
- targetBeatの設定
- initialScaleの記録
- コンポーネントのキャッシュ
- Holdノーツ処理の初期化

#### 2.2 Update メソッドの改良
- 既存のZ座標更新処理を維持
- 距離スケーリング処理の追加
- ミス判定処理の改良

#### 2.3 CheckHitTiming メソッドの新規実装
- Z座標に基づく判定距離の計算
- 4段階判定の返却

### フェーズ3: 視覚効果の実装（即座に実装）

#### 3.1 ApplyDistanceScaling メソッド
- 距離比率の計算
- スケール係数の適用
- NoteDataのVisualScale考慮

#### 3.2 SetupHoldTrail メソッド
- TrailRendererの設定
- Holdの長さに応じた時間設定

### フェーズ4: 統合とテスト（後続タスク）

#### 4.1 他システムとの連携準備
- ScoreManagerとの連携準備（コメントアウトで実装）
- EffectManagerとの連携準備（コメントアウトで実装）

#### 4.2 テスト実装
- EditModeテストの作成
- PlayModeテストの作成

## 実装手順

### ステップ1: JudgmentType.csの作成
1. Core名前空間にJudgmentType列挙型を定義
2. 4つの判定レベルを定義

### ステップ2: NoteController.csの更新
1. 必要なフィールドの追加
2. Startメソッドの実装
3. Updateメソッドの改良
4. CheckHitTimingメソッドの追加
5. OnHitメソッドの改良
6. プライベートメソッドの追加

### ステップ3: 動作確認
1. Unityエディタでの基本動作確認
2. 各判定タイミングの検証
3. スケーリング効果の確認

## コード変更詳細

### 変更対象ファイル
1. `JirouUnity/Assets/_Jirou/Scripts/Gameplay/NoteController.cs` - 既存ファイルの更新
2. `JirouUnity/Assets/_Jirou/Scripts/Core/JudgmentType.cs` - 新規ファイルの作成

### 主要な変更点

#### NoteController.cs の変更内容

**追加するフィールド:**
```csharp
// パブリック
public float moveSpeed = 10.0f;

// プライベート
private float targetBeat;
private bool hasBeenHit;
private Vector3 initialScale;
private MeshRenderer renderer;
private TrailRenderer trailRenderer;
```

**更新するメソッド:**
- `Start()` - 初期化処理の追加
- `Update()` - スケーリング処理の追加
- `OnHit()` - 判定処理の追加

**新規追加メソッド:**
- `CheckHitTiming()` - タイミング判定
- `ApplyDistanceScaling()` - 距離スケーリング
- `SetupHoldTrail()` - Hold Trail設定
- `OnMiss()` - ミス処理

## 依存関係

### 必須依存
- Conductor（既存）
- NoteData（既存）

### オプション依存（将来実装）
- ScoreManager
- EffectManager

## リスクと対策

### リスク1: パフォーマンスの低下
**対策:** 
- Update内の計算を最小限に抑える
- コンポーネントのキャッシュを徹底

### リスク2: 既存機能への影響
**対策:**
- 既存の基本動作を維持
- 段階的な機能追加

### リスク3: 判定精度の問題
**対策:**
- 判定距離の値を調整可能にする
- テスト環境での検証を重視

## テスト計画

### 単体テスト項目
1. 初期化処理の正常動作
2. Z座標計算の精度
3. スケーリング計算の妥当性
4. 判定タイミングの精度
5. リセット処理の完全性

### 統合テスト項目
1. Conductorとの連携
2. NoteSpawnerからの生成
3. 実機での判定精度
4. Holdノーツの表示

## スケジュール

### 即座に実装（現在のタスク）
- JudgmentType.csの作成
- NoteController.csの更新
- 基本動作の確認

### 後続タスク
- テストコードの作成
- ScoreManagerとの統合
- EffectManagerとの統合
- パフォーマンス最適化

## 成功基準

1. ノーツが奥から手前に正しく移動する
2. 距離に応じてスケールが変化する
3. CheckHitTimingが正確な判定を返す
4. Holdノーツでtrailが表示される
5. 既存機能が正常に動作し続ける

## まとめ

本実装計画に従って、NoteControllerを段階的に拡張します。まず基本機能を実装し、動作確認後に他システムとの統合を進めます。各フェーズで動作確認を行い、安定性を保ちながら機能追加を進めることが重要です。