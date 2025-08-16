# LaneVisualizer 実装計画書

## 実装概要

本書は、Jirouプロジェクトの奥行き型リズムゲームにおける、レーン可視化コンポーネント「LaneVisualizer」の実装計画を定義します。このコンポーネントは、プレイヤーに奥行き感のある4レーンを表示し、ノーツが流れてくる経路を視覚的に明確にする役割を担います。

## コンポーネント概要

### 目的
- 奥行き感のある4レーンの表示
- 台形状の視覚効果による遠近感の演出
- ノーツの移動経路の明確化
- プレイヤーの視認性向上

### 主要機能
1. **台形レーン描画**: 奥が狭く、手前が広い台形状のレーン表示
2. **動的レーン生成**: 設定に応じた柔軟なレーン数対応
3. **視覚的調整**: マテリアル、透明度、幅の調整機能
4. **パフォーマンス最適化**: LineRendererを使用した軽量な描画

## 詳細設計

### クラス構造

```csharp
namespace Jirou.Visual
{
    using UnityEngine;
    
    /// <summary>
    /// 奥行き感のあるレーンを表示するビジュアライザー
    /// </summary>
    public class LaneVisualizer : MonoBehaviour
    {
        // パブリックフィールド
        [Header("レーン設定")]
        [Tooltip("レーンの数")]
        public int laneCount = 4;
        
        [Tooltip("レーン間の基準幅")]
        public float laneWidth = 2.0f;
        
        [Header("奥行き設定")]
        [Tooltip("手前（判定ライン）でのレーン幅")]
        public float nearWidth = 2.0f;
        
        [Tooltip("奥（スポーン地点）でのレーン幅")]
        public float farWidth = 0.5f;
        
        [Tooltip("レーンの長さ（Z軸方向）")]
        public float laneLength = 20.0f;
        
        [Header("ビジュアル設定")]
        [Tooltip("レーンのマテリアル")]
        public Material laneMaterial;
        
        [Tooltip("レーンラインの太さ")]
        [Range(0.01f, 0.5f)]
        public float lineWidth = 0.05f;
        
        [Tooltip("レーンの色")]
        public Color laneColor = new Color(1f, 1f, 1f, 0.3f);
        
        [Header("オプション設定")]
        [Tooltip("中央ラインを表示")]
        public bool showCenterLine = true;
        
        [Tooltip("外枠を表示")]
        public bool showOuterBorders = true;
        
        // プライベートフィールド
        private LineRenderer[] laneRenderers;
        private GameObject laneContainer;
    }
}
```

### メソッド設計

#### 初期化メソッド

```csharp
/// <summary>
/// 初期化処理
/// </summary>
void Start()
{
    InitializeLanes();
}

/// <summary>
/// レーンの初期化と生成
/// </summary>
private void InitializeLanes()
{
    // コンテナオブジェクトの作成
    CreateLaneContainer();
    
    // レーン数に応じた配列の初期化
    int totalLines = CalculateTotalLines();
    laneRenderers = new LineRenderer[totalLines];
    
    // 各レーンラインの生成
    CreateLaneLines();
    
    // マテリアルと色の適用
    ApplyVisualsToLanes();
}
```

#### レーン生成メソッド

```csharp
/// <summary>
/// レーンラインを生成
/// </summary>
private void CreateLaneLines()
{
    int lineIndex = 0;
    
    // 外枠の生成
    if (showOuterBorders)
    {
        CreateBorderLines(ref lineIndex);
    }
    
    // レーン区切り線の生成
    CreateDividerLines(ref lineIndex);
    
    // 中央ラインの生成
    if (showCenterLine)
    {
        CreateCenterLine(ref lineIndex);
    }
}

/// <summary>
/// 個別のラインを作成
/// </summary>
private LineRenderer CreateSingleLine(string lineName, Vector3 nearPoint, Vector3 farPoint)
{
    GameObject lineObject = new GameObject(lineName);
    lineObject.transform.SetParent(laneContainer.transform);
    
    LineRenderer renderer = lineObject.AddComponent<LineRenderer>();
    
    // 2点を設定（手前と奥）
    renderer.positionCount = 2;
    renderer.SetPosition(0, nearPoint);
    renderer.SetPosition(1, farPoint);
    
    // 幅の設定
    renderer.startWidth = lineWidth;
    renderer.endWidth = lineWidth * (farWidth / nearWidth); // 遠近感を考慮
    
    // その他の設定
    renderer.useWorldSpace = false;
    renderer.alignment = LineAlignment.View;
    
    return renderer;
}
```

