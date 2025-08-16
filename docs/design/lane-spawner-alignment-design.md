# Lane VisualizerとNoteSpawner Spawn/Hit Line整合性改善設計書

## 1. 現状分析

### 1.1 現在の実装状況

#### LaneVisualizer
- **座標系**: `transform.position`を基準点として使用
- **レーン描画範囲**: 
  - Near（手前）: Z = 0
  - Far（奥）: Z = `_laneLength`（デフォルト20.0f）
- **X座標計算**: 中央を0として左右対称にレーンを配置
- **Conductor同期**: `conductor.SpawnZ`の値を`_laneLength`に同期

#### NoteSpawner/Conductor
- **座標系**: ワールド座標を直接使用
- **ライン定義**:
  - Spawn Line: Z = `spawnZ`（デフォルト20.0f）
  - Hit Line: Z = `hitZ`（デフォルト0.0f）
- **X座標**: `laneXPositions`配列で定義（[-3, -1, 1, 3]）
- **Gizmos描画**: ワールド座標でSpawn LineとHit Lineを描画

### 1.2 問題点

1. **座標系の不一致**
   - LaneVisualizerは`transform.position`からの相対座標
   - NoteSpawnerはワールド座標を使用
   - LaneVisualizerが原点以外に配置されると位置がずれる

2. **レーン幅の不一致**
   - LaneVisualizer: `_laneWidth`（デフォルト2.0f）でレーン間隔を制御
   - NoteSpawner: 固定値`[-3, -1, 1, 3]`でレーン位置を定義
   - 実際のレーン位置が一致しない可能性

3. **視覚的な整合性の欠如**
   - LaneVisualizerの台形レーン（遠近感）
   - NoteSpawnerの直線的なGizmos
   - 視覚的に一致しない

## 2. 改善方針

### 2.1 設計原則

1. **単一責任の原則**
   - LaneVisualizer: レーンの視覚表現のみを担当
   - NoteSpawner: ノーツの生成と移動ロジックを担当
   - 両者は同じ座標系定義を共有

2. **設定の一元化**
   - レーン位置、Spawn/Hit位置はConductorで一元管理
   - 各コンポーネントはConductorの設定を参照

3. **座標系の統一**
   - すべてワールド座標で統一
   - 相対座標が必要な場合は明示的に変換

### 2.2 改善案

#### 案1: Conductorベースの座標管理（推奨）
**概要**: Conductorがすべての座標情報を管理し、他のコンポーネントが参照する

**メリット**:
- 設定の一元管理が可能
- 変更が容易
- 一貫性が保証される

**デメリット**:
- Conductorへの依存度が高まる

#### 案2: 共有設定ScriptableObject
**概要**: レーン設定を保持するScriptableObjectを作成

**メリット**:
- 設定の再利用が可能
- エディタ上での編集が容易

**デメリット**:
- 追加のアセット管理が必要

## 3. 詳細設計（案1: Conductorベース）

### 3.1 Conductorの拡張

```csharp
// Conductor.cs に追加するプロパティ
public class Conductor : MonoBehaviour
{
    // 既存のプロパティ...
    
    [Header("レーン設定")]
    [Tooltip("レーンのX座標配列")]
    [SerializeField] private float[] _laneXPositions = { -3f, -1f, 1f, 3f };
    
    [Tooltip("レーンの幅（視覚表現用）")]
    [SerializeField] private float _laneWidth = 1.8f;
    
    [Tooltip("ノーツのY座標")]
    [SerializeField] private float _noteY = 0.5f;
    
    // プロパティ
    public float[] LaneXPositions => _laneXPositions;
    public float LaneWidth => _laneWidth;
    public float NoteY => _noteY;
}
```

### 3.2 LaneVisualizerの改修

```csharp
public class LaneVisualizer : MonoBehaviour
{
    // 主な変更点：
    // 1. transform.positionを原点（0,0,0）に固定
    // 2. Conductorからレーン位置を取得
    // 3. ワールド座標で描画
    
    private void CreateLaneLines()
    {
        if (conductor == null) return;
        
        float[] lanePositions = conductor.LaneXPositions;
        float spawnZ = conductor.SpawnZ;
        float hitZ = conductor.HitZ;
        
        // 各レーンの描画
        for (int i = 0; i < lanePositions.Length; i++)
        {
            // レーン中心線をワールド座標で描画
            CreateLaneLine(
                lanePositions[i], 
                hitZ, 
                spawnZ
            );
        }
    }
}
```

### 3.3 NoteSpawnerの改修

