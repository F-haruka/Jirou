using UnityEngine;
using Jirou.Core;
using Jirou.Gameplay;
using System.Collections.Generic;

namespace Jirou.Testing
{
    /// <summary>
    /// NoteSpawnerのテスト用セットアップヘルパー
    /// シーンでNoteSpawnerの動作を確認するための簡易セットアップツール
    /// </summary>
    public class NoteSpawnerTestSetup : MonoBehaviour
    {
        [Header("セットアップ設定")]
        [Tooltip("自動セットアップを有効化")]
        public bool autoSetup = true;
        
        [Tooltip("テスト用の簡易譜面を生成")]
        public bool generateTestChart = true;
        
        [Header("テスト譜面設定")]
        [Tooltip("テスト用BPM")]
        public float testBPM = 120f;
        
        [Tooltip("生成するノーツ数")]
        public int noteCount = 20;
        
        [Tooltip("ノーツ間隔（ビート）")]
        public float noteInterval = 1.0f;
        
        [Header("コンポーネント参照")]
        public Conductor conductor;
        public NoteSpawner noteSpawner;
        public NotePoolManager notePoolManager;
        
        void Awake()
        {
            // AwakeでセットアップすることでNoteSpawnerのStart()より先に実行される
            if (autoSetup)
            {
                SetupTestEnvironment();
            }
        }
        
        void Start()
        {
            // Start()では何もしない（既にAwakeで処理済み）
        }
        
        /// <summary>
        /// テスト環境をセットアップ
        /// </summary>
        public void SetupTestEnvironment()
        {
            Debug.Log("[NoteSpawnerTestSetup] テスト環境のセットアップを開始");
            
            // 1. Conductorのセットアップ（最優先）
            SetupConductor();
            
            // 2. LaneVisualizerがあれば同期を強制
            Jirou.Visual.LaneVisualizer laneVis = FindObjectOfType<Jirou.Visual.LaneVisualizer>();
            if (laneVis != null)
            {
                laneVis.ForceSync();
                Debug.Log("[NoteSpawnerTestSetup] LaneVisualizerをConductorと同期しました");
            }
            
            // 3. NotePoolManagerのセットアップ
            SetupNotePoolManager();
            
            // 4. NoteSpawnerのセットアップ
            SetupNoteSpawner();
            
            // 4. テスト用譜面データの生成
            if (generateTestChart)
            {
                GenerateTestChart();
            }
            
            // 5. プレハブの作成
            CreateTestPrefabs();
            
            Debug.Log("[NoteSpawnerTestSetup] テスト環境のセットアップ完了");
            Debug.Log($"[NoteSpawnerTestSetup] tapNotePrefab is null: {noteSpawner.tapNotePrefab == null}");
            Debug.Log($"[NoteSpawnerTestSetup] holdNotePrefab is null: {noteSpawner.holdNotePrefab == null}");
            Debug.Log($"[NoteSpawnerTestSetup] chartData is null: {noteSpawner.chartData == null}");
            if (noteSpawner.chartData != null)
            {
                Debug.Log($"[NoteSpawnerTestSetup] chartData.Notes.Count: {noteSpawner.chartData.Notes.Count}");
            }
            
            // 6. セットアップ完了後、NoteSpawnerを開始
            if (noteSpawner != null && conductor != null)
            {
                // ConductorのAudioSourceがEnd_Time.wavに設定されているか確認
                AudioSource audioSource = conductor.GetComponent<AudioSource>();
                if (audioSource != null && audioSource.clip != null)
                {
                    Debug.Log($"[NoteSpawnerTestSetup] AudioClip '{audioSource.clip.name}' で楽曲を開始します");
                    noteSpawner.StartSpawning();
                }
                else
                {
                    Debug.LogError("[NoteSpawnerTestSetup] AudioClipが設定されていないため、楽曲を開始できません");
                }
            }
        }
        
        /// <summary>
        /// Conductorをセットアップ
        /// </summary>
        private void SetupConductor()
        {
            if (conductor == null)
            {
                // Conductorが存在しない場合は作成
                GameObject conductorGO = GameObject.Find("Conductor");
                if (conductorGO == null)
                {
                    conductorGO = new GameObject("Conductor");
                }
                
                conductor = conductorGO.GetComponent<Conductor>();
                if (conductor == null)
                {
                    conductor = conductorGO.AddComponent<Conductor>();
                }
            }
            
            // AudioSourceの追加または取得
            AudioSource audioSource = conductor.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = conductor.gameObject.AddComponent<AudioSource>();
            }
            
            // End_Time.wavをAudioClipとして常に設定（既存のclipがあっても上書き）
            #if UNITY_EDITOR
            // エディタモードでAssetDatabaseを使用してロード
            AudioClip endTimeClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Jirou/Audio/Music/End_Time.wav");
            
