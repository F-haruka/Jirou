using UnityEngine;

namespace Jirou.Core
{
    /// <summary>
    /// ゲーム開始時に必要なコンポーネントを自動的に初期化するクラス
    /// シーンに1つ配置することで、必要なマネージャーを自動生成する
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Header("Auto Create Managers")]
        [SerializeField] private bool autoCreateScoreManager = true;
        [SerializeField] private bool autoCreateScoreUIEvents = true;
        
        void Awake()
        {
            Debug.Log("[GameInitializer] Starting game initialization...");
            
            // ScoreManagerの確認と作成
            if (autoCreateScoreManager)
            {
                EnsureScoreManagerExists();
            }
            
            // ScoreUIEventsの確認と作成（リフレクションを使用）
            if (autoCreateScoreUIEvents)
            {
                EnsureScoreUIEventsExists();
            }
            
            Debug.Log("[GameInitializer] Game initialization complete");
        }
        
        /// <summary>
        /// ScoreManagerが存在することを確認し、必要なら作成する
        /// </summary>
        private void EnsureScoreManagerExists()
        {
            var scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager == null)
            {
                Debug.Log("[GameInitializer] ScoreManager not found. Creating...");
                GameObject scoreManagerObj = new GameObject("ScoreManager");
                scoreManagerObj.AddComponent<ScoreManager>();
                Debug.Log("[GameInitializer] ScoreManager created successfully");
            }
            else
            {
                Debug.Log("[GameInitializer] ScoreManager already exists in scene");
            }
        }
        
        /// <summary>
        /// ScoreUIEventsが存在することを確認し、必要なら作成する
        /// </summary>
        private void EnsureScoreUIEventsExists()
        {
            // リフレクションを使用してScoreUIEventsクラスを探す
            var scoreUIEventsType = System.Type.GetType("Jirou.UI.ScoreUIEvents, Jirou.UI");
            
            if (scoreUIEventsType == null)
            {
                Debug.LogError("[GameInitializer] ScoreUIEvents class not found in Jirou.UI assembly!");
                return;
            }
            
            var scoreUIEvents = FindObjectOfType(scoreUIEventsType);
            if (scoreUIEvents == null)
            {
                Debug.Log("[GameInitializer] ScoreUIEvents not found. Creating...");
                GameObject scoreUIEventsObj = new GameObject("ScoreUIEvents");
                scoreUIEventsObj.AddComponent(scoreUIEventsType);
                Debug.Log("[GameInitializer] ScoreUIEvents created successfully");
            }
            else
            {
                Debug.Log("[GameInitializer] ScoreUIEvents already exists in scene");
            }
        }
        
        /// <summary>
        /// 必要に応じて他のマネージャーも初期化可能
        /// </summary>
        public void InitializeManagers()
        {
            // 将来的に他のマネージャーの初期化もここに追加可能
            Debug.Log("[GameInitializer] Initializing all managers...");
        }
    }
}