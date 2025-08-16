using System.Collections.Generic;
using UnityEngine;
using Jirou.Core;

namespace Jirou.Gameplay
{
    /// <summary>
    /// 奥行き型ノーツ生成を管理するコンポーネント
    /// ChartDataに基づいてノーツを生成し、3D空間に配置する
    /// </summary>
    public class NoteSpawner : MonoBehaviour
    {
        // ========== パブリックフィールド ==========
        
        [Header("譜面データ")]
        [Tooltip("再生する譜面データ")]
        public ChartData chartData;
        
        [Header("ノーツプレハブ")]
        [Tooltip("Tapノーツのプレハブ")]
        public GameObject tapNotePrefab;
        
        [Tooltip("Holdノーツのプレハブ")]
        public GameObject holdNotePrefab;
        
        [Header("レーン設定")]
        [Tooltip("各レーンのX座標")]
        public float[] laneXPositions = { -3f, -1f, 1f, 3f };
        
        [Tooltip("ノーツのY座標")]
        public float noteY = 0.5f;
        
        [Header("生成タイミング")]
        [Tooltip("先読みビート数")]
        [Range(1f, 5f)]
        public float beatsShownInAdvance = 3.0f;
        
        [Header("デバッグ設定")]
        [Tooltip("デバッグログを有効化")]
        public bool enableDebugLog = false;
        
        [Tooltip("Gizmoでノーツ経路を表示")]
        public bool showNotePathGizmo = true;
        
        // ========== プライベートフィールド ==========
        
        private int nextNoteIndex = 0;
        private List<GameObject> activeNotes = new List<GameObject>();
        private Conductor conductor;
        private NotePoolManager notePool;
        private bool isSpawning = false;
        
        // パフォーマンス最適化用
        private float lastSpawnCheckBeat = -1f;
        private const float SPAWN_CHECK_INTERVAL = 0.25f; // ビート単位でのチェック間隔
        
        // ========== Unity イベントメソッド ==========
        
        void Awake()
        {
            // 依存コンポーネントの取得
            InitializeDependencies();
        }
        
        void Start()
        {
            // 初期化処理
            Initialize();
            
            // 楽曲を開始
            StartSpawning();
        }
        
        void Update()
        {
            if (!isSpawning) return;
            
            // ノーツ生成処理
            UpdateNoteSpawning();
            
            // アクティブノーツの管理
            UpdateActiveNotes();
        }
        
        void OnDestroy()
        {
            // クリーンアップ処理
            Cleanup();
        }
        
        // ========== 初期化メソッド ==========
        
        private void InitializeDependencies()
        {
            // Conductor取得
            conductor = Conductor.Instance;
            if (conductor == null)
            {
                Debug.LogError("[NoteSpawner] Conductorが見つかりません！");
                enabled = false;
                return;
            }
            
            // NotePoolManager取得
            notePool = NotePoolManager.Instance;
            if (notePool == null)
            {
                Debug.LogWarning("[NoteSpawner] NotePoolManagerが見つかりません。プレハブから直接生成します。");
            }
        }
        
        private void Initialize()
        {
            // データ検証
            if (!ValidateData())
            {
                enabled = false;
                return;
            }
            
            // 譜面データのソート
            chartData.SortNotesByTime();
            
            // 初期化完了ログ
            LogDebug($"NoteSpawner初期化完了 - 総ノーツ数: {chartData.Notes.Count}");
        }
        
        private bool ValidateData()
        {
            if (chartData == null)
            {
                Debug.LogError("[NoteSpawner] ChartDataが設定されていません！");
                return false;
            }
            
            if (tapNotePrefab == null || holdNotePrefab == null)
            {
                Debug.LogError("[NoteSpawner] ノーツプレハブが設定されていません！");
                return false;
            }
            
            if (laneXPositions.Length != 4)
            {
                Debug.LogError("[NoteSpawner] レーン座標は4つ必要です！");
                return false;
            }
            
            // ChartDataのバリデーション
            List<string> errors;
            if (!chartData.ValidateChart(out errors))
            {
                foreach (var error in errors)
                {
                    Debug.LogError($"[NoteSpawner] ChartData検証エラー: {error}");
                }
                return false;
            }
            
            return true;
        }
        
        // ========== ノーツ生成メソッド ==========
        
        private void StartSpawning()
        {
            if (conductor == null || chartData == null) return;
            
            // Conductorに楽曲データを設定
            conductor.songSource.clip = chartData.SongClip;
            conductor.songBpm = chartData.Bpm;
            
            // 楽曲を開始
            conductor.StartSong();
            
            isSpawning = true;
            LogDebug("ノーツ生成開始");
        }
        
