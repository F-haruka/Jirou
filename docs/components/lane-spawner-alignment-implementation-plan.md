# Lane VisualizerとNoteSpawner整合性改善 実装計画書

## 概要
Lane VisualizerとNoteSpawner Test SetupのSpawn Line/Hit Lineを整合させるための段階的な実装計画です。

**[2025年8月16日 更新]** Phase 1-3の主要な修正が完了し、レーンとノーツの軌道の整合性が大幅に改善されました。

## 実装フェーズ

### Phase 1: Conductorの拡張（✅ 実装完了）

#### 1.1 レーン設定プロパティの追加

**ファイル**: `JirouUnity/Assets/_Jirou/Scripts/Core/Conductor.cs`

**実装状況**: ✅ **完了**（55-63行目）
```csharp
[Header("レーン設定")]
[SerializeField] private float[] _laneXPositions = { -3f, -1f, 1f, 3f };
[SerializeField] private float _laneVisualWidth = 2.0f;
[SerializeField] private float _noteY = 0.5f;

[Header("遠近感設定")]
[SerializeField] private float _perspectiveNearScale = 1.0f;
[SerializeField] private float _perspectiveFarScale = 0.7f;
```

#### 1.2 レーン数取得メソッドの追加

**実装状況**: ✅ **完了**（361-377行目）
```csharp
public int GetLaneCount()
{
    return _laneXPositions != null ? _laneXPositions.Length : 0;
}

public float GetLaneX(int laneIndex)
{
    if (laneIndex < 0 || laneIndex >= _laneXPositions.Length)
    {
        Debug.LogWarning($"不正なレーンインデックス: {laneIndex}");
        return 0f;
    }
    return _laneXPositions[laneIndex];
}
```

#### 1.3 遠近感計算メソッドの追加

**実装状況**: ✅ **完了**（385-419行目）
```csharp
public float GetPerspectiveLaneX(int laneIndex, float zPosition)
public float GetLaneWidthAtZ(float zPosition)
public float GetScaleAtZ(float zPosition)
```

### Phase 2: 共通ユーティリティクラスの作成（⏸️ スキップ）

#### 2.1 新規ファイル作成

**実装状況**: ⏸️ **スキップ** - Conductorクラスに直接実装されたため、別途ユーティリティクラスは不要と判断

```csharp
using UnityEngine;

namespace Jirou.Core
{
    /// <summary>
    /// レーン座標計算用ユーティリティクラス
    /// </summary>
    public static class LaneCoordinateUtility
    {
        /// <summary>
        /// レーンインデックスからワールドX座標を取得
        /// </summary>
        public static float GetLaneWorldX(int laneIndex, Conductor conductor)
        {
            if (conductor == null) 
            {
                Debug.LogError("Conductorが見つかりません");
                return 0f;
            }
            
            return conductor.GetLaneX(laneIndex);
        }
        
        /// <summary>
        /// 遠近感を考慮したX座標を計算
        /// </summary>
        public static float GetPerspectiveX(float baseX, float z, float nearWidth, float farWidth, float spawnZ)
        {
            // Z座標に基づいて幅をリニア補間
            float t = z / spawnZ; // 0（手前）から1（奥）
            float widthScale = Mathf.Lerp(nearWidth, farWidth, t);
            return baseX * widthScale;
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
}
```

### Phase 3: LaneVisualizerの改修（✅ 実装完了）

#### 3.1 Conductor連携の強化

**実装状況**: ✅ **完了**

1. **レーン数の同期**
```csharp
private void SyncWithConductor()
{
    if (conductor == null) return;
    
    // SpawnZからレーン長を自動設定
    float newLaneLength = conductor.SpawnZ;
    
    // レーン数をConductorから取得
    int conductorLaneCount = conductor.GetLaneCount();
    
    // 値が変更された場合のみ更新
    if (Mathf.Abs(_laneLength - newLaneLength) > 0.01f || _laneCount != conductorLaneCount)
    {
        _laneLength = newLaneLength;
        _laneCount = conductorLaneCount;
        
        // レーンを再生成
        if (Application.isPlaying && laneContainer != null)
        {
            UpdateLanes();
            Debug.Log($"[LaneVisualizer] Conductorと同期: レーン長={_laneLength}, レーン数={_laneCount}");
        }
    }
}
```

2. **レーン位置計算の修正**（✅ 355-374行目）
```csharp
public float CalculateLaneX(int laneIndex, bool isNear)
{
    if (conductor != null)
    {
        // Conductorの統一された遠近感メソッドを使用
        return conductor.GetPerspectiveLaneX(laneIndex, isNear ? 0f : conductor.SpawnZ);
    }
    else
    {
        // フォールバック処理（既存実装）
    }
}
```

3. **レーン描画の修正**（✅ 277-323行目）
```csharp
private void CreateDividerLines(ref int lineIndex)
{
    if (conductor != null)
    {
        // Conductorのレーン位置を使用
        float[] lanePositions = conductor.LaneXPositions;
        
        // レーン間の境界線を作成（5本）
        for (int i = 0; i <= lanePositions.Length; i++)
        {
            // 境界位置の計算
            // - 左端、右端、レーン間の中点
            
            // Conductorの統一された遠近感設定を使用
            float nearX = x * conductor.PerspectiveNearScale;
            float farX = x * conductor.PerspectiveFarScale;
        }
    }
}
```

