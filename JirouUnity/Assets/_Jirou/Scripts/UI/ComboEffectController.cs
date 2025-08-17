using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Jirou.UI
{
    /// <summary>
    /// コンボエフェクトを制御するコンポーネント
    /// マイルストーン到達時のパーティクルや画面エフェクトを管理
    /// </summary>
    public class ComboEffectController : MonoBehaviour
    {
        [Header("Milestone Effects")]
        [SerializeField] private ParticleSystem combo50Effect;
        [SerializeField] private ParticleSystem combo100Effect;
        [SerializeField] private ParticleSystem combo200Effect;
        [SerializeField] private ParticleSystem combo500Effect;
        [SerializeField] private ParticleSystem combo1000Effect;

        [Header("Screen Effects")]
        [SerializeField] private Image screenFlashImage;
        [SerializeField] private float flashDuration = 0.2f;
        [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Combo Text Effects")]
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private float pulseDuration = 0.3f;
        [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
        [SerializeField] private float maxPulseScale = 1.3f;

        [Header("Milestone Settings")]
        [SerializeField] private ComboMilestone[] milestones = new ComboMilestone[]
        {
            new ComboMilestone { comboCount = 50, flashColor = Color.yellow, flashIntensity = 0.3f },
            new ComboMilestone { comboCount = 100, flashColor = new Color(1f, 0.5f, 0f), flashIntensity = 0.4f },
            new ComboMilestone { comboCount = 200, flashColor = new Color(1f, 0f, 1f), flashIntensity = 0.5f },
            new ComboMilestone { comboCount = 500, flashColor = new Color(0.5f, 0f, 1f), flashIntensity = 0.6f },
            new ComboMilestone { comboCount = 1000, flashColor = new Color(1f, 0.843f, 0f), flashIntensity = 0.7f }
        };

        [Header("Continuous Effects")]
        [SerializeField] private ParticleSystem continuousEffect;
        [SerializeField] private int continuousEffectThreshold = 100;

        [Header("Break Effect")]
        [SerializeField] private float breakShakeDuration = 0.3f;
        [SerializeField] private float breakShakeIntensity = 10f;
        [SerializeField] private Color breakFlashColor = Color.red;

        // 内部状態
        private int lastMilestone = 0;
        private int currentCombo = 0;
        private Coroutine screenFlashCoroutine;
        private Coroutine pulseCoroutine;
        private Coroutine shakeCoroutine;
        private Vector3 originalTextPosition;
        private Vector3 originalTextScale;

        [System.Serializable]
        public class ComboMilestone
        {
            public int comboCount;
            public Color flashColor;
            public float flashIntensity;
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
            if (comboText != null)
            {
                originalTextPosition = comboText.transform.localPosition;
                originalTextScale = comboText.transform.localScale;
            }

            if (screenFlashImage != null)
            {
                screenFlashImage.gameObject.SetActive(false);
            }

            // パーティクルシステムの初期設定
            InitializeParticleSystem(combo50Effect);
            InitializeParticleSystem(combo100Effect);
            InitializeParticleSystem(combo200Effect);
            InitializeParticleSystem(combo500Effect);
            InitializeParticleSystem(combo1000Effect);
            InitializeParticleSystem(continuousEffect);
        }

        /// <summary>
        /// パーティクルシステムの初期化
        /// </summary>
        private void InitializeParticleSystem(ParticleSystem particleSystem)
        {
            if (particleSystem != null)
            {
                particleSystem.Stop();
                var main = particleSystem.main;
                main.playOnAwake = false;
            }
        }

        /// <summary>
        /// コンボ更新時の処理
        /// </summary>
        public void OnComboUpdate(int combo)
        {
            currentCombo = combo;

            // パルスアニメーション
            PulseComboText();

            // マイルストーンチェック
            CheckMilestone(combo);

            // 継続エフェクトの管理
            ManageContinuousEffect(combo);
        }

        /// <summary>
        /// マイルストーンのチェック
        /// </summary>
        private void CheckMilestone(int combo)
        {
            foreach (var milestone in milestones)
            {
                if (combo == milestone.comboCount && lastMilestone < milestone.comboCount)
                {
                    PlayMilestoneEffect(milestone);
                    lastMilestone = milestone.comboCount;
                    break;
                }
            }
        }

        /// <summary>
        /// マイルストーンエフェクトの再生
        /// </summary>
        private void PlayMilestoneEffect(ComboMilestone milestone)
        {
            // パーティクルエフェクト
            ParticleSystem effect = GetParticleSystemForMilestone(milestone.comboCount);
            if (effect != null)
            {
                effect.Play();
            }

            // 画面フラッシュ
            PlayScreenFlash(milestone.flashColor, milestone.flashIntensity);

            // 特別なアニメーション
            PlaySpecialAnimation(milestone.comboCount);
        }

        /// <summary>
        /// マイルストーンに対応するパーティクルシステムを取得
        /// </summary>
        private ParticleSystem GetParticleSystemForMilestone(int comboCount)
        {
            switch (comboCount)
            {
                case 50: return combo50Effect;
                case 100: return combo100Effect;
                case 200: return combo200Effect;
                case 500: return combo500Effect;
                case 1000: return combo1000Effect;
                default: return null;
            }
        }

        /// <summary>
        /// 画面フラッシュの再生
        /// </summary>
        private void PlayScreenFlash(Color color, float intensity)
        {
            if (screenFlashImage == null) return;

            if (screenFlashCoroutine != null)
            {
                StopCoroutine(screenFlashCoroutine);
            }

            screenFlashCoroutine = StartCoroutine(ScreenFlashCoroutine(color, intensity));
        }

        /// <summary>
        /// 画面フラッシュのコルーチン
        /// </summary>
        private IEnumerator ScreenFlashCoroutine(Color color, float intensity)
        {
            screenFlashImage.color = new Color(color.r, color.g, color.b, 0);
            screenFlashImage.gameObject.SetActive(true);

            float elapsed = 0f;

            // フラッシュイン
            float flashInDuration = flashDuration * 0.3f;
            while (elapsed < flashInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashInDuration;
                float alpha = flashCurve.Evaluate(t) * intensity;
                screenFlashImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            // フラッシュアウト
            elapsed = 0f;
            float flashOutDuration = flashDuration * 0.7f;
            while (elapsed < flashOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashOutDuration;
                float alpha = Mathf.Lerp(intensity, 0f, t);
                screenFlashImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            screenFlashImage.gameObject.SetActive(false);
            screenFlashCoroutine = null;
        }

        /// <summary>
        /// コンボテキストのパルスアニメーション
        /// </summary>
        private void PulseComboText()
        {
            if (comboText == null) return;

            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }

            pulseCoroutine = StartCoroutine(PulseTextCoroutine());
        }

        /// <summary>
        /// パルスアニメーションのコルーチン
        /// </summary>
        private IEnumerator PulseTextCoroutine()
        {
            if (comboText == null) yield break;

            float elapsed = 0f;

            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pulseDuration;
                float scale = pulseCurve.Evaluate(t);
                float actualScale = Mathf.Lerp(1f, maxPulseScale, scale);
                comboText.transform.localScale = originalTextScale * actualScale;
                yield return null;
            }

            comboText.transform.localScale = originalTextScale;
            pulseCoroutine = null;
        }

        /// <summary>
        /// 特別なアニメーションの再生
        /// </summary>
        private void PlaySpecialAnimation(int comboCount)
        {
            if (comboCount >= 1000)
            {
                StartCoroutine(UltraComboAnimation());
            }
            else if (comboCount >= 500)
            {
                StartCoroutine(SuperComboAnimation());
            }
            else if (comboCount >= 100)
            {
                StartCoroutine(GreatComboAnimation());
            }
        }

        /// <summary>
        /// ウルトラコンボアニメーション
        /// </summary>
        private IEnumerator UltraComboAnimation()
        {
            if (comboText == null) yield break;

            float duration = 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 虹色エフェクト
                float hue = Mathf.Repeat(t * 2f, 1f);
                comboText.color = Color.HSVToRGB(hue, 1f, 1f);
                
                // 回転
                float rotation = Mathf.Sin(t * Mathf.PI * 4) * 15f;
                comboText.transform.rotation = Quaternion.Euler(0, 0, rotation);

                yield return null;
            }

            comboText.color = Color.white;
            comboText.transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// スーパーコンボアニメーション
        /// </summary>
        private IEnumerator SuperComboAnimation()
        {
            if (comboText == null) yield break;

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // グロー効果（TextMeshProのアウトライン幅で表現）
                float glow = Mathf.Sin(t * Mathf.PI) * 0.5f + 0.5f;
                comboText.outlineWidth = glow * 0.5f;

                yield return null;
            }

            comboText.outlineWidth = 0.2f;
        }

        /// <summary>
        /// グレートコンボアニメーション
        /// </summary>
        private IEnumerator GreatComboAnimation()
        {
            if (comboText == null) yield break;

            // バウンスアニメーション
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float bounce = Mathf.Sin(t * Mathf.PI) * 20f;
                comboText.transform.localPosition = originalTextPosition + Vector3.up * bounce;
                yield return null;
            }

            comboText.transform.localPosition = originalTextPosition;
        }

        /// <summary>
        /// 継続エフェクトの管理
        /// </summary>
        private void ManageContinuousEffect(int combo)
        {
            if (continuousEffect == null) return;

            if (combo >= continuousEffectThreshold)
            {
                if (!continuousEffect.isPlaying)
                {
                    continuousEffect.Play();
                }

                // コンボ数に応じてパーティクルの強度を調整
                var emission = continuousEffect.emission;
                float intensity = Mathf.Clamp01((combo - continuousEffectThreshold) / 900f);
                emission.rateOverTime = Mathf.Lerp(10f, 50f, intensity);
            }
            else
            {
                if (continuousEffect.isPlaying)
                {
                    continuousEffect.Stop();
                }
            }
        }

        /// <summary>
        /// コンボブレイク時の処理
        /// </summary>
        public void OnComboBreak()
        {
            lastMilestone = 0;
            currentCombo = 0;

            // 継続エフェクトを停止
            if (continuousEffect != null && continuousEffect.isPlaying)
            {
                continuousEffect.Stop();
            }

            // ブレイクアニメーション
            PlayBreakAnimation();

            // ブレイクフラッシュ
            PlayScreenFlash(breakFlashColor, 0.2f);
        }

        /// <summary>
        /// ブレイクアニメーションの再生
        /// </summary>
        private void PlayBreakAnimation()
        {
            if (comboText == null) return;

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }

            shakeCoroutine = StartCoroutine(ShakeAnimationCoroutine());
        }

        /// <summary>
        /// シェイクアニメーションのコルーチン
        /// </summary>
        private IEnumerator ShakeAnimationCoroutine()
        {
            if (comboText == null) yield break;

            float elapsed = 0f;
            Color originalColor = comboText.color;

            // 赤く変色
            comboText.color = Color.red;

            while (elapsed < breakShakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / breakShakeDuration;
                
                // シェイク
                float shakeX = Random.Range(-breakShakeIntensity, breakShakeIntensity) * (1f - t);
                float shakeY = Random.Range(-breakShakeIntensity, breakShakeIntensity) * (1f - t);
                comboText.transform.localPosition = originalTextPosition + new Vector3(shakeX, shakeY, 0);

                // 色を徐々に戻す
                comboText.color = Color.Lerp(Color.red, originalColor, t);

                yield return null;
            }

            comboText.transform.localPosition = originalTextPosition;
            comboText.color = originalColor;
            shakeCoroutine = null;
        }

        /// <summary>
        /// すべてのエフェクトをリセット
        /// </summary>
        public void ResetAllEffects()
        {
            lastMilestone = 0;
            currentCombo = 0;

            // コルーチンを停止
            if (screenFlashCoroutine != null) StopCoroutine(screenFlashCoroutine);
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);

            // パーティクルを停止
            StopAllParticles();

            // UIをリセット
            if (screenFlashImage != null)
            {
                screenFlashImage.gameObject.SetActive(false);
            }

            if (comboText != null)
            {
                comboText.transform.localPosition = originalTextPosition;
                comboText.transform.localScale = originalTextScale;
                comboText.transform.rotation = Quaternion.identity;
                comboText.color = Color.white;
            }
        }

        /// <summary>
        /// すべてのパーティクルを停止
        /// </summary>
        private void StopAllParticles()
        {
            if (combo50Effect != null) combo50Effect.Stop();
            if (combo100Effect != null) combo100Effect.Stop();
            if (combo200Effect != null) combo200Effect.Stop();
            if (combo500Effect != null) combo500Effect.Stop();
            if (combo1000Effect != null) combo1000Effect.Stop();
            if (continuousEffect != null) continuousEffect.Stop();
        }

        void OnDestroy()
        {
            if (screenFlashCoroutine != null) StopCoroutine(screenFlashCoroutine);
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        }
    }
}