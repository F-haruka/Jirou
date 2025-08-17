using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Jirou.Core;

namespace Jirou.UI
{
    /// <summary>
    /// スコアUIシステムのデバッグ機能を提供するクラス
    /// テスト用のキー入力とストレステスト機能を実装
    /// </summary>
    public class ScoreUIDebugger : MonoBehaviour
    {
        [Header("Debug Controls")]
        [SerializeField] private bool enableDebugKeys = true;
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private KeyCode toggleDebugKey = KeyCode.F1;

        [Header("Score Settings")]
        [SerializeField] private int scoreIncrement = 1000;
        [SerializeField] private int comboIncrement = 10;

        [Header("Stress Test Settings")]
        [SerializeField] private float stressTestDuration = 10f;
        [SerializeField] private float stressTestInterval = 0.05f;
        [SerializeField] private bool randomizeJudgments = true;

        [Header("Auto Play Settings")]
        [SerializeField] private bool enableAutoPlay = false;
        [SerializeField] private float autoPlayBPM = 120f;
        [SerializeField] private float autoPlayAccuracy = 0.9f; // 90%精度

        [Header("References")]
        [SerializeField] private ScoreUIManager uiManager;

        // デバッグ情報表示用
        private bool showDebugOverlay = false;
        private GUIStyle debugStyle;
        private Rect debugRect;

        // 統計情報
        private int totalJudgments = 0;
        private int currentScore = 0;
        private int currentCombo = 0;
        private float sessionTime = 0f;
        private bool isStressTesting = false;
        private Coroutine stressTestCoroutine;
        private Coroutine autoPlayCoroutine;

        // デバッグログ
        private Queue<string> debugLog = new Queue<string>();
        private const int MAX_LOG_ENTRIES = 10;

        void Awake()
        {
            if (uiManager == null)
            {
                uiManager = GetComponent<ScoreUIManager>();
                if (uiManager == null)
                {
                    uiManager = FindObjectOfType<ScoreUIManager>();
                }
            }

            InitializeDebugStyle();
        }

        void Start()
        {
            Debug.Log("[ScoreUIDebugger] Start called");
            
            if (uiManager == null)
            {
                Debug.LogWarning("[ScoreUIDebugger] ScoreUIManager not found! Trying to find it again...");
                uiManager = FindObjectOfType<ScoreUIManager>();
                
                if (uiManager == null)
                {
                    Debug.LogError("[ScoreUIDebugger] ScoreUIManager still not found! Make sure ScoreUICanvas exists and has ScoreUIManager component.");
                    Debug.LogError("[ScoreUIDebugger] ScoreUIDebugger will be disabled.");
                    enabled = false;
                    return;
                }
                else
                {
                    Debug.Log("[ScoreUIDebugger] ScoreUIManager found in Start!");
                }
            }
            else
            {
                Debug.Log("[ScoreUIDebugger] ScoreUIManager was already assigned");
            }

            if (enableAutoPlay)
            {
                StartAutoPlay();
            }
        }

        /// <summary>
        /// デバッグスタイルの初期化
        /// </summary>
        private void InitializeDebugStyle()
        {
            debugStyle = new GUIStyle();
            debugStyle.normal.textColor = Color.white;
            debugStyle.fontSize = 12;
            debugStyle.normal.background = Texture2D.whiteTexture;
            
            debugRect = new Rect(10, 10, 400, 300);
        }

        void Update()
        {
            if (!enableDebugKeys) return;

            sessionTime += Time.deltaTime;
            HandleDebugInput();
        }

        /// <summary>
        /// デバッグ入力の処理
        /// </summary>
        private void HandleDebugInput()
        {
            // デバッグ表示の切り替え
            if (Input.GetKeyDown(toggleDebugKey))
            {
                showDebugOverlay = !showDebugOverlay;
                AddDebugLog($"Debug overlay: {(showDebugOverlay ? "ON" : "OFF")}");
            }

            // 判定シミュレート (1-4キー)
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SimulateJudgment(JudgmentType.Perfect);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SimulateJudgment(JudgmentType.Great);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SimulateJudgment(JudgmentType.Good);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SimulateJudgment(JudgmentType.Miss);
            }

            // スコア直接追加 (Space)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                AddScore(scoreIncrement);
                AddDebugLog($"Added {scoreIncrement} points");
            }

            // コンボ増加 (C)
            if (Input.GetKeyDown(KeyCode.C))
            {
                currentCombo += comboIncrement;
                ScoreUIEvents.TriggerComboChange(currentCombo);
                AddDebugLog($"Combo: {currentCombo}");
            }

