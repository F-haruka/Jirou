# レーン・ノーツ軌道不整合問題の調査と修正方法

## 問題の概要

~~LaneVisualizerによって描画される4つのレーンと、NoteSpawner/NoteControllerによって生成・制御されるノーツの軌道が一致していない問題が発生しています。この問題により、ノーツがレーンの中心を正確に流れず、ゲームプレイの視覚的整合性が損なわれています。~~

**[2025年8月16日 解決済み]** この問題は、Conductorクラスに統一された遠近感計算メソッドを実装し、全コンポーネントがこれを使用するように修正することで解決されました。

## 問題の根本原因

### 1. 座標系と遠近感計算の不一致

#### 現在の実装状況

| コンポーネント | 座標系 | 遠近感スケール | 問題点 |
|---|---|---|---|
| **LaneVisualizer** | 相対座標（transform基準） | nearWidth=2.0, farWidth=0.5 | 独自の遠近感計算ロジック |
| **NoteSpawner** | ワールド座標 | nearScale=1.0, farScale=0.25 | Gizmo描画で異なるスケール値 |
| **NoteController** | ワールド座標 | なし（X座標固定） | 遠近感を考慮しない移動 |
| **Conductor** | 基準値を提供 | なし | 統一された遠近感設定なし |

### 2. 具体的な問題箇所（解決済み）

#### LaneVisualizer（Visual/LaneVisualizer.cs）
```csharp
// 351-382行目: 独自の遠近感計算
public float CalculateLaneX(int laneIndex, bool isNear)
{
    float x = conductor.GetLaneX(laneIndex);
    if (!isNear)
    {
        x *= (farWidth / nearWidth);  // 独自のスケール計算
    }
    return x;
}
```

#### NoteSpawner（Gameplay/NoteSpawner.cs）
```csharp
// 562-563行目: 異なるスケール値
float nearScale = 1.0f;
float farScale = 0.25f;  // LaneVisualizerの値と異なる
```

### 3. 設定値の不整合

- **LaneVisualizer**: nearWidth/farWidth比率 = 2.0/0.5 = 4.0
- **NoteSpawner**: nearScale/farScale比率 = 1.0/0.25 = 4.0
- 比率は同じだが、実際の適用方法が異なるため視覚的な不一致が発生

## 実装された修正

### Phase 1: Conductorの拡張（✅ 実装完了）

**ファイル**: `Core/Conductor.cs`

```csharp
// 実装済み（55-71行目）
[Header("レーン設定")]
[SerializeField] private float[] _laneXPositions = { -3f, -1f, 1f, 3f };
[SerializeField] private float _laneVisualWidth = 2.0f;
[SerializeField] private float _noteY = 0.5f;

[Header("遠近感設定")]
[SerializeField] private float _perspectiveNearScale = 1.0f;
[SerializeField] private float _perspectiveFarScale = 0.7f;

public float PerspectiveNearScale => _perspectiveNearScale;
public float PerspectiveFarScale => _perspectiveFarScale;

// 実装済み（385-397行目）
public float GetPerspectiveLaneX(int laneIndex, float zPosition)
{
    if (laneIndex < 0 || laneIndex >= _laneXPositions.Length)
    {
        Debug.LogError($"Invalid lane index: {laneIndex}");
        return 0f;
    }
    
    float baseX = _laneXPositions[laneIndex];
    float t = Mathf.Clamp01(zPosition / _spawnZ);
    float scale = Mathf.Lerp(_perspectiveNearScale, _perspectiveFarScale, t);
    return baseX * scale;
}

// 実装済み（404-408行目）
public float GetLaneWidthAtZ(float zPosition)
{
    float t = Mathf.Clamp01(zPosition / _spawnZ);
    return _laneVisualWidth * Mathf.Lerp(_perspectiveNearScale, _perspectiveFarScale, t);
}

// 追加実装（415-419行目）
public float GetScaleAtZ(float zPosition)
{
    float t = Mathf.Clamp01(zPosition / _spawnZ);
    return Mathf.Lerp(_perspectiveNearScale, _perspectiveFarScale, t);
}
```

