using UnityEngine;
using TMPro;
using System.Collections;
using Jirou.Core;

namespace Jirou.UI
{
    /// <summary>
    /// 判定結果のポップアップ表示を制御するコンポーネント
    /// 判定タイプに応じたテキストとアニメーションを表示
    /// </summary>
    public class JudgmentPopup : MonoBehaviour
    {
        [Header("Display Elements")]
        [SerializeField] private TextMeshProUGUI judgmentText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        [Header("Animation Settings")]
        [SerializeField] private float moveDistance = 50f;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private float fadeDelay = 0.3f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Appearance Settings")]
        [SerializeField] private float initialScale = 0f;
        [SerializeField] private float targetScale = 1f;
        [SerializeField] private float overshootScale = 1.2f;

        [Header("Judgment Text Settings")]
        [SerializeField] private JudgmentTextSettings perfectSettings = new JudgmentTextSettings
        {
            text = "PERFECT!",
            color = new Color(1f, 0.843f, 0f),
            fontSize = 48
        };

        [SerializeField] private JudgmentTextSettings greatSettings = new JudgmentTextSettings
        {
            text = "GREAT!",
            color = Color.green,
            fontSize = 44
        };

        [SerializeField] private JudgmentTextSettings goodSettings = new JudgmentTextSettings
        {
            text = "GOOD",
            color = Color.cyan,
            fontSize = 40
        };

        [SerializeField] private JudgmentTextSettings missSettings = new JudgmentTextSettings
        {
            text = "MISS",
            color = Color.red,
            fontSize = 40
        };

        // 内部状態
        private Coroutine animationCoroutine;
        private Vector3 startPosition;
        private float originalFontSize;

        [System.Serializable]
        public class JudgmentTextSettings
        {
            public string text;
            public Color color;
            public float fontSize;
        }

        void Awake()
        {
            InitializeComponents();
        }

        /// <summary>
        /// コンポーネントの初期化
        /// </summary>
        private void InitializeComponents()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (judgmentText == null)
            {
                judgmentText = GetComponentInChildren<TextMeshProUGUI>();
                if (judgmentText == null)
                {
                    Debug.LogError("JudgmentPopup: TextMeshProUGUI component not found!");
                }
            }

            if (judgmentText != null)
            {
                originalFontSize = judgmentText.fontSize;
            }
        }

        /// <summary>
        /// ポップアップを表示
        /// </summary>
        public void Show(JudgmentType type, Vector3 position)
        {
            transform.position = position;
            startPosition = position;
            
            SetupAppearance(type);
            
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            
            animationCoroutine = StartCoroutine(PlayAnimation());
        }

        /// <summary>
        /// ワールド座標で表示
        /// </summary>
        public void ShowAtWorldPosition(JudgmentType type, Vector3 worldPosition, Camera camera)
        {
            Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
            Show(type, screenPosition);
        }

        /// <summary>
        /// 判定タイプに応じた外観設定
        /// </summary>
        private void SetupAppearance(JudgmentType type)
        {
            if (judgmentText == null) return;

            JudgmentTextSettings settings = GetSettings(type);
            
            judgmentText.text = settings.text;
            judgmentText.color = settings.color;
            judgmentText.fontSize = settings.fontSize;

            // グローやアウトラインの設定（TextMeshProのマテリアル設定で調整）
            ApplyTextEffect(type);
        }

        /// <summary>
        /// 判定タイプに応じた設定を取得
        /// </summary>
        private JudgmentTextSettings GetSettings(JudgmentType type)
        {
            switch (type)
            {
                case JudgmentType.Perfect:
                    return perfectSettings;
                case JudgmentType.Great:
                    return greatSettings;
                case JudgmentType.Good:
                    return goodSettings;
                case JudgmentType.Miss:
                    return missSettings;
                default:
                    return goodSettings;
            }
        }

        /// <summary>
        /// テキストエフェクトを適用
        /// </summary>
        private void ApplyTextEffect(JudgmentType type)
        {
            if (judgmentText == null) return;

            // TextMeshProのアウトライン設定
            switch (type)
            {
                case JudgmentType.Perfect:
                    judgmentText.outlineWidth = 0.3f;
                    judgmentText.outlineColor = Color.black;
                    break;
                case JudgmentType.Great:
                    judgmentText.outlineWidth = 0.25f;
                    judgmentText.outlineColor = Color.black;
                    break;
                case JudgmentType.Good:
                    judgmentText.outlineWidth = 0.2f;
                    judgmentText.outlineColor = Color.black;
                    break;
                case JudgmentType.Miss:
                    judgmentText.outlineWidth = 0.25f;
                    judgmentText.outlineColor = new Color(0.5f, 0, 0);
                    break;
            }
        }

        /// <summary>
        /// アニメーションの再生
        /// </summary>
        private IEnumerator PlayAnimation()
        {
            // 初期状態を設定
            transform.localScale = Vector3.one * initialScale;
            canvasGroup.alpha = 1f;

            float elapsed = 0f;

            // スケールインと上方向移動のアニメーション
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // スケールアニメーション
                float scaleT = scaleCurve.Evaluate(t);
                float currentScale = Mathf.Lerp(initialScale, targetScale, scaleT);
                
                // オーバーシュート効果
                if (t < 0.3f)
                {
                    float overshootT = t / 0.3f;
                    currentScale = Mathf.Lerp(initialScale, overshootScale, overshootT);
                }
                else
                {
                    float settleT = (t - 0.3f) / 0.7f;
                    currentScale = Mathf.Lerp(overshootScale, targetScale, settleT);
                }

                transform.localScale = Vector3.one * currentScale;

                // 位置アニメーション
                float moveT = moveCurve.Evaluate(t);
                float yOffset = moveDistance * moveT;
                transform.position = startPosition + new Vector3(0, yOffset, 0);

                // フェードアウト開始
                if (elapsed >= fadeDelay)
                {
                    float fadeT = (elapsed - fadeDelay) / (duration - fadeDelay);
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
                }

                yield return null;
            }

            // アニメーション完了
            canvasGroup.alpha = 0f;
            OnAnimationComplete();
        }

        /// <summary>
        /// アニメーション完了時の処理
        /// </summary>
        private void OnAnimationComplete()
        {
            animationCoroutine = null;

            // プールに返却
            if (UIEffectPool.Instance != null)
            {
                UIEffectPool.Instance.ReturnPopup(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// ポップアップを即座に非表示
        /// </summary>
        public void Hide()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// カスタムアニメーションで表示
        /// </summary>
        public void ShowWithCustomAnimation(JudgmentType type, Vector3 position, float customDuration, float customMoveDistance)
        {
            duration = customDuration;
            moveDistance = customMoveDistance;
            Show(type, position);
        }

        /// <summary>
        /// リセット
        /// </summary>
        public void Reset()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            transform.localScale = Vector3.one;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            
            if (judgmentText != null && originalFontSize > 0)
            {
                judgmentText.fontSize = originalFontSize;
            }
        }

        void OnDisable()
        {
            Reset();
        }

        void OnDestroy()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
        }
    }
}