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
        
        /// <summary>
        /// Cubeの位置とスケールを設定
        /// </summary>
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
        
        // デバッグ表示
        #if UNITY_EDITOR
        void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 120, 300, 200));
            GUILayout.Label("Lane Input Feedback Debug");
            
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
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            
            for (int i = 0; i < LANE_COUNT; i++)
            {
                float laneX = conductor.LaneXPositions[i];
                float laneWidth = conductor.LaneWidth * 0.9f;
                
                Vector3 center = transform.position + new Vector3(laneX, cubeHeight / 2f, conductor.SpawnZ / 2f);
                Vector3 size = new Vector3(laneWidth, cubeHeight, conductor.SpawnZ);
                
                Gizmos.DrawWireCube(center, size);
            }
        }
        #endif
    }
}