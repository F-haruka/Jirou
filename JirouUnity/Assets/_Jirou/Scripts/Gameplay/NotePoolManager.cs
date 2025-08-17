using System.Collections.Generic;
using UnityEngine;
using Jirou.Core;

namespace Jirou.Gameplay
{
    /// <summary>
    /// ノーツのオブジェクトプールを管理するシングルトンクラス
    /// パフォーマンス最適化のため、ノーツの生成と破棄をプールで管理
    /// </summary>
    public class NotePoolManager : MonoBehaviour
    {
        // ========== シングルトン実装 ==========
        
        private static NotePoolManager instance;
        public static NotePoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("NotePoolManager");
                    instance = go.AddComponent<NotePoolManager>();
                    // DontDestroyOnLoadはPlayModeでのみ使用可能
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        // ========== プール設定 ==========
        
        [Header("プール設定")]
        [Tooltip("初期プールサイズ（各タイプ）")]
        public int initialPoolSize = 20;
        
        [Tooltip("最大プールサイズ（各タイプ）")]
        public int maxPoolSize = 50;
        
        [Header("プレハブ参照")]
        [Tooltip("Tapノーツのプレハブ")]
        public GameObject tapNotePrefab;
        
        [Tooltip("Holdノーツのプレハブ")]
        public GameObject holdNotePrefab;
        
        [Header("デバッグ")]
        [Tooltip("デバッグログを有効化")]
        public bool enableDebugLog = false;
        
        // ========== プライベートフィールド ==========
        
        private Queue<GameObject> tapNotePool = new Queue<GameObject>();
        private Queue<GameObject> holdNotePool = new Queue<GameObject>();
        private Transform poolContainer;
        
        // Prefabの元のスケールを保持
        private Vector3 tapNotePrefabScale;
        private Vector3 holdNotePrefabScale;
        
        // Prefabの元の色を保持
        private Color tapNotePrefabColor = Color.cyan;  // 青色をデフォルトに
        private Color holdNotePrefabColor = Color.yellow;  // 黄色をデフォルトに
        
        // 統計情報
        private int totalTapCreated = 0;
        private int totalHoldCreated = 0;
        private int tapPoolHits = 0;
        private int holdPoolHits = 0;
        private int tapPoolMisses = 0;
        private int holdPoolMisses = 0;
        
        // ========== Unity イベントメソッド ==========
        
        void Awake()
        {
            // シングルトンの重複チェック
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
            
            Cleanup();
        }
        
        // ========== 初期化メソッド ==========
        
        private void Initialize()
        {
            // プールコンテナの作成
            poolContainer = new GameObject("NotePoolContainer").transform;
            poolContainer.SetParent(transform, false);  // falseでワールド位置を維持
            poolContainer.localScale = Vector3.one;  // スケールを明示的に(1,1,1)に設定
            
            // プレハブの検証
            if (!ValidatePrefabs())
            {
                Debug.LogError("[NotePoolManager] プレハブが設定されていません！");
                enabled = false;
                return;
            }
            
            // 初期プールの作成
            CreateInitialPool();
            
            LogDebug($"NotePoolManager初期化完了 - 初期プールサイズ: {initialPoolSize}");
        }
        
