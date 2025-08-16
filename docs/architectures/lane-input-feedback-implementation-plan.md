# レーン入力フィードバックシステム 実装計画書

## 1. 実装概要

### 1.1 実装目標
キー入力（D、F、J、K）に応じて対応するレーンが光る視覚的フィードバックシステムを実装し、プレイヤーの入力を明確に可視化する。

### 1.2 実装範囲
- LaneInputFeedbackコンポーネントの作成
- フィードバック用Cubeオブジェクトの動的生成
- Material制御による発光エフェクト
- 入力イベントとの連携
- Unityエディタでの設定手順

## 2. 実装ステップ

### Phase 1: 基本実装（必須）

#### Step 1.1: LaneInputFeedbackクラスの作成
**ファイル**: `Assets/_Jirou/Scripts/Visual/LaneInputFeedback.cs`

```csharp
using System.Collections;
using UnityEngine;
using Jirou.Core;
using Jirou.Gameplay;

namespace Jirou.Visual
{
    /// <summary>
    /// レーン入力時の視覚的フィードバックを管理するコンポーネント
    /// 各レーンのキー入力に応じて、対応するレーンを光らせる
    /// </summary>
    public class LaneInputFeedback : MonoBehaviour
    {
        [Header("Feedback Visual Settings")]
        [Tooltip("フィードバックの発光強度")]
        [Range(0.5f, 5.0f)]
        [SerializeField] private float feedbackIntensity = 2.0f;
        
        [Tooltip("フィードバックの持続時間")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float feedbackDuration = 0.1f;
        
        [Tooltip("フィードバックのアニメーションカーブ")]
        [SerializeField] private AnimationCurve feedbackCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("Cube Settings")]
        [Tooltip("フィードバックCubeの高さ")]
        [Range(0.1f, 2.0f)]
        [SerializeField] private float cubeHeight = 0.5f;
        
        [Tooltip("通常時のCubeの透明度")]
        [Range(0f, 0.5f)]
        [SerializeField] private float normalAlpha = 0.05f;
        
        [Header("Color Settings")]
        [Tooltip("各レーンの基本色")]
        [SerializeField] private Color[] laneColors = new Color[]
        {
            new Color(1f, 0.2f, 0.2f, 1f),  // Lane 0 (D) - 赤
            new Color(1f, 0.8f, 0.2f, 1f),  // Lane 1 (F) - 黄
            new Color(0.2f, 1f, 0.2f, 1f),  // Lane 2 (J) - 緑
            new Color(0.2f, 0.2f, 1f, 1f)   // Lane 3 (K) - 青
        };
        
        [Header("Input Keys")]
        [Tooltip("各レーンに対応するキー")]
        [SerializeField] private KeyCode[] laneKeys = 
        {
            KeyCode.D,
            KeyCode.F,
            KeyCode.J,
            KeyCode.K
        };
        
        // プライベートフィールド
        private const int LANE_COUNT = 4;
        private GameObject[] feedbackCubes;
        private MeshRenderer[] cubeRenderers;
        private Material[] cubeMaterials;
        private Coroutine[] activeCoroutines;
        private bool[] isHolding;
        
        // 依存コンポーネント
        private LaneVisualizer laneVisualizer;
        private Conductor conductor;
        
        // 初期化処理
        void Start()
        {
            InitializeComponents();
            CreateFeedbackCubes();
            SetupMaterials();
        }
        
        // 更新処理（キー入力監視）
        void Update()
        {
            ProcessKeyInputs();
        }
        
        // 実装詳細は後続のStepで追加
    }
}
```

#### Step 1.2: フィードバックCubeの生成処理

