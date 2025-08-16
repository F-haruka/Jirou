# NoteSpawner 実装計画書

## 実装概要

本書は、Jirouプロジェクトのノーツ生成システム「NoteSpawner」の詳細設計と段階的な実装計画を定義します。NoteSpawnerは、譜面データ（ChartData）に基づいて3D空間にノーツを生成し、奥行き型リズムゲームの中核となるゲームプレイを実現します。

### 📊 実装進捗状況

**完了率**: 75% (基本実装完了、テスト・最適化待ち)

#### 実装済み項目
- [x] **NoteSpawner.cs** - ノーツ生成管理システム（完了）
- [x] **Conductorとの統合** - タイミング同期（完了）
- [x] **ChartDataとの連携** - 譜面データ読み込み（完了）
- [x] **NotePoolManagerとの統合** - オブジェクトプール活用（完了）
- [x] **NoteController.cs** - 基本的なノーツ動作制御（完了）
- [x] **デバッグ機能** - GUI表示、Gizmo表示（完了）

#### 未実装項目
- [ ] **ユニットテスト** - テストカバレッジ
- [ ] **エディタ拡張** - カスタムインスペクター
- [ ] **パフォーマンス最適化** - プロファイリングと調整
- [ ] **判定システム統合** - ノーツヒット判定の実装

## システム要件

### 技術スタック
- Unity 6.0 LTS / 2022.3 LTS
- C# 9.0以上
- MonoBehaviour
- 3D奥行き型ゲームプレイ

### 依存関係
- **Conductor.cs** - タイミング管理（シングルトン）✅ 実装済み
- **ChartData.cs** - 譜面データ（ScriptableObject）✅ 実装済み
- **NoteData.cs** - 個別ノーツデータ ✅ 実装済み
- **NoteController.cs** - ノーツの動作制御 ✅ 実装済み
- **NotePoolManager.cs** - オブジェクトプール管理 ✅ 実装済み

## クラス設計

### NoteSpawnerクラス構造

```csharp
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
            LogDebug($"NoteSpawner初期化完了 - 総ノーツ数: {chartData.notes.Count}");
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
            conductor.songSource.clip = chartData.songClip;
            conductor.songBpm = chartData.bpm;
            
            // 楽曲を開始
            conductor.StartSong();
            
            isSpawning = true;
            LogDebug("ノーツ生成開始");
        }
        
        private void UpdateNoteSpawning()
        {
            // まだ生成していないノーツがあるかチェック
            if (nextNoteIndex >= chartData.notes.Count)
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
            while (nextNoteIndex < chartData.notes.Count)
            {
                NoteData noteData = chartData.notes[nextNoteIndex];
                
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
            return conductor.ShouldSpawnNote(noteData.timeToHit, beatsShownInAdvance);
        }
        
        private void SpawnNote(NoteData noteData)
        {
            GameObject notePrefab = noteData.noteType == NoteType.Tap ? 
                                   tapNotePrefab : holdNotePrefab;
            
            GameObject noteObject = null;
            
            // オブジェクトプールから取得
            if (notePool != null)
            {
                noteObject = notePool.GetNote(noteData.noteType);
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
            
            LogDebug($"ノーツ生成 - タイプ: {noteData.noteType}, レーン: {noteData.laneIndex}, " +
                    $"タイミング: {noteData.timeToHit:F2}ビート, 位置: {spawnPos}");
        }
        
        private Vector3 CalculateSpawnPosition(NoteData noteData)
        {
            // レーンインデックスの検証
            int laneIndex = Mathf.Clamp(noteData.laneIndex, 0, laneXPositions.Length - 1);
            
            return new Vector3(
                laneXPositions[laneIndex],
                noteY,
                conductor.spawnZ
            );
        }
        
        private void ApplyNoteCustomization(GameObject noteObject, NoteData noteData)
        {
            // スケールの適用
            if (noteData.visualScale != 1.0f)
            {
                noteObject.transform.localScale = Vector3.one * noteData.visualScale;
            }
            
            // 色の適用
            if (noteData.noteColor != Color.white)
            {
                Renderer renderer = noteObject.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = noteData.noteColor;
                }
            }
            
            // カスタムエフェクトの設定
            if (noteData.customHitEffect != null)
            {
                NoteController controller = noteObject.GetComponent<NoteController>();
                if (controller != null)
                {
                    controller.customHitEffect = noteData.customHitEffect;
                }
            }
            
            // カスタムサウンドの設定
            if (noteData.customHitSound != null)
            {
                NoteController controller = noteObject.GetComponent<NoteController>();
                if (controller != null)
                {
                    controller.customHitSound = noteData.customHitSound;
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
                        notePool.ReturnNote(note, controller.noteData.noteType);
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
                            notePool.ReturnNote(note, controller.noteData.noteType);
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
            totalNotes = chartData != null ? chartData.notes.Count : 0;
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
```