```csharp
public class NoteSpawner : MonoBehaviour
{
    // 主な変更点：
    // 1. laneXPositionsをConductorから取得
    // 2. 初期化時にConductorと同期
    
    void Start()
    {
        if (conductor != null)
        {
            // Conductorからレーン位置を取得
            laneXPositions = conductor.LaneXPositions;
            noteY = conductor.NoteY;
        }
    }
}
```

### 3.4 座標変換の統一

```csharp
// 共通ユーティリティクラス
public static class LaneCoordinateUtility
{
    /// <summary>
    /// レーンインデックスからワールドX座標を取得
    /// </summary>
    public static float GetLaneWorldX(int laneIndex, Conductor conductor)
    {
        if (conductor == null || laneIndex < 0 || laneIndex >= conductor.LaneXPositions.Length)
            return 0f;
            
        return conductor.LaneXPositions[laneIndex];
    }
    
    /// <summary>
    /// ビート時間からワールドZ座標を計算
    /// </summary>
    public static float GetWorldZFromBeat(float beat, Conductor conductor)
    {
        if (conductor == null) return 0f;
        
        float noteSpeed = conductor.NoteSpeed;
        float spawnZ = conductor.SpawnZ;
        
        return spawnZ - (beat * noteSpeed);
    }
}
```

## 4. 視覚的整合性の改善

### 4.1 遠近感の統一

LaneVisualizerの台形表現をNoteSpawnerのGizmosにも適用：

```csharp
// NoteSpawner.OnDrawGizmos()の改善
void OnDrawGizmos()
{
    if (conductor == null) return;
    
    float nearWidth = 1.0f;  // 手前の幅倍率
    float farWidth = 0.3f;   // 奥の幅倍率
    
    // Spawn Lineを遠近感付きで描画
    float spawnLineWidth = farWidth;
    Gizmos.DrawLine(
        new Vector3(laneXPositions[0] * spawnLineWidth - 1f, noteY, spawnZ),
        new Vector3(laneXPositions[3] * spawnLineWidth + 1f, noteY, spawnZ)
    );
    
    // Hit Lineを遠近感付きで描画
    float hitLineWidth = nearWidth;
    Gizmos.DrawLine(
        new Vector3(laneXPositions[0] * hitLineWidth - 1f, noteY, hitZ),
        new Vector3(laneXPositions[3] * hitLineWidth + 1f, noteY, hitZ)
    );
}
```

### 4.2 デバッグ表示の統一

両コンポーネントで同じ色とスタイルを使用：
- Spawn Line: 緑色（Color.green）
- Hit Line: 赤色（Color.red）
- レーン線: 青系（Color.blue * 0.5f）

## 5. 移行計画

### Phase 1: Conductorの拡張
1. Conductorにレーン設定プロパティを追加
2. 既存の値をデフォルト値として設定

### Phase 2: LaneVisualizerの改修
1. Conductor参照の強化
2. ワールド座標での描画に変更
3. transform.positionを(0,0,0)に固定

### Phase 3: NoteSpawnerの改修
1. Conductorからレーン位置を取得
2. Gizmos描画の改善（遠近感追加）

### Phase 4: テストと調整
1. 各コンポーネントの動作確認
2. 視覚的な整合性の確認
3. パフォーマンスの確認

## 6. 考慮事項

### 6.1 後方互換性
- 既存のシーンやプレハブへの影響を最小限に
- デフォルト値で現在の動作を維持

### 6.2 パフォーマンス
- Gizmos描画は開発時のみ
- ランタイムでの座標計算を最適化

### 6.3 拡張性
- 将来的なレーン数変更に対応
- カスタムレーン配置に対応

## 7. テスト項目

1. **座標整合性テスト**
   - LaneVisualizerのレーンとNoteSpawnerのレーンが一致
   - Spawn LineとHit Lineが正しい位置に表示

2. **動的変更テスト**
   - ランタイムでのレーン数変更
   - Conductor設定変更時の同期

3. **視覚テスト**
   - 遠近感の一貫性
   - デバッグ表示の視認性

## 8. リスクと対策

| リスク | 影響度 | 対策 |
|--------|--------|------|
| 座標系変更による既存動作への影響 | 高 | 段階的な移行とテスト |
| Conductor依存度の増加 | 中 | インターフェース化を検討 |
| パフォーマンスへの影響 | 低 | プロファイリングで確認 |

## 9. まとめ

本設計では、LaneVisualizerとNoteSpawnerのSpawn/Hit Lineの整合性を改善するため、Conductorを中心とした座標管理システムを提案しました。これにより、一貫性のあるレーン表示とノーツ生成が可能になります。