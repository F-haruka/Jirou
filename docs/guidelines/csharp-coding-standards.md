# C# コーディングスタンダード

このドキュメントは、Jirouプロジェクトで使用するC#コーディング規約を定義します。Microsoft公式のC#コーディング規約とUnity特有のベストプラクティスを統合しています。

## 1. 命名規則

### 1.1 基本的な命名規則

| 識別子 | 命名規則 | 例 |
|--------|----------|-----|
| クラス、構造体 | PascalCase | `NoteController`, `ScoreManager` |
| インターフェース | PascalCase + I接頭辞 | `INoteTiming`, `IScoreCalculator` |
| メソッド | PascalCase | `CalculateScore`, `SpawnNote` |
| プロパティ | PascalCase | `CurrentScore`, `IsPlaying` |
| イベント | PascalCase | `OnNoteHit`, `OnComboBreak` |
| パブリックフィールド | PascalCase | `MaxHealth` (避けるべき) |
| プライベートフィールド | _camelCase | `_currentBeat`, `_noteSpeed` |
| ローカル変数 | camelCase | `notePosition`, `elapsedTime` |
| パラメーター | camelCase | `targetPosition`, `beatInterval` |
| 定数 | PascalCase | `DefaultBpm`, `MaxCombo` |
| 列挙型 | PascalCase | `JudgmentType`, `NoteType` |
| 列挙値 | PascalCase | `Perfect`, `Great`, `Miss` |

### 1.2 Unity特有の命名規則

```csharp
// 良い例
public class NoteController : MonoBehaviour
{
    [SerializeField] private float _noteSpeed = 5f;
    [SerializeField] private GameObject _notePrefab;
    
    private Transform _cachedTransform;
    private Rigidbody _cachedRigidbody;
    
    public float NoteSpeed => _noteSpeed;
    
    private void Awake()
    {
        _cachedTransform = transform;
        _cachedRigidbody = GetComponent<Rigidbody>();
    }
}

// 悪い例
public class note_controller : MonoBehaviour
{
    public float noteSpeed = 5f;  // publicフィールドは避ける
    GameObject NotePrefab;  // 命名規則が不統一
    
    Transform t;  // 省略した名前
    Rigidbody rb;  // 意味不明な略語
}
```

## 2. 型の使用

### 2.1 varキーワードの使用

```csharp
// 良い例 - 型が明らかな場合
var notes = new List<NoteData>();
var conductor = GetComponent<Conductor>();
var score = CalculateScore(judgment);

// 悪い例 - 型が不明確な場合
var result = ProcessData();  // 戻り値の型が不明
var data = GetValue();  // 何のデータか不明
```

### 2.2 明示的な型宣言

```csharp
// 数値リテラルは明示的に型を指定
float noteSpeed = 5.0f;
double dspTime = AudioSettings.dspTime;
int laneIndex = 2;

// LINQクエリの結果は明示的に
IEnumerable<NoteData> upcomingNotes = notes.Where(n => n.Beat > currentBeat);
```

## 3. コード構造とフォーマット

### 3.1 インデントとブレース

```csharp
// Allmanスタイル（推奨）
public class ScoreManager : MonoBehaviour
{
    private int _currentScore;
    
    public void AddScore(int points)
    {
        if (points > 0)
        {
            _currentScore += points;
            UpdateScoreDisplay();
        }
    }
}
```

### 3.2 メソッドの長さと責任

```csharp
// 良い例 - 単一責任の原則
public class NoteSpawner : MonoBehaviour
{
    public void SpawnNote(NoteData noteData)
    {
        GameObject note = CreateNoteObject(noteData);
        SetNotePosition(note, noteData);
        SetNoteProperties(note, noteData);
    }
    
    private GameObject CreateNoteObject(NoteData noteData)
    {
        return Instantiate(GetNotePrefab(noteData.Type));
    }
    
    private void SetNotePosition(GameObject note, NoteData noteData)
    {
        float xPosition = GetLaneXPosition(noteData.Lane);
        note.transform.position = new Vector3(xPosition, 0.5f, 20f);
    }
}
```

## 4. Unity MonoBehaviourベストプラクティス

### 4.1 ライフサイクルメソッドの順序

```csharp
public class GameplayController : MonoBehaviour
{
    // 1. フィールドとプロパティ
    [SerializeField] private float _gameSpeed = 1.0f;
    private bool _isPlaying;
    
    // 2. Unityライフサイクルメソッド（実行順）
    private void Awake()
    {
        // コンポーネントの取得とキャッシュ
    }
    
    private void OnEnable()
    {
        // イベントの購読
    }
    
    private void Start()
    {
        // 初期化処理
    }
    
    private void Update()
    {
        // 毎フレームの更新処理
    }
    
    private void FixedUpdate()
    {
        // 物理演算の更新
    }
    
    private void OnDisable()
    {
        // イベントの購読解除
    }
    
    private void OnDestroy()
    {
        // クリーンアップ処理
    }
    
    // 3. パブリックメソッド
    public void StartGame()
    {
        _isPlaying = true;
    }
    
    // 4. プライベートメソッド
    private void ProcessInput()
    {
        // 入力処理
    }
}
```