## 実装スケジュール

### 全体スケジュール（1週間）

| フェーズ | 期間 | 内容 | 状態 |
|---------|------|------|--------|
| Phase 1 | Day 1-2 | 基本構造実装 | ✅ 完了 |
| Phase 2 | Day 3 | Conductor統合 | ✅ 完了 |
| Phase 3 | Day 4 | ChartData連携 | ✅ 完了 |
| Phase 4 | Day 5 | NoteController実装 | ✅ 完了 |
| Phase 5 | Day 6-7 | テスト・最適化 | 🔄 進行中 |

## 実装フェーズ詳細

### Phase 1: 基本構造実装（Day 1-2）✅ 完了

#### タスク
1. ✅ NoteSpawner.csの基本クラス作成
2. ✅ フィールドとプロパティの定義
3. ✅ 初期化処理の実装
4. ✅ デバッグ機能の実装

#### 検証項目
- [x] コンパイルエラーなし
- [x] Inspectorでの設定可能
- [x] デバッグ表示の動作確認

### Phase 2: Conductor統合（Day 3）✅ 完了

#### タスク
1. ✅ Conductorインスタンスの取得
2. ✅ タイミング同期の実装
3. ✅ 楽曲制御との連携

#### 検証項目
- [x] Conductorとの通信確立
- [x] タイミング計算の正確性
- [x] 楽曲再生との同期

### Phase 3: ChartData連携（Day 4）✅ 完了

#### タスク
1. ✅ ChartDataの読み込み
2. ✅ ノーツデータのパース
3. ✅ 生成タイミングの管理

#### 検証項目
- [x] 譜面データの正常読み込み
- [x] ノーツ順序の正確性
- [x] データバリデーション

### Phase 4: NoteController実装（Day 5）✅ 完了

#### タスク
1. ✅ NoteControllerクラスの実装
2. ✅ ノーツ移動ロジックの実装
3. ✅ ヒット処理の実装
4. ✅ プールシステム対応（Resetメソッド）

#### 検証項目
- [x] Z軸移動の正確性
- [x] 判定ライン通過検出
- [x] エフェクト・サウンド再生処理
- [x] オブジェクトプール対応

### Phase 5: テスト・最適化（Day 6-7）🔄 進行中

#### 残タスク
1. ⏳ ユニットテストの実装
2. ⏳ パフォーマンステストの実施
3. ⏳ メモリリーク検証
4. ⏳ エディタ拡張の実装

#### パフォーマンステスト項目
- [ ] 1000ノーツでのスムーズな動作
- [ ] メモリ使用量の監視
- [ ] フレームレートの維持（60FPS）
- [ ] オブジェクトプールの効率性検証

## 他コンポーネントとの連携

### 依存関係図

```
NoteSpawner
    ├── Conductor (シングルトン)
    │   ├── タイミング管理
    │   ├── 楽曲制御
    │   └── Z座標計算
    ├── ChartData (ScriptableObject)
    │   ├── 譜面データ
    │   ├── 楽曲情報
    │   └── ノーツリスト
    ├── NotePoolManager (シングルトン)
    │   ├── オブジェクトプール
    │   └── メモリ最適化
    └── NoteController (各ノーツ)
        ├── 移動制御
        ├── ヒット判定
        └── エフェクト管理
```

### インターフェース定義

#### IConductorInterface
```csharp
public interface IConductor
{
    float songPositionInBeats { get; }
    float spawnZ { get; }
    float hitZ { get; }
    float GetNoteZPosition(float noteBeat);
    bool ShouldSpawnNote(float noteBeat, float beatsInAdvance);
    void StartSong();
    void StopSong();
    void PauseSong();
    void ResumeSong();
}
```

#### INotePoolInterface
```csharp
public interface INotePool
{
    GameObject GetNote(NoteType type);
    void ReturnNote(GameObject note, NoteType type);
}
```

## エラー処理

### エラーコード定義

| コード | エラー内容 | 対処法 |
|--------|-----------|--------|
| NS001 | ChartData未設定 | ChartDataを設定 |
| NS002 | プレハブ未設定 | ノーツプレハブを設定 |
| NS003 | Conductor未検出 | Conductorをシーンに配置 |
| NS004 | レーン数不正 | 4レーンに修正 |
| NS005 | 無効なノーツデータ | ChartDataを検証 |

### エラーハンドリング戦略

```csharp
private void HandleError(string errorCode, string message)
{
    Debug.LogError($"[NoteSpawner] {errorCode}: {message}");
    
    switch (errorCode)
    {
        case "NS001":
        case "NS002":
            // 致命的エラー - コンポーネント無効化
            enabled = false;
            break;
        case "NS003":
            // リトライ可能
            StartCoroutine(RetryFindConductor());
            break;
        default:
            // 警告のみ
            Debug.LogWarning($"[NoteSpawner] 未知のエラー: {errorCode}");
            break;
    }
}
```