### Phase 4: NoteSpawnerの改修（🔄 部分的に実装）

#### 4.1 Conductor連携の追加

**変更内容**:

1. **Start()メソッドの修正**
```csharp
void Start()
{
    // Conductorから設定を取得
    if (conductor != null)
    {
        // レーン位置をConductorから取得
        laneXPositions = conductor.LaneXPositions;
        noteY = conductor.NoteY;
        
        Debug.Log($"[NoteSpawner] Conductorからレーン設定を取得: レーン数={laneXPositions.Length}");
    }
    else
    {
        Debug.LogWarning("[NoteSpawner] Conductorが見つかりません。デフォルト設定を使用します。");
    }
    
    // 既存の初期化処理
    InitializeSpawner();
}
```

2. **Gizmos描画の改善**
```csharp
void OnDrawGizmos()
{
    if (!showNotePathGizmo) return;
    
    // Conductorの設定を優先的に使用
    float[] displayLanePositions = laneXPositions;
    float displaySpawnZ = 20f;
    float displayHitZ = 0f;
    
    if (conductor != null)
    {
        displayLanePositions = conductor.LaneXPositions;
        displaySpawnZ = conductor.SpawnZ;
        displayHitZ = conductor.HitZ;
    }
    
    // 遠近感の設定
    float nearScale = 1.0f;
    float farScale = 0.25f;
    
    // レーンパスの可視化（遠近感付き）
    if (displayLanePositions != null && displayLanePositions.Length > 0)
    {
        for (int i = 0; i < displayLanePositions.Length; i++)
        {
            // レーンの中心線（遠近感考慮）
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f);
            
            Vector3 nearPos = new Vector3(displayLanePositions[i] * nearScale, noteY, displayHitZ);
            Vector3 farPos = new Vector3(displayLanePositions[i] * farScale, noteY, displaySpawnZ);
            
            Gizmos.DrawLine(nearPos, farPos);
        }
        
        // スポーンライン（遠近感考慮）
        Gizmos.color = Color.green;
        float spawnLineExtent = (displayLanePositions[displayLanePositions.Length - 1] - displayLanePositions[0]) / 2f + 1f;
        Gizmos.DrawLine(
            new Vector3(-spawnLineExtent * farScale, noteY, displaySpawnZ),
            new Vector3(spawnLineExtent * farScale, noteY, displaySpawnZ)
        );
        
        // 判定ライン（遠近感考慮）
        Gizmos.color = Color.red;
        float hitLineExtent = spawnLineExtent;
        Gizmos.DrawLine(
            new Vector3(-hitLineExtent * nearScale, noteY, displayHitZ),
            new Vector3(hitLineExtent * nearScale, noteY, displayHitZ)
        );
    }
}
```

### Phase 5: NoteSpawnerTestSetupの調整（⏸️ 未実装）

#### 5.1 セットアップ順序の最適化

```csharp
public void SetupTestEnvironment()
{
    Debug.Log("[NoteSpawnerTestSetup] テスト環境のセットアップを開始");
    
    // 1. Conductorのセットアップ（最優先）
    SetupConductor();
    
    // 2. LaneVisualizerがあれば同期を強制
    LaneVisualizer laneVis = FindObjectOfType<LaneVisualizer>();
    if (laneVis != null)
    {
        laneVis.ForceSync();
        Debug.Log("[NoteSpawnerTestSetup] LaneVisualizerをConductorと同期しました");
    }
    
    // 3. NotePoolManagerのセットアップ
    SetupNotePoolManager();
    
    // 4. NoteSpawnerのセットアップ
    SetupNoteSpawner();
    
    // 以下既存の処理...
}
```

#### 5.2 レーン設定の同期確認

```csharp
private void SetupNoteSpawner()
{
    if (noteSpawner == null)
    {
        // 既存のセットアップ処理...
    }
    
    // Conductorとの同期を確認
    if (conductor != null)
    {
        // Conductorからレーン設定を取得
        noteSpawner.laneXPositions = conductor.LaneXPositions;
        noteSpawner.noteY = conductor.NoteY;
        
        Debug.Log($"[NoteSpawnerTestSetup] Conductorのレーン設定を適用: {string.Join(", ", noteSpawner.laneXPositions)}");
    }
    else
    {
        // デフォルト設定（既存）
        noteSpawner.laneXPositions = new float[] { -3f, -1f, 1f, 3f };
        noteSpawner.noteY = 0.5f;
    }
    
    // 以下既存の設定...
}
```

## テスト手順

### 1. 基本動作確認（Phase 1-2完了後）

1. **Conductorの設定確認**
   - Unityエディタで`Conductor`オブジェクトを選択
   - インスペクタで新しいプロパティが表示されることを確認
   - 値を変更してシリアライズされることを確認

