using System;
using UnityEngine;
using Jirou.Core;

namespace Jirou.Gameplay
{
    /// <summary>
    /// 4レーン入力システムの中核コンポーネント
    /// キーボード入力（D、F、J、K）を検出し、ノーツの判定を管理する
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private KeyCode[] inputKeys = 
        { 
            KeyCode.D, 
            KeyCode.F, 
            KeyCode.J, 
            KeyCode.K 
        };
        
        [Header("Judgment Zones")]
        [SerializeField] private JudgmentZone[] judgmentZones = new JudgmentZone[4];
        
        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float effectDuration = 0.5f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool testModeWithoutNotes = false;  // ノーツなしでもテスト判定を表示
        [SerializeField] private Color[] laneDebugColors = 
        {
            Color.red,
            Color.yellow,
            Color.green,
            Color.blue
        };
        
        // プライベートフィールド
        private bool[] holdStates;
        private NoteController[] heldNotes;
        private int[] lastJudgmentFrame;  // 各レーンの最後の判定フレーム（連打防止用）
        
        // 定数
        private const int LANE_COUNT = 4;
        private const int JUDGMENT_COOLDOWN_FRAMES = 10;  // 判定後のクールダウン（フレーム数）
        
        // イベント
        public static event Action<int, JudgmentType> OnNoteJudged;
        public static event Action<int, float> OnHoldProgress;
        
        // シングルトンインスタンス（オプション）
        public static InputManager Instance { get; private set; }
        
        void Awake()
        {
            // シングルトン設定（オプション）
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            // 配列の初期化
            holdStates = new bool[LANE_COUNT];
            heldNotes = new NoteController[LANE_COUNT];
            lastJudgmentFrame = new int[LANE_COUNT];
            
            // コンポーネントの検証
            ValidateComponents();
        }
        
        void Update()
        {
            // 各レーンの入力を処理
            for (int i = 0; i < LANE_COUNT; i++)
            {
                ProcessLaneInput(i);
            }
        }
        
        /// <summary>
        /// 各レーンの入力処理
        /// </summary>
        private void ProcessLaneInput(int laneIndex)
        {
            // KeyDown処理（Tap判定とHold開始）
            if (Input.GetKeyDown(inputKeys[laneIndex]))
            {
                HandleKeyDown(laneIndex);
            }
            // KeyHold処理（Hold継続）
            else if (Input.GetKey(inputKeys[laneIndex]) && holdStates[laneIndex])
            {
                HandleKeyHold(laneIndex);
            }
            // KeyUp処理（Hold終了）
            else if (Input.GetKeyUp(inputKeys[laneIndex]) && holdStates[laneIndex])
            {
                HandleKeyUp(laneIndex);
            }
        }
        
        /// <summary>
        /// キー押下時の処理（Tap判定とHold開始）
        /// </summary>
        private void HandleKeyDown(int laneIndex)
        {
            // 判定ゾーンが設定されていない場合はスキップ
            if (judgmentZones[laneIndex] == null)
            {
                Debug.LogWarning($"[InputManager] JudgmentZone[{laneIndex}] is not assigned!");
                return;
            }
            
            // クールダウン中はスキップ（連打防止）
            if (Time.frameCount - lastJudgmentFrame[laneIndex] < JUDGMENT_COOLDOWN_FRAMES)
            {
                return;
            }
            
            // 最も近いノーツを取得
            NoteController closestNote = judgmentZones[laneIndex].GetClosestNote();
            
            if (closestNote == null)
            {
                Debug.Log($"[InputManager] Lane {laneIndex}: No note to hit (Key: {inputKeys[laneIndex]})");
                
                if (testModeWithoutNotes)
                {
                    // テストモード：ランダムな判定を生成
                    JudgmentType[] testJudgments = { JudgmentType.Perfect, JudgmentType.Great, JudgmentType.Good };
                    JudgmentType testJudgment = testJudgments[UnityEngine.Random.Range(0, testJudgments.Length)];
                    
                    // 最後の判定フレームを記録
                    lastJudgmentFrame[laneIndex] = Time.frameCount;
                    
                    // イベント通知（テスト判定）
                    OnNoteJudged?.Invoke(laneIndex, testJudgment);
                    
                    Debug.Log($"[InputManager] Lane {laneIndex}: Test mode - {testJudgment} event fired");
                }
                else
                {
                    // 通常モード：ミスタップとして扱う
                    JudgmentType missTap = JudgmentType.Miss;
                    
                    // 最後の判定フレームを記録
                    lastJudgmentFrame[laneIndex] = Time.frameCount;
                    
                    // イベント通知（ミスタップ）
                    OnNoteJudged?.Invoke(laneIndex, missTap);
                    
                    Debug.Log($"[InputManager] Lane {laneIndex}: Miss tap event fired");
                }
                return;
            }
            
            // 判定タイプを計算
            JudgmentType judgment = judgmentZones[laneIndex].JudgeHit(closestNote);
            
            // ノーツに判定を通知
            closestNote.Judge(judgment);
            
            // 最後の判定フレームを記録
            lastJudgmentFrame[laneIndex] = Time.frameCount;
            
            // エフェクト生成
            if (hitEffectPrefab != null && judgment != JudgmentType.Miss)
            {
                SpawnHitEffect(laneIndex, judgment);
            }
            
            // イベント通知
            OnNoteJudged?.Invoke(laneIndex, judgment);
            
            Debug.Log($"[InputManager] Lane {laneIndex}: {judgment} (Key: {inputKeys[laneIndex]})");
            
            // Holdノーツの場合、Hold状態を開始
            if (closestNote.IsHoldNote() && judgment != JudgmentType.Miss)
            {
                holdStates[laneIndex] = true;
                heldNotes[laneIndex] = closestNote;
                closestNote.StartHold();
                Debug.Log($"[InputManager] Lane {laneIndex}: Hold started");
            }
        }
        