```csharp
private void CreateFeedbackCubes()
{
    feedbackCubes = new GameObject[LANE_COUNT];
    cubeRenderers = new MeshRenderer[LANE_COUNT];
    
    GameObject container = new GameObject("FeedbackCubes");
    container.transform.SetParent(transform);
    container.transform.localPosition = Vector3.zero;
    
    for (int i = 0; i < LANE_COUNT; i++)
    {
        // Cubeオブジェクトの作成
        feedbackCubes[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
        feedbackCubes[i].name = $"LaneFeedback_{i}";
        feedbackCubes[i].transform.SetParent(container.transform);
        
        // Colliderを無効化（判定に影響しないように）
        Destroy(feedbackCubes[i].GetComponent<Collider>());
        
        // Rendererの取得
        cubeRenderers[i] = feedbackCubes[i].GetComponent<MeshRenderer>();
        
        // 位置とスケールの設定
        ConfigureCubeTransform(i);
    }
}

private void ConfigureCubeTransform(int laneIndex)
{
    if (conductor == null) return;
    
    GameObject cube = feedbackCubes[laneIndex];
    
    // レーンのX座標を取得
    float laneX = conductor.LaneXPositions[laneIndex];
    
    // 位置設定（レーンの中央、Z軸は中間点）
    float zCenter = conductor.SpawnZ / 2f;
    cube.transform.localPosition = new Vector3(laneX, cubeHeight / 2f, zCenter);
    
    // スケール設定（レーン幅、高さ、レーン長）
    float laneWidth = conductor.LaneWidth * 0.9f; // 少し狭めに
    cube.transform.localScale = new Vector3(laneWidth, cubeHeight, conductor.SpawnZ);
}
```

#### Step 1.3: Material設定と発光制御

```csharp
private void SetupMaterials()
{
    cubeMaterials = new Material[LANE_COUNT];
    
    for (int i = 0; i < LANE_COUNT; i++)
    {
        // 新しいMaterialインスタンスを作成
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        // 基本設定
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0);   // Alpha
        mat.SetFloat("_AlphaClip", 0);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0);
        mat.renderQueue = 3000;
        
        // 色設定
        Color baseColor = laneColors[i];
        baseColor.a = normalAlpha;
        mat.SetColor("_BaseColor", baseColor);
        
        // Emission設定（初期は無効）
        mat.DisableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.black);
        
        cubeMaterials[i] = mat;
        cubeRenderers[i].material = mat;
    }
}
```

#### Step 1.4: キー入力処理とフィードバック制御

```csharp
private void ProcessKeyInputs()
{
    for (int i = 0; i < LANE_COUNT; i++)
    {
        // キー押下検出
        if (Input.GetKeyDown(laneKeys[i]))
        {
            TriggerFeedback(i);
        }
        
        // キー長押し検出（オプション）
        if (Input.GetKey(laneKeys[i]))
        {
            if (!isHolding[i])
            {
                isHolding[i] = true;
                StartHoldFeedback(i);
            }
        }
        
        // キー離し検出
        if (Input.GetKeyUp(laneKeys[i]))
        {
            if (isHolding[i])
            {
                isHolding[i] = false;
                StopHoldFeedback(i);
            }
        }
    }
}

private void TriggerFeedback(int laneIndex)
{
    // 既存のコルーチンを停止
    if (activeCoroutines[laneIndex] != null)
    {
        StopCoroutine(activeCoroutines[laneIndex]);
    }
    
    // 新しいフィードバックを開始
    activeCoroutines[laneIndex] = StartCoroutine(AnimateFeedback(laneIndex));
}

private IEnumerator AnimateFeedback(int laneIndex)
{
    Material mat = cubeMaterials[laneIndex];
    float elapsed = 0f;
    
    // Emissionを有効化
    mat.EnableKeyword("_EMISSION");
    
    while (elapsed < feedbackDuration)
    {
        float t = elapsed / feedbackDuration;
        float intensity = feedbackCurve.Evaluate(t) * feedbackIntensity;
        
        // HDR色で発光
        Color emissionColor = laneColors[laneIndex] * Mathf.LinearToGammaSpace(intensity);
        mat.SetColor("_EmissionColor", emissionColor);
        
        // 透明度も調整
        Color baseColor = laneColors[laneIndex];
        baseColor.a = Mathf.Lerp(normalAlpha, 0.3f, feedbackCurve.Evaluate(t));
        mat.SetColor("_BaseColor", baseColor);
        
        elapsed += Time.deltaTime;
        yield return null;
    }
    
    // 元の状態に戻す
    mat.DisableKeyword("_EMISSION");
    mat.SetColor("_EmissionColor", Color.black);
    
    Color finalColor = laneColors[laneIndex];
    finalColor.a = normalAlpha;
    mat.SetColor("_BaseColor", finalColor);
    
    activeCoroutines[laneIndex] = null;
}
```

### Phase 2: 遠近感対応（推奨）