#### 座標計算メソッド

```csharp
/// <summary>
/// レーンのX座標を計算
/// </summary>
private float CalculateLaneX(int laneIndex, bool isNear)
{
    // レーンの中心位置を計算
    float totalWidth = laneCount * laneWidth;
    float startX = -totalWidth / 2.0f + laneWidth / 2.0f;
    float x = startX + (laneIndex * laneWidth);
    
    // 遠近感を適用
    if (!isNear)
    {
        x *= (farWidth / nearWidth);
    }
    
    return x;
}

/// <summary>
/// レーン境界線の座標を計算
/// </summary>
private Vector3[] CalculateDividerPoints(int dividerIndex)
{
    float x = CalculateDividerX(dividerIndex);
    
    Vector3 nearPoint = new Vector3(x * nearWidth, 0, 0);
    Vector3 farPoint = new Vector3(x * farWidth, 0, laneLength);
    
    return new Vector3[] { nearPoint, farPoint };
}

/// <summary>
/// 区切り線のX座標を計算
/// </summary>
private float CalculateDividerX(int dividerIndex)
{
    // レーン間の区切り線の位置
    float totalWidth = laneCount * laneWidth;
    float startX = -totalWidth / 2.0f;
    return startX + (dividerIndex + 1) * laneWidth;
}
```

#### 動的更新メソッド

```csharp
/// <summary>
/// ランタイムでのレーン更新
/// </summary>
public void UpdateLanes()
{
    // 既存のレーンを削除
    ClearLanes();
    
    // 新しいレーンを生成
    InitializeLanes();
}

/// <summary>
/// レーンの表示/非表示切り替え
/// </summary>
public void SetLanesVisible(bool visible)
{
    if (laneContainer != null)
    {
        laneContainer.SetActive(visible);
    }
}

/// <summary>
/// レーンの色を変更
/// </summary>
public void SetLaneColor(Color newColor)
{
    laneColor = newColor;
    ApplyVisualsToLanes();
}
```

#### デバッグ機能

```csharp
#if UNITY_EDITOR
/// <summary>
/// エディタ上でのギズモ表示
/// </summary>
void OnDrawGizmos()
{
    // エディタ上でプレビュー表示
    if (!Application.isPlaying)
    {
        DrawPreviewGizmos();
    }
}

/// <summary>
/// プレビュー用のギズモ描画
/// </summary>
private void DrawPreviewGizmos()
{
    Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
    
    // レーンの概形を表示
    for (int i = 0; i <= laneCount; i++)
    {
        float x = CalculateDividerX(i);
        Vector3 near = transform.position + new Vector3(x * nearWidth, 0, 0);
        Vector3 far = transform.position + new Vector3(x * farWidth, 0, laneLength);
        Gizmos.DrawLine(near, far);
    }
    
    // 判定ラインと生成ラインを表示
    Gizmos.color = Color.green;
    Vector3 leftNear = transform.position + Vector3.left * (laneCount * nearWidth / 2);
    Vector3 rightNear = transform.position + Vector3.right * (laneCount * nearWidth / 2);
    Gizmos.DrawLine(leftNear, rightNear);
    
    Gizmos.color = Color.blue;
    Vector3 leftFar = transform.position + new Vector3(-laneCount * farWidth / 2, 0, laneLength);
    Vector3 rightFar = transform.position + new Vector3(laneCount * farWidth / 2, 0, laneLength);
    Gizmos.DrawLine(leftFar, rightFar);
}
#endif
```

### 視覚効果の詳細