### Phase 2: LaneVisualizerの修正（✅ 実装完了）

**ファイル**: `Visual/LaneVisualizer.cs`

実装済みの修正：
1. `CreateDividerLines()`メソッドの統一（277-323行目）
2. `CalculateLaneX()`メソッドの簡略化（355-374行目）

```csharp
// CreateDividerLines()の修正（277-311行目）
private void CreateDividerLines(ref int lineIndex)
{
    if (conductor == null) return;
    
    float[] lanePositions = conductor.LaneXPositions;
    
    // レーン間の境界線を作成（5本）
    for (int i = 0; i <= lanePositions.Length; i++)
    {
        float x;
        if (i == 0)
        {
            x = lanePositions[0] - conductor.LaneWidth;
        }
        else if (i == lanePositions.Length)
        {
            x = lanePositions[lanePositions.Length - 1] + conductor.LaneWidth;
        }
        else
        {
            x = (lanePositions[i - 1] + lanePositions[i]) / 2f;
        }
        
        // Conductorの統一された遠近感設定を使用
        float nearX = x * conductor.PerspectiveNearScale;
        float farX = x * conductor.PerspectiveFarScale;
        
        Vector3 nearPoint = transform.position + new Vector3(nearX, 0, 0);
        Vector3 farPoint = transform.position + new Vector3(farX, 0, laneLength);
        
        CreateLaneLine(ref lineIndex, nearPoint, farPoint, dividerColor, dividerWidth);
    }
}

// CalculateLaneX()の簡略化（351-382行目）
public float CalculateLaneX(int laneIndex, bool isNear)
{
    if (conductor != null)
    {
        return conductor.GetPerspectiveLaneX(laneIndex, isNear ? 0f : conductor.SpawnZ);
    }
    
    // フォールバック（Conductorがない場合）
    float x = (laneIndex - 1.5f) * laneWidth;
    return isNear ? x : x * (farWidth / nearWidth);
}
```

### Phase 3: NoteControllerの修正（✅ 実装完了）

**ファイル**: `Gameplay/NoteController.cs`

実装済みの遠近感を考慮した移動：

```csharp
// 実装済み（Update()メソッド内 90-125行目）
void Update()
{
    if (conductor == null || isCompleted) return;
    
    // 1. Z座標の更新（奥から手前へ移動）
    float newZ = conductor.GetNoteZPosition(targetBeat);
    
    // 2. 遠近感を考慮したX座標の更新
    float perspectiveX = conductor.GetPerspectiveLaneX(laneIndex, newZ);
    
    transform.position = new Vector3(
        perspectiveX,
        transform.position.y,
        newZ
    );
    
    // 3. 距離に応じたスケール変更（遠近感対応）
    UpdateScale(newZ);
    // ...
}

// 実装済み（232-247行目）
private void UpdateScale(float zPosition)
{
    if (conductor == null) return;
    
    // Conductorの統一されたスケール値を取得
    float scale = conductor.GetScaleAtZ(zPosition);
    
    // NoteDataのVisualScaleも考慮
    if (noteData != null)
    {
        scale *= noteData.VisualScale;
    }
    
    // スケールを適用
    transform.localScale = initialScale * scale;
}
```

### Phase 4: NoteSpawnerの修正（🔄 部分的に実装）

**ファイル**: `Gameplay/NoteSpawner.cs`

**注意**: NoteSpawnerでの遠近感を考慮した初期位置設定については、現在の実装では`CalculateSpawnPosition()`メソッド内で処理されていますが、初期スケール設定の統一化が必要な可能性があります。

