# ノーツデータ アーキテクチャ設計書

## 概要

本書は、奥行き型リズムゲーム「Jirou」のノーツデータ構造の詳細設計を定義します。ノーツはゲームプレイの中核要素として、譜面データの保存、ノーツの生成、判定、視覚表現のすべてに関わる重要なデータ構造です。

## 設計原則

### 1. データ駆動設計
- ScriptableObjectを活用した編集しやすいデータ構造
- Inspectorでの直感的な編集
- ランタイムでの効率的なデータアクセス

### 2. 拡張性
- 新しいノーツタイプの追加が容易
- 視覚効果やサウンドエフェクトの柔軟な設定
- 将来的な機能追加を考慮した設計

### 3. 奥行き表現の最適化
- Z軸移動に特化したデータ構造
- 3D空間での判定に必要な情報の格納
- パフォーマンスを考慮したメモリ効率

## データ構造設計

### NoteType列挙型

```csharp
[System.Serializable]
public enum NoteType
{
    Tap = 0,    // 単押しノーツ
    Hold = 1    // 長押しノーツ
}
```

**設計理由**:
- シンプルな2種類のノーツタイプで初心者にも分かりやすい
- 将来的な拡張（Slide、Flickなど）も考慮した列挙型
- シリアライズ可能でInspectorでの選択が容易

### NoteDataクラス

```csharp
[System.Serializable]
public class NoteData
{
    [Header("基本情報")]
    public NoteType noteType;
    public int laneIndex;        // レーン番号（0-3）
    public float timeToHit;      // ヒットタイミング（ビート単位）
    
    [Header("Holdノーツ専用")]
    public float holdDuration;   // Holdノーツの長さ（ビート単位）
    
    [Header("視覚調整")]
    public float visualScale = 1.0f;  // ノーツの大きさ倍率
    public Color noteColor = Color.white;  // ノーツの色（デフォルト: 白）
    
    [Header("サウンド設定")]
    public AudioClip customHitSound;  // カスタムヒット音（null可）
    
    [Header("エフェクト設定")]
    public GameObject customHitEffect;  // カスタムヒットエフェクト（null可）
    
    [Header("スコア設定")]
    public int baseScore = 100;  // 基本スコア値
    public float scoreMultiplier = 1.0f;  // スコア倍率
}
```

### フィールド詳細仕様

#### 基本情報フィールド

| フィールド名 | 型 | 範囲/制約 | 説明 |
|------------|---|----------|------|
| noteType | NoteType | Tap/Hold | ノーツの種類 |
| laneIndex | int | 0-3 | 4レーンのインデックス |
| timeToHit | float | 0以上 | 判定タイミング（ビート単位） |

#### Holdノーツ専用フィールド

| フィールド名 | 型 | 範囲/制約 | 説明 |
|------------|---|----------|------|
| holdDuration | float | 0以上 | 長押し継続時間（ビート単位） |

**設計理由**:
- Tapノーツの場合は0として扱う
- ビート単位で管理することでBPM変更に対応

#### 視覚調整フィールド

| フィールド名 | 型 | デフォルト値 | 説明 |
|------------|---|------------|------|
| visualScale | float | 1.0f | ノーツサイズの倍率 |
| noteColor | Color | white | ノーツの色調整 |

**設計理由**:
- 譜面の強調表現や難易度の視覚的表現に使用
- 特殊なノーツ（ボーナスノーツなど）の差別化

#### レーン位置マッピング

```csharp
// レーンインデックスとX座標の対応
public static readonly float[] LaneXPositions = { -3f, -1f, 1f, 3f };

// レーンインデックスと入力キーの対応
public static readonly KeyCode[] LaneKeys = 
{ 
    KeyCode.D, 
    KeyCode.F, 
    KeyCode.J, 
    KeyCode.K 
};
```

### ChartDataクラス（ScriptableObject）

詳細は [ChartData アーキテクチャ設計書](./chart-data-architecture.md) を参照してください。

ChartDataは楽曲情報とノーツデータを統合管理するScriptableObjectです。

## メソッド設計

### NoteDataのユーティリティメソッド

NoteDataクラスには、個別のノーツデータを管理・検証するためのメソッドが実装されています。
ChartDataクラスのメソッドについては、[ChartData アーキテクチャ設計書](./chart-data-architecture.md) を参照してください。

## Z軸移動の計算モデル

### ノーツの3D位置計算

```csharp
public struct NotePosition3D
{
    public float x;  // レーン位置
    public float y;  // 高さ（基本的に固定）
    public float z;  // 奥行き位置
    
    // コンストラクタ
    public NotePosition3D(int laneIndex, float zPosition, float yPosition = 0.5f)
    {
        x = NoteData.LaneXPositions[laneIndex];
        y = yPosition;
        z = zPosition;
    }
    
    // Unity Vector3への変換
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}
```

### 動的スケーリング計算

