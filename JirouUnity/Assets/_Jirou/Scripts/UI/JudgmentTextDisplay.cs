using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Jirou.Core;
using Jirou.Gameplay;

namespace Jirou.UI
{
    /// <summary>
    /// 判定結果をテキストで表示するシステム
    /// </summary>
    public class JudgmentTextDisplay : MonoBehaviour
    {
        [Header("Text Settings")]
        [SerializeField] private GameObject textPrefab;
        [SerializeField] private float displayDuration = 1.0f;
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("Judgment Settings")]
        [SerializeField] private JudgmentDisplaySettings[] judgmentSettings = new JudgmentDisplaySettings[]
        {
            new JudgmentDisplaySettings { type = JudgmentType.Perfect, displayText = "Perfect", color = Color.yellow, scale = 1.2f },
            new JudgmentDisplaySettings { type = JudgmentType.Great, displayText = "Great", color = Color.green, scale = 1.1f },
            new JudgmentDisplaySettings { type = JudgmentType.Good, displayText = "Good", color = Color.cyan, scale = 1.0f },
            new JudgmentDisplaySettings { type = JudgmentType.Miss, displayText = "Miss", color = Color.red, scale = 0.9f }
        };
        
        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 20;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        private Queue<GameObject> textPool;
        private Dictionary<JudgmentType, JudgmentDisplaySettings> settingsDict;
        private int activeTextCount = 0;
        private JudgmentType lastJudgmentType;
        
        [System.Serializable]
        public class JudgmentDisplaySettings
        {
            public JudgmentType type;
            public string displayText;
            public Color color = Color.white;
            public float scale = 1.0f;
        }
        
        void Awake()
        {
            InitializePool();
            InitializeSettings();
        }
        
        void OnEnable()
        {
            Debug.Log($"[JudgmentTextDisplay] OnEnable called. InputManager.Instance = {InputManager.Instance}");
            if (InputManager.Instance != null)
            {
                InputManager.OnNoteJudged += OnNoteJudged;
                Debug.Log("[JudgmentTextDisplay] Successfully subscribed to OnNoteJudged event");
            }
            else
            {
                Debug.LogWarning("[JudgmentTextDisplay] InputManager.Instance is null in OnEnable!");
                // 少し遅れて再試行
                StartCoroutine(DelayedEventSubscription());
            }
        }
        