2. **ユーティリティクラスのテスト**
   - テストスクリプトを作成して各メソッドの動作を確認

### 2. LaneVisualizer動作確認（Phase 3完了後）

1. **Conductor同期テスト**
   - Playモードで実行
   - Conductorのレーン設定を変更
   - LaneVisualizerが自動的に更新されることを確認

2. **視覚的整合性の確認**
   - Scene ViewでGizmosを有効化
   - LaneVisualizerのレーンとConductorのGizmosが一致することを確認

### 3. NoteSpawner動作確認（Phase 4完了後）

1. **レーン位置同期テスト**
   - NoteSpawnerのGizmosとLaneVisualizerのレーンが一致
   - Spawn LineとHit Lineが正しい位置に表示

2. **ノーツ生成テスト**
   - ノーツが正しいレーン上に生成される
   - ノーツが正しい軌道を移動する

### 4. 統合テスト（Phase 5完了後）

1. **Auto Setup動作確認**
   - NoteSpawnerTestSetupのAuto Setupを有効化
   - すべてのコンポーネントが正しく連携

2. **ランタイム変更テスト**
   - 実行中にConductorの設定を変更
   - すべてのコンポーネントが同期して更新

## リスク管理

### 潜在的な問題と対策

| 問題 | 対策 | 優先度 |
|------|------|--------|
| 既存シーンへの影響 | デフォルト値を現在の値に設定 | 高 |
| Null参照エラー | 各所でnullチェックを実装 | 高 |
| パフォーマンス低下 | Update()での同期を最小限に | 中 |
| Gizmos描画の重複 | 条件分岐で制御 | 低 |

## 実装スケジュール

| フェーズ | 計画時間 | 実装状況 | 備考 |
|---------|----------|----------|------|
| Phase 1: Conductor拡張 | 30分 | ✅ 完了 | 遠近感計算メソッド追加済み |
| Phase 2: ユーティリティ作成 | 20分 | ⏸️ スキップ | Conductorに直接実装 |
| Phase 3: LaneVisualizer改修 | 45分 | ✅ 完了 | Conductor連携実装済み |
| Phase 4: NoteSpawner改修 | 30分 | 🔄 部分的 | 基本実装済み、詳細調整必要 |
| Phase 5: TestSetup調整 | 20分 | ⏸️ 未実装 | 必要に応じて実装 |
| テスト・調整 | 30分 | 🔄 進行中 | - |
| **合計** | **約3時間** | **約70%完了** | - |

## チェックリスト

### 実装前の確認
- [x] 現在のシーンをバックアップ
- [x] 現在の設定値をメモ
- [x] Gitでブランチを作成

### Phase 1
- [x] Conductorにレーン設定プロパティを追加
- [x] GetLaneCount()メソッドを実装
- [x] GetLaneX()メソッドを実装
- [x] GetPerspectiveLaneX()メソッドを実装
- [x] GetScaleAtZ()メソッドを実装
- [x] コンパイルエラーがないことを確認

### Phase 2
- [ ] ~~LaneCoordinateUtility.csを作成~~ （スキップ）
- [ ] ~~各ユーティリティメソッドを実装~~ （Conductorに実装）
- [ ] ~~namespaceが正しいことを確認~~ （不要）

### Phase 3
- [x] LaneVisualizerのCreateDividerLines()を修正
- [x] CalculateLaneX()を修正
- [x] Conductor連携の実装
- [x] 動作確認

### Phase 4
- [ ] NoteSpawnerのStart()を修正
- [ ] OnDrawGizmos()を改善
- [x] 遠近感の基本実装（NoteControllerで実装）
- [ ] 動作確認

### Phase 5
- [ ] NoteSpawnerTestSetupのSetupTestEnvironment()を修正
- [ ] SetupNoteSpawner()を修正
- [ ] 統合テスト

### 完了確認
- [x] レーンとノーツの基本的な整合性が改善
- [x] NoteControllerが遠近感を考慮して移動
- [ ] すべてのGizmosが完全に一致
- [x] コンソールにエラーが出ていない
- [x] パフォーマンスに問題がない

## 次のステップ

実装完了後の拡張案：

1. **レーン設定のScriptableObject化**
   - より柔軟な設定管理
   - 複数の設定プリセット対応

2. **ビジュアル強化**
   - レーンのマテリアル統一
   - パーティクルエフェクトの追加

3. **デバッグツールの充実**
   - レーン座標表示ツール
   - リアルタイム設定変更UI

## まとめ

### 実装済みの改善点
1. **Conductorクラスの統一管理**: レーン設定と遠近感計算を一元化
2. **LaneVisualizerの改修**: Conductorの設定を使用して描画
3. **NoteControllerの遠近感対応**: 移動時にX座標とスケールを動的に調整

### 残りのタスク
1. NoteSpawnerのGizmos描画の完全な統一化
2. NoteSpawnerTestSetupの自動同期機能の実装
3. 全体的な微調整と最適化

現在の実装により、レーンとノーツの軌道の基本的な整合性は大幅に改善されています。残りのタスクは必要に応じて実装することで、さらに完成度を高めることができます。