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
        
        [Header("グラデーション設定")]
        [SerializeField] private bool enableGradient = false;
        [SerializeField] private Gradient lineGradient;
        
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
            
            // グラデーションの適用
            if (enableGradient)
            {
                ApplyGradient();
            }
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
        
        /// <summary>
        /// グラデーションを適用
        /// </summary>
        private void ApplyGradient()
        {
            if (lineGradient == null)
            {
                // デフォルトのグラデーションを作成
                lineGradient = new Gradient();
                lineGradient.SetKeys(
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
            }
            
            lineRenderer.colorGradient = lineGradient;
        }
    }
}