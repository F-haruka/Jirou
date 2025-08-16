using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jirou.Core;
using Jirou.Gameplay;

namespace Jirou.Visual
{
    /// <summary>
    /// 判定エフェクトを統合管理するシステム
    /// </summary>
    public class JudgmentEffectManager : MonoBehaviour
    {
        [Header("Effect Prefabs")]
        [SerializeField] private GameObject perfectEffectPrefab;
        [SerializeField] private GameObject greatEffectPrefab;
        [SerializeField] private GameObject goodEffectPrefab;
        [SerializeField] private GameObject missEffectPrefab;
        
        [Header("Effect Settings")]
        [SerializeField] private float effectDuration = 1.5f;
        [SerializeField] private bool useObjectPool = true;
        [SerializeField] private int poolSizePerType = 10;
        [SerializeField] private Vector3 effectPositionOffset = Vector3.zero;
        
        [Header("Quality Settings")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float lodDistance = 10f;
        [SerializeField] private float lodQualityReduction = 0.5f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        private Dictionary<JudgmentType, GameObject> effectPrefabs;
        private Dictionary<JudgmentType, Queue<GameObject>> effectPools;
        private Camera mainCamera;
        private int activeEffectsCount = 0;
        private JudgmentType lastJudgmentType;
        
        void Awake()
        {
            mainCamera = Camera.main;
            InitializePrefabDictionary();
            
            if (useObjectPool)
            {
                InitializeObjectPools();
            }
        }
        
        void OnEnable()
        {
            Debug.Log($"[JudgmentEffectManager] OnEnable called. InputManager.Instance = {InputManager.Instance}");
            if (InputManager.Instance != null)
            {
                InputManager.OnNoteJudged += OnNoteJudged;
                Debug.Log("[JudgmentEffectManager] Successfully subscribed to OnNoteJudged event");
            }
            else
            {
                Debug.LogWarning("[JudgmentEffectManager] InputManager.Instance is null in OnEnable!");
                StartCoroutine(DelayedEventSubscription());
            }
        }
        
        void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.OnNoteJudged -= OnNoteJudged;
                Debug.Log("[JudgmentEffectManager] Unsubscribed from OnNoteJudged event");
            }
        }
        
        private System.Collections.IEnumerator DelayedEventSubscription()
        {
            yield return new WaitForSeconds(0.1f);
            if (InputManager.Instance != null)
            {
                InputManager.OnNoteJudged += OnNoteJudged;
                Debug.Log("[JudgmentEffectManager] Successfully subscribed to OnNoteJudged event (delayed)");
            }
            else
            {
                Debug.LogError("[JudgmentEffectManager] Failed to subscribe to event - InputManager.Instance is still null!");
            }
        }
        
        /// <summary>
        /// プレハブ辞書の初期化
        /// </summary>
        private void InitializePrefabDictionary()
        {
            effectPrefabs = new Dictionary<JudgmentType, GameObject>
            {
                { JudgmentType.Perfect, perfectEffectPrefab },
                { JudgmentType.Great, greatEffectPrefab },
                { JudgmentType.Good, goodEffectPrefab },
                { JudgmentType.Miss, missEffectPrefab }
            };
            
            // プレハブの検証
            foreach (var kvp in effectPrefabs)
            {
                if (kvp.Value == null)
                {
                    Debug.LogWarning($"[JudgmentEffectManager] Effect prefab for {kvp.Key} is not assigned!");
                }
            }
        }
        
        /// <summary>
        /// オブジェクトプールの初期化
        /// </summary>
        private void InitializeObjectPools()
        {
            effectPools = new Dictionary<JudgmentType, Queue<GameObject>>();
            
            foreach (var kvp in effectPrefabs)
            {
                if (kvp.Value == null) continue;
                
                Queue<GameObject> pool = new Queue<GameObject>();
                
                for (int i = 0; i < poolSizePerType; i++)
                {
                    GameObject effect = Instantiate(kvp.Value, transform);
                    effect.SetActive(false);
                    pool.Enqueue(effect);
                }
                
                effectPools[kvp.Key] = pool;
                Debug.Log($"[JudgmentEffectManager] Pool for {kvp.Key} initialized with {poolSizePerType} objects");
            }
        }
        
        /// <summary>
        /// ノーツ判定時のイベントハンドラ
        /// </summary>
        private void OnNoteJudged(int laneIndex, JudgmentType judgment)
        {
            Debug.Log($"[JudgmentEffectManager] OnNoteJudged received: Lane {laneIndex}, Judgment: {judgment}");
            lastJudgmentType = judgment;
            
            // Miss以外の判定でエフェクトを生成（Missエフェクトも表示したい場合はこの条件を削除）
            SpawnEffect(laneIndex, judgment);
        }
        
        /// <summary>
        /// エフェクトの生成
        /// </summary>
        private void SpawnEffect(int laneIndex, JudgmentType judgment)
        {
            // プレハブが設定されていない場合はスキップ
            if (!effectPrefabs.ContainsKey(judgment) || effectPrefabs[judgment] == null)
            {
                Debug.LogWarning($"[JudgmentEffectManager] No effect prefab for {judgment}");
                return;
            }
            
            Vector3 effectPosition = GetEffectPosition(laneIndex) + effectPositionOffset;
            GameObject effect = null;
            
            if (useObjectPool)
            {
                effect = GetPooledEffect(judgment);
            }
            else
            {
                effect = Instantiate(effectPrefabs[judgment], effectPosition, Quaternion.identity, transform);
            }
            
            if (effect != null)
            {
                effect.transform.position = effectPosition;
                effect.SetActive(true);
                activeEffectsCount++;
                
                // LOD適用
                if (enableLOD && mainCamera != null)
                {
                    ApplyLOD(effect, effectPosition);
                }
                
                // エフェクトの自動非アクティブ化
                StartCoroutine(DeactivateAfterDelay(effect, judgment));
            }
        }
        
