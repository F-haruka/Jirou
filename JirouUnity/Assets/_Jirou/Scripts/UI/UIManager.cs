using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Jirou.Core;

namespace Jirou.UI
{
    /// <summary>
    /// ゲームUI全体を管理するマネージャークラス
    /// スコア、コンボ、判定の表示を制御する
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private string scoreFormat = "SCORE: {0:D7}";
        
        [Header("Combo Display")]
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private GameObject comboContainer;
        [SerializeField] private string comboFormat = "{0}";
        
        [Header("Judgment Display")]
        [SerializeField] private TextMeshProUGUI judgmentText;
        [SerializeField] private float judgmentDisplayDuration = 0.5f;
        
        [Header("Accuracy Display")]
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private string accuracyFormat = "{0:F2}%";
        
        [Header("Animation Settings")]
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
        [SerializeField] private float comboScaleMultiplier = 1.2f;
        [SerializeField] private float animationDuration = 0.3f;
        
        // シングルトンインスタンス
        private static UIManager instance;
        public static UIManager Instance => instance;
        
        // 現在のコルーチン
        private Coroutine judgmentDisplayCoroutine;
        private Coroutine comboAnimationCoroutine;
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                InitializeUI();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        void OnEnable()
        {
            // イベントを購読
            ScoreUIEvents.OnScoreChanged += UpdateScore;
            ScoreUIEvents.OnComboChanged += UpdateCombo;
            ScoreUIEvents.OnComboBreak += OnComboBreak;
            ScoreUIEvents.OnJudgment += ShowJudgment;
        }
        
        void OnDisable()
        {
            // イベントの購読を解除
            ScoreUIEvents.OnScoreChanged -= UpdateScore;
            ScoreUIEvents.OnComboChanged -= UpdateCombo;
            ScoreUIEvents.OnComboBreak -= OnComboBreak;
            ScoreUIEvents.OnJudgment -= ShowJudgment;
        }
        
        /// <summary>
        /// UIの初期化
        /// </summary>
        private void InitializeUI()
        {
            // 初期表示設定
            if (scoreText != null)
                scoreText.text = string.Format(scoreFormat, 0);
                
            if (comboContainer != null)
                comboContainer.SetActive(false);
                
            if (judgmentText != null)
                judgmentText.gameObject.SetActive(false);
                
            if (accuracyText != null)
                accuracyText.text = string.Format(accuracyFormat, 100f);
        }
        
        /// <summary>
        /// スコア表示を更新
        /// </summary>
        private void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = string.Format(scoreFormat, score);
            }
        }
        
        /// <summary>
        /// コンボ表示を更新
        /// </summary>
        private void UpdateCombo(int combo)
        {
            if (comboText != null && comboContainer != null)
            {
                // コンボが1以上の時のみ表示
                bool shouldShow = combo > 0;
                comboContainer.SetActive(shouldShow);
                
                if (shouldShow)
                {
                    comboText.text = string.Format(comboFormat, combo);
                    
                    // コンボアニメーション
                    if (comboAnimationCoroutine != null)
                        StopCoroutine(comboAnimationCoroutine);
                    comboAnimationCoroutine = StartCoroutine(AnimateCombo());
                }
            }
        }
        
        /// <summary>
        /// コンボブレイク時の処理
        /// </summary>
        private void OnComboBreak()
        {
            if (comboContainer != null)
            {
                // コンボ表示を非表示
                comboContainer.SetActive(false);
            }
        }
        
        /// <summary>
        /// 判定表示
        /// </summary>
        private void ShowJudgment(JudgmentType type)
        {
            if (judgmentText != null)
            {
                // 前の表示コルーチンを停止
                if (judgmentDisplayCoroutine != null)
                    StopCoroutine(judgmentDisplayCoroutine);
                    
                // 判定テキストを設定
                judgmentText.text = GetJudgmentText(type);
                judgmentText.color = GetJudgmentColor(type);
                
                // 表示コルーチンを開始
                judgmentDisplayCoroutine = StartCoroutine(DisplayJudgment());
            }
        }
        
        /// <summary>
        /// 判定タイプに応じたテキストを取得
        /// </summary>
        private string GetJudgmentText(JudgmentType type)
        {
            switch (type)
            {
                case JudgmentType.Perfect:
                    return "PERFECT!";
                case JudgmentType.Great:
                    return "GREAT!";
                case JudgmentType.Good:
                    return "GOOD";
                case JudgmentType.Miss:
                    return "MISS";
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// 判定タイプに応じた色を取得
        /// </summary>
        private Color GetJudgmentColor(JudgmentType type)
        {
            switch (type)
            {
                case JudgmentType.Perfect:
                    return new Color(1f, 0.843f, 0f); // ゴールド
                case JudgmentType.Great:
                    return new Color(0.4f, 1f, 0.4f); // グリーン
                case JudgmentType.Good:
                    return new Color(0.4f, 0.7f, 1f); // ブルー
                case JudgmentType.Miss:
                    return new Color(1f, 0.3f, 0.3f); // レッド
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// 判定表示のコルーチン
        /// </summary>
        private IEnumerator DisplayJudgment()
        {
            judgmentText.gameObject.SetActive(true);
            
            // フェードインとスケールアニメーション
            float elapsed = 0f;
            Vector3 originalScale = judgmentText.transform.localScale;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                
                // スケールアニメーション
                float scale = scaleCurve.Evaluate(t);
                judgmentText.transform.localScale = originalScale * scale;
                
                // アルファアニメーション
                Color color = judgmentText.color;
                color.a = Mathf.Lerp(0f, 1f, t);
                judgmentText.color = color;
                
                yield return null;
            }
            
            // 表示時間待機
            yield return new WaitForSeconds(judgmentDisplayDuration);
            
            // フェードアウト
            elapsed = 0f;
            while (elapsed < animationDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (animationDuration * 0.5f);
                
                Color color = judgmentText.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                judgmentText.color = color;
                
                yield return null;
            }
            
            judgmentText.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// コンボアニメーションのコルーチン
        /// </summary>
        private IEnumerator AnimateCombo()
        {
            if (comboText == null) yield break;
            
            Transform comboTransform = comboText.transform;
            Vector3 originalScale = comboTransform.localScale;
            
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                
                // スケールをアニメーション
                float scale = scaleCurve.Evaluate(t);
                scale = 1f + (comboScaleMultiplier - 1f) * (1f - scale);
                comboTransform.localScale = originalScale * scale;
                
                yield return null;
            }
            
            comboTransform.localScale = originalScale;
        }
        
        /// <summary>
        /// 精度表示を更新
        /// </summary>
        public void UpdateAccuracy(float accuracy)
        {
            if (accuracyText != null)
            {
                accuracyText.text = string.Format(accuracyFormat, accuracy);
            }
        }
        
        /// <summary>
        /// 全UIをリセット
        /// </summary>
        public void ResetUI()
        {
            InitializeUI();
            
            // アニメーションコルーチンを停止
            if (judgmentDisplayCoroutine != null)
            {
                StopCoroutine(judgmentDisplayCoroutine);
                judgmentDisplayCoroutine = null;
            }
            
            if (comboAnimationCoroutine != null)
            {
                StopCoroutine(comboAnimationCoroutine);
                comboAnimationCoroutine = null;
            }
        }
        
        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}