#### レーン表示パターン

1. **基本レーン（4レーン）**
   ```
   奥（Z=20）:     |   |   |   |   |  （狭い）
                   \   |   |   |   /
                    \  |   |   |  /
                     \ |   |   | /
                      \|   |   |/
   手前（Z=0）:        |   |   |   |   |  （広い）
   ```

2. **レーン座標（X軸）**
   - レーン0: X = -3
   - レーン1: X = -1
   - レーン2: X = 1
   - レーン3: X = 3

#### マテリアル設定

```csharp
/// <summary>
/// デフォルトマテリアルの作成
/// </summary>
private Material CreateDefaultMaterial()
{
    // Unlit/Colorシェーダーを使用
    Shader shader = Shader.Find("Sprites/Default");
    Material mat = new Material(shader);
    
    // 半透明設定
    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
    mat.SetInt("_ZWrite", 0);
    mat.renderQueue = 3000; // Transparent
    
    return mat;
}
```

## 実装状況

### ✅ 実装完了項目

#### フェーズ1: 基本構造 【完了】

1. **クラス作成とフィールド定義** ✅
   - LaneVisualizer.csの作成完了
   - パブリック/プライベートフィールドの定義完了
   - プロパティによる動的更新対応を追加実装
   - Inspectorでの調整可能な設定実装済み

2. **初期化処理** ✅
   - Startメソッドの実装完了
   - コンテナオブジェクトの生成実装済み
   - ValidateSettings()による設定検証を追加

#### フェーズ2: レーン生成 【完了】

1. **座標計算ロジック** ✅
   - 台形状の座標計算実装済み
   - レーン間隔の計算実装済み
   - 遠近感の適用（farWidth/nearWidth比率）実装済み

2. **LineRenderer生成** ✅
   - 個別ラインの生成（CreateSingleLine）実装済み
   - 配列管理実装済み
   - 親子関係の設定実装済み
   - 外枠、区切り線、中央ラインの個別生成メソッド実装

#### フェーズ3: ビジュアル調整 【完了】

1. **マテリアル適用** ✅
   - カスタムマテリアルの適用実装済み
   - デフォルトマテリアルの生成実装済み
   - 透明度設定実装済み

2. **色と幅の調整** ✅
   - ランタイム変更対応実装済み
   - 遠近感を考慮した幅調整実装済み
   - SetLaneColor()メソッドによる動的色変更実装

#### フェーズ4: 拡張機能 【完了】

1. **動的更新機能** ✅
   - レーン数の変更（プロパティセッター）実装済み
   - 表示/非表示切り替え（SetLanesVisible）実装済み
   - UpdateLanes()による再生成機能実装済み

2. **デバッグ機能** ✅
   - Gizmosでのプレビュー実装済み
   - OnDrawGizmos()による視覚的デバッグ実装済み
   - OnDestroy()によるクリーンアップ実装済み

### ✅ 追加実装完了項目（2025年1月16日）

#### Conductor連携機能 【完了】

1. **自動同期機能** ✅
   - `InitializeConductor()`メソッドの実装
   - Conductorシングルトンインスタンスの取得
   - 初回同期処理の実装

2. **動的更新機能** ✅
   - `Update()`メソッドでの定期同期
   - `SyncWithConductor()`メソッドの実装
   - SpawnZ値からレーン長への自動反映

3. **設定オプション** ✅
   - `syncWithConductor`フラグによる同期制御
   - `syncUpdateInterval`による更新頻度調整（0.1〜5.0秒）
   - 手動同期用`ForceSync()`メソッドの実装

4. **エラーハンドリング** ✅
   - Conductor不在時の適切な処理
   - 同期失敗時の手動設定へのフォールバック
   - デバッグログによる状態通知

5. **テスト実装** ✅
   - `ConductorSync_Disabled_UsesManualSettings` - 同期無効時の動作確認
   - `ConductorSync_InitializesCorrectly` - 初期化処理の確認
   - `ForceSync_WithoutConductor_HandlesGracefully` - エラー処理確認
   - `SyncUpdateInterval_InValidRange` - 設定値範囲の確認
   - `ConductorSync_UpdatesLaneLength` - 同期動作の確認