### 4.2 SerializeFieldの使用

```csharp
// 良い例
public class NoteManager : MonoBehaviour
{
    [Header("ノート設定")]
    [SerializeField] private GameObject _tapNotePrefab;
    [SerializeField] private GameObject _holdNotePrefab;
    
    [Header("スポーン設定")]
    [SerializeField] private float _spawnDistance = 20f;
    [SerializeField] private float _noteSpeed = 5f;
    
    [Header("判定設定")]
    [SerializeField, Range(0f, 0.1f)] private float _perfectWindow = 0.05f;
    [SerializeField, Range(0f, 0.2f)] private float _greatWindow = 0.1f;
}
```

## 5. パフォーマンス最適化

### 5.1 GetComponentの最適化

```csharp
// 良い例 - コンポーネントをキャッシュ
public class NoteMovement : MonoBehaviour
{
    private Transform _transform;
    private Rigidbody _rigidbody;
    private Renderer _renderer;
    
    private void Awake()
    {
        _transform = transform;
        _rigidbody = GetComponent<Rigidbody>();
        _renderer = GetComponent<Renderer>();
    }
    
    private void Update()
    {
        // キャッシュされたコンポーネントを使用
        _transform.Translate(Vector3.back * _noteSpeed * Time.deltaTime);
    }
}

// 悪い例 - 毎フレームGetComponent
public class BadNoteMovement : MonoBehaviour
{
    private void Update()
    {
        GetComponent<Transform>().Translate(Vector3.back * 5f * Time.deltaTime);
        GetComponent<Renderer>().material.color = Color.red;
    }
}
```

### 5.2 オブジェクトプーリング

```csharp
public class NotePool : MonoBehaviour
{
    private readonly Queue<GameObject> _notePool = new Queue<GameObject>();
    
    public GameObject GetNote()
    {
        if (_notePool.Count > 0)
        {
            GameObject note = _notePool.Dequeue();
            note.SetActive(true);
            return note;
        }
        else
        {
            return Instantiate(_notePrefab);
        }
    }
    
    public void ReturnNote(GameObject note)
    {
        note.SetActive(false);
        note.transform.position = Vector3.zero;
        _notePool.Enqueue(note);
    }
}
```

## 6. 奥行き型リズムゲーム固有の規約

### 6.1 タイミング処理

```csharp
// 良い例 - AudioSettings.dspTimeを使用
public class Conductor : MonoBehaviour
{
    private double _startDspTime;
    private float _bpm = 120f;
    
    public double CurrentBeat
    {
        get
        {
            double elapsedTime = AudioSettings.dspTime - _startDspTime;
            return elapsedTime * _bpm / 60.0;
        }
    }
}

// 悪い例 - Time.timeを使用（リズムゲームでは不正確）
public class BadConductor : MonoBehaviour
{
    private float _startTime;
    
    public float CurrentBeat
    {
        get
        {
            float elapsedTime = Time.time - _startTime;  // 避けるべき
            return elapsedTime * 120f / 60f;
        }
    }
}
```

### 6.2 3D座標とレーン管理

```csharp
public class LaneManager : MonoBehaviour
{
    // レーンのX座標を定数で定義
    private const float Lane1X = -3f;
    private const float Lane2X = -1f;
    private const float Lane3X = 1f;
    private const float Lane4X = 3f;
    
    // ノートのZ軸設定
    private const float SpawnZ = 20f;
    private const float JudgmentZ = 0f;
    private const float DespawnZ = -2f;
    
    public float GetLaneXPosition(int laneIndex)
    {
        return laneIndex switch
        {
            0 => Lane1X,
            1 => Lane2X,
            2 => Lane3X,
            3 => Lane4X,
            _ => throw new ArgumentOutOfRangeException(nameof(laneIndex))
        };
    }
    
    public float CalculateNoteZPosition(double beatsPassed, float noteSpeed)
    {
        return SpawnZ - (float)(beatsPassed * noteSpeed);
    }
}
```

## 7. 例外処理