        /// <summary>
        /// エフェクト生成位置の計算
        /// </summary>
        private Vector3 GetEffectPosition(int laneIndex)
        {
            // レーンのX座標
            float xPosition = -3f + (laneIndex * 2f);
            
            // Y座標: 判定ライン上
            float yPosition = 0.5f;
            
            // Z座標: 判定ライン
            float zPosition = 0f;
            
            return new Vector3(xPosition, yPosition, zPosition);
        }
        
        /// <summary>
        /// プールからエフェクトを取得
        /// </summary>
        private GameObject GetPooledEffect(JudgmentType judgment)
        {
            if (!effectPools.ContainsKey(judgment))
            {
                Debug.LogWarning($"[JudgmentEffectManager] No pool for {judgment}");
                return null;
            }
            
            Queue<GameObject> pool = effectPools[judgment];
            
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
            
            // プールが空の場合、動的に拡張
            if (effectPrefabs[judgment] != null)
            {
                GameObject newEffect = Instantiate(effectPrefabs[judgment], transform);
                newEffect.SetActive(false);
                Debug.Log($"[JudgmentEffectManager] Pool expanded for {judgment}");
                return newEffect;
            }
            
            return null;
        }
        
        /// <summary>
        /// エフェクトをプールに返却
        /// </summary>
        private void ReturnToPool(GameObject effect, JudgmentType judgment)
        {
            effect.SetActive(false);
            
            if (effectPools.ContainsKey(judgment))
            {
                effectPools[judgment].Enqueue(effect);
            }
            
            activeEffectsCount--;
        }
        
        /// <summary>
        /// 一定時間後にエフェクトを非アクティブ化
        /// </summary>
        private IEnumerator DeactivateAfterDelay(GameObject effect, JudgmentType judgment)
        {
            yield return new WaitForSeconds(effectDuration);
            
            if (useObjectPool)
            {
                ReturnToPool(effect, judgment);
            }
            else
            {
                Destroy(effect);
                activeEffectsCount--;
            }
        }
        
        /// <summary>
        /// LOD（Level of Detail）の適用
        /// </summary>
        private void ApplyLOD(GameObject effect, Vector3 position)
        {
            if (mainCamera == null) return;
            
            float distance = Vector3.Distance(mainCamera.transform.position, position);
            
            if (distance > lodDistance)
            {
                // カメラから遠い場合、パーティクルの品質を下げる
                ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particles)
                {
                    var emission = ps.emission;
                    float originalRate = emission.rateOverTime.constant;
                    emission.rateOverTime = originalRate * lodQualityReduction;
                    
                    var main = ps.main;
                    main.maxParticles = Mathf.RoundToInt(main.maxParticles * lodQualityReduction);
                }
            }
        }
        
        /// <summary>
        /// プールサイズの動的調整
        /// </summary>
        public void ExpandPool(JudgmentType type, int additionalCount)
        {
            if (!effectPrefabs.ContainsKey(type) || effectPrefabs[type] == null) return;
            
            if (!effectPools.ContainsKey(type))
            {
                effectPools[type] = new Queue<GameObject>();
            }
            
            for (int i = 0; i < additionalCount; i++)
            {
                GameObject obj = Instantiate(effectPrefabs[type], transform);
                obj.SetActive(false);
                effectPools[type].Enqueue(obj);
            }
            
            Debug.Log($"[JudgmentEffectManager] Pool for {type} expanded by {additionalCount}");
        }
        
        /// <summary>
        /// すべてのエフェクトをリセット
        /// </summary>
        public void ResetEffects()
        {
            StopAllCoroutines();
            
            // すべてのアクティブなエフェクトを非表示にする
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                }
            }
            
            // プールを再初期化
            if (useObjectPool)
            {
                InitializeObjectPools();
            }
            
            activeEffectsCount = 0;
            Debug.Log("[JudgmentEffectManager] All effects reset");
        }
        
        /// <summary>
        /// エフェクトプレハブを動的に設定
        /// </summary>
        public void SetEffectPrefab(JudgmentType type, GameObject prefab)
        {
            effectPrefabs[type] = prefab;
            
            // プールを使用している場合は再初期化が必要
            if (useObjectPool && effectPools.ContainsKey(type))
            {
                // 既存のプールをクリア
                while (effectPools[type].Count > 0)
                {
                    GameObject oldEffect = effectPools[type].Dequeue();
                    Destroy(oldEffect);
                }
                
                // 新しいプレハブでプールを作成
                for (int i = 0; i < poolSizePerType; i++)
                {
                    GameObject effect = Instantiate(prefab, transform);
                    effect.SetActive(false);
                    effectPools[type].Enqueue(effect);
                }
            }
        }
        
        #if UNITY_EDITOR
        void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            
            // 背景ボックス
            GUI.Box(new Rect(10, 210, 300, 100), "");
            
            GUI.Label(new Rect(20, 215, 280, 20), "Judgment Effect Manager Debug", style);
            GUI.Label(new Rect(20, 235, 280, 20), $"Active Effects: {activeEffectsCount}", style);
            GUI.Label(new Rect(20, 255, 280, 20), $"Object Pooling: {(useObjectPool ? "Enabled" : "Disabled")}", style);
            GUI.Label(new Rect(20, 275, 280, 20), $"LOD: {(enableLOD ? "Enabled" : "Disabled")}", style);
            GUI.Label(new Rect(20, 295, 280, 20), $"Last Judgment: {lastJudgmentType}", style);
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