            if (endTimeClip != null)
            {
                audioSource.clip = endTimeClip;
                audioSource.playOnAwake = false; // 自動再生を無効化
                audioSource.loop = false; // ループを無効化
                Debug.Log($"[NoteSpawnerTestSetup] End_Time.wav({endTimeClip.length}秒)をAudioClipとして設定しました");
            }
            else
            {
                Debug.LogError("[NoteSpawnerTestSetup] End_Time.wavが見つかりませんでした。パスを確認してください: Assets/_Jirou/Audio/Music/End_Time.wav");
            }
            #else
            // ビルド時はResourcesフォルダからロード（必要に応じて）
            Debug.LogWarning("[NoteSpawnerTestSetup] ビルド環境では、End_Time.wavをResourcesフォルダに配置するか、別の方法でロードしてください");
            #endif
            
            // BPMを180に設定（End_Time.wavに合わせて）
            conductor.songBpm = 180f;
            
            // 遠近感設定（統一された値を設定）
            // これらの値はConductorのデフォルト値として設定済みですが、明示的に設定することも可能
            // conductor.PerspectiveNearScale = 1.0f;  // 読み取り専用プロパティなので設定不要
            // conductor.PerspectiveFarScale = 0.25f;  // 読み取り専用プロパティなので設定不要
            
            Debug.Log($"[NoteSpawnerTestSetup] Conductorをセットアップしました (BPM: {conductor.songBpm}, 遠近感: Near={conductor.PerspectiveNearScale}, Far={conductor.PerspectiveFarScale})");
        }
        
        /// <summary>
        /// NotePoolManagerをセットアップ
        /// </summary>
        private void SetupNotePoolManager()
        {
            if (notePoolManager == null)
            {
                // NotePoolManagerのインスタンスを取得（シングルトン）
                notePoolManager = NotePoolManager.Instance;
                
                // 初期設定
                notePoolManager.initialPoolSize = 10;
                notePoolManager.maxPoolSize = 30;
                
                Debug.Log("[NoteSpawnerTestSetup] NotePoolManagerをセットアップしました");
            }
        }
        
        /// <summary>
        /// NoteSpawnerをセットアップ
        /// </summary>
        private void SetupNoteSpawner()
        {
            if (noteSpawner == null)
            {
                GameObject spawnerGO = GameObject.Find("NoteSpawner");
                if (spawnerGO == null)
                {
                    spawnerGO = new GameObject("NoteSpawner");
                }
                
                noteSpawner = spawnerGO.GetComponent<NoteSpawner>();
                if (noteSpawner == null)
                {
                    noteSpawner = spawnerGO.AddComponent<NoteSpawner>();
                }
            }
            
            // Conductorとの同期を確認
            if (conductor != null)
            {
                // Conductorからレーン設定を取得
                noteSpawner.laneXPositions = conductor.LaneXPositions;
                noteSpawner.noteY = conductor.NoteY;
                
                Debug.Log($"[NoteSpawnerTestSetup] Conductorのレーン設定を適用: {string.Join(", ", noteSpawner.laneXPositions)}");
            }
            else
            {
                // デフォルト設定（既存）
                noteSpawner.laneXPositions = new float[] { -3f, -1f, 1f, 3f };
                noteSpawner.noteY = 0.5f;
            }
            
            // その他の設定
            noteSpawner.beatsShownInAdvance = 3.0f;
            noteSpawner.enableDebugLog = true;
            noteSpawner.showNotePathGizmo = true;
            
            Debug.Log("[NoteSpawnerTestSetup] NoteSpawnerをセットアップしました");
        }
        
