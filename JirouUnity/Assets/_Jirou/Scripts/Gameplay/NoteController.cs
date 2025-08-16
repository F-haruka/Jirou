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
        
        // InputManager連携用プロパティ
        public int LaneIndex => laneIndex;
        public bool IsJudged => isJudged;
        
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
            laneIndex = data.LaneIndex;  // レーンインデックスを設定
            
            // 初期状態の記録
            initialZ = transform.position.z;
            // Initialize時のlocalScaleを保持（Prefabからの値）
            // レーン幅ベースのスケーリングではY軸とZ軸のみ保持
            if (initialScale == Vector3.zero)
            {
                // Y軸とZ軸のスケールのみ保持（X軸はレーン幅で動的に設定）
                initialScale = new Vector3(1.0f, transform.localScale.y, transform.localScale.z);
            }
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
            
            // 初期スケールを記録（まだ設定されていない場合のみ）
            // レーン幅ベースのスケーリングではY軸とZ軸のみ保持
            if (initialScale == Vector3.zero)
            {
                // Y軸とZ軸のスケールのみ保持（X軸はレーン幅で動的に設定）
                initialScale = new Vector3(1.0f, transform.localScale.y, transform.localScale.z);
            }
            
            // レンダラーをキャッシュ
            renderer = GetComponent<MeshRenderer>();
            
            // Holdノーツの場合、Trailの長さを設定
            if (noteData != null && noteData.NoteType == NoteType.Hold)
            {
                SetupHoldTrail(noteData.HoldDuration);
            }
            
            // Colliderの確認と設定（JudgmentZoneとの衝突検出用）
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                // BoxColliderを追加
                var boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector3(1f, 1f, 1f);
                Debug.Log($"[NoteController] Added BoxCollider to note - Lane: {laneIndex}");
            }
            else
            {
                // 既存のColliderがTriggerであることを確認
                collider.isTrigger = true;
            }
            
            // Rigidbodyの確認と設定（トリガー検出に必要）
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;  // 物理演算は不要
                rb.useGravity = false;
                Debug.Log($"[NoteController] Added Rigidbody to note - Lane: {laneIndex}");
            }
        }
        
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
            
            // 最初のフレームだけデバッグログを出力
            if (Time.frameCount % 60 == 0) // 60フレームごとにログ出力
            {
                Debug.Log($"[NoteController] Moving - TargetBeat: {targetBeat:F2}, CurrentZ: {newZ:F2}, PerspectiveX: {perspectiveX:F2}, Active: {gameObject.activeSelf}");
            }
            
            // 3. 距離に応じたスケール変更（遠近感対応）
            UpdateScale(newZ);
            
            // 4. 判定ラインを通過したかチェック
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
        /// ノーツの状態をリセット（プール返却時用）
        /// </summary>
        public void ResetNote()
        {
            // 状態フラグをリセット
            hasBeenHit = false;
            isCompleted = false;
            isJudged = false;
            isHolding = false;
            isActive = false;
            
            // スケールをリセット（初期値をクリアして次回Initialize時に再設定）
            // レーン幅ベースのスケーリング用にリセット
            initialScale = Vector3.zero;
            
            // データ参照をクリア
            noteData = null;
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
        /// Z座標に基づいたスケール更新（遠近感対応・レーン幅対応）
        /// </summary>
        private void UpdateScale(float zPosition)
        {
            if (conductor == null) return;
            
            // 現在のZ座標でのレーン幅を取得
            float laneWidth = conductor.GetLaneWidthAtZ(zPosition);
            
            // Conductorの統一されたスケール値を取得（Y軸とZ軸用）
            float distanceScale = conductor.GetScaleAtZ(zPosition);
            
            // NoteDataのVisualScaleも考慮
            float visualScale = 1.0f;
            if (noteData != null)
            {
                visualScale = noteData.VisualScale;
            }
            
            // X軸はレーンの幅と同じ値、Y軸とZ軸は初期値に距離スケールを適用
            Vector3 newScale = new Vector3(
                laneWidth * visualScale,  // X軸はレーンの幅と同じ値に設定（VisualScaleも考慮）
                initialScale.y * distanceScale * visualScale,  // Y軸は初期値と距離スケール
                initialScale.z * distanceScale * visualScale   // Z軸は初期値と距離スケール
            );
            
            // スケールを適用
            transform.localScale = newScale;
            
            // デバッグログ（1秒に1回程度）
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[NoteController] UpdateScale - Z: {zPosition:F2}, レーン幅: {laneWidth:F2}, 距離スケール: {distanceScale:F2}, 最終スケール: {newScale}");
            }
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
        
        /// <summary>
        /// 判定処理（InputManagerから呼び出される）
        /// </summary>
        public void Judge(JudgmentType judgment)
        {
            if (isJudged) return;  // 二重判定防止
            
            isJudged = true;
            isCompleted = true;
            
            Debug.Log($"[NoteController] Judged - Lane: {laneIndex}, Judgment: {judgment}, Z: {transform.position.z:F2}");
            
            // 判定タイプに応じた処理
            switch (judgment)
            {
                case JudgmentType.Perfect:
                case JudgmentType.Great:
                case JudgmentType.Good:
                    // ヒットエフェクト生成
                    if (customHitEffect != null)
                    {
                        GameObject effect = Instantiate(customHitEffect, transform.position, Quaternion.identity);
                        Destroy(effect, 0.5f);
                    }
                    
                    // ヒットサウンド再生
                    if (customHitSound != null)
                    {
                        AudioSource.PlayClipAtPoint(customHitSound, transform.position);
                    }
                    break;
                    
                case JudgmentType.Miss:
                    // ミス処理
                    break;
            }
            
            // スコア加算（ScoreManagerが実装されたら連携）
            // ScoreManager.Instance.AddScore(judgment, noteData);
            
            // Holdノーツでない場合は即座に非表示
            if (!IsHoldNote())
            {
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Holdノーツかどうかを判定
        /// </summary>
        public bool IsHoldNote()
        {
            return noteType == NoteType.Hold || (noteData != null && noteData.NoteType == NoteType.Hold);
        }
        
        /// <summary>
        /// Hold開始処理
        /// </summary>
        public void StartHold()
        {
            if (!IsHoldNote()) return;
            
            isHolding = true;
            Debug.Log($"[NoteController] Hold started - Lane: {laneIndex}");
            
            // Hold開始エフェクト（実装予定）
            // EffectManager.Instance.PlayHoldStartEffect(transform.position);
        }
        
        /// <summary>
        /// Hold継続処理
        /// </summary>
        public void UpdateHold()
        {
            if (!isHolding) return;
            
            // Hold継続中の視覚効果更新（実装予定）
            // 例：パーティクルエフェクトの更新、トレイルの延長など
            
            // Hold進捗の計算（実装予定）
            // float progress = CalculateHoldProgress();
            // OnHoldProgress?.Invoke(progress);
        }
        
        /// <summary>
        /// Hold終了処理
        /// </summary>
        public void EndHold()
        {
            if (!isHolding) return;
            
            isHolding = false;
            isCompleted = true;
            
            Debug.Log($"[NoteController] Hold ended - Lane: {laneIndex}");
            
            // Hold成功判定（実装予定）
            // bool holdSuccess = CalculateHoldSuccess();
            // if (holdSuccess)
            // {
            //     // 成功エフェクト
            // }
            // else
            // {
            //     // 失敗エフェクト
            // }
            
            // ノーツを非表示
            gameObject.SetActive(false);
        }
    }
}