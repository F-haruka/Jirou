using UnityEngine;
using TMPro;
using System.Collections;

namespace Jirou.UI
{
    /// <summary>
    /// コンボ表示を管理するコンポーネント
    /// コンボ数の表示、アニメーション、エフェクトを制御
    /// </summary>
    public class ComboDisplay : MonoBehaviour
    {
        [Header("Display Elements")]
        [SerializeField] private TextMeshProUGUI comboNumberText;
        [SerializeField] private TextMeshProUGUI comboLabelText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private bool autoHide = false;
        [SerializeField] private int autoHideThreshold = 5;

        [Header("Scale Animation")]
        [SerializeField] private bool enableScaleAnimation = true;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
        [SerializeField] private float maxScale = 1.5f;
        [SerializeField] private float scaleAnimationDuration = 0.3f;

        [Header("Milestone Settings")]
        [SerializeField] private int[] milestones = { 50, 100, 200, 500, 1000 };
        [SerializeField] private Color[] milestoneColors = {
            Color.yellow,
            new Color(1f, 0.5f, 0f), // オレンジ
            new Color(1f, 0f, 1f),   // マゼンタ
            new Color(0.5f, 0f, 1f), // 紫
            new Color(1f, 0.843f, 0f) // ゴールド
        };

        [Header("Effects")]
        [SerializeField] private ParticleSystem comboEffect;

        // 内部状態
        private int currentCombo;
        private Coroutine fadeCoroutine;
        private Coroutine scaleCoroutine;
        private Coroutine autoHideCoroutine;
        private Vector3 originalNumberScale;
        private Vector3 originalLabelScale;
        private Color originalNumberColor;
        private Color originalLabelColor;

        void Awake()
        {
            InitializeComponents();
            CacheOriginalValues();
        }

        void Start()
        {
            ResetCombo();
        }
        
        void OnEnable()
        {
            // ScoreUIEventsのイベントに購読
            ScoreUIEvents.OnComboChanged += SetCombo;
            ScoreUIEvents.OnComboBreak += ResetCombo;
            Debug.Log("[ComboDisplay] Subscribed to ScoreUIEvents");
        }
        
        void OnDisable()
        {
            // イベントの購読解除
            ScoreUIEvents.OnComboChanged -= SetCombo;
            ScoreUIEvents.OnComboBreak -= ResetCombo;
            Debug.Log("[ComboDisplay] Unsubscribed from ScoreUIEvents");
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

            if (comboNumberText == null)
            {
                Debug.LogWarning("ComboDisplay: Combo number text not assigned!");
            }

            if (comboLabelText == null)
            {
                Debug.LogWarning("ComboDisplay: Combo label text not assigned!");
            }
        }

        /// <summary>
        /// 元の値をキャッシュ
        /// </summary>
        private void CacheOriginalValues()
        {
            if (comboNumberText != null)
            {
                originalNumberScale = comboNumberText.transform.localScale;
                originalNumberColor = comboNumberText.color;
            }

            if (comboLabelText != null)
            {
                originalLabelScale = comboLabelText.transform.localScale;
                originalLabelColor = comboLabelText.color;
            }
        }

        /// <summary>
        /// コンボを設定
        /// </summary>
        public void SetCombo(int combo)
        {
            currentCombo = combo;
            UpdateComboText();
            
            // マイルストーンチェック
            CheckMilestone(combo);

            // スケールアニメーション
            if (enableScaleAnimation)
            {
                PlayScaleAnimation();
            }

            // 自動非表示の管理
            if (autoHide && combo >= autoHideThreshold)
            {
                ManageAutoHide();
            }

            // 表示状態の管理
            if (canvasGroup.alpha == 0)
            {
                Show();
            }
        }

        /// <summary>
        /// コンボをリセット
        /// </summary>
        public void ResetCombo()
        {
            currentCombo = 0;
            UpdateComboText();
            Hide();
            ResetColors();
            ResetScale();
        }

        /// <summary>
        /// コンボテキストの更新
        /// </summary>
        private void UpdateComboText()
        {
            if (comboNumberText != null)
            {
                comboNumberText.text = currentCombo.ToString();
            }

            if (comboLabelText != null)
            {
                // コンボ数に応じてラベルを変更
                if (currentCombo >= 1000)
                {
                    comboLabelText.text = "MAX COMBO!";
                }
                else if (currentCombo >= 500)
                {
                    comboLabelText.text = "SUPER COMBO!";
                }
                else if (currentCombo >= 100)
                {
                    comboLabelText.text = "GREAT COMBO!";
                }
                else
                {
                    comboLabelText.text = "COMBO";
                }
            }
        }