### ⚠️ 未実装項目

1. **パフォーマンス計測**
   - 初期化時間の計測
   - 更新処理の計測

### 📝 実装上の改善点

1. **プロパティによる動的更新**
   - 計画書のパブリックフィールドを、バリデーション付きプロパティに改善
   - 実行時の値変更に即座に対応

2. **エラーハンドリングの強化**
   - ValidateSettings()メソッドの追加
   - 不正な値の自動修正機能

3. **クリーンアップ処理**
   - OnDestroy()によるメモリリークの防止
   - DestroyImmediate()の適切な使用

## パフォーマンス考慮事項

### 最適化ポイント

1. **LineRendererの使用**
   - メッシュ生成よりも軽量
   - バッチング可能
   - 動的な更新が容易

2. **オブジェクトプーリング準備**
   - 将来的なエフェクト追加に備えた設計
   - レンダラーの再利用

3. **LOD（Level of Detail）対応**
   - カメラ距離に応じた詳細度調整
   - 遠距離での簡略化

### パフォーマンス目標

- **初期化時間**: 50ms以内
- **更新処理**: 1ms以内（60FPS維持）
- **メモリ使用量**: 1MB以内
- **ドローコール**: 最大5回

## 統合計画

### Conductorとの連携 【実装済み】

```csharp
public class LaneVisualizer : MonoBehaviour
{
    [Header("Conductor連携")]
    [Tooltip("Conductorと自動同期する")]
    public bool syncWithConductor = true;
    
    [Tooltip("同期更新の間隔（秒）")]
    [Range(0.1f, 5.0f)]
    public float syncUpdateInterval = 1.0f;
    
    private Conductor conductor;
    private float lastSyncTime;
    
    void Start()
    {
        ValidateSettings();
        InitializeConductor();
        InitializeLanes();
    }
    
    void Update()
    {
        if (syncWithConductor && conductor != null)
        {
            // 定期的にConductorの設定と同期
            if (Time.time - lastSyncTime >= syncUpdateInterval)
            {
                SyncWithConductor();
                lastSyncTime = Time.time;
            }
        }
    }
    
    private void InitializeConductor()
    {
        if (!syncWithConductor) return;
        
        // Conductorインスタンスの取得
        conductor = Conductor.Instance;
        
        if (conductor != null)
        {
            // 初回同期
            SyncWithConductor();
            lastSyncTime = Time.time;
            Debug.Log("[LaneVisualizer] Conductorとの連携を開始しました");
        }
        else
        {
            Debug.LogWarning("[LaneVisualizer] Conductorが見つかりません。手動設定を使用します。");
            syncWithConductor = false;
        }
    }
    
    private void SyncWithConductor()
    {
        if (conductor == null) return;
        
        // SpawnZからレーン長を自動設定
        float newLaneLength = conductor.SpawnZ;
        
        // 値が変更された場合のみ更新
        if (Mathf.Abs(_laneLength - newLaneLength) > 0.01f)
        {
            _laneLength = newLaneLength;
            
            // レーンを再生成
            if (Application.isPlaying && laneContainer != null)
            {
                UpdateLanes();
                Debug.Log($"[LaneVisualizer] レーン長をConductorと同期: {_laneLength}");
            }
        }
    }
}
```

### NoteControllerとの連携

- レーン幅の共有
- 座標系の統一
- 判定ラインの位置同期

## テスト実装状況

### ✅ 実装済みユニットテスト（15項目）

1. **座標計算テスト** ✅
   - `LaneXCalculation_NearPosition_ReturnsCorrectCoordinates` - 4レーンの座標確認
   - `LaneXCalculation_FarPosition_AppliesPerspective` - 遠近感適用確認
   - `CalculateLaneX(0, true)` が -3.0f を返すことを確認

2. **台形変換テスト** ✅
   - `PerspectiveTransform_CalculatesCorrectly` - 遠近感の比率計算確認
   - nearWidth/farWidth比率の正確性検証