        private bool ValidatePrefabs()
        {
            // プレハブが設定されているかチェック
            // 設定されていない場合は、NoteSpawnerから取得を試みる
            if (tapNotePrefab == null || holdNotePrefab == null)
            {
                NoteSpawner spawner = FindObjectOfType<NoteSpawner>();
                if (spawner != null)
                {
                    if (tapNotePrefab == null) tapNotePrefab = spawner.tapNotePrefab;
                    if (holdNotePrefab == null) holdNotePrefab = spawner.holdNotePrefab;
                }
            }
            
            // Prefabの元のスケールを保存
            if (tapNotePrefab != null)
            {
                tapNotePrefabScale = tapNotePrefab.transform.localScale;
                LogDebug($"TapNote Prefabスケール保存: {tapNotePrefabScale}");
                
                // Prefabの元の色を保存
                Renderer tapRenderer = tapNotePrefab.GetComponent<Renderer>();
                if (tapRenderer != null && tapRenderer.sharedMaterial != null)
                {
                    tapNotePrefabColor = tapRenderer.sharedMaterial.color;
                    LogDebug($"TapNote Prefab色保存: {tapNotePrefabColor}");
                }
            }
            if (holdNotePrefab != null)
            {
                holdNotePrefabScale = holdNotePrefab.transform.localScale;
                LogDebug($"HoldNote Prefabスケール保存: {holdNotePrefabScale}");
                
                // Prefabの元の色を保存
                Renderer holdRenderer = holdNotePrefab.GetComponent<Renderer>();
                if (holdRenderer != null && holdRenderer.sharedMaterial != null)
                {
                    holdNotePrefabColor = holdRenderer.sharedMaterial.color;
                    LogDebug($"HoldNote Prefab色保存: {holdNotePrefabColor}");
                }
            }
            
            return tapNotePrefab != null && holdNotePrefab != null;
        }
        
        private void CreateInitialPool()
        {
            // プレハブが設定されていない場合はプールを作成しない
            if (tapNotePrefab == null || holdNotePrefab == null)
            {
                LogDebug("プレハブが設定されていないため、初期プールを作成しません");
                return;
            }
            
            // Tapノーツの初期プール作成
            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject note = CreateNewNote(NoteType.Tap);
                note.SetActive(false);
                tapNotePool.Enqueue(note);
            }
            