        private void UpdateNoteSpawning()
        {
            // まだ生成していないノーツがあるかチェック
            if (nextNoteIndex >= chartData.Notes.Count)
            {
                // すべてのノーツを生成済み
                if (activeNotes.Count == 0 && isSpawning)
                {
                    OnAllNotesCompleted();
                }
                return;
            }
            
            // パフォーマンス最適化: 一定間隔でのみチェック
            float currentBeat = conductor.songPositionInBeats;
            if (currentBeat - lastSpawnCheckBeat < SPAWN_CHECK_INTERVAL)
            {
                return;
            }
            lastSpawnCheckBeat = currentBeat;
            
            // 生成タイミングのチェックと生成
            while (nextNoteIndex < chartData.Notes.Count)
            {
                NoteData noteData = chartData.Notes[nextNoteIndex];
                
                // 生成タイミングの判定
                if (ShouldSpawnNote(noteData))
                {
                    SpawnNote(noteData);
                    nextNoteIndex++;
                }
                else
                {
                    // まだ生成タイミングではない
                    break;
                }
            }
        }
        
        private bool ShouldSpawnNote(NoteData noteData)
        {
            // Conductorのメソッドを使用してタイミング判定
            return conductor.ShouldSpawnNote(noteData.TimeToHit, beatsShownInAdvance);
        }
        
        private void SpawnNote(NoteData noteData)
        {
            GameObject notePrefab = noteData.NoteType == NoteType.Tap ? 
                                   tapNotePrefab : holdNotePrefab;
            
            GameObject noteObject = null;
            
            // オブジェクトプールから取得
            if (notePool != null)
            {
                noteObject = notePool.GetNote(noteData.NoteType);
            }
            
            // プールから取得できない場合は直接生成
            if (noteObject == null)
            {
                noteObject = Instantiate(notePrefab);
            }
            
            // 位置の設定
            Vector3 spawnPos = CalculateSpawnPosition(noteData);
            noteObject.transform.position = spawnPos;
            
            // NoteControllerコンポーネントの設定
            NoteController controller = noteObject.GetComponent<NoteController>();
            if (controller != null)
            {
                controller.Initialize(noteData, conductor);
            }
            else
            {
                Debug.LogWarning("[NoteSpawner] NoteControllerコンポーネントが見つかりません！");
            }
            
            // カスタマイズの適用
            ApplyNoteCustomization(noteObject, noteData);
            
            // アクティブリストに追加
            activeNotes.Add(noteObject);
            
            LogDebug($"ノーツ生成 - タイプ: {noteData.NoteType}, レーン: {noteData.LaneIndex}, " +
                    $"タイミング: {noteData.TimeToHit:F2}ビート, 位置: {spawnPos}");
        }
        
        private Vector3 CalculateSpawnPosition(NoteData noteData)
        {
            // レーンインデックスの検証
            int laneIndex = Mathf.Clamp(noteData.LaneIndex, 0, laneXPositions.Length - 1);
            
            return new Vector3(
                laneXPositions[laneIndex],
                noteY,
                conductor.spawnZ
            );
        }
        
        private void ApplyNoteCustomization(GameObject noteObject, NoteData noteData)
        {
            // スケールの適用
            if (noteData.VisualScale != 1.0f)
            {
                noteObject.transform.localScale = Vector3.one * noteData.VisualScale;
            }
            
            // 色の適用
            if (noteData.NoteColor != Color.white)
            {
                Renderer renderer = noteObject.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = noteData.NoteColor;
                }
            }
            
            // カスタムエフェクトの設定
            if (noteData.CustomHitEffect != null)
            {
                NoteController controller = noteObject.GetComponent<NoteController>();
                if (controller != null)
                {
                    controller.customHitEffect = noteData.CustomHitEffect;
                }
            }
            
            // カスタムサウンドの設定
            if (noteData.CustomHitSound != null)
            {
                NoteController controller = noteObject.GetComponent<NoteController>();
                if (controller != null)
                {
                    controller.customHitSound = noteData.CustomHitSound;
                }
            }
        }
        
        // ========== アクティブノーツ管理 ==========
        
        private void UpdateActiveNotes()
        {
            // 削除予定のノーツを記録
            List<GameObject> notesToRemove = new List<GameObject>();
            
            foreach (GameObject note in activeNotes)
            {
                if (note == null)
                {
                    notesToRemove.Add(note);
                    continue;
                }
                
                // ノーツが判定ラインを通過したか確認
                NoteController controller = note.GetComponent<NoteController>();
                if (controller != null && controller.IsCompleted())
                {
                    notesToRemove.Add(note);
                    
                    // オブジェクトプールに返却
                    if (notePool != null && controller.noteData != null)
                    {
                        notePool.ReturnNote(note, controller.noteData.NoteType);
                    }
                    else
                    {
                        Destroy(note);
                    }
                }
            }
            
            // リストから削除
            foreach (GameObject note in notesToRemove)
            {
                activeNotes.Remove(note);
            }
        }
        
        // ========== 公開メソッド ==========
        
        /// <summary>
        /// ノーツ生成を一時停止
        /// </summary>
        public void PauseSpawning()
        {
            isSpawning = false;
            conductor.PauseSong();
            LogDebug("ノーツ生成一時停止");
        }
        