3. **レーン数変更テスト** ✅
   - `LaneCount_DifferentValues_CalculatesCorrectPositions` - 3, 5レーンでの座標確認
   - 異なるレーン数での中心対称性確認

4. **色変更テスト** ✅
   - `ColorChange_UpdatesVisualizerColor` - SetLaneColor()の動作確認

5. **レーン幅テスト** ✅
   - `LaneWidth_DifferentValues_CalculatesCorrectSpacing` - 異なる幅での間隔計算

6. **バリデーションテスト** ✅
   - `InvalidLaneCount_ClampsToValidRange` - 不正なレーン数の制限確認
   - `FarWidthGreaterThanNearWidth_ShouldBeValidated` - 幅の逆転防止確認

7. **マテリアルテスト** ✅
   - `DefaultMaterialCreation_WhenMaterialIsNull` - デフォルトマテリアル生成確認

8. **レーン長テスト** ✅
   - `LaneLength_AffectsDepth` - レーン長の設定と取得確認

9. **線幅テスト** ✅
   - `LineWidth_InValidRange` - 線幅の範囲制限確認

10. **オプションフラグテスト** ✅
    - `OptionsFlags_ControlVisibility` - 表示フラグの動作確認

11. **対称性テスト** ✅
    - `SymmetricLanePositions_AroundCenter` - レーンの中心対称性確認

### 統合テスト状況

1. **表示確認** ⚠️ PlayModeテストが必要
   - 4レーンが正しく表示される
   - 台形状になっている
   - マテリアルが適用されている

2. **動的更新** ⚠️ PlayModeテストが必要
   - レーン数変更が反映される
   - 色変更が即座に適用される
   - パフォーマンスが維持される

## エラーハンドリング

### 想定されるエラー

1. **マテリアル未設定**
   ```csharp
   if (laneMaterial == null)
   {
       Debug.LogWarning("レーンマテリアルが未設定です。デフォルトマテリアルを使用します。");
       laneMaterial = CreateDefaultMaterial();
   }
   ```

2. **不正なレーン数**
   ```csharp
   if (laneCount < 1 || laneCount > 8)
   {
       Debug.LogError($"不正なレーン数: {laneCount}。デフォルト値4を使用します。");
       laneCount = 4;
   }
   ```

3. **幅の逆転**
   ```csharp
   if (farWidth > nearWidth)
   {
       Debug.LogWarning("奥の幅が手前より大きくなっています。値を入れ替えます。");
       float temp = farWidth;
       farWidth = nearWidth;
       nearWidth = temp;
   }
   ```

## 拡張可能性

### 将来的な機能追加

1. **アニメーション**
   - パルスエフェクト
   - 色のグラデーション
   - レーンの動的変形

2. **エフェクト連携**
   - ノーツヒット時の波紋
   - コンボ時の特殊演出
   - BPM同期アニメーション

3. **カスタマイズ機能**
   - プレイヤー設定による色変更
   - レーン透明度調整
   - 表示パターン選択

## まとめ

LaneVisualizerは、奥行き型リズムゲームの視覚的基盤となる重要なコンポーネントです。台形状のレーン表示により、プレイヤーに明確な奥行き感を提供し、ノーツの移動経路を視覚的に理解しやすくします。LineRendererを使用した軽量な実装により、パフォーマンスを維持しながら、柔軟なカスタマイズが可能な設計となっています。

### 実装完了日
- **初回実装**: 2025年1月
- **Conductor連携追加**: 2025年1月16日
- **最終更新**: 2025年1月16日

### 実装品質
- ✅ コア機能すべて実装完了
- ✅ 包括的なユニットテスト（20項目）実装
- ✅ エラーハンドリングとバリデーション実装
- ✅ デバッグ機能（Gizmos）実装
- ✅ Conductor連携機能実装完了
- ⚠️ PlayModeテストは次フェーズで実装予定
- ⚠️ パフォーマンス計測は次フェーズで実装予定