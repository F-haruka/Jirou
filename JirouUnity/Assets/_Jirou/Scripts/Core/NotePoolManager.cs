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
        [SerializeField] private GameObject _tapNotePrefab;
        [SerializeField] private GameObject _holdNotePrefab;
        
        [Header("プール設定")]
        [SerializeField] private int _initialPoolSize = 50;
        [SerializeField] private int _maxPoolSize = 200;
        
        private Queue<GameObject> _tapNotePool = new Queue<GameObject>();
        private Queue<GameObject> _holdNotePool = new Queue<GameObject>();
        private Transform _poolContainer;
        
        private static NotePoolManager _instance;
        public static NotePoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NotePoolManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("NotePoolManagerが見つかりません！シーンに配置してください。");
                    }
                }
                return _instance;
            }
        }
        
        void Awake()
        {
            // シングルトンパターンの実装
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePool();
            }
            else if (_instance != this)
            {
                Debug.LogWarning("重複するNotePoolManagerインスタンスを破棄します");
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// プールを初期化
        /// </summary>
        private void InitializePool()
        {
            CreatePoolContainer();
            CreateInitialNotes();
            
            Debug.Log($"[NotePool] 初期化完了 - Tap: {_tapNotePool.Count}, Hold: {_holdNotePool.Count}");
        }
        
        /// <summary>
        /// プールコンテナを作成
        /// </summary>
        private void CreatePoolContainer()
        {
            GameObject container = new GameObject("NotePool");
            container.transform.SetParent(transform);
            _poolContainer = container.transform;
        }
        
        /// <summary>
        /// 初期ノーツを作成
        /// </summary>
        private void CreateInitialNotes()
        {
            int holdNoteCount = _initialPoolSize / 2;
            
            for (int i = 0; i < _initialPoolSize; i++)
            {
                CreatePooledNote(NoteType.Tap);
                
                if (i < holdNoteCount)
                {
                    CreatePooledNote(NoteType.Hold);
                }
            }
        }
        
        /// <summary>
        /// プール用のノーツを作成
        /// </summary>
        private GameObject CreatePooledNote(NoteType type)
        {
            GameObject prefab = GetPrefabForType(type);
            
            if (prefab == null)
            {
                Debug.LogError($"プレハブが設定されていません: {type}");
                return null;
            }
            
            GameObject note = InstantiateNote(prefab);
            AddNoteToPool(note, type);
            
            return note;
        }
        
        /// <summary>
        /// ノーツタイプに対応するプレハブを取得
        /// </summary>
        private GameObject GetPrefabForType(NoteType type)
        {
            return type == NoteType.Tap ? _tapNotePrefab : _holdNotePrefab;
        }
        
        /// <summary>
        /// ノーットをインスタンシエート
        /// </summary>
        private GameObject InstantiateNote(GameObject prefab)
        {
            GameObject note = Instantiate(prefab, _poolContainer);
            note.SetActive(false);
            return note;
        }
        
        /// <summary>
        /// ノーツをプールに追加
        /// </summary>
        private void AddNoteToPool(GameObject note, NoteType type)
        {
            Queue<GameObject> pool = GetPoolForType(type);
            pool.Enqueue(note);
        }
        
        /// <summary>
        /// ノーツタイプに対応するプールを取得
        /// </summary>
        private Queue<GameObject> GetPoolForType(NoteType type)
        {
            return type == NoteType.Tap ? _tapNotePool : _holdNotePool;
        }
        
        /// <summary>
        /// プールからノーツを取得
        /// </summary>
        public GameObject GetNote(NoteType type)
        {
            Queue<GameObject> pool = GetPoolForType(type);
            
            GameObject note = TryGetNoteFromPool(pool);
            
            // プールが空の場合は新規作成
            if (note == null)
            {
                note = CreateNewNote(type, pool);
            }
            
            return note;
        }
        
        /// <summary>
        /// プールからノーツを取得を試みる
        /// </summary>
        private GameObject TryGetNoteFromPool(Queue<GameObject> pool)
        {
            while (pool.Count > 0)
            {
                GameObject note = pool.Dequeue();
                
                if (note != null)
                {
                    note.SetActive(true);
                    return note;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 新しいノーツを作成
        /// </summary>
        private GameObject CreateNewNote(NoteType type, Queue<GameObject> pool)
        {
            GameObject note = CreatePooledNote(type);
            
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
            
            ResetNote(note);
            
            Queue<GameObject> pool = GetPoolForType(type);
            
            // プールサイズ制限チェック
            if (pool.Count < _maxPoolSize)
            {
                pool.Enqueue(note);
            }
            else
            {
                Destroy(note);
            }
        }
        
        /// <summary>
        /// ノーツをリセット
        /// </summary>
        private void ResetNote(GameObject note)
        {
            note.SetActive(false);
            note.transform.SetParent(_poolContainer);
            note.transform.position = Vector3.zero;
            note.transform.rotation = Quaternion.identity;
            note.transform.localScale = Vector3.one;
        }
        
        /// <summary>
        /// プールの統計情報を取得
        /// </summary>
        public void GetPoolStatistics(out int tapActive, out int tapPooled, 
                                      out int holdActive, out int holdPooled)
        {
            tapPooled = _tapNotePool.Count;
            holdPooled = _holdNotePool.Count;
            
            CountActiveNotes(out tapActive, out holdActive);
        }
        
        /// <summary>
        /// アクティブなノーツをカウント
        /// </summary>
        private void CountActiveNotes(out int tapActive, out int holdActive)
        {
            tapActive = 0;
            holdActive = 0;
            
            foreach (Transform child in _poolContainer)
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
            DeactivateAllNotes();
            RebuildPools();
            
            Debug.Log("[NotePool] プールをクリアしました");
        }
        
        /// <summary>
        /// すべてのノーツを非アクティブ化
        /// </summary>
        private void DeactivateAllNotes()
        {
            foreach (Transform child in _poolContainer)
            {
                child.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// プールを再構築
        /// </summary>
        private void RebuildPools()
        {
            _tapNotePool.Clear();
            _holdNotePool.Clear();
            
            foreach (Transform child in _poolContainer)
            {
                if (child.name.Contains("Tap"))
                    _tapNotePool.Enqueue(child.gameObject);
                else if (child.name.Contains("Hold"))
                    _holdNotePool.Enqueue(child.gameObject);
            }
        }
    }
}