        /// <summary>
        /// マイルストーンのチェック
        /// </summary>
        private void CheckMilestone(int combo)
        {
            for (int i = milestones.Length - 1; i >= 0; i--)
            {
                if (combo == milestones[i])
                {
                    OnMilestoneReached(i);
                    break;
                }
            }

            // 色の更新
            UpdateColorByMilestone(combo);
        }

        /// <summary>
        /// マイルストーン到達時の処理
        /// </summary>
        private void OnMilestoneReached(int milestoneIndex)
        {
            // エフェクト再生
            if (comboEffect != null)
            {
                comboEffect.Play();
            }

            // 特別なアニメーション
            PlayMilestoneAnimation();
        }

        /// <summary>
        /// マイルストーンに応じた色更新
        /// </summary>
        private void UpdateColorByMilestone(int combo)
        {
            Color targetColor = originalNumberColor;

            for (int i = milestones.Length - 1; i >= 0; i--)
            {
                if (combo >= milestones[i])
                {
                    targetColor = milestoneColors[Mathf.Min(i, milestoneColors.Length - 1)];
                    break;
                }
            }

            if (comboNumberText != null)
            {
                comboNumberText.color = targetColor;
            }
        }

        /// <summary>
        /// 表示
        /// </summary>
        public void Show()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(FadeIn());
        }

        /// <summary>
        /// 非表示
        /// </summary>
        public void Hide()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }

            fadeCoroutine = StartCoroutine(FadeOut());
        }

        /// <summary>
        /// フェードイン
        /// </summary>
        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            fadeCoroutine = null;
        }

        /// <summary>
        /// フェードアウト
        /// </summary>
        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            fadeCoroutine = null;
        }

        /// <summary>
        /// スケールアニメーションの再生
        /// </summary>
        private void PlayScaleAnimation()
        {
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }

            scaleCoroutine = StartCoroutine(ScaleAnimationCoroutine());
        }

        /// <summary>
        /// スケールアニメーションのコルーチン
        /// </summary>
        private IEnumerator ScaleAnimationCoroutine()
        {
            if (comboNumberText == null)
                yield break;

            float elapsed = 0f;

            while (elapsed < scaleAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scaleAnimationDuration;
                float scale = scaleCurve.Evaluate(t) * (maxScale - 1f) + 1f;
                
                comboNumberText.transform.localScale = originalNumberScale * scale;

                yield return null;
            }

            comboNumberText.transform.localScale = originalNumberScale;
            scaleCoroutine = null;
        }

        /// <summary>
        /// マイルストーンアニメーション
        /// </summary>
        private void PlayMilestoneAnimation()
        {
            StartCoroutine(MilestoneAnimationCoroutine());
        }

        /// <summary>
        /// マイルストーンアニメーションのコルーチン
        /// </summary>
        private IEnumerator MilestoneAnimationCoroutine()
        {
            if (comboNumberText == null)
                yield break;

            // 回転アニメーション
            float duration = 0.5f;
            float elapsed = 0f;
            float rotationAmount = 360f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float rotation = Mathf.Lerp(0, rotationAmount, t);
                comboNumberText.transform.rotation = Quaternion.Euler(0, 0, rotation);
                
                // スケールも同時に変更
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.5f;
                comboNumberText.transform.localScale = originalNumberScale * scale;

                yield return null;
            }

            comboNumberText.transform.rotation = Quaternion.identity;
            comboNumberText.transform.localScale = originalNumberScale;
        }

        /// <summary>
        /// 自動非表示の管理
        /// </summary>
        private void ManageAutoHide()
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }

            autoHideCoroutine = StartCoroutine(AutoHideCoroutine());
        }

        /// <summary>
        /// 自動非表示のコルーチン
        /// </summary>
        private IEnumerator AutoHideCoroutine()
        {
            yield return new WaitForSeconds(displayDuration);
            Hide();
            autoHideCoroutine = null;
        }

        /// <summary>
        /// 色をリセット
        /// </summary>
        private void ResetColors()
        {
            if (comboNumberText != null)
            {
                comboNumberText.color = originalNumberColor;
            }

            if (comboLabelText != null)
            {
                comboLabelText.color = originalLabelColor;
            }
        }

        /// <summary>
        /// スケールをリセット
        /// </summary>
        private void ResetScale()
        {
            if (comboNumberText != null)
            {
                comboNumberText.transform.localScale = originalNumberScale;
                comboNumberText.transform.rotation = Quaternion.identity;
            }

            if (comboLabelText != null)
            {
                comboLabelText.transform.localScale = originalLabelScale;
            }
        }

        /// <summary>
        /// 現在のコンボ数を取得
        /// </summary>
        public int GetCurrentCombo()
        {
            return currentCombo;
        }

        void OnDestroy()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }

            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }
        }
    }
}