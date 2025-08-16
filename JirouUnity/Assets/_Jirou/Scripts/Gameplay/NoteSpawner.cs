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
        
        [Header("再生設定")]
        [Tooltip("自動的に楽曲を開始")]
        public bool autoStart = false;
        
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
        private bool isInitialized = false;  // 初期化済みフラグ
        
        // パフォーマンス最適化用
        private float lastSpawnCheckBeat = -1f;
        private const float SPAWN_CHECK_INTERVAL = 0.25f; // ビート単位でのチェック間隔
        
        // ========== Unity イベントメソッド ==========
        
        void Awake()
        {
            // Awakeでは何もしない（依存関係の取得はStartで行う）
        }
        
        void Start()
        {
            // 依存コンポーネントの取得
            InitializeDependencies();
            
            // Conductorから設定を取得
            if (conductor != null)
            {
                // レーン位置をConductorから取得
                laneXPositions = conductor.LaneXPositions;
                noteY = conductor.NoteY;
                
                LogDebug($"Conductorからレーン設定を取得: レーン数={laneXPositions.Length}");
            }
            else
            {
                Debug.LogWarning("[NoteSpawner] Conductorが見つかりません。デフォルト設定を使用します。");
            }
            
            // ChartDataが設定されている場合のみ初期化処理を実行
            // TestSetupから後で設定される場合があるため、ここではスキップ可能
            if (chartData != null)
            {
                // 初期化処理
                Initialize();
                
                // 自動開始が有効な場合のみ楽曲を開始
                if (autoStart)
                {
                    StartSpawning();
                }
            }
            else
            {
                LogDebug("[NoteSpawner] ChartDataが未設定のため、初期化をスキップします");
            }
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
            // 既に初期化済みの場合はスキップ
            if (isInitialized) return;
            
            // データ検証
            if (!ValidateData())
            {
                enabled = false;
                return;
            }
            
            // 譜面データのソート
            chartData.SortNotesByTime();
            
            // 初期化完了フラグを設定
            isInitialized = true;
            
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
        
        public void StartSpawning()
        {
            // ChartDataが設定されていて、まだ初期化されていない場合は初期化を実行
            if (chartData != null && !isInitialized)
            {
                Initialize();
            }
            
            if (conductor == null || chartData == null) 
            {
                Debug.LogError($"[NoteSpawner] StartSpawning失敗 - conductor is null: {conductor == null}, chartData is null: {chartData == null}");
                return;
            }
            
            // AudioSourceの存在確認
            if (conductor.songSource == null)
            {
                Debug.LogError("[NoteSpawner] ConductorのAudioSourceが設定されていません！");
                return;
            }
            
            // ChartDataにSongClipがある場合のみ設定（なければ既存のAudioClipを使用）
            if (chartData.SongClip != null)
            {
                conductor.songSource.clip = chartData.SongClip;
                conductor.songBpm = chartData.Bpm;
            }
            else
            {
                // ChartDataにSongClipがない場合、既存のAudioClipとBPMを使用
                Debug.Log($"[NoteSpawner] ChartDataにSongClipがないため、既存のAudioClip '{conductor.songSource.clip?.name}' を使用します");
            }
            
            // AudioClipが設定されているか最終確認
            if (conductor.songSource.clip == null)
            {
                Debug.LogError("[NoteSpawner] AudioClipが設定されていません！ConductorのAudioSourceにAudioClipを設定してください。");
                return;
            }
            
            // 楽曲を開始
            conductor.StartSong();
            
            isSpawning = true;
            LogDebug($"ノーツ生成開始 - AudioClip: {conductor.songSource.clip.name}, BPM: {conductor.songBpm}");
            LogDebug($"ChartData - Notes Count: {chartData.Notes.Count}, First Note Time: {(chartData.Notes.Count > 0 ? chartData.Notes[0].TimeToHit.ToString() : "N/A")}");
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
            
            // デバッグログ：現在の状態を出力
            LogDebug($"UpdateNoteSpawning - nextNoteIndex: {nextNoteIndex}, currentBeat: {conductor.songPositionInBeats:F2}");
            
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
                // 生成されたノーツを確実にアクティブ化
                noteObject.SetActive(true);
            }
            
            // 位置の設定
            Vector3 spawnPos = CalculateSpawnPosition(noteData);
            noteObject.transform.position = spawnPos;
            
            // 初期スケールを遠近感に応じて設定
            float initialScale = conductor.GetScaleAtZ(conductor.SpawnZ);
            noteObject.transform.localScale = Vector3.one * initialScale;
            
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
            
            // ノーツを確実にアクティブ化
            noteObject.SetActive(true);
            
            // アクティブリストに追加
            activeNotes.Add(noteObject);
            
            LogDebug($"ノーツ生成 - タイプ: {noteData.NoteType}, レーン: {noteData.LaneIndex}, " +
                    $"タイミング: {noteData.TimeToHit:F2}ビート, 位置: {spawnPos}, スケール: {initialScale:F2}, Active: {noteObject.activeSelf}");
        }
        
        private Vector3 CalculateSpawnPosition(NoteData noteData)
        {
            // レーンインデックスの検証
            int laneIndex = Mathf.Clamp(noteData.LaneIndex, 0, laneXPositions.Length - 1);
            
            // X座標を遠近感を考慮して設定
            float xPos = conductor.GetPerspectiveLaneX(laneIndex, conductor.SpawnZ);
            
            return new Vector3(
                xPos,
                noteY,
                conductor.SpawnZ
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
            
            // Conductorの設定を優先的に使用
            float[] displayLanePositions = laneXPositions;
            float displaySpawnZ = 20f;
            float displayHitZ = 0f;
            float displayNoteY = noteY;
            
            if (conductor != null)
            {
                displayLanePositions = conductor.LaneXPositions;
                displaySpawnZ = conductor.SpawnZ;
                displayHitZ = conductor.HitZ;
                displayNoteY = conductor.NoteY;
            }
            
            // Conductorの統一された遠近感設定を使用
            float nearScale = conductor != null ? conductor.PerspectiveNearScale : 1.0f;
            float farScale = conductor != null ? conductor.PerspectiveFarScale : 0.25f;
            
            // レーンパスの可視化（遠近感付き）
            if (displayLanePositions != null && displayLanePositions.Length > 0)
            {
                for (int i = 0; i < displayLanePositions.Length; i++)
                {
                    // レーンの中心線（Conductorの遠近感メソッドを使用）
                    Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f);
                    
                    float nearX = conductor != null ? conductor.GetPerspectiveLaneX(i, displayHitZ) : displayLanePositions[i] * nearScale;
                    float farX = conductor != null ? conductor.GetPerspectiveLaneX(i, displaySpawnZ) : displayLanePositions[i] * farScale;
                    
                    Vector3 nearPos = new Vector3(nearX, displayNoteY, displayHitZ);
                    Vector3 farPos = new Vector3(farX, displayNoteY, displaySpawnZ);
                    
                    Gizmos.DrawLine(nearPos, farPos);
                }
                
                // スポーンライン（遠近感考慮）
                Gizmos.color = Color.green;
                float spawnLineExtent = (displayLanePositions[displayLanePositions.Length - 1] - displayLanePositions[0]) / 2f + 1f;
                Gizmos.DrawLine(
                    new Vector3(-spawnLineExtent * farScale, displayNoteY, displaySpawnZ),
                    new Vector3(spawnLineExtent * farScale, displayNoteY, displaySpawnZ)
                );
                
                // 判定ライン（遠近感考慮）
                Gizmos.color = Color.red;
                float hitLineExtent = spawnLineExtent;
                Gizmos.DrawLine(
                    new Vector3(-hitLineExtent * nearScale, displayNoteY, displayHitZ),
                    new Vector3(hitLineExtent * nearScale, displayNoteY, displayHitZ)
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