            // Holdノーツの初期プール作成
            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject note = CreateNewNote(NoteType.Hold);
                note.SetActive(false);
                holdNotePool.Enqueue(note);
            }
        }
        
        private GameObject CreateNewNote(NoteType type)
        {
            GameObject prefab = type == NoteType.Tap ? tapNotePrefab : holdNotePrefab;
            // 親を指定せずにインスタンス化
            GameObject note = Instantiate(prefab);
            
            // Prefabのスケールを保持
            Vector3 originalScale = type == NoteType.Tap ? tapNotePrefabScale : holdNotePrefabScale;
            note.transform.localScale = originalScale;
            
            // スケール設定後に親を設定（worldPositionStays=falseでローカル座標を維持）
            note.transform.SetParent(poolContainer, false);
            
            LogDebug($"新規{type}ノート作成 - スケール: {originalScale}, 実際のスケール: {note.transform.localScale}");
            
            // 統計情報の更新
            if (type == NoteType.Tap)
                totalTapCreated++;
            else
                totalHoldCreated++;
            
            return note;
        }
        
        // ========== 公開メソッド ==========
        
        /// <summary>
        /// プールからノーツを取得
        /// </summary>
        public GameObject GetNote(NoteType type)
        {
            Queue<GameObject> pool = type == NoteType.Tap ? tapNotePool : holdNotePool;
            GameObject note = null;
            
            // プールから取得を試みる
            while (pool.Count > 0)
            {
                note = pool.Dequeue();
                
                // オブジェクトが破棄されていないかチェック
                if (note != null)
                {
                    note.SetActive(true);
                    
                    // 統計情報の更新
                    if (type == NoteType.Tap)
                        tapPoolHits++;
                    else
                        holdPoolHits++;
                    
                    LogDebug($"プールから{type}ノーツを取得 (残り: {pool.Count}), スケール: {note.transform.localScale}");
                    return note;
                }
            }
            
            // プールが空の場合、新規作成
            if (CanCreateNew(type))
            {
                note = CreateNewNote(type);
                note.SetActive(true);
                
                // 統計情報の更新
                if (type == NoteType.Tap)
                    tapPoolMisses++;
                else
                    holdPoolMisses++;
                
                LogDebug($"新規{type}ノーツを作成 (プールミス)");
                return note;
            }
            
            Debug.LogWarning($"[NotePoolManager] {type}ノーツの最大プールサイズに達しました");
            return null;
        }
        
        /// <summary>
        /// ノーツをプールに返却
        /// </summary>
        public void ReturnNote(GameObject note, NoteType type)
        {
            if (note == null) return;
            
            // ノーツをリセット（タイプを渡す）
            ResetNote(note, type);
            
            // 非アクティブ化
            note.SetActive(false);
            
            // プールコンテナの子として配置（スケールを維持）
            note.transform.SetParent(poolContainer, false);
            
            // 適切なプールに返却
            Queue<GameObject> pool = type == NoteType.Tap ? tapNotePool : holdNotePool;
            
            // プールサイズのチェック
            if (pool.Count < maxPoolSize)
            {
                pool.Enqueue(note);
                LogDebug($"{type}ノーツをプールに返却 (プールサイズ: {pool.Count})");
            }
            else
            {
                // プールが満杯の場合は破棄
                Destroy(note);
                LogDebug($"{type}ノーツを破棄 (プール満杯)");
            }
        }
        
        /// <summary>
        /// プールをクリア
        /// </summary>
        public void ClearPool()
        {
            // Tapノーツプールのクリア
            while (tapNotePool.Count > 0)
            {
                GameObject note = tapNotePool.Dequeue();
                if (note != null) Destroy(note);
            }
            
            // Holdノーツプールのクリア
            while (holdNotePool.Count > 0)
            {
                GameObject note = holdNotePool.Dequeue();
                if (note != null) Destroy(note);
            }
            
            LogDebug("プールをクリアしました");
        }
        
        /// <summary>
        /// プールを指定サイズにリサイズ
        /// </summary>
        public void ResizePool(int newSize)
        {
            initialPoolSize = Mathf.Clamp(newSize, 0, maxPoolSize);
            
            // 現在のプールサイズを調整
            AdjustPoolSize(tapNotePool, NoteType.Tap, initialPoolSize);
            AdjustPoolSize(holdNotePool, NoteType.Hold, initialPoolSize);
            
            LogDebug($"プールサイズを{initialPoolSize}にリサイズしました");
        }
        
        /// <summary>
        /// 統計情報を取得
        /// </summary>
        public void GetStatistics(out int tapCreated, out int holdCreated,
                                  out int tapHits, out int holdHits,
                                  out int tapMisses, out int holdMisses)
        {
            tapCreated = totalTapCreated;
            holdCreated = totalHoldCreated;
            tapHits = tapPoolHits;
            holdHits = holdPoolHits;
            tapMisses = tapPoolMisses;
            holdMisses = holdPoolMisses;
        }
        
        /// <summary>
        /// プールの現在の状態を取得
        /// </summary>
        public void GetPoolStatus(out int tapPoolSize, out int holdPoolSize)
        {
            tapPoolSize = tapNotePool.Count;
            holdPoolSize = holdNotePool.Count;
        }
        
        /// <summary>
        /// Prefabの元のスケールを取得
        /// </summary>
        public Vector3 GetPrefabScale(NoteType type)
        {
            // Prefabスケールがまだ取得されていない場合は取得
            if (tapNotePrefabScale == Vector3.zero || holdNotePrefabScale == Vector3.zero)
            {
                ValidatePrefabs();
                LogDebug($"GetPrefabScale - Prefabスケール再取得 Tap: {tapNotePrefabScale}, Hold: {holdNotePrefabScale}");
            }
            
            Vector3 scale = type == NoteType.Tap ? tapNotePrefabScale : holdNotePrefabScale;
            LogDebug($"GetPrefabScale - {type}タイプのスケール: {scale}");
            return scale;
        }
        
        /// <summary>
        /// Prefabの元の色を取得
        /// </summary>
        public Color GetPrefabColor(NoteType type)
        {
            return type == NoteType.Tap ? tapNotePrefabColor : holdNotePrefabColor;
        }
        
        // ========== プライベートメソッド ==========
        
        private bool CanCreateNew(NoteType type)
        {
            int currentTotal = type == NoteType.Tap ? totalTapCreated : totalHoldCreated;
            return currentTotal < maxPoolSize;
        }
        
        private void ResetNote(GameObject note, NoteType type)
        {
            // 位置と回転をリセット
            note.transform.position = Vector3.zero;
            note.transform.rotation = Quaternion.identity;
            
            // スケールはPrefabの元の値を使用
            Vector3 originalScale = type == NoteType.Tap ? tapNotePrefabScale : holdNotePrefabScale;
            note.transform.localScale = originalScale;
            LogDebug($"{type}ノートリセット - スケール: {originalScale}, 実際のスケール: {note.transform.localScale}");
            
            // NoteControllerのリセット
            NoteController controller = note.GetComponent<NoteController>();
            if (controller != null)
            {
                controller.ResetNote();
            }
            
            // レンダラーの色をPrefabの元の色にリセット
            Renderer renderer = note.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                // タイプに応じた元の色に戻す
                Color originalColor = type == NoteType.Tap ? tapNotePrefabColor : holdNotePrefabColor;
                renderer.material.color = originalColor;
                LogDebug($"{type}ノートの色をリセット: {originalColor}");
            }
        }
        
        private void AdjustPoolSize(Queue<GameObject> pool, NoteType type, int targetSize)
        {
            // プールサイズが目標より大きい場合、削除
            while (pool.Count > targetSize)
            {
                GameObject note = pool.Dequeue();
                if (note != null) Destroy(note);
            }
            
            // プールサイズが目標より小さい場合、追加
            while (pool.Count < targetSize)
            {
                GameObject note = CreateNewNote(type);
                note.SetActive(false);
                pool.Enqueue(note);
            }
        }
        
        private void Cleanup()
        {
            ClearPool();
            
            if (poolContainer != null)
            {
                Destroy(poolContainer.gameObject);
            }
        }
        
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[NotePoolManager] {message}");
            }
        }
        
        // ========== デバッグ表示 ==========
        