## パフォーマンス最適化

### 最適化項目

| 項目 | 手法 | 効果 |
|-----|------|------|
| オブジェクト生成 | オブジェクトプール | GC削減 |
| 生成チェック | 間隔制限 | CPU負荷軽減 |
| アクティブリスト | 定期クリーンアップ | メモリ効率化 |
| デバッグ表示 | 条件付きコンパイル | リリース時削除 |

### ベンチマーク目標

```
環境: Unity 2022.3 LTS
ターゲット: Windows PC

目標性能:
- ノーツ数: 1000+
- FPS: 60以上維持
- メモリ使用: 100MB以下
- 生成遅延: 16ms以下
```

## デバッグ機能

### ランタイムデバッグ
1. **GUI表示**
   - 生成統計
   - アクティブノーツ数
   - 残りノーツ数

2. **Gizmo表示**
   - レーン可視化
   - ノーツ経路
   - スポーン/判定ライン

3. **ログ出力**
   - 生成タイミング
   - エラー情報
   - パフォーマンス指標

### エディタ拡張（オプション）

```csharp
[CustomEditor(typeof(NoteSpawner))]
public class NoteSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        NoteSpawner spawner = (NoteSpawner)target;
        
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Statistics", EditorStyles.boldLabel);
            
            int total, spawned, active, remaining;
            spawner.GetStatistics(out total, out spawned, out active, out remaining);
            
            EditorGUILayout.LabelField($"Total Notes: {total}");
            EditorGUILayout.LabelField($"Spawned: {spawned}");
            EditorGUILayout.LabelField($"Active: {active}");
            EditorGUILayout.LabelField($"Remaining: {remaining}");
            
            if (GUILayout.Button("Pause/Resume"))
            {
                // トグル処理
            }
            
            if (GUILayout.Button("Reset"))
            {
                spawner.StopAndReset();
            }
        }
    }
}
```

## テスト計画

### ユニットテスト項目
1. **初期化テスト**
   - 依存関係の取得
   - データ検証
   - エラーハンドリング

2. **生成ロジックテスト**
   - タイミング計算
   - レーン配置
   - カスタマイズ適用

3. **メモリ管理テスト**
   - プール使用
   - リーク検出
   - クリーンアップ

### 統合テスト項目
1. **楽曲同期テスト**
   - 実楽曲での動作
   - タイミング精度
   - 完走確認

2. **パフォーマンステスト**
   - 高密度譜面
   - 長時間動作
   - メモリ使用量

3. **エッジケーステスト**
   - 空の譜面
   - 異常なBPM
   - 大量同時ノーツ

## リスク管理

| リスク | 影響度 | 発生確率 | 対策 |
|--------|--------|----------|------|
| タイミングずれ | 高 | 中 | AudioSettings.dspTime使用 |
| メモリリーク | 高 | 低 | オブジェクトプール徹底 |
| パフォーマンス低下 | 中 | 中 | 最適化とLOD実装 |
| データ不整合 | 中 | 低 | バリデーション強化 |

## 今後の拡張計画

### 短期計画（1-2週間）
- [x] 基本実装の完成
- [x] NoteController完全実装
- [ ] 判定システム統合
- [ ] ユニットテスト完成

### 中期計画（1ヶ月）
- [ ] エフェクトシステム
- [ ] コンボシステム
- [ ] スコア計算統合
- [ ] 入力システム統合

### 長期計画（2-3ヶ月）
- [ ] マルチプレイヤー対応
- [ ] リプレイ機能
- [ ] カスタム譜面エディタ統合
- [ ] 高度なビジュアルエフェクト

## まとめ

NoteSpawnerは奥行き型リズムゲーム「Jirou」の中核となるコンポーネントです。現在、基本実装は完了し、テストと最適化のフェーズに入っています。

### 現在の実装状況
- ✅ **基本機能完了** - ノーツ生成、Conductor統合、ChartData連携
- ✅ **プールシステム完了** - メモリ効率的なオブジェクト管理
- ✅ **デバッグツール完了** - GUI表示、Gizmo可視化
- 🔄 **テスト・最適化中** - ユニットテスト、パフォーマンス検証

### 次のステップ
1. **ユニットテストの実装** - テストカバレッジの向上
2. **判定システムとの統合** - 入力処理とスコアリング
3. **パフォーマンス最適化** - プロファイリングと調整
4. **エディタ拡張** - 開発効率の向上

### 成功基準
- ✅ 譜面データに基づいた正確なノーツ生成
- ⏳ 60FPS以上の安定動作（検証中）
- ✅ メモリ効率的な実装（オブジェクトプール実装済み）
- ✅ 拡張可能なアーキテクチャ

本計画書は実装の進捗に応じて適宜更新されます。
最終更新日: 2025年8月16日