```csharp
public static class NoteVisualCalculator
{
    // 距離に基づくスケール計算
    public static float CalculateScaleByDistance(float currentZ, float spawnZ)
    {
        // 奥（spawnZ）で0.5倍、手前（0）で1.5倍にスケーリング
        float distanceRatio = currentZ / spawnZ;
        return Mathf.Lerp(1.5f, 0.5f, distanceRatio);
    }
    
    // 透明度計算（フェードイン効果）
    public static float CalculateAlphaByDistance(float currentZ, float spawnZ)
    {
        // 奥20%で透明度0から1へフェードイン
        float fadeStartZ = spawnZ * 0.8f;
        if (currentZ > fadeStartZ)
        {
            float fadeRatio = (currentZ - fadeStartZ) / (spawnZ - fadeStartZ);
            return 1f - fadeRatio;
        }
        return 1f;
    }
}
```

## パフォーマンス最適化

### メモリ使用量の最適化

```csharp
// ノーツプール管理
public class NotePool
{
    private Queue<GameObject> tapNotePool = new Queue<GameObject>();
    private Queue<GameObject> holdNotePool = new Queue<GameObject>();
    
    public GameObject GetNote(NoteType type)
    {
        Queue<GameObject> pool = type == NoteType.Tap ? tapNotePool : holdNotePool;
        
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        
        // プールが空の場合は新規作成
        return CreateNewNote(type);
    }
    
    public void ReturnNote(GameObject note, NoteType type)
    {
        note.SetActive(false);
        Queue<GameObject> pool = type == NoteType.Tap ? tapNotePool : holdNotePool;
        pool.Enqueue(note);
    }
}
```

### 処理負荷の分散

```csharp
// ノーツの段階的ロード
public class ChartLoader
{
    private const int NOTES_PER_FRAME = 10;  // 1フレームあたりの処理数
    
    public IEnumerator LoadChartAsync(ChartData chart, Action<float> onProgress)
    {
        int totalNotes = chart.notes.Count;
        int processed = 0;
        
        for (int i = 0; i < totalNotes; i += NOTES_PER_FRAME)
        {
            int end = Mathf.Min(i + NOTES_PER_FRAME, totalNotes);
            
            for (int j = i; j < end; j++)
            {
                // ノーツデータの前処理
                PreprocessNoteData(chart.notes[j]);
                processed++;
            }
            
            // 進捗通知
            onProgress?.Invoke((float)processed / totalNotes);
            
            // 次フレームまで待機
            yield return null;
        }
    }
}
```

## エディタ拡張

### カスタムPropertyDrawer

```csharp
#if UNITY_EDITOR
using UnityEditor;

[CustomPropertyDrawer(typeof(NoteData))]
public class NoteDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // ノーツタイプに応じて背景色を変更
        var noteType = property.FindPropertyRelative("noteType");
        if (noteType.enumValueIndex == (int)NoteType.Hold)
        {
            EditorGUI.DrawRect(position, new Color(1f, 1f, 0.5f, 0.1f));
        }
        
        // プロパティを描画
        EditorGUI.PropertyField(position, property, label, true);
        
        EditorGUI.EndProperty();
    }
}
#endif
```

### 譜面エディタツール

```csharp
#if UNITY_EDITOR
public class ChartEditorWindow : EditorWindow
{
    [MenuItem("Jirou/Chart Editor")]
    public static void ShowWindow()
    {
        GetWindow<ChartEditorWindow>("Chart Editor");
    }
    
    private ChartData currentChart;
    private Vector2 scrollPosition;
    
    void OnGUI()
    {
        // チャート選択
        currentChart = EditorGUILayout.ObjectField(
            "Chart", 
            currentChart, 
            typeof(ChartData), 
            false) as ChartData;
            
        if (currentChart == null) return;
        
        // 譜面情報表示
        EditorGUILayout.LabelField("曲名:", currentChart.songName);
        EditorGUILayout.LabelField("BPM:", currentChart.bpm.ToString());
        EditorGUILayout.LabelField("ノーツ数:", currentChart.notes.Count.ToString());
        
        // ツールボタン
        if (GUILayout.Button("ノーツをソート"))
        {
            currentChart.SortNotesByTime();
            EditorUtility.SetDirty(currentChart);
        }
        
        if (GUILayout.Button("譜面を検証"))
        {
            List<string> errors;
            if (!currentChart.ValidateChart(out errors))
            {
                foreach (var error in errors)
                {
                    Debug.LogError(error);
                }
            }
            else
            {
                Debug.Log("譜面データは正常です");
            }
        }
    }
}
#endif
```

## JSON形式でのインポート/エクスポート

### JSONデータ構造

```json
{
  "version": "1.0",
  "songInfo": {
    "name": "テスト楽曲",
    "artist": "アーティスト名",
    "bpm": 120,
    "offset": 0.5
  },
  "difficulty": {
    "level": 5,
    "name": "Normal"
  },
  "notes": [
    {
      "type": "Tap",
      "lane": 0,
      "time": 1.0,
      "scale": 1.0,
      "color": "#FFFFFF"
    },
    {
      "type": "Hold",
      "lane": 2,
      "time": 2.0,
      "duration": 1.0,
      "scale": 1.2,
      "color": "#FFFF00"
    }
  ]
}
```