        void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.OnNoteJudged -= OnNoteJudged;
                Debug.Log("[JudgmentTextDisplay] Unsubscribed from OnNoteJudged event");
            }
        }
        
        private System.Collections.IEnumerator DelayedEventSubscription()
        {
            yield return new WaitForSeconds(0.1f);
            if (InputManager.Instance != null)
            {
                InputManager.OnNoteJudged += OnNoteJudged;
                Debug.Log("[JudgmentTextDisplay] Successfully subscribed to OnNoteJudged event (delayed)");
            }
            else
            {
                Debug.LogError("[JudgmentTextDisplay] Failed to subscribe to event - InputManager.Instance is still null!");
            }
        }
        
        /// <summary>
        /// オブジェクトプールの初期化
        /// </summary>
        private void InitializePool()
        {
            textPool = new Queue<GameObject>();
            
            if (textPrefab == null)
            {
                Debug.LogError("[JudgmentTextDisplay] Text prefab is not assigned!");
                return;
            }
            
            for (int i = 0; i < poolSize; i++)
            {
                GameObject textObj = Instantiate(textPrefab, transform);
                textObj.SetActive(false);
                textPool.Enqueue(textObj);
            }
            
            Debug.Log($"[JudgmentTextDisplay] Object pool initialized with {poolSize} objects");
        }
        
        /// <summary>
        /// 設定の初期化
        /// </summary>
        private void InitializeSettings()
        {
            settingsDict = new Dictionary<JudgmentType, JudgmentDisplaySettings>();
            
            foreach (var setting in judgmentSettings)
            {
                if (!settingsDict.ContainsKey(setting.type))
                {
                    settingsDict.Add(setting.type, setting);
                }
            }
        }
        
        /// <summary>
        /// ノーツ判定時のイベントハンドラ
        /// </summary>
        private void OnNoteJudged(int laneIndex, JudgmentType judgment)
        {
            Debug.Log($"[JudgmentTextDisplay] OnNoteJudged received: Lane {laneIndex}, Judgment: {judgment}");
            lastJudgmentType = judgment;
            DisplayJudgmentText(laneIndex, judgment);
        }
        
        /// <summary>
        /// 判定テキストの表示
        /// </summary>
        private void DisplayJudgmentText(int laneIndex, JudgmentType judgment)
        {
            GameObject textObj = GetPooledText();
            
            if (textObj == null)
            {
                Debug.LogWarning("[JudgmentTextDisplay] No available text objects in pool!");
                return;
            }
            
            // 位置計算
            float xPosition = -3f + (laneIndex * 2f);
            float yPosition = 2.0f;
            float zPosition = 0.5f;
            Vector3 displayPosition = new Vector3(xPosition, yPosition, zPosition);
            
            // オブジェクトを配置してアクティブ化
            textObj.transform.position = displayPosition;
            textObj.SetActive(true);
            activeTextCount++;
            
            // テキストの設定
            TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
            if (tmp != null && settingsDict.ContainsKey(judgment))
            {
                var settings = settingsDict[judgment];
                tmp.text = settings.displayText;
                tmp.color = settings.color;
                textObj.transform.localScale = Vector3.one * settings.scale;
            }
            
            // アニメーション開始
            StartCoroutine(AnimateText(textObj, displayPosition));
        }
        
        /// <summary>
        /// プールからテキストオブジェクトを取得
        /// </summary>
        private GameObject GetPooledText()
        {
            if (textPool.Count > 0)
            {
                return textPool.Dequeue();
            }
            
            // プールが空の場合、新しいオブジェクトを作成（動的拡張）
            if (textPrefab != null)
            {
                GameObject newObj = Instantiate(textPrefab, transform);
                newObj.SetActive(false);
                Debug.Log("[JudgmentTextDisplay] Pool expanded - created new text object");
                return newObj;
            }
            
            return null;
        }
        
        /// <summary>
        /// オブジェクトをプールに返却
        /// </summary>
        private void ReturnToPool(GameObject textObj)
        {
            textObj.SetActive(false);
            textPool.Enqueue(textObj);
            activeTextCount--;
        }
        
        /// <summary>
        /// テキストのアニメーション処理
        /// </summary>
        private IEnumerator AnimateText(GameObject textObj, Vector3 startPos)
        {
            float elapsed = 0f;
            TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
            
            if (tmp == null)
            {
                ReturnToPool(textObj);
                yield break;
            }
            
            Color originalColor = tmp.color;
            Vector3 originalScale = textObj.transform.localScale;
            
            while (elapsed < displayDuration)
            {
                float t = elapsed / displayDuration;
                
                // 上方向への移動
                float yOffset = moveSpeed * t;
                textObj.transform.position = startPos + Vector3.up * yOffset;
                
                // フェードアウト
                float alpha = fadeCurve.Evaluate(t);
                Color currentColor = originalColor;
                currentColor.a = alpha;
                tmp.color = currentColor;
                
                // スケールアニメーション（バウンス効果）
                float scaleMultiplier = 1.0f + (0.3f * Mathf.Sin(t * Mathf.PI));
                textObj.transform.localScale = originalScale * scaleMultiplier;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // アニメーション終了後、プールに返却
            ReturnToPool(textObj);
        }
        
        /// <summary>
        /// アクティブなテキスト数を取得（デバッグ用）
        /// </summary>
        public int GetActiveTextCount()
        {
            return activeTextCount;
        }
        
        /// <summary>
        /// リセット処理
        /// </summary>
        public void ResetDisplay()
        {
            // すべてのアクティブなテキストを非表示にしてプールに戻す
            StopAllCoroutines();
            
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf)
                {
                    ReturnToPool(child.gameObject);
                }
            }
            
            activeTextCount = 0;
            Debug.Log("[JudgmentTextDisplay] Display reset");
        }
        
        #if UNITY_EDITOR
        void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            
            // 背景ボックス
            GUI.Box(new Rect(10, 120, 300, 80), "");
            
            GUI.Label(new Rect(20, 125, 280, 20), "Judgment Text Display Debug", style);
            GUI.Label(new Rect(20, 145, 280, 20), $"Active Texts: {activeTextCount}", style);
            GUI.Label(new Rect(20, 165, 280, 20), $"Pool Size: {textPool?.Count ?? 0}", style);
            GUI.Label(new Rect(20, 185, 280, 20), $"Last Judgment: {lastJudgmentType}", style);
        }
        #endif
        
        void OnDestroy()
        {
            // イベントの購読解除
            if (InputManager.Instance != null)
            {
                InputManager.OnNoteJudged -= OnNoteJudged;
            }
        }
    }
}