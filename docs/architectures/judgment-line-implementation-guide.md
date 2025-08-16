# 判定ライン表示実装手順書

## 概要
このドキュメントは、Jirouプロジェクトで判定ラインをUnityエディタ上に表示させるための実装手順を説明します。判定ラインは、ノーツが到達するZ=0の位置に表示される視覚的なマーカーです。

## 現在の実装状況

### 既存のコンポーネント
1. **LaneVisualizer** (`Assets/_Jirou/Scripts/Visual/LaneVisualizer.cs`)
   - レーンの奥行き表現（台形状）を実装済み
   - LineRendererを使用してレーンの境界線を描画
   - Conductorとの連携機能を持つ
   - 判定ライン位置（Z=0）と生成ライン位置（SpawnZ）の概念はあるが、明示的な判定ライン表示はない

2. **JudgmentZone** (`Assets/_Jirou/Scripts/Gameplay/JudgmentZone.cs`)
   - 各レーンの判定処理を担当
   - BoxColliderを使用した判定範囲の設定
   - ギズモで判定範囲を可視化（エディタ内のみ）

## 実装手順

### ステップ1: JudgmentLineコンポーネントの作成

新しいスクリプトファイル `Assets/_Jirou/Scripts/Visual/JudgmentLine.cs` を作成します。

```csharp
using UnityEngine;
using Jirou.Core;

namespace Jirou.Visual
{
    /// <summary>
    /// 判定ラインを表示するコンポーネント
    /// Z=0の位置に横線を描画し、ノーツの到達地点を明示する
    /// </summary>
    public class JudgmentLine : MonoBehaviour
    {
        [Header("ライン設定")]
        [SerializeField] private float lineHeight = 0.1f;
        [SerializeField] private float lineThickness = 0.1f;
        [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.8f);
        
        [Header("エフェクト設定")]
        [SerializeField] private bool enablePulseEffect = true;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseIntensity = 0.2f;
        
        private LineRenderer lineRenderer;
        private Conductor conductor;
        private Material lineMaterial;
        
        void Start()
        {
            InitializeLine();
            FindConductor();
        }
        
        private void InitializeLine()
        {
            // LineRendererコンポーネントを追加または取得
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
            
            // マテリアルの設定
            CreateLineMaterial();
            
            // ラインの基本設定
            UpdateLinePositions();
        }
        
        private void UpdateLinePositions()
        {
            if (conductor != null)
            {
                // Conductorの設定を使用
                float leftX = conductor.LaneXPositions[0] - conductor.LaneWidth;
                float rightX = conductor.LaneXPositions[3] + conductor.LaneWidth;
                
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, new Vector3(leftX, lineHeight, 0));
                lineRenderer.SetPosition(1, new Vector3(rightX, lineHeight, 0));
            }
            else
            {
                // デフォルトの4レーン幅を使用
                float width = 8f;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, new Vector3(-width/2, lineHeight, 0));
                lineRenderer.SetPosition(1, new Vector3(width/2, lineHeight, 0));
            }
            
            lineRenderer.startWidth = lineThickness;
            lineRenderer.endWidth = lineThickness;
        }
        
        private void CreateLineMaterial()
        {
            // Unlit/Colorシェーダーを使用
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            
            lineMaterial = new Material(shader);
            lineMaterial.color = lineColor;
            lineRenderer.material = lineMaterial;
        }
        
        private void FindConductor()
        {
            conductor = Conductor.Instance;
            if (conductor != null)
            {
                UpdateLinePositions();
            }
        }
        
        void Update()
        {
            if (enablePulseEffect && lineMaterial != null)
            {
                // パルスエフェクト（ビートに合わせて光る）
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
                Color currentColor = lineColor;
                currentColor.a = lineColor.a * pulse;
                lineMaterial.color = currentColor;
            }
        }
    }
}
```

### ステップ2: 判定ラインプレハブの作成

1. **空のGameObjectを作成**
   - Hierarchyウィンドウで右クリック → Create Empty
   - 名前を「JudgmentLine」に変更

2. **Transformの設定**
   - Position: (0, 0, 0)
   - Rotation: (0, 0, 0)
   - Scale: (1, 1, 1)

3. **JudgmentLineコンポーネントを追加**
   - Add Component → Scripts → Jirou.Visual → JudgmentLine

4. **プレハブとして保存**
   - `Assets/_Jirou/Prefabs/Stage/JudgmentLine.prefab` として保存

### ステップ3: LaneVisualizerの拡張

`LaneVisualizer.cs` に判定ライン表示機能を追加：

