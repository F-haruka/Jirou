using UnityEngine;
using System.Collections.Generic;

namespace Jirou.UI
{
    /// <summary>
    /// UIエフェクトのオブジェクトプールを管理するシングルトンクラス
    /// 判定ポップアップやエフェクトの再利用を効率的に行う
    /// </summary>
    public class UIEffectPool : MonoBehaviour
    {
        private static UIEffectPool instance;
        public static UIEffectPool Instance => instance;

        [Header("Prefabs")]
        [SerializeField] private GameObject judgmentPopupPrefab;
        [SerializeField] private GameObject comboEffectPrefab;
        [SerializeField] private GameObject scorePopupPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private int maxPoolSize = 50;
        [SerializeField] private bool expandable = true;

        [Header("Parent Containers")]
        [SerializeField] private Transform popupContainer;
        [SerializeField] private Transform effectContainer;

        // プール管理
        private Queue<JudgmentPopup> popupPool;
        private Queue<GameObject> effectPool;
        private Queue<GameObject> scorePopupPool;
        private List<JudgmentPopup> activePopups;
        private List<GameObject> activeEffects;

        // 統計情報
        private int totalPopupsCreated = 0;
        private int totalEffectsCreated = 0;
        private int peakActivePopups = 0;
        private int peakActiveEffects = 0;

        void Awake()
        {
            // シングルトンの設定
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePools();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// プールの初期化
        /// </summary>
        private void InitializePools()
        {
            // コンテナの作成
            CreateContainers();

            // プールの初期化
            popupPool = new Queue<JudgmentPopup>();
            effectPool = new Queue<GameObject>();
            scorePopupPool = new Queue<GameObject>();
            activePopups = new List<JudgmentPopup>();
            activeEffects = new List<GameObject>();

            // 初期オブジェクトの生成
            PrewarmPool();
        }

        /// <summary>
        /// コンテナの作成
        /// </summary>
        private void CreateContainers()
        {
            if (popupContainer == null)
            {
                GameObject popupContainerObj = new GameObject("PopupContainer");
                popupContainerObj.transform.SetParent(transform);
                popupContainer = popupContainerObj.transform;
            }

            if (effectContainer == null)
            {
                GameObject effectContainerObj = new GameObject("EffectContainer");
                effectContainerObj.transform.SetParent(transform);
                effectContainer = effectContainerObj.transform;
            }
        }

        /// <summary>
        /// プールの事前生成
        /// </summary>
        private void PrewarmPool()
        {
            // 判定ポップアップの事前生成
            if (judgmentPopupPrefab != null)
            {
                for (int i = 0; i < initialPoolSize; i++)
                {
                    CreateNewPopup();
                }
            }

            // エフェクトの事前生成
            if (comboEffectPrefab != null)
            {
                for (int i = 0; i < initialPoolSize / 2; i++)
                {
                    CreateNewEffect();
                }
            }

            // スコアポップアップの事前生成
            if (scorePopupPrefab != null)
            {
                for (int i = 0; i < initialPoolSize / 2; i++)
                {
                    CreateNewScorePopup();
                }
            }
        }

        /// <summary>
        /// 新しい判定ポップアップを作成
        /// </summary>
        private JudgmentPopup CreateNewPopup()
        {
            if (judgmentPopupPrefab == null)
            {
                Debug.LogError("UIEffectPool: JudgmentPopup prefab is not assigned!");
                return null;
            }

            GameObject obj = Instantiate(judgmentPopupPrefab, popupContainer);
            obj.SetActive(false);
            JudgmentPopup popup = obj.GetComponent<JudgmentPopup>();
            
            if (popup == null)
            {
                popup = obj.AddComponent<JudgmentPopup>();
            }

            popupPool.Enqueue(popup);
            totalPopupsCreated++;

            return popup;
        }

        /// <summary>
        /// 新しいエフェクトを作成
        /// </summary>
        private GameObject CreateNewEffect()
        {
            if (comboEffectPrefab == null)
            {
                Debug.LogWarning("UIEffectPool: ComboEffect prefab is not assigned");
                return null;
            }

            GameObject obj = Instantiate(comboEffectPrefab, effectContainer);
            obj.SetActive(false);
            effectPool.Enqueue(obj);
            totalEffectsCreated++;

            return obj;
        }

        /// <summary>
        /// 新しいスコアポップアップを作成
        /// </summary>
        private GameObject CreateNewScorePopup()
        {
            if (scorePopupPrefab == null)
            {
                return null;
            }

            GameObject obj = Instantiate(scorePopupPrefab, popupContainer);
            obj.SetActive(false);
            scorePopupPool.Enqueue(obj);

            return obj;
        }

        /// <summary>
        /// 判定ポップアップを取得
        /// </summary>
        public JudgmentPopup GetPopup()
        {
            JudgmentPopup popup = null;

            // プールから取得
            if (popupPool.Count > 0)
            {
                popup = popupPool.Dequeue();
            }
            // プールが空の場合
            else if (expandable || totalPopupsCreated < maxPoolSize)
            {
                popup = CreateNewPopup();
                if (popup != null)
                {
                    popupPool.Dequeue(); // 作成時にキューに入るので取り出す
                }
            }
            else
            {
                Debug.LogWarning("UIEffectPool: Popup pool exhausted and expansion disabled");
                return null;
            }

            if (popup != null)
            {
                popup.gameObject.SetActive(true);
                activePopups.Add(popup);
                UpdatePeakStats();
            }

            return popup;
        }

        /// <summary>
        /// 判定ポップアップを返却
        /// </summary>
        public void ReturnPopup(JudgmentPopup popup)
        {
            if (popup == null) return;

            popup.Reset();
            popup.gameObject.SetActive(false);
            
            if (activePopups.Contains(popup))
            {
                activePopups.Remove(popup);
            }

            if (!popupPool.Contains(popup))
            {
                popupPool.Enqueue(popup);
            }
        }

        /// <summary>
        /// エフェクトを取得
        /// </summary>
        public GameObject GetEffect()
        {
            GameObject effect = null;

            // プールから取得
            if (effectPool.Count > 0)
            {
                effect = effectPool.Dequeue();
            }
            // プールが空の場合
            else if (expandable || totalEffectsCreated < maxPoolSize)
            {
                effect = CreateNewEffect();
                if (effect != null)
                {
                    effectPool.Dequeue(); // 作成時にキューに入るので取り出す
                }
            }
            else
            {
                Debug.LogWarning("UIEffectPool: Effect pool exhausted and expansion disabled");
                return null;
            }

            if (effect != null)
            {
                effect.SetActive(true);
                activeEffects.Add(effect);
                UpdatePeakStats();
            }

            return effect;
        }

        /// <summary>
        /// エフェクトを返却
        /// </summary>
        public void ReturnEffect(GameObject effect)
        {
            if (effect == null) return;

            effect.SetActive(false);
            
            if (activeEffects.Contains(effect))
            {
                activeEffects.Remove(effect);
            }

            if (!effectPool.Contains(effect))
            {
                effectPool.Enqueue(effect);
            }
        }

        /// <summary>
        /// スコアポップアップを取得
        /// </summary>
        public GameObject GetScorePopup()
        {
            GameObject popup = null;

            if (scorePopupPool.Count > 0)
            {
                popup = scorePopupPool.Dequeue();
            }
            else if (scorePopupPrefab != null)
            {
                popup = CreateNewScorePopup();
                if (popup != null)
                {
                    scorePopupPool.Dequeue();
                }
            }

            if (popup != null)
            {
                popup.SetActive(true);
            }

            return popup;
        }

        /// <summary>
        /// スコアポップアップを返却
        /// </summary>
        public void ReturnScorePopup(GameObject popup)
        {
            if (popup == null) return;

            popup.SetActive(false);
            
            if (!scorePopupPool.Contains(popup))
            {
                scorePopupPool.Enqueue(popup);
            }
        }

        /// <summary>
        /// すべてのアクティブなエフェクトを返却
        /// </summary>
        public void ReturnAllActive()
        {
            // ポップアップを返却
            for (int i = activePopups.Count - 1; i >= 0; i--)
            {
                ReturnPopup(activePopups[i]);
            }

            // エフェクトを返却
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                ReturnEffect(activeEffects[i]);
            }
        }