            // コンボブレイク (B)
            if (Input.GetKeyDown(KeyCode.B))
            {
                currentCombo = 0;
                ScoreUIEvents.TriggerComboBreak();
                AddDebugLog("Combo break!");
            }

            // リセット (R)
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetAll();
            }

            // ストレステスト (T)
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (!isStressTesting)
                {
                    StartStressTest();
                }
                else
                {
                    StopStressTest();
                }
            }

            // オートプレイ切り替え (A)
            if (Input.GetKeyDown(KeyCode.A))
            {
                enableAutoPlay = !enableAutoPlay;
                if (enableAutoPlay)
                {
                    StartAutoPlay();
                }
                else
                {
                    StopAutoPlay();
                }
                AddDebugLog($"Auto-play: {(enableAutoPlay ? "ON" : "OFF")}");
            }

            // パフォーマンステスト (P)
            if (Input.GetKeyDown(KeyCode.P))
            {
                RunPerformanceTest();
            }

            // メモリ情報表示 (M)
            if (Input.GetKeyDown(KeyCode.M))
            {
                LogMemoryInfo();
            }
        }

        /// <summary>
        /// 判定をシミュレート
        /// </summary>
        private void SimulateJudgment(JudgmentType type)
        {
            ScoreUIEvents.TriggerJudgment(type);
            totalJudgments++;

            int points = GetPointsForJudgment(type);
            
            if (type != JudgmentType.Miss)
            {
                currentCombo++;
                currentScore += points;
                ScoreUIEvents.TriggerComboChange(currentCombo);
            }
            else
            {
                currentCombo = 0;
                ScoreUIEvents.TriggerComboBreak();
            }

            ScoreUIEvents.TriggerScoreChange(currentScore);
            
            AddDebugLog($"{type}: +{points} points, Combo: {currentCombo}");
        }

        /// <summary>
        /// 判定タイプに応じたポイントを取得
        /// </summary>
        private int GetPointsForJudgment(JudgmentType type)
        {
            switch (type)
            {
                case JudgmentType.Perfect:
                    return 1000 + (currentCombo * 10);
                case JudgmentType.Great:
                    return 500 + (currentCombo * 5);
                case JudgmentType.Good:
                    return 100 + (currentCombo * 2);
                case JudgmentType.Miss:
                    return 0;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// スコアを追加
        /// </summary>
        private void AddScore(int points)
        {
            currentScore += points;
            ScoreUIEvents.TriggerScoreChange(currentScore);
        }

        /// <summary>
        /// すべてリセット
        /// </summary>
        private void ResetAll()
        {
            currentScore = 0;
            currentCombo = 0;
            totalJudgments = 0;
            sessionTime = 0f;

            if (uiManager != null)
            {
                uiManager.ResetAll();
            }

            AddDebugLog("All values reset");
        }

        /// <summary>
        /// ストレステスト開始
        /// </summary>
        private void StartStressTest()
        {
            if (stressTestCoroutine != null)
            {
                StopCoroutine(stressTestCoroutine);
            }

            isStressTesting = true;
            stressTestCoroutine = StartCoroutine(StressTestCoroutine());
            AddDebugLog("Stress test started");
        }

        /// <summary>
        /// ストレステスト停止
        /// </summary>
        private void StopStressTest()
        {
            if (stressTestCoroutine != null)
            {
                StopCoroutine(stressTestCoroutine);
                stressTestCoroutine = null;
            }

            isStressTesting = false;
            AddDebugLog("Stress test stopped");
        }

        /// <summary>
        /// ストレステストのコルーチン
        /// </summary>
        private IEnumerator StressTestCoroutine()
        {
            float elapsed = 0f;
            int testJudgments = 0;

            while (elapsed < stressTestDuration)
            {
                JudgmentType type;
                
                if (randomizeJudgments)
                {
                    type = (JudgmentType)Random.Range(0, 4);
                }
                else
                {
                    // パターンテスト
                    type = (JudgmentType)(testJudgments % 4);
                }

                SimulateJudgment(type);
                testJudgments++;

                yield return new WaitForSeconds(stressTestInterval);
                elapsed += stressTestInterval;
            }

            isStressTesting = false;
            AddDebugLog($"Stress test complete: {testJudgments} judgments in {stressTestDuration}s");
            stressTestCoroutine = null;
        }

        /// <summary>
        /// オートプレイ開始
        /// </summary>
        private void StartAutoPlay()
        {
            if (autoPlayCoroutine != null)
            {
                StopCoroutine(autoPlayCoroutine);
            }

            autoPlayCoroutine = StartCoroutine(AutoPlayCoroutine());
        }

        /// <summary>
        /// オートプレイ停止
        /// </summary>
        private void StopAutoPlay()
        {
            if (autoPlayCoroutine != null)
            {
                StopCoroutine(autoPlayCoroutine);
                autoPlayCoroutine = null;
            }
        }

        /// <summary>
        /// オートプレイのコルーチン
        /// </summary>
        private IEnumerator AutoPlayCoroutine()
        {
            float beatInterval = 60f / autoPlayBPM;

            while (enableAutoPlay)
            {
                // 精度に基づいて判定を決定
                float random = Random.value;
                JudgmentType type;

                if (random < autoPlayAccuracy * 0.6f)
                {
                    type = JudgmentType.Perfect;
                }
                else if (random < autoPlayAccuracy * 0.85f)
                {
                    type = JudgmentType.Great;
                }
                else if (random < autoPlayAccuracy)
                {
                    type = JudgmentType.Good;
                }
                else
                {
                    type = JudgmentType.Miss;
                }

                SimulateJudgment(type);

                yield return new WaitForSeconds(beatInterval);
            }
        }

        /// <summary>
        /// パフォーマンステスト実行
        /// </summary>
        private void RunPerformanceTest()
        {
            AddDebugLog("Running performance test...");
            
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            // 1000回の判定を一気に実行
            for (int i = 0; i < 1000; i++)
            {
                JudgmentType type = (JudgmentType)(i % 4);
                ScoreUIEvents.TriggerJudgment(type);
                ScoreUIEvents.TriggerScoreChange(i * 100);
                
                if (i % 10 == 0)
                {
                    ScoreUIEvents.TriggerComboChange(i / 10);
                }
            }

            stopwatch.Stop();
            
            AddDebugLog($"Performance test: 1000 updates in {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// メモリ情報をログ出力
        /// </summary>
        private void LogMemoryInfo()
        {
            long totalMemory = System.GC.GetTotalMemory(false);
            string memoryInfo = $"Memory: {totalMemory / 1024 / 1024}MB";
            
            if (UIEffectPool.Instance != null)
            {
                var stats = UIEffectPool.Instance.GetStatistics();
                memoryInfo += $"\nPool - Popups: {stats.activePopups}/{stats.currentPopupsInPool}";
                memoryInfo += $"\nPool - Effects: {stats.activeEffects}/{stats.currentEffectsInPool}";
            }

            AddDebugLog(memoryInfo);
        }

        /// <summary>
        /// デバッグログに追加
        /// </summary>
        private void AddDebugLog(string message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            debugLog.Enqueue($"[{timestamp}] {message}");
            
            while (debugLog.Count > MAX_LOG_ENTRIES)
            {
                debugLog.Dequeue();
            }

            Debug.Log($"[ScoreUIDebugger] {message}");
        }

        void OnGUI()
        {
            if (!showDebugInfo || !showDebugOverlay) return;

            // 背景
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(debugRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(debugRect);
            GUILayout.Label("=== Score UI Debug Info ===", debugStyle);
            GUILayout.Label($"Session Time: {sessionTime:F1}s", debugStyle);
            GUILayout.Label($"Score: {currentScore}", debugStyle);
            GUILayout.Label($"Combo: {currentCombo}", debugStyle);
            GUILayout.Label($"Total Judgments: {totalJudgments}", debugStyle);
            GUILayout.Label($"Stress Test: {(isStressTesting ? "RUNNING" : "OFF")}", debugStyle);
            GUILayout.Label($"Auto Play: {(enableAutoPlay ? "ON" : "OFF")}", debugStyle);

            GUILayout.Space(10);
            GUILayout.Label("--- Debug Log ---", debugStyle);
            
            foreach (string log in debugLog)
            {
                GUILayout.Label(log, debugStyle);
            }

            GUILayout.Space(10);
            GUILayout.Label("--- Controls ---", debugStyle);
            GUILayout.Label("1-4: Judgments | Space: +Score", debugStyle);
            GUILayout.Label("C: +Combo | B: Break | R: Reset", debugStyle);
            GUILayout.Label("T: Stress Test | A: Auto Play", debugStyle);
            GUILayout.Label("P: Perf Test | M: Memory | F1: Toggle", debugStyle);

            GUILayout.EndArea();
        }

        void OnDestroy()
        {
            StopStressTest();
            StopAutoPlay();
        }
    }
}