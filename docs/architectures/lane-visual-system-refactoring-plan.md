# レーンビジュアルシステム リファクタリング計画

## 現在の問題点

### 1. 座標系の不一致
- LaneVisualizerとLaneInputFeedbackAdvancedが別々のGameObjectとして存在
- 誤った位置設定により表示がずれる可能性がある
- 手動での位置調整が必要

### 2. 重複したレーン計算ロジック
- 両コンポーネントが独立してレーン境界を計算
- 同じConductorの値を参照しているが、計算が分離している
- メンテナンスが困難

### 3. 責任の分離が不明確
- LaneVisualizer: レーンの境界線表示
- LaneInputFeedbackAdvanced: キー入力時の視覚効果
- 両者が密接に関連しているが独立して動作

## 提案する解決策

### 案1: コンポーネントの統合（推奨）

#### 新しいコンポーネント: `LaneVisualizationSystem`

```csharp
namespace Jirou.Visual
{
    /// <summary>
    /// レーンの表示と入力フィードバックを統合管理するシステム
    /// </summary>
    public class LaneVisualizationSystem : MonoBehaviour
    {
        // レーン表示機能
        private void CreateLaneBoundaries() { }
        
        // 入力フィードバック機能
        private void CreateFeedbackMeshes() { }
        
        // 共通のレーン計算ロジック
        private void CalculateLaneBounds(int laneIndex, out float left, out float right) { }
    }
}
```

**メリット:**
- 座標系の問題が発生しない
- レーン計算ロジックの一元化
- メンテナンスが容易

**デメリット:**
- クラスが大きくなる可能性
- 機能の切り替えが複雑になる可能性

### 案2: 親子関係の明確化

```
Stage (GameObject)
└── LaneVisualizationContainer (位置: 0,0,0)
    ├── LaneVisualizer (位置: 0,0,0)
    └── LaneInputFeedback (位置: 0,0,0)
```

**実装方法:**
1. 専用のコンテナオブジェクトを作成
2. 両コンポーネントを子要素として配置
3. 初期化時に相互参照を設定

```csharp
// LaneInputFeedbackAdvanced.cs
void Start()
{
    // 同じ親から LaneVisualizer を取得
    laneVisualizer = transform.parent.GetComponentInChildren<LaneVisualizer>();
    
    // LaneVisualizerから直接レーン情報を取得
    if (laneVisualizer != null)
    {
        SyncWithLaneVisualizer();
    }
}

private void SyncWithLaneVisualizer()
{
    // LaneVisualizerのLineRendererから位置情報を取得
    // これにより完全に同期された表示が可能
}
```

### 案3: 共通基底クラスの作成

```csharp
namespace Jirou.Visual
{
    /// <summary>
    /// レーン計算の共通ロジックを提供する基底クラス
    /// </summary>
    public abstract class LaneVisualizationBase : MonoBehaviour
    {
        protected Conductor conductor;
        
        /// <summary>
        /// レーンの境界を計算
        /// </summary>
        protected void GetLaneBounds(int laneIndex, out float left, out float right)
        {
            float[] lanePositions = conductor.LaneXPositions;
            
            if (laneIndex == 0)
            {
                left = lanePositions[0] - conductor.LaneWidth;
                right = (lanePositions[0] + lanePositions[1]) / 2f;
            }
            else if (laneIndex == lanePositions.Length - 1)
            {
                left = (lanePositions[laneIndex - 1] + lanePositions[laneIndex]) / 2f;
                right = lanePositions[laneIndex] + conductor.LaneWidth;
            }
            else
            {
                left = (lanePositions[laneIndex - 1] + lanePositions[laneIndex]) / 2f;
                right = (lanePositions[laneIndex] + lanePositions[laneIndex + 1]) / 2f;
            }
        }
        
        /// <summary>
        /// 遠近感を適用した座標を取得
        /// </summary>
        protected Vector3 GetPerspectivePosition(float x, float z)
        {
            float t = Mathf.Clamp01(z / conductor.SpawnZ);
            float scale = Mathf.Lerp(conductor.PerspectiveNearScale, conductor.PerspectiveFarScale, t);
            return new Vector3(x * scale, 0, z);
        }
    }
}
```

## 実装優先順位

### フェーズ1: 即座の修正（完了）
- [x] GameSceneでLaneVisualizerの位置を(0,0,0)に修正

### フェーズ2: 短期的改善
- [ ] LaneInputFeedbackAdvancedにLaneVisualizerとの同期機能を追加
- [ ] 両コンポーネントの初期化順序を保証

### フェーズ3: 長期的リファクタリング
- [ ] 統合コンポーネントの設計と実装
- [ ] 既存コンポーネントの段階的移行
- [ ] テストの追加

## テスト項目

### 位置の整合性
- [ ] 各レーンの境界線と入力フィードバック領域が一致
- [ ] すべての解像度で正しく表示
- [ ] カメラ角度を変更しても整合性を維持

### パフォーマンス
- [ ] フレームレートへの影響を測定
- [ ] メモリ使用量の確認
- [ ] 描画呼び出し回数の最適化

## まとめ

現在の問題は**位置設定のミス**という単純なものでしたが、これは設計上の問題を示しています。将来的には、レーン表示システムを統合し、より堅牢で保守しやすいアーキテクチャに移行することを推奨します。