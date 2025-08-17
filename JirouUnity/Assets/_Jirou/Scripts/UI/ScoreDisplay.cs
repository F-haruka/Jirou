using UnityEngine;
using TMPro;
using System.Collections;

namespace Jirou.UI
{
    /// <summary>
    /// スコア表示を管理する個別コンポーネント
    /// 各UI要素に直接アタッチして使用
    /// </summary>
    public class ScoreDisplay : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private TextMeshProUGUI displayText;
        [SerializeField] private string displayFormat = "{0:D7}";
        [SerializeField] private bool animateOnChange = true;
        
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
        [SerializeField] private float scaleMultiplier = 1.2f;
        
        private int currentValue = 0;
        private int targetValue = 0;
        private Coroutine animationCoroutine;
        private Vector3 originalScale = Vector3.one;
        
        void Awake()
        {
            if (displayText == null)
            {
                displayText = GetComponent<TextMeshProUGUI>();
            }
            
            // 初期スケールを保存（アニメーションの基準値として使用）
            if (displayText != null)
            {
                originalScale = displayText.transform.localScale;
            }
            
            UpdateDisplay(currentValue);
        }
        
        void OnEnable()
        {
            ScoreUIEvents.OnScoreChanged += SetScore;
        }
        
        void OnDisable()
        {
            ScoreUIEvents.OnScoreChanged -= SetScore;
        }
        
        /// <summary>
        /// スコアを設定
        /// </summary>
        public void SetScore(int score)
        {
            targetValue = score;
            
            if (animateOnChange && gameObject.activeInHierarchy)
            {
                if (animationCoroutine != null)
                    StopCoroutine(animationCoroutine);
                animationCoroutine = StartCoroutine(AnimateScore());
            }
            else
            {
                currentValue = targetValue;
                UpdateDisplay(currentValue);
            }
        }
        
        /// <summary>
        /// スコアアニメーションのコルーチン
        /// </summary>
        private IEnumerator AnimateScore()
        {
            int startValue = currentValue;
            float elapsed = 0f;
            
            // アニメーション開始前にスケールをリセット
            displayText.transform.localScale = originalScale;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                
                // 数値を補間
                currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, t));
                UpdateDisplay(currentValue);
                
                // スケールアニメーション
                if (scaleCurve != null)
                {
                    float scale = scaleCurve.Evaluate(t);
                    scale = 1f + (scaleMultiplier - 1f) * scale;
                    displayText.transform.localScale = originalScale * scale;
                }
                
                yield return null;
            }
            
            // 最終値を設定
            currentValue = targetValue;
            UpdateDisplay(currentValue);
            displayText.transform.localScale = originalScale;
        }
        
        /// <summary>
        /// 表示を更新
        /// </summary>
        private void UpdateDisplay(int value)
        {
            if (displayText != null)
            {
                displayText.text = string.Format(displayFormat, value);
            }
        }
        
        /// <summary>
        /// 表示をリセット
        /// </summary>
        public void ResetDisplay()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
            
            currentValue = 0;
            targetValue = 0;
            UpdateDisplay(0);
            
            // スケールも初期値にリセット
            if (displayText != null)
            {
                displayText.transform.localScale = originalScale;
            }
        }
    }
}