        /// <summary>
        /// キー長押し中の処理（Hold継続）
        /// </summary>
        private void HandleKeyHold(int laneIndex)
        {
            if (heldNotes[laneIndex] == null)
            {
                holdStates[laneIndex] = false;
                return;
            }
            
            // Hold継続処理
            heldNotes[laneIndex].UpdateHold();
            
            // Hold進捗を通知（必要に応じて）
            // float progress = CalculateHoldProgress(heldNotes[laneIndex]);
            // OnHoldProgress?.Invoke(laneIndex, progress);
        }
        
        /// <summary>
        /// キー離した時の処理（Hold終了）
        /// </summary>
        private void HandleKeyUp(int laneIndex)
        {
            if (heldNotes[laneIndex] == null)
            {
                holdStates[laneIndex] = false;
                return;
            }
            
            // Hold終了処理
            heldNotes[laneIndex].EndHold();
            
            // 状態をリセット
            holdStates[laneIndex] = false;
            heldNotes[laneIndex] = null;
            
            Debug.Log($"[InputManager] Lane {laneIndex}: Hold ended");
        }
        
        /// <summary>
        /// ヒットエフェクトの生成
        /// </summary>
        private void SpawnHitEffect(int laneIndex, JudgmentType judgment)
        {
            // レーンのX座標を計算（-3, -1, 1, 3）
            float xPosition = -3f + (laneIndex * 2f);
            Vector3 effectPosition = new Vector3(xPosition, 0.5f, 0f);
            
            GameObject effect = Instantiate(hitEffectPrefab, effectPosition, Quaternion.identity);
            
            // 判定タイプに応じて色を変更（オプション）
            var renderer = effect.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color effectColor = GetJudgmentColor(judgment);
                renderer.material.color = effectColor;
            }
            
            // エフェクトを一定時間後に破棄
            Destroy(effect, effectDuration);
        }
        
        /// <summary>
        /// 判定タイプに応じた色を取得
        /// </summary>
        private Color GetJudgmentColor(JudgmentType judgment)
        {
            switch (judgment)
            {
                case JudgmentType.Perfect:
                    return Color.yellow;
                case JudgmentType.Great:
                    return Color.green;
                case JudgmentType.Good:
                    return Color.cyan;
                case JudgmentType.Miss:
                    return Color.red;
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// コンポーネントの検証
        /// </summary>
        private void ValidateComponents()
        {
            // キー設定の確認
            if (inputKeys.Length != LANE_COUNT)
            {
                Debug.LogError($"[InputManager] inputKeys length must be {LANE_COUNT}!");
            }
            
            // 判定ゾーンの確認
            for (int i = 0; i < LANE_COUNT; i++)
            {
                if (judgmentZones[i] == null)
                {
                    Debug.LogWarning($"[InputManager] JudgmentZone[{i}] is not assigned!");
                }
            }
            
            // エフェクトプレハブの確認
            if (hitEffectPrefab == null)
            {
                Debug.LogWarning("[InputManager] Hit effect prefab is not assigned!");
            }
        }
        
        /// <summary>
        /// デバッグ情報の表示（エディタ用）
        /// </summary>
        #if UNITY_EDITOR
        void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            
            // 背景ボックス
            GUI.Box(new Rect(10, 10, 650, 100), "");
            
            // タイトル
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(20, 15, 200, 30), "Input Manager Debug", style);
            
            // 各レーンの状態表示
            for (int i = 0; i < LANE_COUNT; i++)
            {
                // キーの現在の入力状態を確認
                bool isKeyPressed = Input.GetKey(inputKeys[i]);
                string status = isKeyPressed ? (holdStates[i] ? "HOLD" : "PRESSED") : "READY";
                
                int notesInZone = 0;
                
                if (judgmentZones[i] != null)
                {
                    notesInZone = judgmentZones[i].GetNotesInZoneCount();
                }
                
                // キーが押されている場合は色を変更
                if (isKeyPressed)
                {
                    style.normal.textColor = Color.yellow;
                }
                else
                {
                    style.normal.textColor = laneDebugColors[i];
                }
                
                GUI.Label(
                    new Rect(20 + i * 150, 50, 140, 30),
                    $"Lane {i} [{inputKeys[i]}]",
                    style
                );
                
                // ステータスの色設定
                if (isKeyPressed)
                {
                    style.normal.textColor = holdStates[i] ? Color.cyan : Color.green;
                }
                else
                {
                    style.normal.textColor = Color.white;
                }
                
                GUI.Label(
                    new Rect(20 + i * 150, 75, 140, 30),
                    $"{status} | Notes: {notesInZone}",
                    style
                );
            }
        }
        #endif
        
        /// <summary>
        /// 手動で判定ゾーンを設定（テスト用）
        /// </summary>
        public void SetJudgmentZone(int laneIndex, JudgmentZone zone)
        {
            if (laneIndex >= 0 && laneIndex < LANE_COUNT)
            {
                judgmentZones[laneIndex] = zone;
                Debug.Log($"[InputManager] JudgmentZone set for lane {laneIndex}");
            }
        }
        
        /// <summary>
        /// リセット処理
        /// </summary>
        public void ResetInputManager()
        {
            // 全レーンの状態をリセット
            for (int i = 0; i < LANE_COUNT; i++)
            {
                holdStates[i] = false;
                heldNotes[i] = null;
                lastJudgmentFrame[i] = 0;
                
                if (judgmentZones[i] != null)
                {
                    judgmentZones[i].ResetZone();
                }
            }
            
            Debug.Log("[InputManager] Reset completed");
        }
        
        void OnDestroy()
        {
            // シングルトンのクリーンアップ
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}