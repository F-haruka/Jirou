using UnityEngine;
using Jirou.Core;

namespace Jirou.Visual
{
    /// <summary>
    /// 奥行き感のあるレーンを表示するビジュアライザー
    /// Conductorと連携して自動的にレーン長を調整
    /// </summary>
    public class LaneVisualizer : MonoBehaviour
    {
        // パブリックフィールド
        [Header("レーン設定")]
        [Tooltip("レーンの数")]
        [SerializeField] private int _laneCount = 4;
        public int laneCount
        {
            get => _laneCount;
            set
            {
                _laneCount = Mathf.Clamp(value, 1, 8);
                if (Application.isPlaying && laneContainer != null)
                {
                    UpdateLanes();
                }
            }
        }
        
        [Tooltip("レーン間の基準幅")]
        [SerializeField] private float _laneWidth = 2.0f;
        public float laneWidth
        {
            get => _laneWidth;
            set
            {
                _laneWidth = Mathf.Max(0.1f, value);
                if (Application.isPlaying && laneContainer != null)
                {
                    UpdateLanes();
                }
            }
        }
        
        [Header("奥行き設定")]
        [Tooltip("手前（判定ライン）でのレーン幅")]
        [SerializeField] private float _nearWidth = 2.0f;
        public float nearWidth
        {
            get => _nearWidth;
            set
            {
                _nearWidth = Mathf.Max(0.1f, value);
                // farWidthより大きくなるようにする（手前が広い）
                if (_nearWidth < _farWidth)
                {
                    _farWidth = _nearWidth;
                }
                if (Application.isPlaying && laneContainer != null)
                {
                    UpdateLanes();
                }
            }
        }
        
        [Tooltip("奥（スポーン地点）でのレーン幅")]
        [SerializeField] private float _farWidth = 0.5f;
        public float farWidth
        {
            get => _farWidth;
            set
            {
                _farWidth = Mathf.Max(0.1f, value);
                // nearWidthより小さくなるようにする（奥が狭い）
                if (_farWidth > _nearWidth)
                {
                    _farWidth = _nearWidth;
                }
                if (Application.isPlaying && laneContainer != null)
                {
                    UpdateLanes();
                }
            }
        }
        
        [Tooltip("レーンの長さ（Z軸方向）")]
        [SerializeField] private float _laneLength = 20.0f;
        public float laneLength
        {
            get => _laneLength;
            set
            {
                _laneLength = Mathf.Max(1.0f, value);
                if (Application.isPlaying && laneContainer != null)
                {
                    UpdateLanes();
                }
            }
        }
        
        [Header("ビジュアル設定")]
        [Tooltip("レーンのマテリアル")]
        public Material laneMaterial;
        
