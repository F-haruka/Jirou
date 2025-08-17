using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Jirou.Core;

namespace Jirou.UI
{
    /// <summary>
    /// スコア表示UI全体を管理するマネージャークラス
    /// スコア、判定カウント、コンボの表示を統括する
    /// </summary>
    public class ScoreUIManager : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private ScoreDisplay scoreDisplay;

        [Header("Judgment Counts")]
        [SerializeField] private TextMeshProUGUI perfectCountText;
        [SerializeField] private TextMeshProUGUI greatCountText;
        [SerializeField] private TextMeshProUGUI goodCountText;
        [SerializeField] private TextMeshProUGUI missCountText;
        [SerializeField] private JudgmentCountDisplay judgmentCountDisplay;

        [Header("Combo Display")]
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private GameObject comboContainer;
        [SerializeField] private ComboDisplay comboDisplay;

        [Header("Effect Controllers")]
        [SerializeField] private ComboEffectController comboEffectController;

        [Header("Settings")]
        [SerializeField] private bool useAnimations = true;
        [SerializeField] private float comboHideThreshold = 0f;

        // 内部状態
        private int currentScore;
        private Dictionary<JudgmentType, int> judgmentCounts;
        private int currentCombo;
        private ScoreManager scoreManager;

        void Awake()
        {
            Debug.Log("[ScoreUIManager] Awake called");
            InitializeComponents();
        }

        void Start()
        {
            Debug.Log("[ScoreUIManager] Start called");
            InitializeUI();
            SubscribeToEvents();
            FindScoreManager();
            Debug.Log($"[ScoreUIManager] Initialization complete. comboContainer: {comboContainer}, comboText: {comboText}");
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// コンポーネントの初期化
        /// </summary>
        private void InitializeComponents()
        {
            judgmentCounts = new Dictionary<JudgmentType, int>
            {
                { JudgmentType.Perfect, 0 },
                { JudgmentType.Great, 0 },
                { JudgmentType.Good, 0 },
                { JudgmentType.Miss, 0 }
            };

            // 子コンポーネントが未設定の場合は自動取得を試みる
            if (scoreDisplay == null)
                scoreDisplay = GetComponentInChildren<ScoreDisplay>();
            if (judgmentCountDisplay == null)
                judgmentCountDisplay = GetComponentInChildren<JudgmentCountDisplay>();
            if (comboDisplay == null)
                comboDisplay = GetComponentInChildren<ComboDisplay>();
            if (comboEffectController == null)
                comboEffectController = GetComponentInChildren<ComboEffectController>();
        }

        /// <summary>
        /// UIの初期化
        /// </summary>
        private void InitializeUI()
        {
            UpdateScoreDisplay(0);
            ResetJudgmentCounts();
            HideCombo();
        }

        /// <summary>
        /// イベントの購読
        /// </summary>
        private void SubscribeToEvents()
        {
            Debug.Log("[ScoreUIManager] Subscribing to ScoreUIEvents");
            ScoreUIEvents.OnScoreChanged += OnScoreChanged;
            ScoreUIEvents.OnJudgment += OnJudgmentOccurred;
            ScoreUIEvents.OnComboChanged += OnComboChanged;
            ScoreUIEvents.OnComboBreak += OnComboBreak;
            Debug.Log("[ScoreUIManager] Event subscription complete");
        }

        /// <summary>
        /// イベントの購読解除
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            ScoreUIEvents.OnScoreChanged -= OnScoreChanged;
            ScoreUIEvents.OnJudgment -= OnJudgmentOccurred;
            ScoreUIEvents.OnComboChanged -= OnComboChanged;
            ScoreUIEvents.OnComboBreak -= OnComboBreak;
        }