        /// <summary>
        /// プールをクリア
        /// </summary>
        public void ClearPool()
        {
            ReturnAllActive();

            // オブジェクトを破棄
            while (popupPool.Count > 0)
            {
                var popup = popupPool.Dequeue();
                if (popup != null)
                {
                    Destroy(popup.gameObject);
                }
            }

            while (effectPool.Count > 0)
            {
                var effect = effectPool.Dequeue();
                if (effect != null)
                {
                    Destroy(effect);
                }
            }

            while (scorePopupPool.Count > 0)
            {
                var popup = scorePopupPool.Dequeue();
                if (popup != null)
                {
                    Destroy(popup);
                }
            }

            // 統計をリセット
            totalPopupsCreated = 0;
            totalEffectsCreated = 0;
            peakActivePopups = 0;
            peakActiveEffects = 0;
        }

        /// <summary>
        /// ピーク統計の更新
        /// </summary>
        private void UpdatePeakStats()
        {
            if (activePopups.Count > peakActivePopups)
            {
                peakActivePopups = activePopups.Count;
            }

            if (activeEffects.Count > peakActiveEffects)
            {
                peakActiveEffects = activeEffects.Count;
            }
        }

        /// <summary>
        /// プールの統計情報を取得
        /// </summary>
        public PoolStatistics GetStatistics()
        {
            return new PoolStatistics
            {
                totalPopupsCreated = totalPopupsCreated,
                totalEffectsCreated = totalEffectsCreated,
                currentPopupsInPool = popupPool.Count,
                currentEffectsInPool = effectPool.Count,
                activePopups = activePopups.Count,
                activeEffects = activeEffects.Count,
                peakActivePopups = peakActivePopups,
                peakActiveEffects = peakActiveEffects
            };
        }

        /// <summary>
        /// プールサイズを調整
        /// </summary>
        public void ResizePool(int newSize)
        {
            if (newSize < 0) return;

            // 縮小の場合
            if (newSize < popupPool.Count)
            {
                int removeCount = popupPool.Count - newSize;
                for (int i = 0; i < removeCount; i++)
                {
                    if (popupPool.Count > 0)
                    {
                        var popup = popupPool.Dequeue();
                        if (popup != null)
                        {
                            Destroy(popup.gameObject);
                        }
                        totalPopupsCreated--;
                    }
                }
            }
            // 拡大の場合
            else if (newSize > popupPool.Count)
            {
                int addCount = newSize - popupPool.Count;
                for (int i = 0; i < addCount; i++)
                {
                    CreateNewPopup();
                }
            }
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                ClearPool();
                instance = null;
            }
        }

        /// <summary>
        /// プール統計情報構造体
        /// </summary>
        [System.Serializable]
        public struct PoolStatistics
        {
            public int totalPopupsCreated;
            public int totalEffectsCreated;
            public int currentPopupsInPool;
            public int currentEffectsInPool;
            public int activePopups;
            public int activeEffects;
            public int peakActivePopups;
            public int peakActiveEffects;
        }
    }
}