#### Step 2.1: 台形状Mesh生成

```csharp
private void CreateTrapezoidMesh(int laneIndex)
{
    GameObject cube = feedbackCubes[laneIndex];
    
    // 既存のCube Meshを置き換え
    MeshFilter meshFilter = cube.GetComponent<MeshFilter>();
    Mesh trapezoidMesh = new Mesh();
    trapezoidMesh.name = $"LaneTrapezoid_{laneIndex}";
    
    // レーン位置情報の取得
    float laneX = conductor.LaneXPositions[laneIndex];
    float halfWidth = conductor.LaneWidth / 2f;
    
    // 頂点座標の計算（遠近感対応）
    float nearScale = conductor.PerspectiveNearScale;
    float farScale = conductor.PerspectiveFarScale;
    
    Vector3[] vertices = new Vector3[]
    {
        // 手前の頂点（判定ライン側）
        new Vector3((laneX - halfWidth) * nearScale, 0, 0),
        new Vector3((laneX + halfWidth) * nearScale, 0, 0),
        new Vector3((laneX + halfWidth) * nearScale, cubeHeight, 0),
        new Vector3((laneX - halfWidth) * nearScale, cubeHeight, 0),
        
        // 奥の頂点（スポーン側）
        new Vector3((laneX - halfWidth) * farScale, 0, conductor.SpawnZ),
        new Vector3((laneX + halfWidth) * farScale, 0, conductor.SpawnZ),
        new Vector3((laneX + halfWidth) * farScale, cubeHeight, conductor.SpawnZ),
        new Vector3((laneX - halfWidth) * farScale, cubeHeight, conductor.SpawnZ)
    };
    
    // 三角形インデックス
    int[] triangles = new int[]
    {
        // 上面
        3, 2, 6, 3, 6, 7,
        // 底面
        0, 4, 5, 0, 5, 1,
        // 前面
        0, 1, 2, 0, 2, 3,
        // 背面
        5, 4, 7, 5, 7, 6,
        // 左面
        4, 0, 3, 4, 3, 7,
        // 右面
        1, 5, 6, 1, 6, 2
    };
    
    // UV座標
    Vector2[] uv = new Vector2[]
    {
        new Vector2(0, 0), new Vector2(1, 0),
        new Vector2(1, 1), new Vector2(0, 1),
        new Vector2(0, 0), new Vector2(1, 0),
        new Vector2(1, 1), new Vector2(0, 1)
    };
    
    // Meshの構築
    trapezoidMesh.vertices = vertices;
    trapezoidMesh.triangles = triangles;
    trapezoidMesh.uv = uv;
    trapezoidMesh.RecalculateNormals();
    trapezoidMesh.RecalculateBounds();
    
    meshFilter.mesh = trapezoidMesh;
}
```

### Phase 3: 高度な機能（オプション）

#### Step 3.1: 判定連動フィードバック

```csharp
private void OnEnable()
{
    // InputManagerのイベントを購読
    InputManager.OnNoteJudged += HandleNoteJudged;
}

private void OnDisable()
{
    InputManager.OnNoteJudged -= HandleNoteJudged;
}

private void HandleNoteJudged(int laneIndex, JudgmentType judgment)
{
    // 判定タイプに応じた特別なフィードバック
    switch (judgment)
    {
        case JudgmentType.Perfect:
            TriggerSpecialFeedback(laneIndex, 3.0f, Color.yellow);
            break;
        case JudgmentType.Great:
            TriggerSpecialFeedback(laneIndex, 2.0f, Color.green);
            break;
        case JudgmentType.Good:
            TriggerSpecialFeedback(laneIndex, 1.5f, Color.cyan);
            break;
        case JudgmentType.Miss:
            TriggerSpecialFeedback(laneIndex, 1.0f, Color.red);
            break;
    }
}
```

## 3. Unity エディタでの設定手順

### 3.1 オブジェクト階層の構築

```
Scene Hierarchy:
├── GameManager
│   ├── Conductor
│   ├── InputManager
│   └── ScoreManager
├── Stage
│   ├── LaneVisualizer
│   ├── LaneInputFeedback  ← 新規追加
│   └── JudgmentLine
└── Cameras
    └── Main Camera
```

### 3.2 LaneInputFeedbackの設定手順

