using UnityEngine;
using UnityEngine.Profiling;
using System.Text;
using System.Collections.Generic;

namespace Jirou.UI
{
    /// <summary>
    /// UIのパフォーマンスを最適化するヘルパークラス
    /// 文字列キャッシュ、更新頻度制御、バッチング最適化を提供
    /// </summary>
    public class UIPerformanceOptimizer : MonoBehaviour
    {
        [Header("Update Settings")]
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] private bool limitUpdateRate = true;
        [SerializeField] private float minUpdateInterval = 0.016f; // 60FPS

        [Header("String Cache Settings")]
        [SerializeField] private int maxCachedNumbers = 10000;
        [SerializeField] private bool useCachedStrings = true;

        [Header("Performance Monitoring")]
        [SerializeField] private bool enableProfiling = false;
        [SerializeField] private float performanceLogInterval = 1f;

        // 文字列キャッシュ
        private Dictionary<int, string> cachedNumbers;
        private Dictionary<string, string> cachedFormats;
        private StringBuilder stringBuilder;

        // 更新制御
        private float updateInterval;
        private float lastUpdateTime;
        private float lastPerformanceLog;

        // パフォーマンス統計
        private int frameCount;
        private float deltaTimeAccumulator;
        private float averageFPS;
        private int uiUpdatesPerSecond;
        private int uiUpdateCounter;
        private float uiUpdateTimer;

        // 定数
        private const string SCORE_FORMAT = "D7";
        private const string COMBO_FORMAT = "D4";
        private const string COUNT_FORMAT = "D3";

        // シングルトンインスタンス（オプション）
        private static UIPerformanceOptimizer instance;
        public static UIPerformanceOptimizer Instance => instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                Initialize();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void Initialize()
        {
            // 更新間隔の計算
            updateInterval = limitUpdateRate ? minUpdateInterval : 0f;

            // StringBuilderの初期化
            stringBuilder = new StringBuilder(32);

            // キャッシュの初期化
            InitializeCaches();

            // FPS設定
            if (targetFrameRate > 0)
            {
                Application.targetFrameRate = (int)targetFrameRate;
            }
        }

        /// <summary>
        /// キャッシュの初期化
        /// </summary>
        private void InitializeCaches()
        {
            if (!useCachedStrings) return;

            cachedNumbers = new Dictionary<int, string>(maxCachedNumbers);
            cachedFormats = new Dictionary<string, string>();

            // 数値文字列のプリキャッシュ
            StartCoroutine(PreCacheNumbersAsync());
        }

        /// <summary>
        /// 非同期で数値をプリキャッシュ
        /// </summary>
        private System.Collections.IEnumerator PreCacheNumbersAsync()
        {
            int batchSize = 100;
            int currentBatch = 0;

            for (int i = 0; i < maxCachedNumbers; i++)
            {
                cachedNumbers[i] = i.ToString();
                currentBatch++;

                if (currentBatch >= batchSize)
                {
                    currentBatch = 0;
                    yield return null; // フレームを跨ぐ
                }
            }

            Debug.Log($"UIPerformanceOptimizer: Pre-cached {maxCachedNumbers} number strings");
        }

