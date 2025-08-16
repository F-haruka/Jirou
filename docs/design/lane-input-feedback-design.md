# レーン入力フィードバックシステム 詳細設計書

## 1. 概要

### 1.1 目的
レーン入力フィードバックシステムは、プレイヤーがキー（D、F、J、K）を押した際に、対応するレーンが視覚的に光ることで、どのレーンのキーが押されたかを明確に示すビジュアルフィードバック機能を提供します。

### 1.2 システムの位置づけ
- **LaneVisualizer**: レーンの境界線と位置情報を提供
- **InputManager**: キー入力イベントを検出・通知
- **LaneInputFeedback**: 上記2つを連携させ、視覚的フィードバックを実現（新規実装）

## 2. 技術設計

### 2.1 システム構成

```
┌─────────────────────┐
│   InputManager      │
│  (キー入力検出)      │
└──────────┬──────────┘
           │ イベント通知
           ↓
┌─────────────────────┐     ┌─────────────────────┐
│ LaneInputFeedback   │←───→│   LaneVisualizer    │
│ (フィードバック制御) │     │  (レーン位置情報)    │
└──────────┬──────────┘     └─────────────────────┘
           │
           ↓
┌─────────────────────┐
│  Lane Feedback      │
│   Cube Objects      │
│  (視覚的表現)       │
└─────────────────────┘
```

### 2.2 コンポーネント設計

#### 2.2.1 LaneInputFeedback クラス
**責務**: レーン入力フィードバックシステムの中核制御

```csharp
namespace Jirou.Visual
{
    public class LaneInputFeedback : MonoBehaviour
    {
        // 設定
        [Header("Feedback Settings")]
        [SerializeField] private float feedbackIntensity = 2.0f;
        [SerializeField] private float feedbackDuration = 0.1f;
        [SerializeField] private AnimationCurve feedbackCurve;
        
        // レーン毎のフィードバックオブジェクト
        private GameObject[] laneFeedbackObjects;
        private Material[] laneMaterials;
        private Coroutine[] activeCoroutines;
        
        // 依存コンポーネント
        private LaneVisualizer laneVisualizer;
        private InputManager inputManager;
        private Conductor conductor;
    }
}
```

### 2.3 フィードバックCubeの仕様

#### 2.3.1 位置とサイズ
- **配置**: 各レーンの中央、判定ライン（Z=0）から奥（SpawnZ）まで
- **幅**: レーン幅に合わせて動的調整（遠近感対応）
- **高さ**: Y=0 から Y=0.5 程度の薄い板状
- **奥行き**: レーン全長をカバー

#### 2.3.2 視覚効果
- **通常時**: 半透明または非表示
- **キー押下時**: Emissionを使用した発光効果
- **アニメーション**: フェードイン・フェードアウト

### 2.4 レーン位置の計算

```csharp
// Conductorの遠近感設定を利用した位置計算
private void CalculateLanePositions()
{
    for (int i = 0; i < LANE_COUNT; i++)
    {
        // レーン中心のX座標を取得
        float laneX = conductor.LaneXPositions[i];
        
        // 手前（判定ライン）での幅
        float nearX = laneX * conductor.PerspectiveNearScale;
        float nearWidth = conductor.LaneWidth * conductor.PerspectiveNearScale;
        
        // 奥（スポーン地点）での幅
        float farX = laneX * conductor.PerspectiveFarScale;
        float farWidth = conductor.LaneWidth * conductor.PerspectiveFarScale;
        
        // Cubeの配置とスケール調整
        ConfigureFeedbackCube(i, nearX, nearWidth, farX, farWidth);
    }
}
```

## 3. Material設計

### 3.1 基本Material設定
- **Shader**: Universal Render Pipeline/Lit または Unlit
- **Rendering Mode**: Transparent
- **Color**: レーン毎に異なる色（設定可能）
  - Lane 0 (D): 赤系
  - Lane 1 (F): 黄系
  - Lane 2 (J): 緑系
  - Lane 3 (K): 青系

### 3.2 発光効果の実装

