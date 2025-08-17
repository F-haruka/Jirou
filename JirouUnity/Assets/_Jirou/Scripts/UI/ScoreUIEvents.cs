using UnityEngine;
using System;
using Jirou.Core;

namespace Jirou.UI
{
    /// <summary>
    /// スコア関連のUIイベントを管理するクラス
    /// ScoreManagerからのイベントをUIシステムに中継する
    /// </summary>
    public class ScoreUIEvents : MonoBehaviour
    {
        // UIイベント（UIコンポーネントが購読する）
        public static event Action<int> OnScoreChanged;
        public static event Action<int> OnComboChanged;
        public static event Action OnComboBreak;
        public static event Action<JudgmentType> OnJudgment;
        
        private static ScoreUIEvents instance;
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                SubscribeToScoreManager();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// ScoreManagerのイベントに購読
        /// </summary>
        private void SubscribeToScoreManager()
        {
            Debug.Log("[ScoreUIEvents] Subscribing to ScoreManager events...");
            
            // ScoreManagerの静的イベントに購読
            ScoreManager.OnScoreChanged += HandleScoreChanged;
            ScoreManager.OnComboChanged += HandleComboChanged;
            ScoreManager.OnComboBreak += HandleComboBreak;
            ScoreManager.OnJudgment += HandleJudgment;
            
            Debug.Log("[ScoreUIEvents] Successfully subscribed to all ScoreManager events");
        }
        
        /// <summary>
        /// ScoreManagerのイベント購読を解除
        /// </summary>
        private void UnsubscribeFromScoreManager()
        {
            ScoreManager.OnScoreChanged -= HandleScoreChanged;
            ScoreManager.OnComboChanged -= HandleComboChanged;
            ScoreManager.OnComboBreak -= HandleComboBreak;
            ScoreManager.OnJudgment -= HandleJudgment;
        }
        
        private void HandleScoreChanged(int newScore)
        {
            Debug.Log($"[ScoreUIEvents] HandleScoreChanged received: {newScore}");
            OnScoreChanged?.Invoke(newScore);
        }
        
        private void HandleComboChanged(int newCombo)
        {
            Debug.Log($"[ScoreUIEvents] HandleComboChanged received: {newCombo}");
            OnComboChanged?.Invoke(newCombo);
        }
        
        private void HandleComboBreak()
        {
            Debug.Log("[ScoreUIEvents] HandleComboBreak received");
            OnComboBreak?.Invoke();
        }
        
        private void HandleJudgment(JudgmentType type)
        {
            Debug.Log($"[ScoreUIEvents] HandleJudgment received: {type}");
            OnJudgment?.Invoke(type);
        }
        
        /// <summary>
        /// スコア変更を通知（後方互換性のため残す）
        /// </summary>
        public static void TriggerScoreChange(int newScore)
        {
            OnScoreChanged?.Invoke(newScore);
        }
        
        /// <summary>
        /// コンボ変更を通知（後方互換性のため残す）
        /// </summary>
        public static void TriggerComboChange(int newCombo)
        {
            Debug.Log($"[ScoreUIEvents] TriggerComboChange called with combo: {newCombo}");
            if (OnComboChanged != null)
            {
                Debug.Log($"[ScoreUIEvents] OnComboChanged has {OnComboChanged.GetInvocationList().Length} subscribers");
                OnComboChanged.Invoke(newCombo);
            }
            else
            {
                Debug.LogWarning("[ScoreUIEvents] OnComboChanged has no subscribers!");
            }
        }
        
        /// <summary>
        /// コンボブレイクを通知（後方互換性のため残す）
        /// </summary>
        public static void TriggerComboBreak()
        {
            OnComboBreak?.Invoke();
        }
        
        /// <summary>
        /// 判定結果を通知（後方互換性のため残す）
        /// </summary>
        public static void TriggerJudgment(JudgmentType type)
        {
            OnJudgment?.Invoke(type);
        }
        
        void OnDestroy()
        {
            if (instance == this)
            {
                UnsubscribeFromScoreManager();
                instance = null;
            }
        }
        
        /// <summary>
        /// 全イベントリスナーをクリア
        /// </summary>
        public static void ClearAllListeners()
        {
            OnScoreChanged = null;
            OnComboChanged = null;
            OnComboBreak = null;
            OnJudgment = null;
        }
    }
}