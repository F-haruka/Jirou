using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Jirou.Core;

namespace Jirou.UI
{
    /// <summary>
    /// 判定カウントの表示を管理するコンポーネント
    /// Perfect, Great, Good, Missのカウントを個別に表示
    /// </summary>
    public class JudgmentCountDisplay : MonoBehaviour
    {
        [Header("Count Text Elements")]
        [SerializeField] private TextMeshProUGUI perfectCountText;
        [SerializeField] private TextMeshProUGUI greatCountText;
        [SerializeField] private TextMeshProUGUI goodCountText;
        [SerializeField] private TextMeshProUGUI missCountText;

        [Header("Label Text Elements (Optional)")]
        [SerializeField] private TextMeshProUGUI perfectLabelText;
        [SerializeField] private TextMeshProUGUI greatLabelText;
        [SerializeField] private TextMeshProUGUI goodLabelText;
        [SerializeField] private TextMeshProUGUI missLabelText;

        [Header("Display Settings")]
        [SerializeField] private string countFormat = "D3";
        [SerializeField] private int maxCountDisplay = 9999;
        [SerializeField] private bool animateOnIncrement = true;
        [SerializeField] private float animationDuration = 0.2f;

        [Header("Colors")]
        [SerializeField] private Color perfectColor = new Color(1f, 0.843f, 0f); // ゴールド
        [SerializeField] private Color greatColor = Color.green;
        [SerializeField] private Color goodColor = Color.cyan;
        [SerializeField] private Color missColor = Color.red;

        // カウント管理
        private Dictionary<JudgmentType, int> judgmentCounts;
        private Dictionary<JudgmentType, TextMeshProUGUI> countTexts;
        private Dictionary<JudgmentType, TextMeshProUGUI> labelTexts;
        private Dictionary<JudgmentType, Coroutine> animationCoroutines;
        private Dictionary<JudgmentType, Vector3> originalScales;

        void Awake()
        {
            InitializeDictionaries();
            ApplyColors();
        }

        void Start()
        {
            ResetCounts();
        }
        
        void OnEnable()
        {
            // ScoreUIEventsのイベントに購読
            ScoreUIEvents.OnJudgment += IncrementCount;
            Debug.Log("[JudgmentCountDisplay] Subscribed to ScoreUIEvents.OnJudgment");
        }
        
        void OnDisable()
        {
            // イベントの購読解除
            ScoreUIEvents.OnJudgment -= IncrementCount;
            Debug.Log("[JudgmentCountDisplay] Unsubscribed from ScoreUIEvents.OnJudgment");
        }

        /// <summary>
        /// 辞書の初期化
        /// </summary>
        private void InitializeDictionaries()
        {
            judgmentCounts = new Dictionary<JudgmentType, int>
            {
                { JudgmentType.Perfect, 0 },
                { JudgmentType.Great, 0 },
                { JudgmentType.Good, 0 },
                { JudgmentType.Miss, 0 }
            };

            countTexts = new Dictionary<JudgmentType, TextMeshProUGUI>
            {
                { JudgmentType.Perfect, perfectCountText },
                { JudgmentType.Great, greatCountText },
                { JudgmentType.Good, goodCountText },
                { JudgmentType.Miss, missCountText }
            };

            labelTexts = new Dictionary<JudgmentType, TextMeshProUGUI>
            {
                { JudgmentType.Perfect, perfectLabelText },
                { JudgmentType.Great, greatLabelText },
                { JudgmentType.Good, goodLabelText },
                { JudgmentType.Miss, missLabelText }
            };

            animationCoroutines = new Dictionary<JudgmentType, Coroutine>
            {
                { JudgmentType.Perfect, null },
                { JudgmentType.Great, null },
                { JudgmentType.Good, null },
                { JudgmentType.Miss, null }
            };

            // 各テキストの初期スケールを保存
            originalScales = new Dictionary<JudgmentType, Vector3>();
            foreach (var kvp in countTexts)
            {
                if (kvp.Value != null)
                {
                    originalScales[kvp.Key] = kvp.Value.transform.localScale;
                }
                else
                {
                    originalScales[kvp.Key] = Vector3.one;
                }
            }
        }

        /// <summary>
        /// 色の適用
        /// </summary>
        private void ApplyColors()
        {
            ApplyColorToTexts(JudgmentType.Perfect, perfectColor);
            ApplyColorToTexts(JudgmentType.Great, greatColor);
            ApplyColorToTexts(JudgmentType.Good, goodColor);
            ApplyColorToTexts(JudgmentType.Miss, missColor);
        }

        /// <summary>
        /// 特定の判定タイプのテキストに色を適用
        /// </summary>
        private void ApplyColorToTexts(JudgmentType type, Color color)
        {
            if (countTexts[type] != null)
            {
                countTexts[type].color = color;
            }

            if (labelTexts[type] != null)
            {
                labelTexts[type].color = color;
            }
        }

        /// <summary>
        /// カウントを増加
        /// </summary>
        public void IncrementCount(JudgmentType type)
        {
            if (!judgmentCounts.ContainsKey(type))
                return;

            judgmentCounts[type]++;
            UpdateCountText(type);

            if (animateOnIncrement)
            {
                PlayIncrementAnimation(type);
            }
        }