```csharp
public class ChartLoader : MonoBehaviour
{
    public ChartData LoadChart(string chartPath)
    {
        if (string.IsNullOrEmpty(chartPath))
        {
            throw new ArgumentNullException(nameof(chartPath), "譜面パスが指定されていません");
        }
        
        try
        {
            ChartData chart = Resources.Load<ChartData>(chartPath);
            if (chart == null)
            {
                throw new FileNotFoundException($"譜面ファイルが見つかりません: {chartPath}");
            }
            
            ValidateChart(chart);
            return chart;
        }
        catch (Exception ex)
        {
            Debug.LogError($"譜面の読み込みに失敗しました: {ex.Message}");
            throw;
        }
    }
    
    private void ValidateChart(ChartData chart)
    {
        if (chart.Notes == null || chart.Notes.Count == 0)
        {
            throw new InvalidOperationException("譜面にノーツが含まれていません");
        }
        
        if (chart.Bpm <= 0)
        {
            throw new InvalidOperationException($"不正なBPM値: {chart.Bpm}");
        }
    }
}
```

## 8. コメントとドキュメント

### 8.1 XMLドキュメントコメント

```csharp
/// <summary>
/// ノーツの判定を行い、判定結果を返します
/// </summary>
/// <param name="noteZ">ノーツのZ座標</param>
/// <param name="inputTime">入力されたタイミング（DSP時間）</param>
/// <returns>判定結果</returns>
public JudgmentType JudgeNote(float noteZ, double inputTime)
{
    float timingDifference = Mathf.Abs(noteZ - JudgmentZ) / _noteSpeed;
    
    if (timingDifference <= _perfectWindow)
        return JudgmentType.Perfect;
    else if (timingDifference <= _greatWindow)
        return JudgmentType.Great;
    else if (timingDifference <= _goodWindow)
        return JudgmentType.Good;
    else
        return JudgmentType.Miss;
}
```

### 8.2 インラインコメント

```csharp
public void ProcessNoteInput(int laneIndex)
{
    // 該当レーンの判定可能なノーツを取得
    NoteController targetNote = GetJudgableNote(laneIndex);
    
    if (targetNote == null)
    {
        // 空打ちの場合はミス判定
        RegisterMiss();
        return;
    }
    
    // ノーツの位置から判定を計算
    JudgmentType judgment = JudgeNote(targetNote.transform.position.z, AudioSettings.dspTime);
    
    // 判定結果に応じた処理
    ProcessJudgment(judgment, targetNote);
}
```

## 9. LINQ使用規約

```csharp
// 良い例 - 読みやすく効率的なLINQ
public class NoteQuery
{
    public IEnumerable<NoteData> GetUpcomingNotes(List<NoteData> allNotes, double currentBeat, double lookAheadBeats)
    {
        return allNotes
            .Where(note => note.Beat > currentBeat && note.Beat <= currentBeat + lookAheadBeats)
            .OrderBy(note => note.Beat)
            .ThenBy(note => note.Lane);
    }
    
    // チェーンが長い場合は改行
    public NoteData GetNextHoldNote(List<NoteData> notes, int laneIndex, double currentBeat)
    {
        return notes
            .Where(note => note.Type == NoteType.Hold)
            .Where(note => note.Lane == laneIndex)
            .Where(note => note.Beat > currentBeat)
            .OrderBy(note => note.Beat)
            .FirstOrDefault();
    }
}
```

## 10. async/awaitパターン

```csharp
public class AudioLoader : MonoBehaviour
{
    public async Task<AudioClip> LoadMusicAsync(string musicPath)
    {
        try
        {
            // Unityの非同期ロード
            ResourceRequest request = Resources.LoadAsync<AudioClip>(musicPath);
            
            while (!request.isDone)
            {
                await Task.Yield();
            }
            
            AudioClip clip = request.asset as AudioClip;
            if (clip == null)
            {
                throw new InvalidOperationException($"音楽ファイルの読み込みに失敗: {musicPath}");
            }
            
            return clip;
        }
        catch (Exception ex)
        {
            Debug.LogError($"音楽読み込みエラー: {ex.Message}");
            throw;
        }
    }
    
    // キャンセル可能な非同期処理
    public async Task<bool> PreloadAssetsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await LoadMusicAsync("song_01");
            cancellationToken.ThrowIfCancellationRequested();
            
            await LoadChartsAsync();
            cancellationToken.ThrowIfCancellationRequested();
            
            return true;
        }
        catch (OperationCanceledException)
        {
            Debug.Log("アセットの読み込みがキャンセルされました");
            return false;
        }
    }
}
```

## 11. イベントとデリゲート