### インポート/エクスポート実装

```csharp
public static class ChartDataConverter
{
    public static string ExportToJSON(ChartData chart)
    {
        var jsonData = new ChartJSONData
        {
            version = "1.0",
            songInfo = new SongInfo
            {
                name = chart.songName,
                artist = chart.artist,
                bpm = chart.bpm,
                offset = chart.firstBeatOffset
            },
            notes = chart.notes.Select(n => new NoteJSONData
            {
                type = n.noteType.ToString(),
                lane = n.laneIndex,
                time = n.timeToHit,
                duration = n.holdDuration,
                scale = n.visualScale,
                color = ColorUtility.ToHtmlStringRGB(n.noteColor)
            }).ToList()
        };
        
        return JsonUtility.ToJson(jsonData, true);
    }
    
    public static void ImportFromJSON(ChartData chart, string json)
    {
        var jsonData = JsonUtility.FromJson<ChartJSONData>(json);
        
        chart.songName = jsonData.songInfo.name;
        chart.artist = jsonData.songInfo.artist;
        chart.bpm = jsonData.songInfo.bpm;
        chart.firstBeatOffset = jsonData.songInfo.offset;
        
        chart.notes.Clear();
        foreach (var noteJson in jsonData.notes)
        {
            var noteData = new NoteData
            {
                noteType = (NoteType)Enum.Parse(typeof(NoteType), noteJson.type),
                laneIndex = noteJson.lane,
                timeToHit = noteJson.time,
                holdDuration = noteJson.duration,
                visualScale = noteJson.scale
            };
            
            ColorUtility.TryParseHtmlString("#" + noteJson.color, out noteData.noteColor);
            chart.notes.Add(noteData);
        }
    }
}
```

## テスト仕様

### ユニットテスト項目

```csharp
[TestFixture]
public class NoteDataTests
{
    [Test]
    public void TestLaneIndexValidation()
    {
        var note = new NoteData { laneIndex = 2 };
        Assert.IsTrue(note.laneIndex >= 0 && note.laneIndex < 4);
    }
    
    [Test]
    public void TestHoldNoteDuration()
    {
        var note = new NoteData 
        { 
            noteType = NoteType.Hold,
            holdDuration = 2.0f 
        };
        Assert.Greater(note.holdDuration, 0);
    }
    
    [Test]
    public void TestNotePosition3D()
    {
        var pos = new NotePosition3D(1, 10f);
        Assert.AreEqual(-1f, pos.x);  // Lane 1のX座標
        Assert.AreEqual(10f, pos.z);
    }
}
```

### 統合テスト項目

1. **譜面ロードテスト**
   - ScriptableObjectからのデータ読み込み
   - 大量ノーツ（1000個以上）の処理性能
   - メモリ使用量の監視

2. **視覚表現テスト**
   - スケーリング計算の正確性
   - カラー設定の反映
   - エフェクトの生成

3. **判定連携テスト**
   - NoteDataとJudgmentSystemの連携
   - タイミング精度の検証
   - Holdノーツの継続判定

## セキュリティと制限事項

### 入力値の制限

```csharp
public class NoteDataValidator
{
    public const int MAX_NOTES_PER_CHART = 10000;  // 最大ノーツ数
    public const float MAX_CHART_LENGTH_MINUTES = 10f;  // 最大譜面長（分）
    public const float MIN_NOTE_INTERVAL = 0.0625f;  // 最小ノーツ間隔（1/16ビート）
    
    public static bool ValidateNoteCount(int count)
    {
        return count >= 0 && count <= MAX_NOTES_PER_CHART;
    }
    
    public static bool ValidateChartLength(float beats, float bpm)
    {
        float minutes = (beats * 60f) / bpm;
        return minutes <= MAX_CHART_LENGTH_MINUTES;
    }
}
```

### エラー処理

```csharp
public class NoteDataException : System.Exception
{
    public NoteDataException(string message) : base(message) { }
}

public class InvalidLaneIndexException : NoteDataException
{
    public InvalidLaneIndexException(int laneIndex) 
        : base($"無効なレーンインデックス: {laneIndex}") { }
}

public class InvalidNoteTimingException : NoteDataException
{
    public InvalidNoteTimingException(float timing) 
        : base($"無効なタイミング値: {timing}") { }
}
```

## まとめ

ノーツデータ構造は、Jirouの奥行き型リズムゲームにおける中核的なデータモデルです。ScriptableObjectベースの設計により、デザイナーフレンドリーな編集環境を提供しながら、実行時の高いパフォーマンスを実現します。拡張性を考慮した設計により、将来的な機能追加にも柔軟に対応可能です。