using System.Collections.Generic;
using UnityEngine;

namespace Jirou.Core
{
    /// <summary>
    /// ノーツのオブジェクトプール管理
    /// </summary>
    public class NotePoolManager : MonoBehaviour
    {
        [Header("プレハブ設定")]
        [SerializeField] private GameObject tapNotePrefab;
        [SerializeField] private GameObject holdNotePrefab;
        
        [Header("プール設定")]
        [SerializeField] private int initialPoolSize = 50;
        [SerializeField] private int maxPoolSize = 200;
        
        private Queue<GameObject> tapNotePool = new Queue<GameObject>();
        private Queue<GameObject> holdNotePool = new Queue<GameObject>();
        private Transform poolContainer;
        
        private static NotePoolManager instance;
        public static NotePoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<NotePoolManager>();
                }
                return instance;
            }
        }
        
        void Awake()
        {
            instance = this;
            InitializePool();
        }
        
        /// <summary>
        /// プールを初期化
        /// </summary>
        private void InitializePool()
        {
            // プールコンテナを作成
            GameObject container = new GameObject("NotePool");
            container.transform.SetParent(transform);
            poolContainer = container.transform;
            
            // 初期プールを生成
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreatePooledNote(NoteType.Tap);
                
                if (i < initialPoolSize / 2)  // Holdノーツは半分の数
                {
                    CreatePooledNote(NoteType.Hold);
                }
            }
            
            Debug.Log($"[NotePool] 初期化完了 - Tap: {tapNotePool.Count}, Hold: {holdNotePool.Count}");
        }
        
        /// <summary>
        /// プール用のノーツを作成
        /// </summary>
        private GameObject CreatePooledNote(NoteType type)
        {
            GameObject prefab = type == NoteType.Tap ? tapNotePrefab : holdNotePrefab;
            
            if (prefab == null)
            {
                Debug.LogError($"プレハブが設定されていません: {type}");
                return null;
            }
            
            GameObject note = Instantiate(prefab, poolContainer);
            note.SetActive(false);
            
            Queue<GameObject> pool = type == NoteType.Tap ? tapNotePool : holdNotePool;
            pool.Enqueue(note);
            
            return note;
        }
        
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
                
                if (note != null)
                {
                    note.SetActive(true);
                    return note;
                }
            }
            
            // プールが空の場合は新規作成
            note = CreatePooledNote(type);
            
            if (note != null)
            {
                pool.Dequeue();  // 作成時にキューに追加されるため取り出す
                note.SetActive(true);
            }
            
            return note;
        }
        
        /// <summary>
        /// ノーツをプールに返却
        /// </summary>
        public void ReturnNote(GameObject note, NoteType type)
        {
            if (note == null) return;
            
            // リセット処理
            note.SetActive(false);
            note.transform.SetParent(poolContainer);
            note.transform.position = Vector3.zero;
            note.transform.rotation = Quaternion.identity;
            note.transform.localScale = Vector3.one;
            
            Queue<GameObject> pool = type == NoteType.Tap ? tapNotePool : holdNotePool;
            
            // プールサイズ制限チェック
            if (pool.Count < maxPoolSize)
            {
                pool.Enqueue(note);
            }
            else
            {
                Destroy(note);
            }
        }
        
        /// <summary>
        /// プールの統計情報を取得
        /// </summary>
        public void GetPoolStatistics(out int tapActive, out int tapPooled, 
                                      out int holdActive, out int holdPooled)
        {
            tapPooled = tapNotePool.Count;
            holdPooled = holdNotePool.Count;
            
            // アクティブなノーツをカウント
            tapActive = 0;
            holdActive = 0;
            
            foreach (Transform child in poolContainer)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    if (child.name.Contains("Tap"))
                        tapActive++;
                    else if (child.name.Contains("Hold"))
                        holdActive++;
                }
            }
        }
        
        /// <summary>
        /// プールをクリア
        /// </summary>
        public void ClearPool()
        {
            // すべてのノーツを非アクティブ化
            foreach (Transform child in poolContainer)
            {
                child.gameObject.SetActive(false);
            }
            
            // プールを再構築
            tapNotePool.Clear();
            holdNotePool.Clear();
            
            foreach (Transform child in poolContainer)
            {
                if (child.name.Contains("Tap"))
                    tapNotePool.Enqueue(child.gameObject);
                else if (child.name.Contains("Hold"))
                    holdNotePool.Enqueue(child.gameObject);
            }
            
            Debug.Log("[NotePool] プールをクリアしました");
        }
    }
}