        /// <summary>
        /// テスト用の譜面データを生成
        /// </summary>
        private void GenerateTestChart()
        {
            // ScriptableObjectとしてChartDataを作成
            ChartData testChart = ScriptableObject.CreateInstance<ChartData>();
            
            // テスト用の基本データを設定
            // 注意: ChartDataのプロパティは読み取り専用なので、
            // リフレクションを使って内部フィールドを設定
            var chartType = typeof(ChartData);
            chartType.GetField("_songName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(testChart, "Test Song");
            chartType.GetField("_artist", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(testChart, "Test Artist");
            chartType.GetField("_bpm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(testChart, testBPM);
            chartType.GetField("_difficulty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(testChart, 1);
            
            // ノーツリストの取得
            var notesField = chartType.GetField("_notes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var notesList = new List<NoteData>();
            
            // テスト用ノーツを生成
            for (int i = 0; i < noteCount; i++)
            {
                NoteData note = new NoteData();
                
                // NoteDataのプロパティを設定
                var noteType = typeof(NoteData);
                noteType.GetField("_noteType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(note, (i % 3 == 0) ? NoteType.Hold : NoteType.Tap);
                noteType.GetField("_laneIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(note, UnityEngine.Random.Range(0, 4));
                noteType.GetField("_timeToHit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(note, (i + 1) * noteInterval);
                noteType.GetField("_visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(note, 1.0f);
                noteType.GetField("_noteColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(note, GetRandomColor());
                noteType.GetField("_scoreMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(note, 1.0f);
                
                // Holdノーツの場合は長さを設定
                if (note.NoteType == NoteType.Hold)
                {
                    noteType.GetField("_holdDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(note, UnityEngine.Random.Range(0.5f, 2.0f));
                }
                
                notesList.Add(note);
            }
            
            // ノーツリストを設定
            notesField.SetValue(testChart, notesList);
            
            // ChartDataをNoteSpawnerに設定
            noteSpawner.chartData = testChart;
            
            Debug.Log($"[NoteSpawnerTestSetup] {noteCount}個のテストノーツを含む譜面データを生成しました");
        }
        
        /// <summary>
        /// テスト用のノーツプレハブを作成
        /// </summary>
        private void CreateTestPrefabs()
        {
            // Tapノーツプレハブの作成
            if (noteSpawner.tapNotePrefab == null)
            {
                GameObject tapPrefab = CreateSimpleNotePrefab("TapNote", Color.cyan);
                noteSpawner.tapNotePrefab = tapPrefab;
                
                // NotePoolManagerにも設定
                if (notePoolManager != null)
                {
                    notePoolManager.tapNotePrefab = tapPrefab;
                }
            }
            
            // Holdノーツプレハブの作成
            if (noteSpawner.holdNotePrefab == null)
            {
                GameObject holdPrefab = CreateSimpleNotePrefab("HoldNote", Color.yellow);
                noteSpawner.holdNotePrefab = holdPrefab;
                
                // NotePoolManagerにも設定
                if (notePoolManager != null)
                {
                    notePoolManager.holdNotePrefab = holdPrefab;
                }
            }
            
            Debug.Log("[NoteSpawnerTestSetup] テスト用プレハブを作成しました");
        }
        
        /// <summary>
        /// シンプルなノーツプレハブを作成
        /// </summary>
        private GameObject CreateSimpleNotePrefab(string name, Color color)
        {
            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = name;
            
            // スケールを調整
            prefab.transform.localScale = new Vector3(0.8f, 0.3f, 0.5f);
            
            // マテリアルの設定
            Renderer renderer = prefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                renderer.material = mat;
            }
            
            // NoteControllerを追加
            prefab.AddComponent<NoteController>();
            
            // BoxColliderをトリガーに設定
            BoxCollider collider = prefab.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
            
            // プレハブはアクティブ状態のままにする（Instantiate時にアクティブなオブジェクトが生成される）
            // プール管理は各インスタンスで行う
            
            return prefab;
        }
        
        /// <summary>
        /// ランダムな色を取得
        /// </summary>
        private Color GetRandomColor()
        {
            Color[] colors = new Color[]
            {
                Color.red,
                Color.green,
                Color.blue,
                Color.yellow,
                Color.magenta,
                Color.cyan,
                Color.white
            };
            
            return colors[UnityEngine.Random.Range(0, colors.Length)];
        }
        
        /// <summary>
        /// テストを開始
        /// </summary>
        [ContextMenu("Start Test")]
        public void StartTest()
        {
            if (conductor == null || noteSpawner == null)
            {
                Debug.LogError("[NoteSpawnerTestSetup] 必要なコンポーネントが設定されていません");
                return;
            }
            
            AudioSource audioSource = conductor.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                // 既にAudioClipが設定されていない場合のみダミーを作成
                if (audioSource.clip == null)
                {
                    // ダミーのAudioClipを作成（無音）
                    int sampleRate = 44100;
                    int sampleLength = sampleRate * 30; // 30秒の無音
                    AudioClip dummyClip = AudioClip.Create("DummyClip", sampleLength, 1, sampleRate, false);
                    audioSource.clip = dummyClip;
                    Debug.Log("[NoteSpawnerTestSetup] ダミーのAudioClipを作成しました（既存のクリップが無かったため）");
                }
                else
                {
                    Debug.Log($"[NoteSpawnerTestSetup] 既存のAudioClip '{audioSource.clip.name}' ({audioSource.clip.length}秒) を使用します");
                }
            }
            
            Debug.Log("[NoteSpawnerTestSetup] テスト開始 - NoteSpawnerが動作を開始します");
        }
        
        /// <summary>
        /// テストを停止
        /// </summary>
        [ContextMenu("Stop Test")]
        public void StopTest()
        {
            if (noteSpawner != null)
            {
                noteSpawner.StopAndReset();
                Debug.Log("[NoteSpawnerTestSetup] テストを停止しました");
            }
        }
        
        /// <summary>
        /// 統計情報を表示
        /// </summary>
        [ContextMenu("Show Statistics")]
        public void ShowStatistics()
        {
            if (noteSpawner != null)
            {
                int total, spawned, active, remaining;
                noteSpawner.GetStatistics(out total, out spawned, out active, out remaining);
                
                Debug.Log("=== NoteSpawner統計情報 ===");
                Debug.Log($"総ノーツ数: {total}");
                Debug.Log($"生成済み: {spawned}");
                Debug.Log($"アクティブ: {active}");
                Debug.Log($"残り: {remaining}");
            }
            
            if (notePoolManager != null)
            {
                int tapPool, holdPool;
                notePoolManager.GetPoolStatus(out tapPool, out holdPool);
                
                Debug.Log("=== NotePool統計情報 ===");
                Debug.Log($"Tapプールサイズ: {tapPool}");
                Debug.Log($"Holdプールサイズ: {holdPool}");
            }
        }
    }
}