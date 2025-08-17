using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Jirou.Core;

namespace Jirou.Core
{
    /// <summary>
    /// スコアとコンボを管理するマネージャークラス
    /// 判定結果に基づいてスコア計算を行い、UIイベントを通知する
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [Header("Score Settings")]
        [SerializeField] private int maxScore = 9999999;
        [SerializeField] private int baseScorePerNote = 1000;
        
        [Header("Score Multipliers")]
        [SerializeField] private float perfectMultiplier = 1.0f;
        [SerializeField] private float greatMultiplier = 0.5f;
        [SerializeField] private float goodMultiplier = 0.1f;
        [SerializeField] private float missMultiplier = 0f;

        [Header("Combo Settings")]
        [SerializeField] private int comboScoreBonus = 10;
        [SerializeField] private int maxCombo = 9999;
        [SerializeField] private bool breakComboOnGood = false;

        [Header("Combo Milestones")]
        [SerializeField] private int[] comboMilestones = { 50, 100, 200, 500, 1000 };
        [SerializeField] private float[] milestoneBonusMultipliers = { 1.1f, 1.2f, 1.3f, 1.5f, 2.0f };

        // 現在のスコア情報
        private int currentScore = 0;
        private int currentCombo = 0;
        private int maxComboReached = 0;
        
        // 判定カウント
        private Dictionary<JudgmentType, int> judgmentCounts;
        
        // 統計情報
        private int totalNotes = 0;
        private float accuracy = 0f;
        private int totalPossibleScore = 0;

        // シングルトンインスタンス（オプション）
        private static ScoreManager instance;
        public static ScoreManager Instance => instance;

        // イベント定義
        public static event Action<int> OnScoreChanged;
        public static event Action<int> OnComboChanged;
        public static event Action OnComboBreak;
        public static event Action<JudgmentType> OnJudgment;

        // プロパティ
        public int CurrentScore => currentScore;
        public int CurrentCombo => currentCombo;
        public int MaxCombo => maxComboReached;
        public float Accuracy => accuracy;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                InitializeScoreManager();
                // ScoreUIEventsの存在をログで確認
                CheckForScoreUIEvents();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// ScoreUIEventsの存在を確認してログを出力
        /// </summary>
        private void CheckForScoreUIEvents()
        {
            // リフレクションを使用してScoreUIEventsクラスを探す
            var scoreUIEventsType = System.Type.GetType("Jirou.UI.ScoreUIEvents, Jirou.UI");
            if (scoreUIEventsType != null)
            {
                var scoreUIEvents = FindObjectOfType(scoreUIEventsType);
                if (scoreUIEvents == null)
                {
                    Debug.LogWarning("[ScoreManager] ScoreUIEvents component not found in scene!");
                    Debug.LogWarning("[ScoreManager] Please add a GameObject with ScoreUIEvents component to the scene.");
                    Debug.LogWarning("[ScoreManager] Or create an empty GameObject and add the ScoreUIEvents component to it.");
                }
                else
                {
                    Debug.Log("[ScoreManager] ScoreUIEvents found in scene - UI events will work correctly");
                }
            }
            else
            {
                Debug.LogWarning("[ScoreManager] ScoreUIEvents class not found in project!");
            }
        }

        void OnEnable()
        {
            // ScoreManagerは他のクラスから呼び出されるのを待つ
            // InputManagerからHandleNoteJudgedが呼ばれる
            Debug.Log("[ScoreManager] OnEnable - Ready to receive judgment events");
        }

        void OnDisable()
        {
            // 特に何もしない
            Debug.Log("[ScoreManager] OnDisable");
        }

        
        /// <summary>
        /// InputManagerからのノーツ判定イベントを処理
        /// </summary>
        public void HandleNoteJudged(int laneIndex, JudgmentType judgment)
        {
            Debug.Log($"[ScoreManager] HandleNoteJudged - Lane: {laneIndex}, Judgment: {judgment}");
            ProcessJudgment(judgment);
        }

        /// <summary>
        /// スコアマネージャーの初期化
        /// </summary>
        private void InitializeScoreManager()
        {
            judgmentCounts = new Dictionary<JudgmentType, int>
            {
                { JudgmentType.Perfect, 0 },
                { JudgmentType.Great, 0 },
                { JudgmentType.Good, 0 },
                { JudgmentType.Miss, 0 }
            };

            ResetScore();
        }