#if UNITY_EDITOR
        void OnGUI()
        {
            if (!Application.isPlaying || !enableDebugLog) return;
            
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 12;
            style.normal.textColor = Color.white;
            
            // 背景ボックス
            GUI.Box(new Rect(10, 250, 250, 150), "NotePool Debug Info");
            
            // プール状態
            GUI.Label(new Rect(20, 275, 230, 20), 
                     $"Tapプール: {tapNotePool.Count}/{maxPoolSize}", style);
            GUI.Label(new Rect(20, 295, 230, 20), 
                     $"Holdプール: {holdNotePool.Count}/{maxPoolSize}", style);
            
            // 統計情報
            GUI.Label(new Rect(20, 320, 230, 20), 
                     $"Tap - 作成: {totalTapCreated} ヒット: {tapPoolHits} ミス: {tapPoolMisses}", style);
            GUI.Label(new Rect(20, 340, 230, 20), 
                     $"Hold - 作成: {totalHoldCreated} ヒット: {holdPoolHits} ミス: {holdPoolMisses}", style);
            
            // ヒット率
            float tapHitRate = tapPoolHits + tapPoolMisses > 0 ? 
                              (float)tapPoolHits / (tapPoolHits + tapPoolMisses) * 100 : 0;
            float holdHitRate = holdPoolHits + holdPoolMisses > 0 ? 
                               (float)holdPoolHits / (holdPoolHits + holdPoolMisses) * 100 : 0;
            
            GUI.Label(new Rect(20, 365, 230, 20), 
                     $"ヒット率 - Tap: {tapHitRate:F1}% Hold: {holdHitRate:F1}%", style);
        }
#endif
    }
}