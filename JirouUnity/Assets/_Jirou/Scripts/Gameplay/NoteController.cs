using UnityEngine;
using Jirou.Core;

namespace Jirou.Gameplay
{
    /// <summary>
    /// 個別ノーツの動作を制御するコンポーネント
    /// </summary>
    public class NoteController : MonoBehaviour
    {
        // パブリックフィールド
        public NoteData noteData { get; private set; }
        public float moveSpeed = 10.0f;
        
        // NoteSpawnerTestSetupで使用される追加プロパティ
        public GameObject customHitEffect { get; set; }
        public AudioClip customHitSound { get; set; }
        public NoteType noteType = NoteType.Tap;
        public int laneIndex;
        public float timeToHit;
        public float holdDuration;
        public bool isActive { get; private set; }
        public bool isJudged { get; private set; }
        public bool isHolding { get; private set; }
        
        // プライベートフィールド
        private float targetBeat;
        private bool hasBeenHit;
        private Vector3 initialScale;
        private MeshRenderer renderer;
        private TrailRenderer trailRenderer;
        private Conductor conductor;
        private bool isCompleted = false;
        private float initialZ;
        
        public void Initialize(NoteData data, Conductor conductorRef)
        {
            // データの設定
            noteData = data;
            conductor = conductorRef;
            targetBeat = data.TimeToHit;
            
            // 初期状態の記録
            initialZ = transform.position.z;
            initialScale = transform.localScale;
            hasBeenHit = false;
            isCompleted = false;
            
            // レンダラーの取得とキャッシュ
            renderer = GetComponent<MeshRenderer>();
            
            // Holdノーツの場合、Trailの設定
            if (data.NoteType == NoteType.Hold)
            {
                SetupHoldTrail(data.HoldDuration);
            }
            
            Debug.Log($"[NoteController] Initialized - Lane: {data.LaneIndex}, Beat: {targetBeat:F2}, InitialZ: {initialZ:F2}");
        }
        
        void Start()
        {
            // Conductorの参照を取得（Initialize未呼び出しの場合の保険）
            if (conductor == null)
            {
                conductor = Conductor.Instance;
                Debug.Log($"[NoteController] Start - Got Conductor Instance, active: {gameObject.activeSelf}");
            }
            
            // NoteDataからtargetBeatを設定
            if (noteData != null)
            {
                targetBeat = noteData.TimeToHit;
            }
            
            // 初期スケールを記録
            initialScale = transform.localScale;
            
            // レンダラーをキャッシュ
            renderer = GetComponent<MeshRenderer>();
            
            // Holdノーツの場合、Trailの長さを設定
            if (noteData != null && noteData.NoteType == NoteType.Hold)
            {
                SetupHoldTrail(noteData.HoldDuration);
            }
        }
        
        void Update()
        {
            if (conductor == null || isCompleted) return;
            
            // 1. Z座標の更新（奥から手前へ移動）
            float newZ = conductor.GetNoteZPosition(targetBeat);
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                newZ
            );
            
            // 最初のフレームだけデバッグログを出力
            if (Time.frameCount % 60 == 0) // 60フレームごとにログ出力
            {
                Debug.Log($"[NoteController] Moving - TargetBeat: {targetBeat:F2}, CurrentZ: {newZ:F2}, Active: {gameObject.activeSelf}");
            }
            
            // 2. 距離に応じたスケール変更
            ApplyDistanceScaling(newZ);
            
            // 3. 判定ラインを通過したかチェック
            if (newZ < conductor.hitZ - 2.0f)
            {
                // ミス処理
                if (!hasBeenHit)
                {
                    OnMiss();
                }
                isCompleted = true;
            }
        }
        
        public bool IsCompleted()
        {
            return isCompleted;
        }
        