```csharp
public class GameEventManager : MonoBehaviour
{
    // イベントの定義
    public static event Action<JudgmentType> OnNoteJudged;
    public static event Action<int> OnComboChanged;
    public static event Action<float> OnBeatChanged;
    
    // カスタムイベント引数
    public class NoteHitEventArgs : EventArgs
    {
        public int Lane { get; set; }
        public JudgmentType Judgment { get; set; }
        public float Timing { get; set; }
    }
    
    public static event EventHandler<NoteHitEventArgs> OnNoteHit;
    
    // イベントの発火
    public void TriggerNoteJudgment(JudgmentType judgment)
    {
        OnNoteJudged?.Invoke(judgment);
    }
    
    // イベントの購読と購読解除
    private void OnEnable()
    {
        OnNoteJudged += HandleNoteJudged;
        OnComboChanged += HandleComboChanged;
    }
    
    private void OnDisable()
    {
        OnNoteJudged -= HandleNoteJudged;
        OnComboChanged -= HandleComboChanged;
    }
}
```

## 12. 定数とマジックナンバー

```csharp
public static class GameConstants
{
    // ゲーム設定
    public const int MaxCombo = 9999;
    public const float DefaultBpm = 120f;
    public const int LaneCount = 4;
    
    // 判定ウィンドウ（秒）
    public const float PerfectWindow = 0.05f;
    public const float GreatWindow = 0.1f;
    public const float GoodWindow = 0.15f;
    
    // スコア
    public const int PerfectScore = 1000;
    public const int GreatScore = 500;
    public const int GoodScore = 100;
    
    // 3D空間設定
    public const float NoteSpawnZ = 20f;
    public const float NoteJudgmentZ = 0f;
    public const float NoteDespawnZ = -2f;
}

// 使用例
public class ScoreCalculator
{
    public int CalculateNoteScore(JudgmentType judgment)
    {
        return judgment switch
        {
            JudgmentType.Perfect => GameConstants.PerfectScore,
            JudgmentType.Great => GameConstants.GreatScore,
            JudgmentType.Good => GameConstants.GoodScore,
            _ => 0
        };
    }
}
```

## 13. ScriptableObjectの活用

```csharp
[CreateAssetMenu(fileName = "ChartData", menuName = "Jirou/Chart Data")]
public class ChartData : ScriptableObject
{
    [Header("楽曲情報")]
    [SerializeField] private string _songTitle;
    [SerializeField] private string _artist;
    [SerializeField] private AudioClip _musicClip;
    
    [Header("譜面設定")]
    [SerializeField] private float _bpm = 120f;
    [SerializeField] private float _offset = 0f;
    
    [Header("ノーツデータ")]
    [SerializeField] private List<NoteData> _notes = new List<NoteData>();
    
    // プロパティで読み取り専用アクセスを提供
    public string SongTitle => _songTitle;
    public string Artist => _artist;
    public AudioClip MusicClip => _musicClip;
    public float Bpm => _bpm;
    public float Offset => _offset;
    public IReadOnlyList<NoteData> Notes => _notes;
}
```

## 14. 禁止事項

### 14.1 #regionディレクティブの使用禁止

```csharp
// 悪い例 - #regionを使用しない
public class BadExample : MonoBehaviour
{
    #region Fields  // 使用禁止
    private int _score;
    #endregion
}

// 良い例 - 適切なクラス設計で構造を表現
public class GoodExample : MonoBehaviour
{
    // フィールド
    private int _score;
    
    // プロパティ
    public int Score => _score;
    
    // メソッド
    public void AddScore(int points)
    {
        _score += points;
    }
}
```

### 14.2 Time.timeの使用禁止（リズムゲーム）

```csharp
// 絶対に使用しない
float currentTime = Time.time;  // リズムゲームでは不正確

// 必ずAudioSettings.dspTimeを使用
double currentTime = AudioSettings.dspTime;  // 音楽と完全同期
```

## 15. プロジェクト固有のルール

### 15.1 日本語コメント

```csharp
public class NoteController : MonoBehaviour
{
    // ノーツの移動速度
    [SerializeField] private float _noteSpeed = 5f;
    
    /// <summary>
    /// ノーツを判定ラインに向けて移動させる
    /// </summary>
    private void MoveNote()
    {
        // Z軸方向に移動（奥から手前へ）
        transform.Translate(Vector3.back * _noteSpeed * Time.deltaTime);
    }
}
```

### 15.2 ファイル構造

```
Scripts/
├── Core/           # ゲームの核となるシステム
│   ├── Conductor.cs
│   └── GameManager.cs
├── Gameplay/       # ゲームプレイロジック
│   ├── NoteController.cs
│   └── JudgmentManager.cs
├── UI/            # UI関連
│   └── ScoreDisplay.cs
└── Visual/        # ビジュアルエフェクト
    └── LaneRenderer.cs
```

---

このガイドラインは、Jirouプロジェクトの開発において一貫性のある高品質なコードを維持するための基準です。新しいコードを書く際は、必ずこのガイドラインに従ってください。