1. **GameObject作成**
   - Hierarchyで右クリック → Create Empty
   - 名前を「LaneInputFeedback」に変更
   - Stageオブジェクトの子として配置

2. **コンポーネント追加**
   - Add Component → Scripts → Jirou.Visual → LaneInputFeedback

3. **Inspector設定**
   ```
   Feedback Visual Settings:
   - Feedback Intensity: 2.0
   - Feedback Duration: 0.1
   - Feedback Curve: デフォルトカーブ（EaseInOut）
   
   Cube Settings:
   - Cube Height: 0.5
   - Normal Alpha: 0.05
   
   Color Settings:
   - Lane Colors:
     [0]: FF3333FF (赤)
     [1]: FFCC33FF (黄)
     [2]: 33FF33FF (緑)
     [3]: 3333FFFF (青)
   
   Input Keys:
   - [0]: D
   - [1]: F
   - [2]: J
   - [3]: K
   ```

4. **依存関係の確認**
   - ConductorがSceneに存在することを確認
   - LaneVisualizerが同じ親（Stage）に存在することを確認

5. **Material設定（オプション）**
   - Project → Create → Material → 「LaneFeedbackMaterial」
   - Shader: Universal Render Pipeline/Lit
   - Surface Type: Transparent
   - 各レーン用に複製して色を設定

### 3.3 URP設定の確認

1. **Project Settings → Graphics**
   - Scriptable Render Pipeline Settings: URP-HighFidelity

2. **URP Asset設定**
   - HDR: Enabled（発光効果のため）
   - Anti-aliasing: 推奨

3. **Post Processing（オプション）**
   - Bloom効果を追加して発光を強調

## 4. テスト手順

### 4.1 単体テスト

1. **キー入力テスト**
   - D、F、J、Kキーを順番に押す
   - 各レーンが正しく光ることを確認

2. **同時押しテスト**
   - 複数キーを同時に押す
   - 全レーンが独立して光ることを確認

3. **長押しテスト**
   - キーを長押し
   - 継続的なフィードバックを確認

### 4.2 統合テスト

1. **LaneVisualizerとの整合性**
   - フィードバックCubeがレーン境界内に収まることを確認
   - 遠近感が正しく適用されることを確認

2. **InputManagerとの連携**
   - ノーツ判定時にフィードバックが発生することを確認
   - 判定タイプに応じた色変化を確認

## 5. トラブルシューティング

### 5.1 よくある問題と解決方法

| 問題 | 原因 | 解決方法 |
|------|------|----------|
| Cubeが表示されない | Materialの設定ミス | Transparentモードを確認 |
| 発光しない | HDRが無効 | URP設定でHDRを有効化 |
| 位置がずれる | Conductor未初期化 | Start()の実行順序を調整 |
| 色が暗い | Linear/Gamma不一致 | Color SpaceをLinearに設定 |

### 5.2 デバッグ用コード

```csharp
#if UNITY_EDITOR
void OnGUI()
{
    if (!showDebugInfo) return;
    
    GUILayout.BeginArea(new Rect(10, 120, 300, 200));
    GUILayout.Label("Lane Input Feedback Debug");
    
    for (int i = 0; i < LANE_COUNT; i++)
    {
        string status = isHolding[i] ? "HOLDING" : "READY";
        bool hasCoroutine = activeCoroutines[i] != null;
        
        GUILayout.Label($"Lane {i}: {status} | Active: {hasCoroutine}");
    }
    
    GUILayout.EndArea();
}
#endif
```

## 6. パフォーマンス最適化

### 6.1 最適化ポイント

1. **Material共有**
   - 同じ設定のMaterialは共有インスタンスを使用

2. **コルーチン管理**
   - 不要なコルーチンは即座に停止
   - オブジェクトプールの使用を検討

3. **Update最適化**
   - キー入力チェックを最小限に
   - 条件分岐の順序を最適化

### 6.2 推奨設定

- Target Frame Rate: 60 FPS
- V-Sync: On
- Quality Settings: High以上

## 7. 今後の拡張案

1. **パーティクルエフェクト追加**
   - キー押下時にパーティクル発生

2. **カスタムシェーダー**
   - より高度な視覚効果の実装

3. **設定の外部化**
   - ScriptableObjectによる設定管理

4. **アニメーション強化**
   - DOTweenによるスムーズなアニメーション