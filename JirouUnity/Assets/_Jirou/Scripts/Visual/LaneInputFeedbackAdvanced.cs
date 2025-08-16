using System.Collections;
using UnityEngine;
using Jirou.Core;
using Jirou.Gameplay;

namespace Jirou.Visual
{
    /// <summary>
    /// レーン入力時の視覚的フィードバックを管理するコンポーネント（高度版）
    /// 遠近感対応の台形Meshを使用してより立体的な表現を実現
    /// </summary>
    public class LaneInputFeedbackAdvanced : MonoBehaviour
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
        
        [Tooltip("台形Meshを使用（遠近感対応）")]
        [SerializeField] private bool useTrapezoidMesh = true;
        
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
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
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
            if (ValidateComponents())
            {
                CreateFeedbackCubes();
                SetupMaterials();
            }
        }
        
        // 更新処理（キー入力監視）
        void Update()
        {
            if (conductor != null)
            {
                ProcessKeyInputs();
            }
        }
        
        /// <summary>
        /// コンポーネントの初期化
        /// </summary>
        private void InitializeComponents()
        {
            // Conductorの取得
            conductor = Conductor.Instance;
            
            // LaneVisualizerの取得（同じ階層から探す）
            laneVisualizer = transform.parent?.GetComponentInChildren<LaneVisualizer>();
            
            // 配列の初期化
            activeCoroutines = new Coroutine[LANE_COUNT];
            isHolding = new bool[LANE_COUNT];
        }
        
        /// <summary>
        /// 必要なコンポーネントの検証
        /// </summary>
        private bool ValidateComponents()
        {
            // Conductor存在確認
            if (conductor == null)
            {
                Debug.LogError("[LaneInputFeedback] Conductor not found!");
                return false;
            }
            
            // LaneVisualizer確認（オプション）
            if (laneVisualizer == null)
            {
                Debug.LogWarning("[LaneInputFeedback] LaneVisualizer not found. Some features may be limited.");
            }
            
            return true;
        }
        
        /// <summary>
        /// フィードバックCubeの生成
        /// </summary>
        private void CreateFeedbackCubes()
        {
            feedbackCubes = new GameObject[LANE_COUNT];
            cubeRenderers = new MeshRenderer[LANE_COUNT];
            
            GameObject container = new GameObject("FeedbackCubes");
            container.transform.SetParent(transform);
            container.transform.localPosition = Vector3.zero;
            
            for (int i = 0; i < LANE_COUNT; i++)
            {
                if (useTrapezoidMesh)
                {
                    // カスタム台形Meshを作成
                    CreateTrapezoidFeedback(i, container);
                }
                else
                {
                    // 通常のCubeを作成
                    CreateCubeFeedback(i, container);
                }
            }
        }
        
        /// <summary>
        /// 通常のCubeフィードバックを作成
        /// </summary>
        private void CreateCubeFeedback(int laneIndex, GameObject container)
        {
            // Cubeオブジェクトの作成
            feedbackCubes[laneIndex] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            feedbackCubes[laneIndex].name = $"LaneFeedback_{laneIndex}";
            feedbackCubes[laneIndex].transform.SetParent(container.transform);
            
            // Colliderを無効化（判定に影響しないように）
            Destroy(feedbackCubes[laneIndex].GetComponent<Collider>());
            
            // Rendererの取得
            cubeRenderers[laneIndex] = feedbackCubes[laneIndex].GetComponent<MeshRenderer>();
            
            // 位置とスケールの設定
            ConfigureCubeTransform(laneIndex);
        }
        
        /// <summary>
        /// 台形Meshフィードバックを作成
        /// </summary>
        private void CreateTrapezoidFeedback(int laneIndex, GameObject container)
        {
            // GameObjectの作成
            feedbackCubes[laneIndex] = new GameObject($"LaneFeedback_{laneIndex}");
            feedbackCubes[laneIndex].transform.SetParent(container.transform);
            feedbackCubes[laneIndex].transform.localPosition = Vector3.zero;
            
            // MeshFilterとMeshRendererを追加
            MeshFilter meshFilter = feedbackCubes[laneIndex].AddComponent<MeshFilter>();
            cubeRenderers[laneIndex] = feedbackCubes[laneIndex].AddComponent<MeshRenderer>();
            
            // 台形Meshを作成
            CreateTrapezoidMesh(laneIndex, meshFilter);
        }
        
        /// <summary>
        /// 台形Meshの生成
        /// </summary>
        private void CreateTrapezoidMesh(int laneIndex, MeshFilter meshFilter)
        {
            Mesh trapezoidMesh = new Mesh();
            trapezoidMesh.name = $"LaneTrapezoid_{laneIndex}";
            
            // レーンの境界線を計算（LaneVisualizerと同じロジック）
            float leftBoundary, rightBoundary;
            float[] lanePositions = conductor.LaneXPositions;
            
            if (laneIndex == 0)
            {
                // レーン0の左端
                leftBoundary = lanePositions[0] - conductor.LaneWidth;
                rightBoundary = (lanePositions[0] + lanePositions[1]) / 2f;
            }
            else if (laneIndex == lanePositions.Length - 1)
            {
                // 最後のレーンの右端
                leftBoundary = (lanePositions[laneIndex - 1] + lanePositions[laneIndex]) / 2f;
                rightBoundary = lanePositions[laneIndex] + conductor.LaneWidth;
            }
            else
            {
                // 中間のレーン
                leftBoundary = (lanePositions[laneIndex - 1] + lanePositions[laneIndex]) / 2f;
                rightBoundary = (lanePositions[laneIndex] + lanePositions[laneIndex + 1]) / 2f;
            }
            
            // 少し内側にマージンを設ける（オプション）
            float margin = 0.05f; // 5%のマージン
            float width = rightBoundary - leftBoundary;
            leftBoundary += width * margin;
            rightBoundary -= width * margin;
            
            // 遠近感スケールの取得
            float nearScale = conductor.PerspectiveNearScale;
            float farScale = conductor.PerspectiveFarScale;
            
            // 頂点座標の計算（台形状）
            Vector3[] vertices = new Vector3[]
            {
                // 手前の頂点（判定ライン側、Z=0）- 底面
                new Vector3(leftBoundary * nearScale, 0, 0),
                new Vector3(rightBoundary * nearScale, 0, 0),
                new Vector3(rightBoundary * nearScale, cubeHeight, 0),
                new Vector3(leftBoundary * nearScale, cubeHeight, 0),
                
                // 奥の頂点（スポーン側、Z=SpawnZ）- 底面
                new Vector3(leftBoundary * farScale, 0, conductor.SpawnZ),
                new Vector3(rightBoundary * farScale, 0, conductor.SpawnZ),
                new Vector3(rightBoundary * farScale, cubeHeight, conductor.SpawnZ),
                new Vector3(leftBoundary * farScale, cubeHeight, conductor.SpawnZ)
            };
            
            // 三角形インデックス（時計回りで表面を定義）
            int[] triangles = new int[]
            {
                // 上面（Y+方向）
                3, 2, 6,  3, 6, 7,
                // 底面（Y-方向）
                0, 4, 5,  0, 5, 1,
                // 前面（Z-方向、判定ライン側）
                0, 1, 2,  0, 2, 3,
                // 背面（Z+方向、スポーン側）
                5, 4, 7,  5, 7, 6,
                // 左面（X-方向）
                4, 0, 3,  4, 3, 7,
                // 右面（X+方向）
                1, 5, 6,  1, 6, 2
            };
            
            // UV座標（各頂点に対応）
            Vector2[] uv = new Vector2[]
            {
                // 手前の頂点
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(1, 1), new Vector2(0, 1),
                // 奥の頂点
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(1, 1), new Vector2(0, 1)
            };
            
            // 法線ベクトル（各頂点に対応）
            Vector3[] normals = new Vector3[]
            {
                // 手前の頂点（前向き）
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                // 奥の頂点（前向き）
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward
            };
            
            // Meshの構築
            trapezoidMesh.vertices = vertices;
            trapezoidMesh.triangles = triangles;
            trapezoidMesh.uv = uv;
            trapezoidMesh.normals = normals;
            trapezoidMesh.RecalculateTangents();
            trapezoidMesh.RecalculateBounds();
            
            // 法線を再計算してライティングを正しくする
            trapezoidMesh.RecalculateNormals();
            
            meshFilter.mesh = trapezoidMesh;
        }
        
        /// <summary>
        /// Cubeの位置とスケールを設定
        /// </summary>
        private void ConfigureCubeTransform(int laneIndex)
        {
            if (conductor == null) return;
            
            GameObject cube = feedbackCubes[laneIndex];
            float[] lanePositions = conductor.LaneXPositions;
            
            // レーンの境界線を計算（LaneVisualizerと同じロジック）
            float leftBoundary, rightBoundary;
            
            if (laneIndex == 0)
            {
                // レーン0の左端
                leftBoundary = lanePositions[0] - conductor.LaneWidth;
                rightBoundary = (lanePositions[0] + lanePositions[1]) / 2f;
            }
            else if (laneIndex == lanePositions.Length - 1)
            {
                // 最後のレーンの右端
                leftBoundary = (lanePositions[laneIndex - 1] + lanePositions[laneIndex]) / 2f;
                rightBoundary = lanePositions[laneIndex] + conductor.LaneWidth;
            }
            else
            {
                // 中間のレーン
                leftBoundary = (lanePositions[laneIndex - 1] + lanePositions[laneIndex]) / 2f;
                rightBoundary = (lanePositions[laneIndex] + lanePositions[laneIndex + 1]) / 2f;
            }
            
            // 少し内側にマージンを設ける（オプション）
            float margin = 0.05f; // 5%のマージン
            float width = rightBoundary - leftBoundary;
            leftBoundary += width * margin;
            rightBoundary -= width * margin;
            
            // レーンの中央と幅を計算
            float laneCenter = (leftBoundary + rightBoundary) / 2f;
            float laneWidth = rightBoundary - leftBoundary;
            
            // 位置設定（レーンの中央、Z軸は中間点）
            float zCenter = conductor.SpawnZ / 2f;
            cube.transform.localPosition = new Vector3(laneCenter, cubeHeight / 2f, zCenter);
            
            // スケール設定（レーン幅、高さ、レーン長）
            cube.transform.localScale = new Vector3(laneWidth, cubeHeight, conductor.SpawnZ);
        }
        
        /// <summary>
        /// Materialの設定
        /// </summary>
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
                
                // 両面描画を有効化（台形Meshの場合）
                if (useTrapezoidMesh)
                {
                    mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
                }
                
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
        
        /// <summary>
        /// キー入力の処理
        /// </summary>
        private void ProcessKeyInputs()
        {
            for (int i = 0; i < LANE_COUNT; i++)
            {
                // キー押下検出
                if (Input.GetKeyDown(laneKeys[i]))
                {
                    TriggerFeedback(i);
                }
                
                // キー長押し検出
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
        
        /// <summary>
        /// フィードバックをトリガー
        /// </summary>
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
        
        /// <summary>
        /// フィードバックのアニメーション
        /// </summary>
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
        
        /// <summary>
        /// ホールドフィードバック開始
        /// </summary>
        private void StartHoldFeedback(int laneIndex)
        {
            if (activeCoroutines[laneIndex] != null)
            {
                StopCoroutine(activeCoroutines[laneIndex]);
            }
            
            activeCoroutines[laneIndex] = StartCoroutine(AnimateHoldFeedback(laneIndex));
        }
        
        /// <summary>
        /// ホールドフィードバック停止
        /// </summary>
        private void StopHoldFeedback(int laneIndex)
        {
            if (activeCoroutines[laneIndex] != null)
            {
                StopCoroutine(activeCoroutines[laneIndex]);
                activeCoroutines[laneIndex] = null;
            }
            
            // マテリアルを元に戻す
            Material mat = cubeMaterials[laneIndex];
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            
            Color baseColor = laneColors[laneIndex];
            baseColor.a = normalAlpha;
            mat.SetColor("_BaseColor", baseColor);
        }
        
        /// <summary>
        /// ホールド中のアニメーション
        /// </summary>
        private IEnumerator AnimateHoldFeedback(int laneIndex)
        {
            Material mat = cubeMaterials[laneIndex];
            mat.EnableKeyword("_EMISSION");
            
            while (isHolding[laneIndex])
            {
                // パルスアニメーション
                float pulse = Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f;
                float intensity = Mathf.Lerp(1f, feedbackIntensity, pulse);
                
                Color emissionColor = laneColors[laneIndex] * Mathf.LinearToGammaSpace(intensity);
                mat.SetColor("_EmissionColor", emissionColor);
                
                Color baseColor = laneColors[laneIndex];
                baseColor.a = Mathf.Lerp(0.1f, 0.4f, pulse);
                mat.SetColor("_BaseColor", baseColor);
                
                yield return null;
            }
        }
        
        // イベント購読の設定
        void OnEnable()
        {
            // InputManagerのイベントを購読（オプション）
            if (InputManager.Instance != null)
            {
                InputManager.OnNoteJudged += HandleNoteJudged;
            }
        }
        
        void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.OnNoteJudged -= HandleNoteJudged;
            }
            
            // すべてのコルーチンを停止
            for (int i = 0; i < LANE_COUNT; i++)
            {
                if (activeCoroutines[i] != null)
                {
                    StopCoroutine(activeCoroutines[i]);
                    activeCoroutines[i] = null;
                }
            }
        }
        
        /// <summary>
        /// ノート判定時の特別なフィードバック
        /// </summary>
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
        
        /// <summary>
        /// 判定タイプに応じた特別なフィードバック
        /// </summary>
        private void TriggerSpecialFeedback(int laneIndex, float intensity, Color color)
        {
            if (activeCoroutines[laneIndex] != null)
            {
                StopCoroutine(activeCoroutines[laneIndex]);
            }
            
            activeCoroutines[laneIndex] = StartCoroutine(AnimateSpecialFeedback(laneIndex, intensity, color));
        }
        
        /// <summary>
        /// 特別なフィードバックのアニメーション
        /// </summary>
        private IEnumerator AnimateSpecialFeedback(int laneIndex, float intensity, Color color)
        {
            Material mat = cubeMaterials[laneIndex];
            float elapsed = 0f;
            float duration = feedbackDuration * 1.5f; // 少し長めに
            
            mat.EnableKeyword("_EMISSION");
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float curveValue = feedbackCurve.Evaluate(t);
                
                // 判定色と元の色をブレンド
                Color blendedColor = Color.Lerp(laneColors[laneIndex], color, curveValue * 0.5f);
                Color emissionColor = blendedColor * Mathf.LinearToGammaSpace(intensity * curveValue);
                mat.SetColor("_EmissionColor", emissionColor);
                
                Color baseColor = blendedColor;
                baseColor.a = Mathf.Lerp(normalAlpha, 0.5f, curveValue);
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
        
        /// <summary>
        /// レーン設定の動的更新（Conductorから）
        /// </summary>
        public void RefreshLaneConfiguration()
        {
            if (conductor == null) return;
            
            // 台形Meshを使用している場合は再生成
            if (useTrapezoidMesh)
            {
                for (int i = 0; i < LANE_COUNT; i++)
                {
                    MeshFilter meshFilter = feedbackCubes[i].GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        CreateTrapezoidMesh(i, meshFilter);
                    }
                }
            }
            else
            {
                // 通常のCubeの場合は位置とスケールを更新
                for (int i = 0; i < LANE_COUNT; i++)
                {
                    ConfigureCubeTransform(i);
                }
            }
        }
        
        // デバッグ表示
        #if UNITY_EDITOR
        void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 120, 350, 250));
            GUILayout.Label("Lane Input Feedback Debug (Advanced)");
            
            GUILayout.Label($"Mode: {(useTrapezoidMesh ? "Trapezoid" : "Cube")}");
            
            for (int i = 0; i < LANE_COUNT; i++)
            {
                string status = isHolding[i] ? "HOLDING" : "READY";
                bool hasCoroutine = activeCoroutines[i] != null;
                string keyName = laneKeys[i].ToString();
                
                GUILayout.Label($"Lane {i} ({keyName}): {status} | Active: {hasCoroutine}");
            }
            
            if (conductor != null)
            {
                GUILayout.Label($"Conductor: Active");
                GUILayout.Label($"SpawnZ: {conductor.SpawnZ:F2}");
                GUILayout.Label($"Near Scale: {conductor.PerspectiveNearScale:F2}");
                GUILayout.Label($"Far Scale: {conductor.PerspectiveFarScale:F2}");
            }
            else
            {
                GUILayout.Label("Conductor: Not Found");
            }
            
            GUILayout.EndArea();
        }
        
        void OnDrawGizmos()
        {
            if (!showDebugInfo || conductor == null) return;
            
            // レーン位置のプレビュー
            if (useTrapezoidMesh)
            {
                // 台形のワイヤーフレーム表示
                Gizmos.color = new Color(1, 1, 0, 0.3f);
                
                for (int i = 0; i < LANE_COUNT; i++)
                {
                    float[] lanePositions = conductor.LaneXPositions;
                    float leftBoundary, rightBoundary;
                    
                    // レーンの境界線を計算（LaneVisualizerと同じロジック）
                    if (i == 0)
                    {
                        leftBoundary = lanePositions[0] - conductor.LaneWidth;
                        rightBoundary = (lanePositions[0] + lanePositions[1]) / 2f;
                    }
                    else if (i == lanePositions.Length - 1)
                    {
                        leftBoundary = (lanePositions[i - 1] + lanePositions[i]) / 2f;
                        rightBoundary = lanePositions[i] + conductor.LaneWidth;
                    }
                    else
                    {
                        leftBoundary = (lanePositions[i - 1] + lanePositions[i]) / 2f;
                        rightBoundary = (lanePositions[i] + lanePositions[i + 1]) / 2f;
                    }
                    
                    // マージンを適用
                    float margin = 0.05f;
                    float width = rightBoundary - leftBoundary;
                    leftBoundary += width * margin;
                    rightBoundary -= width * margin;
                    
                    float nearScale = conductor.PerspectiveNearScale;
                    float farScale = conductor.PerspectiveFarScale;
                    
                    Vector3 basePos = transform.position;
                    
                    // 台形の8頂点
                    Vector3[] corners = new Vector3[]
                    {
                        // 手前（判定ライン側）
                        basePos + new Vector3(leftBoundary * nearScale, 0, 0),
                        basePos + new Vector3(rightBoundary * nearScale, 0, 0),
                        basePos + new Vector3(rightBoundary * nearScale, cubeHeight, 0),
                        basePos + new Vector3(leftBoundary * nearScale, cubeHeight, 0),
                        // 奥（スポーン側）
                        basePos + new Vector3(leftBoundary * farScale, 0, conductor.SpawnZ),
                        basePos + new Vector3(rightBoundary * farScale, 0, conductor.SpawnZ),
                        basePos + new Vector3(rightBoundary * farScale, cubeHeight, conductor.SpawnZ),
                        basePos + new Vector3(leftBoundary * farScale, cubeHeight, conductor.SpawnZ)
                    };
                    
                    // 台形のエッジを描画
                    // 前面
                    Gizmos.DrawLine(corners[0], corners[1]);
                    Gizmos.DrawLine(corners[1], corners[2]);
                    Gizmos.DrawLine(corners[2], corners[3]);
                    Gizmos.DrawLine(corners[3], corners[0]);
                    // 背面
                    Gizmos.DrawLine(corners[4], corners[5]);
                    Gizmos.DrawLine(corners[5], corners[6]);
                    Gizmos.DrawLine(corners[6], corners[7]);
                    Gizmos.DrawLine(corners[7], corners[4]);
                    // 接続線
                    Gizmos.DrawLine(corners[0], corners[4]);
                    Gizmos.DrawLine(corners[1], corners[5]);
                    Gizmos.DrawLine(corners[2], corners[6]);
                    Gizmos.DrawLine(corners[3], corners[7]);
                }
            }
            else
            {
                // 通常のCubeプレビュー
                Gizmos.color = new Color(1, 1, 0, 0.3f);
                
                for (int i = 0; i < LANE_COUNT; i++)
                {
                    float[] lanePositions = conductor.LaneXPositions;
                    float leftBoundary, rightBoundary;
                    
                    // レーンの境界線を計算（LaneVisualizerと同じロジック）
                    if (i == 0)
                    {
                        leftBoundary = lanePositions[0] - conductor.LaneWidth;
                        rightBoundary = (lanePositions[0] + lanePositions[1]) / 2f;
                    }
                    else if (i == lanePositions.Length - 1)
                    {
                        leftBoundary = (lanePositions[i - 1] + lanePositions[i]) / 2f;
                        rightBoundary = lanePositions[i] + conductor.LaneWidth;
                    }
                    else
                    {
                        leftBoundary = (lanePositions[i - 1] + lanePositions[i]) / 2f;
                        rightBoundary = (lanePositions[i] + lanePositions[i + 1]) / 2f;
                    }
                    
                    // マージンを適用
                    float margin = 0.05f;
                    float width = rightBoundary - leftBoundary;
                    leftBoundary += width * margin;
                    rightBoundary -= width * margin;
                    
                    float laneCenter = (leftBoundary + rightBoundary) / 2f;
                    float laneWidth = rightBoundary - leftBoundary;
                    
                    Vector3 center = transform.position + new Vector3(laneCenter, cubeHeight / 2f, conductor.SpawnZ / 2f);
                    Vector3 size = new Vector3(laneWidth, cubeHeight, conductor.SpawnZ);
                    
                    Gizmos.DrawWireCube(center, size);
                }
            }
        }
        #endif
    }
}