        /// <summary>
        /// ScoreManagerを検索して参照を取得
        /// </summary>
        private void FindScoreManager()
        {
            scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager == null)
            {
                Debug.LogWarning("ScoreManager not found in scene");
            }
        }

        /// <summary>
        /// スコア変更時の処理
        /// </summary>
        private void OnScoreChanged(int newScore)
        {
            currentScore = newScore;
            UpdateScoreDisplay(newScore);
        }

        /// <summary>
        /// 判定発生時の処理
        /// </summary>
        private void OnJudgmentOccurred(JudgmentType type)
        {
            IncrementJudgmentCount(type);
            ShowJudgmentPopup(type);
        }

        /// <summary>
        /// コンボ変更時の処理
        /// </summary>
        private void OnComboChanged(int combo)
        {
            Debug.Log($"[ScoreUIManager] OnComboChanged event received with combo: {combo}");
            currentCombo = combo;
            UpdateComboDisplay(combo);
            
            if (comboEffectController != null)
            {
                comboEffectController.OnComboUpdate(combo);
            }
        }

        /// <summary>
        /// コンボブレイク時の処理
        /// </summary>
        private void OnComboBreak()
        {
            currentCombo = 0;
            ResetComboDisplay();
            
            if (comboEffectController != null)
            {
                comboEffectController.OnComboBreak();
            }
        }

        /// <summary>
        /// スコア表示の更新
        /// </summary>
        private void UpdateScoreDisplay(int score)
        {
            if (scoreDisplay != null && useAnimations)
            {
                scoreDisplay.SetScore(score);
            }
            else if (scoreText != null)
            {
                scoreText.text = score.ToString("D7");
            }
        }

        /// <summary>
        /// 判定カウントの増加
        /// </summary>
        private void IncrementJudgmentCount(JudgmentType type)
        {
            if (judgmentCounts.ContainsKey(type))
            {
                judgmentCounts[type]++;
                UpdateJudgmentCountDisplay(type);
            }
        }

        /// <summary>
        /// 判定カウント表示の更新
        /// </summary>
        private void UpdateJudgmentCountDisplay(JudgmentType type)
        {
            if (judgmentCountDisplay != null)
            {
                judgmentCountDisplay.IncrementCount(type);
            }
            else
            {
                // フォールバック: 直接テキスト更新
                switch (type)
                {
                    case JudgmentType.Perfect:
                        if (perfectCountText != null)
                            perfectCountText.text = judgmentCounts[type].ToString("D3");
                        break;
                    case JudgmentType.Great:
                        if (greatCountText != null)
                            greatCountText.text = judgmentCounts[type].ToString("D3");
                        break;
                    case JudgmentType.Good:
                        if (goodCountText != null)
                            goodCountText.text = judgmentCounts[type].ToString("D3");
                        break;
                    case JudgmentType.Miss:
                        if (missCountText != null)
                            missCountText.text = judgmentCounts[type].ToString("D3");
                        break;
                }
            }
        }

        /// <summary>
        /// コンボ表示の更新
        /// </summary>
        private void UpdateComboDisplay(int combo)
        {
            Debug.Log($"[ScoreUIManager] UpdateComboDisplay called with combo: {combo}");
            
            if (combo <= comboHideThreshold)
            {
                Debug.Log($"[ScoreUIManager] Combo {combo} is below threshold {comboHideThreshold}, hiding combo");
                HideCombo();
                return;
            }

            ShowCombo();

            if (comboDisplay != null)
            {
                Debug.Log("[ScoreUIManager] Using ComboDisplay component to update combo");
                comboDisplay.SetCombo(combo);
            }
            else if (comboText != null)
            {
                Debug.Log($"[ScoreUIManager] Using fallback text update. ComboText value: {comboText.text} -> {combo}");
                comboText.text = combo.ToString();
            }
            else
            {
                Debug.LogWarning("[ScoreUIManager] Both comboDisplay and comboText are null! Cannot update combo display.");
            }
        }

        /// <summary>
        /// コンボ表示のリセット
        /// </summary>
        private void ResetComboDisplay()
        {
            if (comboDisplay != null)
            {
                comboDisplay.ResetCombo();
            }
            HideCombo();
        }

        /// <summary>
        /// 判定カウントのリセット
        /// </summary>
        private void ResetJudgmentCounts()
        {
            // ToList()で列挙を安全にする
            foreach (var key in judgmentCounts.Keys.ToList())
            {
                judgmentCounts[key] = 0;
            }

            if (judgmentCountDisplay != null)
            {
                judgmentCountDisplay.ResetCounts();
            }
            else
            {
                // フォールバック: 直接テキストリセット
                if (perfectCountText != null) perfectCountText.text = "000";
                if (greatCountText != null) greatCountText.text = "000";
                if (goodCountText != null) goodCountText.text = "000";
                if (missCountText != null) missCountText.text = "000";
            }
        }

        /// <summary>
        /// コンボ表示を表示
        /// </summary>
        private void ShowCombo()
        {
            if (comboContainer != null)
            {
                if (!comboContainer.activeSelf)
                {
                    Debug.Log("[ScoreUIManager] Activating combo container");
                    comboContainer.SetActive(true);
                }
                else
                {
                    Debug.Log("[ScoreUIManager] Combo container is already active");
                }
            }
            else
            {
                Debug.LogWarning("[ScoreUIManager] comboContainer is null! Cannot show combo.");
            }
        }

        /// <summary>
        /// コンボ表示を非表示
        /// </summary>
        private void HideCombo()
        {
            if (comboContainer != null)
            {
                if (comboContainer.activeSelf)
                {
                    Debug.Log("[ScoreUIManager] Deactivating combo container");
                    comboContainer.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning("[ScoreUIManager] comboContainer is null! Cannot hide combo.");
            }
        }

        /// <summary>
        /// 判定ポップアップの表示
        /// </summary>
        private void ShowJudgmentPopup(JudgmentType type)
        {
            // UIEffectPoolが利用可能な場合はポップアップを表示
            if (UIEffectPool.Instance != null)
            {
                var popup = UIEffectPool.Instance.GetPopup();
                if (popup != null)
                {
                    // 判定位置を計算（仮の位置）
                    Vector3 position = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
                    popup.Show(type, position);
                }
            }
        }

        /// <summary>
        /// 全UIをリセット
        /// </summary>
        public void ResetAll()
        {
            currentScore = 0;
            currentCombo = 0;
            UpdateScoreDisplay(0);
            ResetJudgmentCounts();
            ResetComboDisplay();
        }

        /// <summary>
        /// 現在のスコアを取得
        /// </summary>
        public int GetCurrentScore()
        {
            return currentScore;
        }

        /// <summary>
        /// 現在のコンボを取得
        /// </summary>
        public int GetCurrentCombo()
        {
            return currentCombo;
        }

        /// <summary>
        /// 判定カウントを取得
        /// </summary>
        public int GetJudgmentCount(JudgmentType type)
        {
            return judgmentCounts.ContainsKey(type) ? judgmentCounts[type] : 0;
        }

        /// <summary>
        /// デバッグ用: 表示されているスコアテキストを取得
        /// </summary>
        public string GetDisplayedScore()
        {
            if (scoreText != null)
                return scoreText.text;
            return currentScore.ToString("D7");
        }
    }
}