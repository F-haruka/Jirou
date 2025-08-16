# InputManager 詳細設計書

## 1. 概要

### 1.1 目的
InputManagerは「Jirou」リズムゲームにおける4レーン入力システムの中核を担うコンポーネントです。キーボード入力（D、F、J、K）を検出し、適切なタイミングでノーツの判定を行い、ゲームプレイの基盤となる入力処理を管理します。

### 1.2 責務
- キーボード入力の検出と処理
- 各レーンの判定ゾーンとの連携
- ノーツのタイミング判定の実行
- Tap/Holdノーツの状態管理
- ヒットエフェクトの生成制御
- 判定結果のイベント通知

### 1.3 位置づけ
```
[Player Input]
      ↓
[InputManager] ← [Conductor]（タイミング情報）
      ↓
[JudgmentZone] → [NoteController]
      ↓
[ScoreManager] / [EffectSystem]
```

## 2. 詳細仕様

### 2.1 クラス構成

```csharp
namespace Jirou.Gameplay
{
    public class InputManager : MonoBehaviour
    {
        // Public Fields
        public KeyCode[] inputKeys;
        public JudgmentZone[] judgmentZones;
        public GameObject hitEffectPrefab;
        
        // Private Fields
        private bool[] holdStates;
        private NoteController[] heldNotes;
        
        // Events
        public static event System.Action<int, JudgmentType> OnNoteJudged;
        public static event System.Action<int, float> OnHoldProgress;
        
        // Methods
        private void Update();
        private void ProcessLaneInput(int laneIndex);
        private void HandleKeyDown(int laneIndex);
        private void HandleKeyHold(int laneIndex);
        private void HandleKeyUp(int laneIndex);
        private JudgmentType CalculateJudgment(float timingDifference);
        private void SpawnHitEffect(int laneIndex, JudgmentType judgment);
    }
}
```

### 2.2 フィールド仕様

#### Public フィールド

| フィールド名 | 型 | デフォルト値 | 説明 |
|------------|---|------------|------|
| inputKeys | KeyCode[] | {D, F, J, K} | 各レーンに対応するキー設定 |
| judgmentZones | JudgmentZone[] | (4要素) | 各レーンの判定ゾーンへの参照 |
| hitEffectPrefab | GameObject | null | ヒットエフェクトのプレハブ |

#### Private フィールド

| フィールド名 | 型 | 初期値 | 説明 |
|------------|---|-------|------|
| holdStates | bool[] | new bool[4] | 各レーンの長押し状態フラグ |
| heldNotes | NoteController[] | new NoteController[4] | 現在長押し中のノーツ参照 |

### 2.3 処理フロー

#### Update() メソッドの処理

```
Update()
├── foreach レーン (0-3)
│   └── ProcessLaneInput(laneIndex)
│       ├── if GetKeyDown
│       │   └── HandleKeyDown()
│       │       ├── GetClosestNote()
│       │       ├── CalculateJudgment()
│       │       ├── SpawnHitEffect()
│       │       └── if HoldNote → BeginHold()
│       ├── if GetKey && holdStates[i]
│       │   └── HandleKeyHold()
│       │       └── UpdateHoldProgress()
│       └── if GetKeyUp && holdStates[i]
│           └── HandleKeyUp()
│               └── EndHold()
```

### 2.4 判定タイミング仕様

| 判定タイプ | タイミング差（秒） | スコア倍率 |
|-----------|-----------------|-----------|
| Perfect | ±0.05 | 1.0x |
| Great | ±0.10 | 0.7x |
| Good | ±0.15 | 0.3x |
| Miss | >±0.15 or no input | 0x |

### 2.5 Holdノーツ処理仕様

#### 状態遷移
```
[待機] --KeyDown--> [Hold開始]
                         ↓
                    [Hold継続中]
                    ↓          ↓
             KeyUp正常    KeyUp早期
                    ↓          ↓
              [Hold成功]  [Hold失敗]
```

#### Hold判定基準
- 開始: KeyDownでノーツヒット時
- 継続: GetKeyがtrueの間
- 成功条件: 必要時間の95%以上保持
- 失敗条件: 95%未満で離す

## 3. 依存関係と連携

### 3.1 主要な依存コンポーネント

| コンポーネント | 関係性 | 用途 |
|--------------|--------|------|
| JudgmentZone | 参照 | ノーツ取得、判定範囲チェック |
| NoteController | 操作 | ノーツ状態変更、判定実行 |
| Conductor | 参照 | 現在の音楽時間取得 |
| ScoreManager | 通知 | スコア加算の通知 |

### 3.2 イベント通知

```csharp
// ノーツ判定時
OnNoteJudged?.Invoke(laneIndex, judgmentType);

// Hold進捗更新時（毎フレーム）
OnHoldProgress?.Invoke(laneIndex, progressRatio);
```

## 4. エッジケースと例外処理

### 4.1 考慮すべきエッジケース

1. **同時押し処理**
   - 複数レーンの同時入力を個別に処理
   - 各レーンは独立して判定

2. **複数ノーツの重なり**
   - 最も判定ラインに近いノーツを優先
   - Z座標の絶対値が最小のものを選択