```csharp
// SpawnNote()メソッドの修正（169-229行目）
private void SpawnNote(NoteData noteData)
{
    // ... 既存のコード ...
    
    // X座標を遠近感を考慮して設定
    float xPos = conductor.GetPerspectiveLaneX(noteData.Lane, conductor.SpawnZ);
    Vector3 spawnPosition = new Vector3(xPos, noteY, conductor.SpawnZ);
    
    GameObject noteObject = poolManager.GetNote(noteData.Type);
    noteObject.transform.position = spawnPosition;
    
    // 初期スケールを設定
    float initialScale = conductor.PerspectiveFarScale;
    noteObject.transform.localScale = Vector3.one * initialScale;
    
    // ... 残りのコード ...
}

// OnDrawGizmos()の修正（543-608行目）
void OnDrawGizmos()
{
    if (!showNotePathGizmo || conductor == null) return;
    
    // Conductorの統一された設定を使用
    float nearScale = conductor.PerspectiveNearScale;
    float farScale = conductor.PerspectiveFarScale;
    
    // これらの値を使用してGizmosを描画
    // ... 既存のGizmo描画コード ...
}
```

## テスト手順

### 1. 設定値の統一確認
1. UnityエディタでConductorコンポーネントのインスペクタを開く
2. 遠近感設定を以下に設定：
   - Perspective Near Scale: 1.0
   - Perspective Far Scale: 0.25

### 2. 視覚的整合性の確認
1. Scene Viewでレーンとノーツの軌道を確認
2. LaneVisualizerのGizmosとNoteSpawnerのGizmosが完全に一致することを確認
3. Play Modeでノーツがレーンの中心を正確に流れることを確認

### 3. 動的変更のテスト
1. Play Mode中にConductorの設定を変更
2. すべてのコンポーネントが即座に同期することを確認

### 4. パフォーマンステスト
1. 大量のノーツを生成してフレームレートを確認
2. 遠近感計算が原因でパフォーマンスが低下していないことを確認

## 実装状況サマリー

1. **Phase 1**: Conductorの拡張 ✅ **完了**
2. **Phase 2**: LaneVisualizerの修正 ✅ **完了**
3. **Phase 3**: NoteControllerの修正 ✅ **完了**
4. **Phase 4**: NoteSpawnerの修正 🔄 **部分的に実装**

## 実装結果

実装された修正により：
- ✅ Conductorクラスに統一された遠近感計算メソッド（`GetPerspectiveLaneX()`, `GetScaleAtZ()`）を実装
- ✅ LaneVisualizerがConductorの遠近感設定を使用するように修正
- ✅ NoteControllerが移動時に遠近感を考慮したX座標とスケーリングを適用
- ✅ レーンとノーツの軌道の整合性が大幅に改善
- ✅ 設定の一元管理が実現（Conductorの`PerspectiveNearScale`と`PerspectiveFarScale`で制御）

## 関連ドキュメント

- [レーン・スポーナー整合性設計](../design/lane-spawner-alignment-design.md)
- [レーン・スポーナー整合性実装計画](../components/lane-spawner-alignment-implementation-plan.md)
- [NoteController設計](../components/note-controller-design.md)
- [NoteSpawner実装計画](../components/note-spawner-implementation-plan.md)

## 追加の推奨事項

### レーン設定の外部化

将来的には、レーン設定をScriptableObjectとして外部化することを推奨：

```csharp
[CreateAssetMenu(fileName = "LaneConfiguration", menuName = "Jirou/Lane Configuration")]
public class LaneConfiguration : ScriptableObject
{
    [Header("レーン基本設定")]
    public float[] laneXPositions = { -3f, -1f, 1f, 3f };
    public float laneWidth = 2.0f;
    
    [Header("遠近感設定")]
    public float perspectiveNearScale = 1.0f;
    public float perspectiveFarScale = 0.25f;
    
    [Header("判定設定")]
    public float judgmentZ = 0f;
    public float spawnZ = 20f;
    
    // ヘルパーメソッド
    public float GetPerspectiveLaneX(int laneIndex, float z)
    {
        // 実装
    }
}
```

これにより、異なるステージや難易度で異なるレーン設定を簡単に切り替えることができるようになります。