        public void OnHit()
        {
            if (hasBeenHit) return;  // 二重ヒット防止
            
            hasBeenHit = true;
            isCompleted = true;
            
            // タイミング判定を取得
            JudgmentType judgment = CheckHitTiming();
            
            // スコア加算（ScoreManagerが実装されたら連携）
            // ScoreManager.Instance.AddScore(judgment, noteData);
            
            // エフェクト生成
            if (noteData != null && noteData.CustomHitEffect != null)
            {
                Instantiate(noteData.CustomHitEffect, transform.position, Quaternion.identity);
            }
            else if (customHitEffect != null)
            {
                Instantiate(customHitEffect, transform.position, Quaternion.identity);
            }
            
            // サウンド再生
            if (noteData != null && noteData.CustomHitSound != null)
            {
                AudioSource.PlayClipAtPoint(noteData.CustomHitSound, transform.position);
            }
            else if (customHitSound != null)
            {
                AudioSource.PlayClipAtPoint(customHitSound, transform.position);
            }
            
            // ノーツを非表示にする（プーリング対応）
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// タイミング判定を返す
        /// </summary>
        public JudgmentType CheckHitTiming()
        {
            // 現在のZ座標を取得
            float currentZ = transform.position.z;
            
            // 判定ラインとの距離を計算
            float distance = Mathf.Abs(currentZ - conductor.hitZ);
            
            // 距離に応じた判定を返す
            if (distance <= 0.5f)
            {
                return JudgmentType.Perfect;  // ±0.5単位以内
            }
            else if (distance <= 1.0f)
            {
                return JudgmentType.Great;    // ±1.0単位以内
            }
            else if (distance <= 1.5f)
            {
                return JudgmentType.Good;     // ±1.5単位以内
            }
            else
            {
                return JudgmentType.Miss;     // それ以外
            }
        }
        
        /// <summary>
        /// ノーツをリセット（プール返却時）
        /// </summary>
        public void Reset()
        {
            // フラグのリセット
            isActive = false;
            isJudged = false;
            isHolding = false;
            hasBeenHit = false;
            isCompleted = false;
            
            // データのクリア
            noteData = null;
            targetBeat = 0f;
            
            // 位置とスケールのリセット
            transform.position = Vector3.zero;
            transform.localScale = Vector3.one;
            
            // Trailのリセット
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
            
            // 表示を有効化
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 距離に応じたスケール変更を適用
        /// </summary>
        private void ApplyDistanceScaling(float currentZ)
        {
            // 距離の比率を計算（0=手前、1=奥）
            float distanceRatio = Mathf.Clamp01(currentZ / conductor.spawnZ);
            
            // スケール係数を計算（手前1.5倍、奥0.5倍）
            float scaleFactor = Mathf.Lerp(1.5f, 0.5f, distanceRatio);
            
            // NoteDataのVisualScaleも考慮
            float finalScale = scaleFactor;
            if (noteData != null)
            {
                finalScale *= noteData.VisualScale;
            }
            
            // スケールを適用
            transform.localScale = initialScale * finalScale;
        }
        
        /// <summary>
        /// Holdノーツ用のTrailを設定
        /// </summary>
        private void SetupHoldTrail(float holdDuration)
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
            }
            
            // Holdの長さに応じてTrailの時間を設定
            // ビート数を秒数に変換
            float holdSeconds = holdDuration * (60.0f / conductor.songBpm);
            trailRenderer.time = holdSeconds;
            
            // Trail視覚設定
            trailRenderer.startWidth = 0.5f;
            trailRenderer.endWidth = 0.1f;
            if (renderer != null && renderer.material != null)
            {
                trailRenderer.material = renderer.material;
            }
        }
        
        /// <summary>
        /// ミス処理
        /// </summary>
        private void OnMiss()
        {
            // ミス処理
            if (noteData != null)
            {
                Debug.Log($"ノーツミス: Lane {noteData.LaneIndex}, Beat {targetBeat}");
            }
            
            // ミスエフェクト（実装予定）
            // EffectManager.Instance.PlayMissEffect(transform.position);
            
            // スコア減点（ScoreManager実装後）
            // ScoreManager.Instance.OnMiss();
        }
    }
}