        /// <summary>
        /// 更新が必要かチェック
        /// </summary>
        public bool ShouldUpdate()
        {
            if (!limitUpdateRate) return true;

            float currentTime = Time.time;
            if (currentTime - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = currentTime;
                uiUpdateCounter++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// キャッシュから数値文字列を取得
        /// </summary>
        public string GetCachedNumber(int value)
        {
            if (!useCachedStrings) return value.ToString();

            if (cachedNumbers != null && cachedNumbers.TryGetValue(value, out string cached))
            {
                return cached;
            }

            // キャッシュミスの場合は新規作成してキャッシュ
            string newString = value.ToString();
            if (cachedNumbers != null && cachedNumbers.Count < maxCachedNumbers * 2)
            {
                cachedNumbers[value] = newString;
            }
            return newString;
        }

        /// <summary>
        /// フォーマット済み文字列を取得（スコア用）
        /// </summary>
        public string FormatScore(int score)
        {
            if (!useCachedStrings)
            {
                return score.ToString(SCORE_FORMAT);
            }

            stringBuilder.Clear();
            
            // 7桁にパディング
            string scoreStr = score.ToString();
            int padding = 7 - scoreStr.Length;
            for (int i = 0; i < padding; i++)
            {
                stringBuilder.Append('0');
            }
            stringBuilder.Append(scoreStr);

            return stringBuilder.ToString();
        }

        /// <summary>
        /// フォーマット済み文字列を取得（コンボ用）
        /// </summary>
        public string FormatCombo(int combo)
        {
            if (combo < 10000 && cachedNumbers != null && cachedNumbers.TryGetValue(combo, out string cached))
            {
                return cached;
            }
            return combo.ToString();
        }

        /// <summary>
        /// フォーマット済み文字列を取得（カウント用）
        /// </summary>
        public string FormatCount(int count)
        {
            if (!useCachedStrings)
            {
                return count.ToString(COUNT_FORMAT);
            }

            if (count > 999)
            {
                return "999+";
            }

            stringBuilder.Clear();
            
            // 3桁にパディング
            string countStr = count.ToString();
            int padding = 3 - countStr.Length;
            for (int i = 0; i < padding; i++)
            {
                stringBuilder.Append('0');
            }
            stringBuilder.Append(countStr);

            return stringBuilder.ToString();
        }

        /// <summary>
        /// UI更新のバッチング開始
        /// </summary>
        public void BeginUIBatch()
        {
            if (enableProfiling)
            {
                Profiler.BeginSample("UI Update Batch");
            }
        }

        /// <summary>
        /// UI更新のバッチング終了
        /// </summary>
        public void EndUIBatch()
        {
            if (enableProfiling)
            {
                Profiler.EndSample();
            }
        }

        /// <summary>
        /// Canvas更新の最適化設定
        /// </summary>
        public static void OptimizeCanvas(Canvas canvas)
        {
            if (canvas == null) return;

            // ピクセルパーフェクトを無効化（パフォーマンス向上）
            canvas.pixelPerfect = false;

            // 追加の最適化設定があればここに追加
        }

        /// <summary>
        /// TextMeshProの最適化設定
        /// </summary>
        public static void OptimizeTextMeshPro(TMPro.TextMeshProUGUI tmpText)
        {
            if (tmpText == null) return;

            // レイキャスト対象から除外
            tmpText.raycastTarget = false;

            // マテリアルプリセットの使用
            // 必要に応じて設定
        }

        void Update()
        {
            if (!enableProfiling) return;

            UpdatePerformanceStats();
        }

        /// <summary>
        /// パフォーマンス統計の更新
        /// </summary>
        private void UpdatePerformanceStats()
        {
            // FPS計算
            frameCount++;
            deltaTimeAccumulator += Time.unscaledDeltaTime;

            // UI更新頻度の計算
            uiUpdateTimer += Time.unscaledDeltaTime;
            if (uiUpdateTimer >= 1f)
            {
                uiUpdatesPerSecond = uiUpdateCounter;
                uiUpdateCounter = 0;
                uiUpdateTimer = 0f;
            }

            // ログ出力
            if (Time.time - lastPerformanceLog >= performanceLogInterval)
            {
                if (frameCount > 0)
                {
                    averageFPS = frameCount / deltaTimeAccumulator;
                    LogPerformanceStats();
                }

                frameCount = 0;
                deltaTimeAccumulator = 0f;
                lastPerformanceLog = Time.time;
            }
        }

        /// <summary>
        /// パフォーマンス統計のログ出力
        /// </summary>
        private void LogPerformanceStats()
        {
            stringBuilder.Clear();
            stringBuilder.AppendLine("=== UI Performance Stats ===");
            stringBuilder.AppendFormat("Average FPS: {0:F1}\n", averageFPS);
            stringBuilder.AppendFormat("UI Updates/sec: {0}\n", uiUpdatesPerSecond);
            stringBuilder.AppendFormat("Cached Strings: {0}\n", cachedNumbers?.Count ?? 0);
            
            if (UIEffectPool.Instance != null)
            {
                var poolStats = UIEffectPool.Instance.GetStatistics();
                stringBuilder.AppendFormat("Active Popups: {0}\n", poolStats.activePopups);
                stringBuilder.AppendFormat("Active Effects: {0}\n", poolStats.activeEffects);
            }

            Debug.Log(stringBuilder.ToString());
        }

        /// <summary>
        /// キャッシュをクリア
        /// </summary>
        public void ClearCaches()
        {
            cachedNumbers?.Clear();
            cachedFormats?.Clear();
            stringBuilder?.Clear();
        }

        /// <summary>
        /// メモリ使用量を取得
        /// </summary>
        public long GetMemoryUsage()
        {
            return Profiler.GetTotalAllocatedMemoryLong();
        }

        /// <summary>
        /// ガベージコレクションを強制実行
        /// </summary>
        public void ForceGarbageCollection()
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                ClearCaches();
                instance = null;
            }
        }

        /// <summary>
        /// パフォーマンス設定
        /// </summary>
        [System.Serializable]
        public class PerformanceSettings
        {
            public bool enableStringCaching = true;
            public bool enableUpdateRateLimiting = true;
            public bool enableObjectPooling = true;
            public int targetFPS = 60;
            public float uiUpdateRate = 60f;
        }

        /// <summary>
        /// パフォーマンス設定を適用
        /// </summary>
        public void ApplySettings(PerformanceSettings settings)
        {
            useCachedStrings = settings.enableStringCaching;
            limitUpdateRate = settings.enableUpdateRateLimiting;
            targetFrameRate = settings.targetFPS;
            minUpdateInterval = 1f / settings.uiUpdateRate;
            
            Application.targetFrameRate = settings.targetFPS;
        }
    }
}