```csharp
// LaneVisualizerクラス内に以下のフィールドを追加
[Header("判定ライン設定")]
[SerializeField] private bool showJudgmentLine = true;
[SerializeField] private GameObject judgmentLinePrefab;
private GameObject judgmentLineInstance;

// InitializeLanes()メソッドの最後に以下を追加
private void InitializeLanes()
{
    // 既存のコード...
    
    // 判定ラインの生成
    if (showJudgmentLine)
    {
        CreateJudgmentLine();
    }
}

// 新しいメソッドを追加
private void CreateJudgmentLine()
{
    // 既存の判定ラインを削除
    if (judgmentLineInstance != null)
    {
        DestroyImmediate(judgmentLineInstance);
    }
    
    // プレハブから生成するか、新規作成
    if (judgmentLinePrefab != null)
    {
        judgmentLineInstance = Instantiate(judgmentLinePrefab, transform);
    }
    else
    {
        // プレハブがない場合は動的に作成
        judgmentLineInstance = new GameObject("JudgmentLine");
        judgmentLineInstance.transform.SetParent(transform);
        judgmentLineInstance.AddComponent<JudgmentLine>();
    }
    
    // 位置をZ=0に固定
    judgmentLineInstance.transform.localPosition = Vector3.zero;
}
```

### ステップ4: シーンへの配置

1. **SampleSceneを開く**
   - `Assets/_Jirou/Scenes/SampleScene.unity` を開く

2. **ゲームマネージャーオブジェクトの作成（まだない場合）**
   - 空のGameObjectを作成し、「GameManager」と命名
   - Position: (0, 0, 0)

3. **LaneVisualizerの追加**
   - GameManagerにLaneVisualizerコンポーネントを追加
   - 設定値：
     - Lane Count: 4
     - Near Width: 2.0
     - Far Width: 0.5
     - Lane Length: 20.0
     - Show Judgment Line: true

4. **判定ラインプレハブの割り当て**
   - LaneVisualizerのJudgment Line Prefabフィールドに作成したプレハブを割り当て

### ステップ5: ビジュアル調整

判定ラインの見た目を調整するための追加オプション：

1. **発光エフェクト（オプション）**
   ```csharp
   // JudgmentLineクラスに追加
   [Header("発光設定")]
   [SerializeField] private bool enableGlow = true;
   [SerializeField] private float glowIntensity = 1.5f;
   
   private void CreateLineMaterial()
   {
       // HDR色を使用して発光感を出す
       if (enableGlow)
       {
           lineMaterial.EnableKeyword("_EMISSION");
           lineMaterial.SetColor("_EmissionColor", lineColor * glowIntensity);
       }
   }
   ```

2. **グラデーション効果（オプション）**
   ```csharp
   // LineRendererにグラデーションを適用
   private void ApplyGradient()
   {
       Gradient gradient = new Gradient();
       gradient.SetKeys(
           new GradientColorKey[] {
               new GradientColorKey(lineColor, 0.0f),
               new GradientColorKey(lineColor * 0.5f, 0.5f),
               new GradientColorKey(lineColor, 1.0f)
           },
           new GradientAlphaKey[] {
               new GradientAlphaKey(0.5f, 0.0f),
               new GradientAlphaKey(1.0f, 0.5f),
               new GradientAlphaKey(0.5f, 1.0f)
           }
       );
       lineRenderer.colorGradient = gradient;
   }
   ```

### ステップ6: テストと確認

1. **Playモードでテスト**
   - Unityエディタでプレイボタンを押す
   - 判定ラインがZ=0の位置に表示されることを確認
   - レーンの幅に合わせて適切に表示されることを確認

2. **エディタ上での確認項目**
   - [ ] 判定ラインがZ=0に正しく配置されている
   - [ ] 4レーンの幅全体をカバーしている
   - [ ] 色と透明度が適切に設定されている
   - [ ] パルスエフェクトが動作している（有効な場合）
   - [ ] Conductorとの連携が正しく機能している

3. **デバッグ表示の確認**
   - Scene viewでギズモ表示を有効にする
   - JudgmentZoneのギズモと判定ラインの位置が一致することを確認

## トラブルシューティング

### 判定ラインが表示されない場合
1. LineRendererのマテリアルが正しく設定されているか確認
2. ラインの高さ（Y座標）が適切か確認（0.1f程度を推奨）
3. カメラの描画範囲内にあるか確認

### 判定ラインの位置がずれている場合
1. Transform.positionがVector3.zeroになっているか確認
2. 親オブジェクトの位置が(0, 0, 0)になっているか確認
3. Conductorとの同期設定を確認

### パフォーマンスの問題
1. パルスエフェクトを無効にする
2. LineRendererの品質設定を下げる
3. マテリアルをよりシンプルなものに変更

## 次のステップ

判定ラインの実装が完了したら、以下の機能を追加することを検討：

1. **判定成功時のエフェクト**
   - 判定ラインが光る演出
   - パーティクルエフェクトの追加

2. **ビートマーカー**
   - BPMに合わせて判定ラインが脈動
   - メトロノーム的な視覚フィードバック

3. **カスタマイズ機能**
   - プレイヤーが色や透明度を調整できる設定
   - 判定ラインのスタイル選択（実線、点線、グラデーション等）

## 参考リソース

- Unity LineRenderer Documentation: https://docs.unity3d.com/Manual/class-LineRenderer.html
- Universal Render Pipeline (URP) マテリアル設定
- 奥行き型リズムゲーム実装ガイド: `docs/plans/3d-rhythm-game-guide.md`