        /// <summary>
        /// カウントを設定
        /// </summary>
        public void SetCount(JudgmentType type, int count)
        {
            if (!judgmentCounts.ContainsKey(type))
                return;

            judgmentCounts[type] = Mathf.Clamp(count, 0, maxCountDisplay);
            UpdateCountText(type);
        }

        /// <summary>
        /// 全カウントをリセット
        /// </summary>
        public void ResetCounts()
        {
            // ToList()で列挙を安全にする
            foreach (var type in judgmentCounts.Keys.ToList())
            {
                judgmentCounts[type] = 0;
                UpdateCountText(type);
                
                // アニメーションを停止してスケールをリセット
                if (animationCoroutines[type] != null)
                {
                    StopCoroutine(animationCoroutines[type]);
                    animationCoroutines[type] = null;
                }
                
                if (countTexts[type] != null && originalScales.ContainsKey(type))
                {
                    countTexts[type].transform.localScale = originalScales[type];
                }
            }
        }

        /// <summary>
        /// 特定の判定タイプのカウントをリセット
        /// </summary>
        public void ResetCount(JudgmentType type)
        {
            if (!judgmentCounts.ContainsKey(type))
                return;

            judgmentCounts[type] = 0;
            UpdateCountText(type);
            
            // アニメーションを停止してスケールをリセット
            if (animationCoroutines.ContainsKey(type) && animationCoroutines[type] != null)
            {
                StopCoroutine(animationCoroutines[type]);
                animationCoroutines[type] = null;
            }
            
            if (countTexts[type] != null && originalScales.ContainsKey(type))
            {
                countTexts[type].transform.localScale = originalScales[type];
            }
        }

        /// <summary>
        /// カウントテキストの更新
        /// </summary>
        private void UpdateCountText(JudgmentType type)
        {
            if (countTexts[type] == null)
                return;

            int displayCount = Mathf.Min(judgmentCounts[type], maxCountDisplay);
            
            if (displayCount >= maxCountDisplay)
            {
                countTexts[type].text = maxCountDisplay.ToString() + "+";
            }
            else
            {
                countTexts[type].text = displayCount.ToString(countFormat);
            }
        }

        /// <summary>
        /// 増加アニメーションの再生
        /// </summary>
        private void PlayIncrementAnimation(JudgmentType type)
        {
            if (countTexts[type] == null)
                return;

            // 既存のアニメーションを停止
            if (animationCoroutines[type] != null)
            {
                StopCoroutine(animationCoroutines[type]);
                // アニメーションを停止した際にスケールをリセット
                if (originalScales.ContainsKey(type))
                {
                    countTexts[type].transform.localScale = originalScales[type];
                }
            }

            animationCoroutines[type] = StartCoroutine(IncrementAnimationCoroutine(type));
        }

        /// <summary>
        /// 増加アニメーションのコルーチン
        /// </summary>
        private IEnumerator IncrementAnimationCoroutine(JudgmentType type)
        {
            var text = countTexts[type];
            if (text == null)
                yield break;

            // 保存した初期スケールを使用
            Vector3 baseScale = originalScales.ContainsKey(type) ? originalScales[type] : Vector3.one;
            
            // アニメーション開始前にスケールをリセット
            text.transform.localScale = baseScale;
            
            float elapsed = 0f;

            // スケールアップ
            while (elapsed < animationDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (animationDuration * 0.5f);
                float scale = Mathf.Lerp(1f, 1.3f, t);
                text.transform.localScale = baseScale * scale;
                yield return null;
            }

            // スケールダウン
            elapsed = 0f;
            while (elapsed < animationDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (animationDuration * 0.5f);
                float scale = Mathf.Lerp(1.3f, 1f, t);
                text.transform.localScale = baseScale * scale;
                yield return null;
            }

            text.transform.localScale = baseScale;
            animationCoroutines[type] = null;
        }

        /// <summary>
        /// フラッシュエフェクトを再生
        /// </summary>
        public void PlayFlashEffect(JudgmentType type)
        {
            StartCoroutine(FlashEffectCoroutine(type));
        }

        /// <summary>
        /// フラッシュエフェクトのコルーチン
        /// </summary>
        private IEnumerator FlashEffectCoroutine(JudgmentType type)
        {
            var text = countTexts[type];
            if (text == null)
                yield break;

            Color originalColor = text.color;
            float flashDuration = 0.1f;

            // 白くフラッシュ
            text.color = Color.white;
            yield return new WaitForSeconds(flashDuration);

            // 元の色に戻す
            text.color = originalColor;
        }

        /// <summary>
        /// カウントを取得
        /// </summary>
        public int GetCount(JudgmentType type)
        {
            return judgmentCounts.ContainsKey(type) ? judgmentCounts[type] : 0;
        }

        /// <summary>
        /// 全カウントの合計を取得
        /// </summary>
        public int GetTotalCount()
        {
            int total = 0;
            foreach (var count in judgmentCounts.Values)
            {
                total += count;
            }
            return total;
        }

        /// <summary>
        /// 表示要素の有効/無効を設定
        /// </summary>
        public void SetDisplayEnabled(JudgmentType type, bool enabled)
        {
            if (countTexts[type] != null)
            {
                countTexts[type].gameObject.SetActive(enabled);
            }

            if (labelTexts[type] != null)
            {
                labelTexts[type].gameObject.SetActive(enabled);
            }
        }

        void OnDestroy()
        {
            // 全てのアニメーションを停止
            foreach (var coroutine in animationCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
        }
    }
}