        /// <summary>
        /// ノーツ生成を再開
        /// </summary>
        public void ResumeSpawning()
        {
            isSpawning = true;
            conductor.ResumeSong();
            LogDebug("ノーツ生成再開");
        }
        
        /// <summary>
        /// ノーツ生成を停止してリセット
        /// </summary>
        public void StopAndReset()
        {
            isSpawning = false;
            conductor.StopSong();
            
            // すべてのアクティブノーツを削除
            foreach (GameObject note in activeNotes)
            {
                if (note != null)
                {
                    if (notePool != null)
                    {
                        NoteController controller = note.GetComponent<NoteController>();
                        if (controller != null && controller.noteData != null)
                        {
                            notePool.ReturnNote(note, controller.noteData.NoteType);
                        }
                        else
                        {
                            Destroy(note);
                        }
                    }
                    else
                    {
                        Destroy(note);
                    }
                }
            }
            
            activeNotes.Clear();
            nextNoteIndex = 0;
            lastSpawnCheckBeat = -1f;
            
            LogDebug("ノーツ生成停止・リセット完了");
        }
        
        /// <summary>
        /// 統計情報を取得
        /// </summary>
        public void GetStatistics(out int totalNotes, out int spawnedNotes, 
                                  out int activeNotesCount, out int remainingNotes)
        {
            totalNotes = chartData != null ? chartData.Notes.Count : 0;
            spawnedNotes = nextNoteIndex;
            activeNotesCount = activeNotes.Count;
            remainingNotes = totalNotes - spawnedNotes;
        }
        
        // ========== イベントハンドラ ==========
        
        private void OnAllNotesCompleted()
        {
            isSpawning = false;
            LogDebug("すべてのノーツの生成・処理が完了しました！");
            
            // イベント通知（将来的に実装）
            // OnSongCompleted?.Invoke();
        }
        
        // ========== クリーンアップ ==========
        
        private void Cleanup()
        {
            StopAndReset();
            conductor = null;
            notePool = null;
        }
        
        // ========== デバッグ機能 ==========
        
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[NoteSpawner] {message}");
            }
        }
        
#if UNITY_EDITOR
        void OnGUI()
        {
            if (!Application.isPlaying || !enableDebugLog) return;
            
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 12;
            style.normal.textColor = Color.white;
            
            // 背景ボックス
            GUI.Box(new Rect(10, 140, 250, 100), "NoteSpawner Debug Info");
            
            // 統計情報
            int total, spawned, active, remaining;
            GetStatistics(out total, out spawned, out active, out remaining);
            
            GUI.Label(new Rect(20, 165, 230, 20), 
                     $"総ノーツ数: {total}", style);
            GUI.Label(new Rect(20, 185, 230, 20), 
                     $"生成済み: {spawned} / アクティブ: {active}", style);
            GUI.Label(new Rect(20, 205, 230, 20), 
                     $"残り: {remaining}", style);
        }
        
        void OnDrawGizmos()
        {
            if (!showNotePathGizmo) return;
            
            // レーンの可視化
            if (laneXPositions != null && laneXPositions.Length == 4)
            {
                float spawnZ = conductor != null ? conductor.spawnZ : 20f;
                float hitZ = conductor != null ? conductor.hitZ : 0f;
                
                for (int i = 0; i < laneXPositions.Length; i++)
                {
                    // レーンの中心線
                    Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f);
                    Gizmos.DrawLine(
                        new Vector3(laneXPositions[i], noteY, spawnZ),
                        new Vector3(laneXPositions[i], noteY, hitZ)
                    );
                    
                    // レーンの境界
                    float laneWidth = 0.8f;
                    Gizmos.color = new Color(0.3f, 0.3f, 0.8f, 0.3f);
                    
                    // 左境界
                    Gizmos.DrawLine(
                        new Vector3(laneXPositions[i] - laneWidth/2, noteY, spawnZ),
                        new Vector3(laneXPositions[i] - laneWidth/2, noteY, hitZ)
                    );
                    
                    // 右境界
                    Gizmos.DrawLine(
                        new Vector3(laneXPositions[i] + laneWidth/2, noteY, spawnZ),
                        new Vector3(laneXPositions[i] + laneWidth/2, noteY, hitZ)
                    );
                }
                
                // スポーンライン
                Gizmos.color = Color.green;
                Gizmos.DrawLine(
                    new Vector3(laneXPositions[0] - 1f, noteY, spawnZ),
                    new Vector3(laneXPositions[3] + 1f, noteY, spawnZ)
                );
                
                // 判定ライン
                Gizmos.color = Color.red;
                Gizmos.DrawLine(
                    new Vector3(laneXPositions[0] - 1f, noteY, hitZ),
                    new Vector3(laneXPositions[3] + 1f, noteY, hitZ)
                );
            }
            
            // アクティブノーツの可視化
            if (Application.isPlaying && activeNotes != null)
            {
                foreach (GameObject note in activeNotes)
                {
                    if (note != null)
                    {
                        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
                        Gizmos.DrawWireCube(note.transform.position, Vector3.one * 0.5f);
                    }
                }
            }
        }
#endif
    }
}