```csharp
// Emission制御によるフィードバック
private IEnumerator AnimateFeedback(int laneIndex)
{
    Material mat = laneMaterials[laneIndex];
    float elapsed = 0f;
    
    // Emissionを有効化
    mat.EnableKeyword("_EMISSION");
    
    while (elapsed < feedbackDuration)
    {
        float t = elapsed / feedbackDuration;
        float intensity = feedbackCurve.Evaluate(t) * feedbackIntensity;
        
        // HDR色で発光強度を制御
        Color emissionColor = baseColors[laneIndex] * intensity;
        mat.SetColor("_EmissionColor", emissionColor);
        
        elapsed += Time.deltaTime;
        yield return null;
    }
    
    // Emissionを無効化
    mat.DisableKeyword("_EMISSION");
    mat.SetColor("_EmissionColor", Color.black);
}
```

## 4. 入力イベントとの連携

### 4.1 イベント購読方式

```csharp
private void OnEnable()
{
    // 方式1: InputManagerの静的イベントを購読
    InputManager.OnNoteJudged += HandleNoteJudged;
    
    // 方式2: Update内で直接キー入力を監視（より即座な反応）
    // Update()内でInput.GetKeyDown()を使用
}

private void OnDisable()
{
    InputManager.OnNoteJudged -= HandleNoteJudged;
}
```

### 4.2 キー入力の直接監視

```csharp
private void Update()
{
    for (int i = 0; i < LANE_COUNT; i++)
    {
        // キー押下検出
        if (Input.GetKeyDown(laneKeys[i]))
        {
            TriggerFeedback(i);
        }
        
        // キー長押し対応（オプション）
        if (Input.GetKey(laneKeys[i]))
        {
            UpdateHoldFeedback(i);
        }
        
        // キー離し検出
        if (Input.GetKeyUp(laneKeys[i]))
        {
            StopFeedback(i);
        }
    }
}
```

## 5. 最適化考慮事項

### 5.1 パフォーマンス最適化
- **Material共有**: 同じMaterialのインスタンスは共有
- **コルーチン管理**: 同一レーンの複数フィードバックは適切に制御
- **オブジェクトプーリング**: エフェクトオブジェクトの再利用

### 5.2 視覚的最適化
- **Z-Fighting対策**: レーン表示とわずかにY座標をオフセット
- **透明度ソート**: RenderQueueの適切な設定
- **カリング設定**: 不要な面の描画を省略

## 6. 拡張性

### 6.1 カスタマイズ可能な要素
- フィードバック色（レーン毎）
- 発光強度とアニメーションカーブ
- フィードバック持続時間
- 判定タイプ別の異なるフィードバック

### 6.2 将来の拡張案
- パーティクルエフェクトの追加
- 判定精度に応じた色変化
- コンボ数に応じた演出強化
- カスタムシェーダーによる特殊効果

## 7. 依存関係

### 7.1 必須コンポーネント
- **Conductor**: レーン位置と遠近感設定
- **LaneVisualizer**: レーン境界情報
- **InputManager**: キー入力イベント（オプション）

### 7.2 Unity設定要件
- Universal Render Pipeline (URP)
- HDR設定（Emission効果用）
- Linear Color Space推奨

## 8. エラーハンドリング

### 8.1 初期化時の検証
```csharp
private bool ValidateComponents()
{
    // Conductor存在確認
    if (Conductor.Instance == null)
    {
        Debug.LogError("[LaneInputFeedback] Conductor not found!");
        return false;
    }
    
    // LaneVisualizer確認
    if (laneVisualizer == null)
    {
        laneVisualizer = GetComponent<LaneVisualizer>();
        if (laneVisualizer == null)
        {
            Debug.LogError("[LaneInputFeedback] LaneVisualizer not found!");
            return false;
        }
    }
    
    return true;
}
```

### 8.2 実行時エラー対策
- Null参照チェック
- 配列境界チェック
- コルーチン競合の防止

## 9. デバッグ機能

### 9.1 Inspector表示
- 各レーンのフィードバック状態
- 現在のEmission値
- アクティブなコルーチン数

### 9.2 Gizmos表示
- フィードバックCubeの配置プレビュー
- レーン境界線との整合性確認

## 10. 設定推奨値

| パラメータ | 推奨値 | 説明 |
|-----------|--------|------|
| feedbackIntensity | 2.0 | 発光強度（HDR） |
| feedbackDuration | 0.1秒 | フィードバック持続時間 |
| cubeHeight | 0.5 | Cubeの高さ |
| cubeAlpha | 0.1 | 通常時の透明度 |
| emissionColor | 各レーン色 × 2 | HDR発光色 |