        /// <summary>
        /// 判定を処理してスコアを更新
        /// </summary>
        public void ProcessJudgment(JudgmentType type)
        {
            Debug.Log($"[ScoreManager] ProcessJudgment called with type: {type}");
            
            // 判定カウントを更新
            if (judgmentCounts.ContainsKey(type))
            {
                judgmentCounts[type]++;
                totalNotes++;
                Debug.Log($"[ScoreManager] Judgment count for {type}: {judgmentCounts[type]}, Total notes: {totalNotes}");
            }

            // スコア計算
            int points = CalculatePoints(type);
            AddScore(points);
            Debug.Log($"[ScoreManager] Points calculated: {points}, Current score: {currentScore}");

            // コンボ処理
            UpdateCombo(type);
            Debug.Log($"[ScoreManager] Current combo: {currentCombo}");

            // 精度計算
            UpdateAccuracy();

            // UIイベント通知
            NotifyUIEvents(type, points);
        }

        /// <summary>
        /// 判定タイプに応じたポイントを計算
        /// </summary>
        private int CalculatePoints(JudgmentType type)
        {
            float baseScore = baseScorePerNote;
            float multiplier = GetJudgmentMultiplier(type);
            
            // コンボボーナス
            float comboBonus = currentCombo * comboScoreBonus;
            
            // マイルストーンボーナス
            float milestoneMultiplier = GetMilestoneMultiplier(currentCombo);
            
            // 最終スコア計算
            int finalScore = Mathf.RoundToInt((baseScore * multiplier + comboBonus) * milestoneMultiplier);
            
            return finalScore;
        }