        [Tooltip("レーンラインの太さ")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _lineWidth = 0.05f;
        public float lineWidth
        {
            get => _lineWidth;
            set
            {
                _lineWidth = Mathf.Clamp(value, 0.01f, 0.5f);
                if (Application.isPlaying && laneContainer != null)
                {
                    ApplyVisualsToLanes();
                }
            }
        }
        
        [Tooltip("レーンの色")]
        public Color laneColor = new Color(1f, 1f, 1f, 0.3f);
        
        [Header("オプション設定")]
        [Tooltip("中央ラインを表示")]
        public bool showCenterLine = true;
        
        [Tooltip("外枠を表示")]
        public bool showOuterBorders = true;
        
        [Header("Conductor連携")]
        [Tooltip("Conductorと自動同期する")]
        public bool syncWithConductor = true;
        
        [Tooltip("同期更新の間隔（秒）")]
        [Range(0.1f, 5.0f)]
        public float syncUpdateInterval = 1.0f;
        
        // プライベートフィールド
        private LineRenderer[] laneRenderers;
        private GameObject laneContainer;
        private Conductor conductor;
        private float lastSyncTime;

        /// <summary>
        /// 初期化処理
        /// </summary>
        void Start()
        {
            ValidateSettings();
            InitializeConductor();
            InitializeLanes();
        }

        /// <summary>
        /// 更新処理（Conductor同期用）
        /// </summary>
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

        /// <summary>
        /// レーンコンテナオブジェクトを作成
        /// </summary>
        private void CreateLaneContainer()
        {
            if (laneContainer != null)
            {
                DestroyImmediate(laneContainer);
            }
            
            laneContainer = new GameObject("LaneContainer");
            laneContainer.transform.SetParent(transform);
            laneContainer.transform.localPosition = Vector3.zero;
            laneContainer.transform.localRotation = Quaternion.identity;
            laneContainer.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 必要なライン総数を計算
        /// </summary>
        private int CalculateTotalLines()
        {
            int lines = 0;
            
            // 外枠（左右2本）
            if (showOuterBorders)
            {
                lines += 2;
            }
            
            // レーン区切り線（レーン数-1本）
            lines += laneCount - 1;
            
            // 中央ライン（1本）
            if (showCenterLine && laneCount % 2 == 0)
            {
                lines += 1;
            }
            
            return lines;
        }

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
            if (showCenterLine && laneCount % 2 == 0)
            {
                CreateCenterLine(ref lineIndex);
            }
        }

        /// <summary>
        /// 外枠ラインを作成
        /// </summary>
        private void CreateBorderLines(ref int lineIndex)
        {
            // 左側の外枠
            float leftX = -laneCount * laneWidth / 2.0f;
            Vector3 leftNear = new Vector3(leftX * nearWidth, 0, 0);
            Vector3 leftFar = new Vector3(leftX * farWidth, 0, laneLength);
            laneRenderers[lineIndex] = CreateSingleLine("LeftBorder", leftNear, leftFar);
            lineIndex++;
            
            // 右側の外枠
            float rightX = laneCount * laneWidth / 2.0f;
            Vector3 rightNear = new Vector3(rightX * nearWidth, 0, 0);
            Vector3 rightFar = new Vector3(rightX * farWidth, 0, laneLength);
            laneRenderers[lineIndex] = CreateSingleLine("RightBorder", rightNear, rightFar);
            lineIndex++;
        }

        /// <summary>
        /// レーン区切り線を作成
        /// </summary>
        private void CreateDividerLines(ref int lineIndex)
        {
            for (int i = 0; i < laneCount - 1; i++)
            {
                float x = CalculateDividerX(i);
                Vector3 nearPoint = new Vector3(x * nearWidth, 0, 0);
                Vector3 farPoint = new Vector3(x * farWidth, 0, laneLength);
                
                string lineName = $"Divider_{i}";
                laneRenderers[lineIndex] = CreateSingleLine(lineName, nearPoint, farPoint);
                lineIndex++;
            }
        }

        /// <summary>
        /// 中央ラインを作成
        /// </summary>
        private void CreateCenterLine(ref int lineIndex)
        {
            Vector3 nearPoint = new Vector3(0, 0, 0);
            Vector3 farPoint = new Vector3(0, 0, laneLength);
            laneRenderers[lineIndex] = CreateSingleLine("CenterLine", nearPoint, farPoint);
        }

        /// <summary>
        /// 個別のラインを作成
        /// </summary>
        private LineRenderer CreateSingleLine(string lineName, Vector3 nearPoint, Vector3 farPoint)
        {
            GameObject lineObject = new GameObject(lineName);
            lineObject.transform.SetParent(laneContainer.transform);
            lineObject.transform.localPosition = Vector3.zero;
            
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

        /// <summary>
        /// レーンのX座標を計算
        /// </summary>
        public float CalculateLaneX(int laneIndex, bool isNear)
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
        /// 区切り線のX座標を計算
        /// </summary>
        private float CalculateDividerX(int dividerIndex)
        {
            // レーン間の区切り線の位置
            float totalWidth = laneCount * laneWidth;
            float startX = -totalWidth / 2.0f;
            return startX + (dividerIndex + 1) * laneWidth;
        }

        /// <summary>
        /// マテリアルと色をレーンに適用
        /// </summary>
        private void ApplyVisualsToLanes()
        {
            // マテリアルが未設定の場合はデフォルトを作成
            if (laneMaterial == null)
            {
                Debug.LogWarning("レーンマテリアルが未設定です。デフォルトマテリアルを使用します。");
                laneMaterial = CreateDefaultMaterial();
            }
            
            // レンダラーが初期化されていない場合はスキップ
            if (laneRenderers == null)
            {
                return;
            }
            
            // 各レンダラーにマテリアルと色を適用
            foreach (var renderer in laneRenderers)
            {
                if (renderer != null)
                {
                    renderer.material = laneMaterial;
                    renderer.startColor = laneColor;
                    renderer.endColor = laneColor;
                }
            }
        }

        /// <summary>
        /// デフォルトマテリアルの作成
        /// </summary>
        private Material CreateDefaultMaterial()
        {
            // Sprites/Defaultシェーダーを使用
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            
            Material mat = new Material(shader);
            
            // 半透明設定
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000; // Transparent
            
            return mat;
        }

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
        /// レーンをクリア
        /// </summary>
        private void ClearLanes()
        {
            if (laneContainer != null)
            {
                DestroyImmediate(laneContainer);
                laneContainer = null;
            }
            
            laneRenderers = null;
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

        /// <summary>
        /// Conductorインスタンスの初期化と連携設定
        /// </summary>
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

        /// <summary>
        /// Conductorの設定と同期
        /// </summary>
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

        /// <summary>
        /// 手動でConductorと同期
        /// </summary>
        public void ForceSync()
        {
            if (conductor == null)
            {
                conductor = Conductor.Instance;
            }
            
            if (conductor != null)
            {
                SyncWithConductor();
                Debug.Log("[LaneVisualizer] 手動同期を実行しました");
            }
        }

        /// <summary>
        /// エラーハンドリング付きの検証
        /// </summary>
        private void ValidateSettings()
        {
            // 不正なレーン数のチェック
            if (_laneCount < 1 || _laneCount > 8)
            {
                Debug.LogError($"不正なレーン数: {_laneCount}。デフォルト値4を使用します。");
                _laneCount = 4;
            }
            
            // 幅の逆転チェック
            if (_farWidth > _nearWidth)
            {
                Debug.LogWarning("奥の幅が手前より大きくなっています。値を入れ替えます。");
                float temp = _farWidth;
                _farWidth = _nearWidth;
                _nearWidth = temp;
            }
        }

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
                float x = -laneCount * laneWidth / 2.0f + i * laneWidth;
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

        /// <summary>
        /// コンポーネント破棄時の処理
        /// </summary>
        void OnDestroy()
        {
            ClearLanes();
        }
    }
}