3. **長押し中の他レーン入力**
   - 他レーンの入力は独立して処理
   - Hold状態は各レーンで管理

4. **フレームレート変動**
   - Time.deltaTimeではなくAudioSettings.dspTimeを使用
   - 音楽同期を最優先

### 4.2 エラーハンドリング

```csharp
// Null チェック例
if (judgmentZones[laneIndex] == null)
{
    Debug.LogWarning($"JudgmentZone for lane {laneIndex} is not set");
    return;
}

// 配列境界チェック
if (laneIndex < 0 || laneIndex >= inputKeys.Length)
{
    Debug.LogError($"Invalid lane index: {laneIndex}");
    return;
}
```

## 5. パフォーマンス最適化

### 5.1 最適化ポイント

1. **オブジェクトプール**
   - ヒットエフェクトのプール化（将来実装）
   - 頻繁な生成/破棄を回避

2. **キャッシュ戦略**
   - Conductor参照のキャッシュ
   - Transform参照の事前取得

3. **判定の効率化**
   - 判定範囲外のノーツは早期スキップ
   - LINQ使用を避け、forループで処理

### 5.2 メモリ管理

```csharp
// 固定配列サイズ
private const int LANE_COUNT = 4;
private readonly bool[] holdStates = new bool[LANE_COUNT];
private readonly NoteController[] heldNotes = new NoteController[LANE_COUNT];
```

## 6. 実装計画

### Phase 1: 基本実装（Week 1）
- [ ] InputManagerクラスの基本構造作成
- [ ] キー入力検出の実装
- [ ] JudgmentZoneとの基本連携

### Phase 2: Tap判定（Week 1）
- [ ] GetClosestNote()の実装
- [ ] タイミング判定ロジック
- [ ] 判定結果の通知システム

### Phase 3: Hold処理（Week 2）
- [ ] Hold状態管理の実装
- [ ] Hold継続判定
- [ ] Hold終了処理

### Phase 4: エフェクト（Week 2）
- [ ] ヒットエフェクト生成
- [ ] エフェクトプール（オプション）

### Phase 5: 統合テスト（Week 3）
- [ ] 他システムとの結合テスト
- [ ] パフォーマンステスト
- [ ] バグ修正と最適化

## 7. テスト計画

### 7.1 単体テスト

```csharp
[Test]
public void InputDetection_KeyDown_RegistersCorrectly()
{
    // D, F, J, K各キーの検出テスト
}

[Test]
public void JudgmentCalculation_WithinPerfectWindow_ReturnsPerfect()
{
    // タイミング判定の精度テスト
}

[Test]
public void HoldState_Management_TracksCorrectly()
{
    // Hold状態の遷移テスト
}
```

### 7.2 統合テスト

1. **JudgmentZone連携テスト**
   - ノーツ取得の正確性
   - 判定タイミングの同期

2. **ScoreManager連携テスト**
   - イベント通知の確認
   - スコア計算の正確性

3. **同時押しテスト**
   - 2レーン同時
   - 3レーン同時
   - 全レーン同時

### 7.3 パフォーマンステスト

- 目標: 60FPS維持
- 4レーン同時Hold時のCPU使用率
- メモリリーク検査

## 8. 設定可能パラメータ

### 8.1 Inspector設定項目

```csharp
[Header("Input Settings")]
[SerializeField] private KeyCode[] inputKeys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };

[Header("Judgment Settings")]
[SerializeField] private float perfectWindow = 0.05f;
[SerializeField] private float greatWindow = 0.10f;
[SerializeField] private float goodWindow = 0.15f;

[Header("Effects")]
[SerializeField] private GameObject hitEffectPrefab;
[SerializeField] private float effectDuration = 0.5f;
```

### 8.2 ScriptableObject設定（将来実装）

```csharp
[CreateAssetMenu(fileName = "InputSettings", menuName = "Jirou/Input Settings")]
public class InputSettings : ScriptableObject
{
    public KeyCode[] laneKeys;
    public JudgmentWindow[] judgmentWindows;
    public float holdRequiredRatio = 0.95f;
}
```

## 9. 今後の拡張可能性

### 9.1 追加機能候補

1. **カスタムキー設定**
   - ユーザー設定可能なキーバインド
   - 設定の保存/読み込み

2. **入力デバイス拡張**
   - ゲームパッド対応
   - タッチスクリーン対応（将来）

3. **高度な判定システム**
   - フリック入力
   - 同時押し専用ノーツ
   - スライドノーツ

4. **入力補助機能**
   - オートプレイ
   - アシストモード
   - 練習モード用スロー再生

### 9.2 アーキテクチャ改善案

1. **Input System Package統合**
   - Unity新入力システムへの移行
   - より柔軟な入力管理

2. **コマンドパターン**
   - 入力の記録/再生
   - リプレイ機能

3. **イベントドリブン強化**
   - より疎結合な設計
   - ReactiveExtensions導入検討

## 10. 参考資料

- Unity Input System Documentation
- 既存リズムゲームの入力処理分析
- docs/plans/3d-rhythm-game-guide.md