        /// <summary>
        /// 判定タイプに応じた倍率を取得
        /// </summary>
        private float GetJudgmentMultiplier(JudgmentType type)
        {
            switch (type)
            {
                case JudgmentType.Perfect:
                    return perfectMultiplier;
                case JudgmentType.Great:
                    return greatMultiplier;
                case JudgmentType.Good:
                    return goodMultiplier;
                case JudgmentType.Miss:
                    return missMultiplier;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// コンボマイルストーンに応じた倍率を取得
        /// </summary>
        private float GetMilestoneMultiplier(int combo)
        {
            for (int i = comboMilestones.Length - 1; i >= 0; i--)
            {
                if (combo >= comboMilestones[i])
                {
                    return milestoneBonusMultipliers[Mathf.Min(i, milestoneBonusMultipliers.Length - 1)];
                }
            }
            return 1.0f;
        }

        /// <summary>
        /// スコアを追加
        /// </summary>
        private void AddScore(int points)
        {
            currentScore = Mathf.Clamp(currentScore + points, 0, maxScore);
            totalPossibleScore += baseScorePerNote;
        }

        /// <summary>
        /// コンボを更新
        /// </summary>
        private void UpdateCombo(JudgmentType type)
        {
            switch (type)
            {
                case JudgmentType.Perfect:
                case JudgmentType.Great:
                    IncrementCombo();
                    break;
                    
                case JudgmentType.Good:
                    if (breakComboOnGood)
                    {
                        ResetCombo();
                    }
                    else
                    {
                        IncrementCombo();
                    }
                    break;
                    
                case JudgmentType.Miss:
                    ResetCombo();
                    break;
            }
        }

        /// <summary>
        /// コンボを増加
        /// </summary>
        private void IncrementCombo()
        {
            currentCombo = Mathf.Clamp(currentCombo + 1, 0, maxCombo);
            
            if (currentCombo > maxComboReached)
            {
                maxComboReached = currentCombo;
            }
        }

        /// <summary>
        /// コンボをリセット
        /// </summary>
        private void ResetCombo()
        {
            if (currentCombo > 0)
            {
                currentCombo = 0;
                OnComboBreak?.Invoke();
            }
        }

        /// <summary>
        /// 精度を更新
        /// </summary>
        private void UpdateAccuracy()
        {
            if (totalNotes == 0)
            {
                accuracy = 0f;
                return;
            }

            float perfectWeight = judgmentCounts[JudgmentType.Perfect] * 1.0f;
            float greatWeight = judgmentCounts[JudgmentType.Great] * 0.75f;
            float goodWeight = judgmentCounts[JudgmentType.Good] * 0.5f;
            float missWeight = judgmentCounts[JudgmentType.Miss] * 0f;

            float totalWeight = perfectWeight + greatWeight + goodWeight + missWeight;
            accuracy = (totalWeight / totalNotes) * 100f;
        }

        /// <summary>
        /// UIイベントを通知
        /// </summary>
        private void NotifyUIEvents(JudgmentType type, int points)
        {
            Debug.Log($"[ScoreManager] NotifyUIEvents - Score: {currentScore}, Judgment: {type}, Combo: {currentCombo}");
            
            // スコア更新通知
            if (OnScoreChanged != null)
            {
                OnScoreChanged.Invoke(currentScore);
                Debug.Log($"[ScoreManager] OnScoreChanged event fired with score: {currentScore}");
            }
            else
            {
                Debug.LogWarning("[ScoreManager] OnScoreChanged has no subscribers!");
            }
            
            // 判定通知
            if (OnJudgment != null)
            {
                OnJudgment.Invoke(type);
                Debug.Log($"[ScoreManager] OnJudgment event fired with type: {type}");
            }
            else
            {
                Debug.LogWarning("[ScoreManager] OnJudgment has no subscribers!");
            }
            
            // コンボ更新通知
            if (currentCombo > 0)
            {
                if (OnComboChanged != null)
                {
                    OnComboChanged.Invoke(currentCombo);
                    Debug.Log($"[ScoreManager] OnComboChanged event fired with combo: {currentCombo}");
                }
                else
                {
                    Debug.LogWarning("[ScoreManager] OnComboChanged has no subscribers!");
                }
            }
        }

        /// <summary>
        /// スコアをリセット
        /// </summary>
        public void ResetScore()
        {
            currentScore = 0;
            currentCombo = 0;
            maxComboReached = 0;
            totalNotes = 0;
            accuracy = 0f;
            totalPossibleScore = 0;

            // ToList()で列挙を安全にする
            foreach (var key in judgmentCounts.Keys.ToList())
            {
                judgmentCounts[key] = 0;
            }

            // UIイベント通知
            OnScoreChanged?.Invoke(0);
            OnComboBreak?.Invoke();
        }

        /// <summary>
        /// 判定カウントを取得
        /// </summary>
        public int GetJudgmentCount(JudgmentType type)
        {
            return judgmentCounts.ContainsKey(type) ? judgmentCounts[type] : 0;
        }

        /// <summary>
        /// 全判定カウントを取得
        /// </summary>
        public Dictionary<JudgmentType, int> GetAllJudgmentCounts()
        {
            return new Dictionary<JudgmentType, int>(judgmentCounts);
        }

        /// <summary>
        /// ランクを計算
        /// </summary>
        public string CalculateRank()
        {
            if (accuracy >= 100f) return "SSS";
            if (accuracy >= 95f) return "SS";
            if (accuracy >= 90f) return "S";
            if (accuracy >= 85f) return "A";
            if (accuracy >= 80f) return "B";
            if (accuracy >= 70f) return "C";
            if (accuracy >= 60f) return "D";
            return "E";
        }

        /// <summary>
        /// フルコンボかどうか
        /// </summary>
        public bool IsFullCombo()
        {
            return judgmentCounts[JudgmentType.Miss] == 0 && totalNotes > 0;
        }

        /// <summary>
        /// パーフェクトかどうか
        /// </summary>
        public bool IsPerfect()
        {
            return judgmentCounts[JudgmentType.Perfect] == totalNotes && totalNotes > 0;
        }

        /// <summary>
        /// スコア統計を取得
        /// </summary>
        public ScoreStatistics GetStatistics()
        {
            return new ScoreStatistics
            {
                score = currentScore,
                combo = currentCombo,
                maxCombo = maxComboReached,
                accuracy = accuracy,
                totalNotes = totalNotes,
                perfectCount = judgmentCounts[JudgmentType.Perfect],
                greatCount = judgmentCounts[JudgmentType.Great],
                goodCount = judgmentCounts[JudgmentType.Good],
                missCount = judgmentCounts[JudgmentType.Miss],
                rank = CalculateRank(),
                isFullCombo = IsFullCombo(),
                isPerfect = IsPerfect()
            };
        }

        /// <summary>
        /// デバッグ用: 直接スコアを設定
        /// </summary>
        public void SetScoreDebug(int score)
        {
            currentScore = Mathf.Clamp(score, 0, maxScore);
            OnScoreChanged?.Invoke(currentScore);
        }

        /// <summary>
        /// デバッグ用: 直接コンボを設定
        /// </summary>
        public void SetComboDebug(int combo)
        {
            currentCombo = Mathf.Clamp(combo, 0, maxCombo);
            if (currentCombo > maxComboReached)
            {
                maxComboReached = currentCombo;
            }
            OnComboChanged?.Invoke(currentCombo);
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// スコア統計構造体
        /// </summary>
        [System.Serializable]
        public struct ScoreStatistics
        {
            public int score;
            public int combo;
            public int maxCombo;
            public float accuracy;
            public int totalNotes;
            public int perfectCount;
            public int greatCount;
            public int goodCount;
            public int missCount;
            public string rank;
            public bool isFullCombo;
            public bool isPerfect;
        }
    }
}