# ChartEditor JSON インポート機能 実装計画書

## 概要

本書は、ChartEditor JSON インポート機能の段階的な実装計画を定義します。NotesEditor形式の譜面データをJirouプロジェクトにインポートできるようにする機能を、リスクを最小限に抑えながら実装します。

## 実装スケジュール

### フェーズ1: 基盤構築（1-2日）
- データモデルクラスの実装
- 基本的な変換ロジックの実装
- ユニットテストの作成

### フェーズ2: インポート機能実装（2-3日）
- IChartImporterインターフェースの実装
- NotesEditorJsonImporterクラスの実装
- ChartImportManagerの実装

### フェーズ3: UI統合（1-2日）
- ChartEditorWindowへの統合
- エラーハンドリングとユーザーフィードバック
- プログレス表示の実装

### フェーズ4: テストと最適化（1-2日）
- 統合テストの実施
- パフォーマンス最適化
- ドキュメント作成

## フェーズ1: 基盤構築

### 1.1 データモデルクラスの作成

**ファイル**: `Assets/_Jirou/Scripts/Editor/Import/NotesEditorData.cs`

```csharp
using System;
using System.Collections.Generic;

namespace Jirou.Editor.Import
{
    /// <summary>
    /// NotesEditor形式のJSONデータ構造
    /// </summary>
    [Serializable]
    public class NotesEditorData
    {
        public string name;           // 曲名
        public int maxBlock;         // 最大ブロック数
        public float BPM;            // BPM
        public int offset;           // オフセット（ミリ秒）
        public List<NotesEditorNote> notes;  // ノーツリスト
    }
    
    /// <summary>
    /// NotesEditor形式のノーツデータ
    /// </summary>
    [Serializable]
    public class NotesEditorNote
    {
        public int LPB;              // Lines Per Beat
        public int num;              // タイミング（LPB単位）
        public int block;            // レーン番号（0-3）
        public int type;             // ノーツタイプ（1=Tap, 2=Hold）
        public List<NotesEditorNote> notes;  // Holdノーツの終了位置
    }
}
```

### 1.2 基本変換ユーティリティの作成

**ファイル**: `Assets/_Jirou/Scripts/Editor/Import/ChartConversionUtility.cs`

```csharp
using UnityEngine;
using Jirou.Core;

namespace Jirou.Editor.Import
{
    /// <summary>
    /// 譜面データ変換ユーティリティ
    /// </summary>
    public static class ChartConversionUtility
    {
        /// <summary>
        /// ミリ秒オフセットをビート単位に変換
        /// </summary>
        public static float ConvertMillisecondsToBeats(int offsetMs, float bpm)
        {
            if (bpm <= 0) return 0f;
            
            float offsetSeconds = offsetMs / 1000f;
            float beatDuration = 60f / bpm;
            return offsetSeconds / beatDuration;
        }
        
        /// <summary>
        /// LPB単位のタイミングをビート単位に変換
        /// </summary>
        public static float ConvertLPBToBeats(int num, int lpb)
        {
            if (lpb <= 0) return 0f;
            return (float)num / lpb;
        }
        
        /// <summary>
        /// ノーツタイプを変換
        /// </summary>
        public static NoteType ConvertNoteType(int type)
        {
            return type == 2 ? NoteType.Hold : NoteType.Tap;
        }
        
        /// <summary>
        /// Holdノーツの継続時間を計算
        /// </summary>
        public static float CalculateHoldDuration(NotesEditorNote startNote)
        {
            if (startNote.type != 2 || startNote.notes == null || startNote.notes.Count == 0)
            {
                return 0f;
            }
            
            var endNote = startNote.notes[0];
            float startTime = ConvertLPBToBeats(startNote.num, startNote.LPB);
            float endTime = ConvertLPBToBeats(endNote.num, endNote.LPB);
            
            return endTime - startTime;
        }
    }
}
```

### 1.3 ユニットテストの作成

**ファイル**: `Assets/Tests/EditMode/Import/ChartConversionUtilityTests.cs`

```csharp
using NUnit.Framework;
using Jirou.Editor.Import;
using Jirou.Core;

namespace Jirou.Tests.Editor
{
    [TestFixture]
    public class ChartConversionUtilityTests
    {
        [Test]
        public void ConvertMillisecondsToBeats_正常な変換()
        {
            // BPM 120の場合、1ビート = 0.5秒 = 500ms
            float result = ChartConversionUtility.ConvertMillisecondsToBeats(1000, 120f);
            Assert.AreEqual(2f, result, 0.001f);
        }
        
        [Test]
        public void ConvertLPBToBeats_正常な変換()
        {
            // num=16, LPB=4 の場合、16/4 = 4ビート
            float result = ChartConversionUtility.ConvertLPBToBeats(16, 4);
            Assert.AreEqual(4f, result, 0.001f);
        }
        
        [Test]
        public void ConvertNoteType_Tapノーツ()
        {
            var result = ChartConversionUtility.ConvertNoteType(1);
            Assert.AreEqual(NoteType.Tap, result);
        }
        
        [Test]
        public void ConvertNoteType_Holdノーツ()
        {
            var result = ChartConversionUtility.ConvertNoteType(2);
            Assert.AreEqual(NoteType.Hold, result);
        }
    }
}
```

## フェーズ2: インポート機能実装

### 2.1 インポーターインターフェースの定義

**ファイル**: `Assets/_Jirou/Scripts/Editor/Import/IChartImporter.cs`

```csharp
using Jirou.Core;

namespace Jirou.Editor.Import
{
    /// <summary>
    /// 譜面インポーターのインターフェース
    /// </summary>
    public interface IChartImporter
    {
        /// <summary>
        /// フォーマット名を取得
        /// </summary>
        string GetFormatName();
        
        /// <summary>
        /// このインポーターが対応可能か判定
        /// </summary>
        bool CanImport(string content);
        
        /// <summary>
        /// インポート実行
        /// </summary>
        ChartData Import(string content);
    }
}
```

### 2.2 NotesEditorJsonImporterの実装

**ファイル**: `Assets/_Jirou/Scripts/Editor/Import/NotesEditorJsonImporter.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Jirou.Core;

namespace Jirou.Editor.Import
{
    /// <summary>
    /// NotesEditor形式のJSONインポーター
    /// </summary>
    public class NotesEditorJsonImporter : IChartImporter
    {
        public string GetFormatName() => "NotesEditor JSON";
        
        public bool CanImport(string json)
        {
            try
            {
                var data = JsonUtility.FromJson<NotesEditorData>(json);
                return data != null && 
                       data.notes != null && 
                       !string.IsNullOrEmpty(data.name);
            }
            catch
            {
                return false;
            }
        }
        
        public ChartData Import(string json)
        {
            // JSONをパース
            var notesEditorData = ParseJson(json);
            
            // データを検証
            ValidateData(notesEditorData);
            
            // ChartDataに変換
            return ConvertToChartData(notesEditorData);
        }
        
        private NotesEditorData ParseJson(string json)
        {
            try
            {
                var data = JsonUtility.FromJson<NotesEditorData>(json);
                if (data == null)
                {
                    throw new ImportException("JSONの解析に失敗しました");
                }
                return data;
            }
            catch (Exception e)
            {
                throw new ImportException($"JSONパースエラー: {e.Message}");
            }
        }
        
        private void ValidateData(NotesEditorData data)
        {
            var errors = new List<string>();
            
            // BPMの検証
            if (data.BPM <= 0 || data.BPM > 999)
            {
                errors.Add($"無効なBPM値: {data.BPM}");
            }
            
            // ノーツの検証
            if (data.notes == null)
            {
                errors.Add("ノーツデータが存在しません");
            }
            else
            {
                foreach (var note in data.notes)
                {
                    if (note.block < 0 || note.block > 3)
                    {
                        errors.Add($"無効なレーン番号: {note.block} (タイミング: {note.num})");
                    }
                    
                    if (note.type != 1 && note.type != 2)
                    {
                        errors.Add($"無効なノーツタイプ: {note.type} (タイミング: {note.num})");
                    }
                    
                    if (note.LPB <= 0)
                    {
                        errors.Add($"無効なLPB値: {note.LPB} (タイミング: {note.num})");
                    }
                }
            }
            
            if (errors.Count > 0)
            {
                throw new ImportException(string.Join("\n", errors));
            }
        }
        
        private ChartData ConvertToChartData(NotesEditorData data)
        {
            // 新しいChartDataアセットを作成
            var chartData = ScriptableObject.CreateInstance<ChartData>();
            
            // SerializedObjectを使用してプライベートフィールドを設定
            var so = new SerializedObject(chartData);
            
            try
            {
                // 基本情報の設定
                SetChartMetadata(so, data);
                
                // ノーツデータの変換と追加
                ConvertAndAddNotes(chartData, data.notes);
                
                // 変更を適用
                so.ApplyModifiedPropertiesWithoutUndo();
                
                // ノーツをソート
                chartData.SortNotesByTime();
                
                return chartData;
            }
            catch (Exception e)
            {
                // エラー時はアセットを破棄
                UnityEngine.Object.DestroyImmediate(chartData);
                throw new ImportException($"ChartData変換エラー: {e.Message}");
            }
        }
        
        private void SetChartMetadata(SerializedObject so, NotesEditorData data)
        {
            // 基本情報の設定
            so.FindProperty("_songName").stringValue = data.name;
            so.FindProperty("_bpm").floatValue = data.BPM;
            
            // オフセットをビート単位に変換
            float offsetBeats = ChartConversionUtility.ConvertMillisecondsToBeats(data.offset, data.BPM);
            so.FindProperty("_firstBeatOffset").floatValue = offsetBeats;
            
            // デフォルト値の設定
            so.FindProperty("_artist").stringValue = "Unknown Artist";
            so.FindProperty("_difficulty").intValue = 5;
            so.FindProperty("_difficultyName").stringValue = "Normal";
            so.FindProperty("_chartAuthor").stringValue = "Imported";
            so.FindProperty("_chartVersion").stringValue = "1.0";
        }
        
        private void ConvertAndAddNotes(ChartData chartData, List<NotesEditorNote> notesEditorNotes)
        {
            chartData.Notes.Clear();
            
            foreach (var notesEditorNote in notesEditorNotes)
            {
                var noteData = ConvertNote(notesEditorNote);
                if (noteData != null)
                {
                    chartData.Notes.Add(noteData);
                }
            }
            
            Debug.Log($"インポート完了: {chartData.Notes.Count}個のノーツを変換しました");
        }
        
        private NoteData ConvertNote(NotesEditorNote notesEditorNote)
        {
            var noteData = new NoteData();
            
            // 基本プロパティの設定
            noteData.LaneIndex = notesEditorNote.block;
            noteData.NoteType = ChartConversionUtility.ConvertNoteType(notesEditorNote.type);
            noteData.TimeToHit = ChartConversionUtility.ConvertLPBToBeats(notesEditorNote.num, notesEditorNote.LPB);
            
            // Holdノーツの場合、duration計算
            if (noteData.NoteType == NoteType.Hold)
            {
                noteData.HoldDuration = ChartConversionUtility.CalculateHoldDuration(notesEditorNote);
            }
            
            // デフォルト値の設定
            noteData.VisualScale = 1.0f;
            noteData.NoteColor = Color.white;
            noteData.BaseScore = 100;
            noteData.ScoreMultiplier = 1.0f;
            
            return noteData;
        }
    }
    
    /// <summary>
    /// インポート例外クラス
    /// </summary>
    public class ImportException : Exception
    {
        public ImportException(string message) : base(message) { }
    }
}
```

### 2.3 ChartImportManagerの実装

**ファイル**: `Assets/_Jirou/Scripts/Editor/Import/ChartImportManager.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Jirou.Core;

namespace Jirou.Editor.Import
{
    /// <summary>
    /// 譜面インポート管理クラス
    /// </summary>
    public static class ChartImportManager
    {
        private static readonly List<IChartImporter> importers = new List<IChartImporter>();
        
        static ChartImportManager()
        {
            // インポーターを登録
            RegisterImporter(new NotesEditorJsonImporter());
            // 将来的に他のインポーターも追加
        }
        
        /// <summary>
        /// インポーターを登録
        /// </summary>
        public static void RegisterImporter(IChartImporter importer)
        {
            if (importer != null && !importers.Contains(importer))
            {
                importers.Add(importer);
                Debug.Log($"インポーター登録: {importer.GetFormatName()}");
            }
        }
        
        /// <summary>
        /// インポートを試行
        /// </summary>
        public static bool TryImport(string content, out ChartData chartData, out string error)
        {
            chartData = null;
            error = null;
            
            // 対応可能なインポーターを探す
            foreach (var importer in importers)
            {
                if (importer.CanImport(content))
                {
                    try
                    {
                        Debug.Log($"{importer.GetFormatName()}形式として認識しました");
                        chartData = importer.Import(content);
                        return true;
                    }
                    catch (Exception e)
                    {
                        error = $"{importer.GetFormatName()}のインポート失敗:\n{e.Message}";
                        Debug.LogError(error);
                    }
                }
            }
            
            // 対応するインポーターが見つからない場合
            error = "対応するインポート形式が見つかりませんでした。\n" +
                   $"対応形式: {string.Join(", ", GetSupportedFormats())}";
            return false;
        }
        
        /// <summary>
        /// サポートされている形式を取得
        /// </summary>
        public static string[] GetSupportedFormats()
        {
            return importers.Select(i => i.GetFormatName()).ToArray();
        }
        
        /// <summary>
        /// フォーマットを自動検出
        /// </summary>
        public static string DetectFormat(string content)
        {
            foreach (var importer in importers)
            {
                if (importer.CanImport(content))
                {
                    return importer.GetFormatName();
                }
            }
            return "Unknown";
        }
    }
}
```

## フェーズ3: UI統合

### 3.1 ChartEditorWindowの修正

**修正箇所**: `ImportFromJSON()`メソッドを更新

```csharp
private void ImportFromJSON()
{
    string path = GetImportPath();
        
    if (!string.IsNullOrEmpty(path))
    {
        try
        {
            string content = System.IO.File.ReadAllText(path);
            
            // 形式を検出
            string format = ChartImportManager.DetectFormat(content);
            
            // 確認ダイアログ
            if (EditorUtility.DisplayDialog("インポート確認",
                $"検出された形式: {format}\n" +
                $"現在の譜面データは上書きされます。続行しますか？",
                "インポート", "キャンセル"))
            {
                ImportWithManager(content, path);
            }
        }
        catch (System.Exception e)
        {
            HandleImportError(e);
        }
    }
}

private void ImportWithManager(string content, string path)
{
    ChartData importedChart;
    string error;
    
    // プログレスバー表示
    EditorUtility.DisplayProgressBar("インポート中", "譜面データを変換しています...", 0.5f);
    
    try
    {
        if (ChartImportManager.TryImport(content, out importedChart, out error))
        {
            // インポート成功
            ApplyImportedChartData(importedChart);
            ShowImportSuccessDialog(path, importedChart.Notes.Count);
        }
        else
        {
            // インポート失敗
            EditorUtility.DisplayDialog("インポートエラー", error, "OK");
        }
    }
    finally
    {
        EditorUtility.ClearProgressBar();
    }
}

private void ApplyImportedChartData(ChartData importedChart)
{
    if (importedChart == null) return;
    
    Undo.RecordObject(_currentChart, "Import Chart");
    
    // ノーツデータをコピー
    _currentChart.Notes.Clear();
    _currentChart.Notes.AddRange(importedChart.Notes);
    
    // メタデータの更新（可能な範囲で）
    // 注: SerializedPropertyを使用する必要がある場合がある
    
    EditorUtility.SetDirty(_currentChart);
    
    // インポートしたChartDataインスタンスを破棄
    UnityEngine.Object.DestroyImmediate(importedChart);
}
```

### 3.2 インポートダイアログの作成

**ファイル**: `Assets/_Jirou/Scripts/Editor/Import/ImportPreviewDialog.cs`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Jirou.Core;

namespace Jirou.Editor.Import
{
    /// <summary>
    /// インポートプレビューダイアログ
    /// </summary>
    public class ImportPreviewDialog : EditorWindow
    {
        private string jsonContent;
        private string detectedFormat;
        private ChartData previewData;
        private Vector2 scrollPosition;
        private bool showJsonContent = false;
        private List<string> validationMessages = new List<string>();
        
        public static void ShowDialog(string json, System.Action<ChartData> onImport)
        {
            var window = GetWindow<ImportPreviewDialog>("譜面インポート プレビュー");
            window.minSize = new Vector2(500, 400);
            window.jsonContent = json;
            window.AnalyzeContent();
        }
        
        private void AnalyzeContent()
        {
            detectedFormat = ChartImportManager.DetectFormat(jsonContent);
            
            // プレビュー用にインポートを試行
            string error;
            if (ChartImportManager.TryImport(jsonContent, out previewData, out error))
            {
                validationMessages.Add($"✓ {previewData.Notes.Count}個のノーツを検出");
                validationMessages.Add($"✓ BPM: {previewData.Bpm}");
                validationMessages.Add($"✓ 曲名: {previewData.SongName}");
            }
            else
            {
                validationMessages.Add($"✗ {error}");
            }
        }
        
        void OnGUI()
        {
            EditorGUILayout.LabelField("インポート形式", detectedFormat);
            EditorGUILayout.Space();
            
            // プレビュー情報
            if (previewData != null)
            {
                DrawPreviewInfo();
            }
            
            // JSONコンテンツの表示/非表示
            showJsonContent = EditorGUILayout.Foldout(showJsonContent, "JSONコンテンツ");
            if (showJsonContent)
            {
                DrawJsonContent();
            }
            
            // 検証結果
            DrawValidationResults();
            
            // ボタン
            DrawButtons();
        }
        
        private void DrawPreviewInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("譜面情報プレビュー", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField($"曲名: {previewData.SongName}");
            EditorGUILayout.LabelField($"BPM: {previewData.Bpm}");
            EditorGUILayout.LabelField($"ノーツ数: {previewData.Notes.Count}");
            
            var stats = previewData.GetStatistics();
            EditorGUILayout.LabelField($"Tapノーツ: {stats.tapNotes}");
            EditorGUILayout.LabelField($"Holdノーツ: {stats.holdNotes}");
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawJsonContent()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            EditorGUILayout.TextArea(jsonContent, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawValidationResults()
        {
            if (validationMessages.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("検証結果", EditorStyles.boldLabel);
                
                foreach (var msg in validationMessages)
                {
                    var style = msg.StartsWith("✓") ? EditorStyles.label : EditorStyles.helpBox;
                    EditorGUILayout.LabelField(msg, style);
                }
            }
        }
        
        private void DrawButtons()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = previewData != null;
            if (GUILayout.Button("インポート", GUILayout.Height(30)))
            {
                // インポート実行
                Close();
            }
            GUI.enabled = true;
            
            if (GUILayout.Button("キャンセル", GUILayout.Height(30)))
            {
                if (previewData != null)
                {
                    DestroyImmediate(previewData);
                }
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
```

## フェーズ4: テストと最適化

### 4.1 統合テストの作成

**ファイル**: `Assets/Tests/EditMode/Import/NotesEditorImportIntegrationTests.cs`

```csharp
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Jirou.Core;
using Jirou.Editor.Import;

namespace Jirou.Tests.Editor
{
    [TestFixture]
    public class NotesEditorImportIntegrationTests
    {
        private string testDataPath;
        
        [SetUp]
        public void Setup()
        {
            // テストデータのパスを設定
            testDataPath = Path.Combine(Application.dataPath, 
                "_Jirou/Data/ChartsJson/End_Time.json");
        }
        
        [Test]
        public void ImportNotesEditorJson_完全なファイル()
        {
            // ファイルが存在することを確認
            Assert.IsTrue(File.Exists(testDataPath), 
                $"テストファイルが見つかりません: {testDataPath}");
            
            // JSONを読み込み
            string json = File.ReadAllText(testDataPath);
            
            // インポート実行
            ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(json, out chartData, out error);
            
            // 検証
            Assert.IsTrue(success, $"インポート失敗: {error}");
            Assert.IsNotNull(chartData);
            Assert.AreEqual("End_Time", chartData.SongName);
            Assert.AreEqual(180f, chartData.Bpm);
            Assert.Greater(chartData.Notes.Count, 0);
            
            // 後処理
            Object.DestroyImmediate(chartData);
        }
        
        [Test]
        public void ImportNotesEditorJson_ノーツ変換確認()
        {
            string simpleJson = @"{
                ""name"": ""Test"",
                ""BPM"": 120,
                ""offset"": 0,
                ""notes"": [
                    {
                        ""LPB"": 4,
                        ""num"": 8,
                        ""block"": 1,
                        ""type"": 1,
                        ""notes"": []
                    }
                ]
            }";
            
            ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(simpleJson, out chartData, out error);
            
            Assert.IsTrue(success);
            Assert.AreEqual(1, chartData.Notes.Count);
            
            var note = chartData.Notes[0];
            Assert.AreEqual(1, note.LaneIndex);
            Assert.AreEqual(NoteType.Tap, note.NoteType);
            Assert.AreEqual(2f, note.TimeToHit); // 8/4 = 2
            
            Object.DestroyImmediate(chartData);
        }
        
        [Test]
        public void ImportNotesEditorJson_Holdノーツ変換()
        {
            string holdJson = @"{
                ""name"": ""Test"",
                ""BPM"": 120,
                ""offset"": 0,
                ""notes"": [
                    {
                        ""LPB"": 4,
                        ""num"": 0,
                        ""block"": 2,
                        ""type"": 2,
                        ""notes"": [
                            {
                                ""LPB"": 4,
                                ""num"": 16,
                                ""block"": 2,
                                ""type"": 2,
                                ""notes"": []
                            }
                        ]
                    }
                ]
            }";
            
            ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(holdJson, out chartData, out error);
            
            Assert.IsTrue(success);
            Assert.AreEqual(1, chartData.Notes.Count);
            
            var note = chartData.Notes[0];
            Assert.AreEqual(NoteType.Hold, note.NoteType);
            Assert.AreEqual(4f, note.HoldDuration); // (16-0)/4 = 4
            
            Object.DestroyImmediate(chartData);
        }
    }
}
```

### 4.2 パフォーマンステスト

**ファイル**: `Assets/Tests/EditMode/Import/ImportPerformanceTests.cs`

```csharp
using System.Diagnostics;
using NUnit.Framework;
using Jirou.Editor.Import;
using Debug = UnityEngine.Debug;

namespace Jirou.Tests.Editor
{
    [TestFixture]
    public class ImportPerformanceTests
    {
        [Test]
        public void ImportLargeFile_パフォーマンス測定()
        {
            // 大量のノーツを含むテストデータを生成
            var testData = GenerateLargeTestData(1000);
            string json = JsonUtility.ToJson(testData);
            
            var stopwatch = Stopwatch.StartNew();
            
            ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(json, out chartData, out error);
            
            stopwatch.Stop();
            
            Assert.IsTrue(success);
            Assert.AreEqual(1000, chartData.Notes.Count);
            
            Debug.Log($"1000ノーツのインポート時間: {stopwatch.ElapsedMilliseconds}ms");
            
            // パフォーマンス基準: 1000ノーツで1秒以内
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000);
            
            UnityEngine.Object.DestroyImmediate(chartData);
        }
        
        private NotesEditorData GenerateLargeTestData(int noteCount)
        {
            var data = new NotesEditorData
            {
                name = "Performance Test",
                BPM = 120,
                offset = 0,
                notes = new System.Collections.Generic.List<NotesEditorNote>()
            };
            
            for (int i = 0; i < noteCount; i++)
            {
                data.notes.Add(new NotesEditorNote
                {
                    LPB = 4,
                    num = i * 4,
                    block = i % 4,
                    type = 1,
                    notes = new System.Collections.Generic.List<NotesEditorNote>()
                });
            }
            
            return data;
        }
    }
}
```

## 実装チェックリスト

### フェーズ1
- [ ] NotesEditorData.cs の作成
- [ ] NotesEditorNote.cs の作成
- [ ] ChartConversionUtility.cs の作成
- [ ] ユニットテストの作成と実行

### フェーズ2
- [ ] IChartImporter.cs の作成
- [ ] NotesEditorJsonImporter.cs の実装
- [ ] ChartImportManager.cs の実装
- [ ] 基本的な動作確認

### フェーズ3
- [ ] ChartEditorWindow.cs の修正
- [ ] ImportPreviewDialog.cs の作成
- [ ] UIの動作確認
- [ ] エラーハンドリングの確認

### フェーズ4
- [ ] 統合テストの作成と実行
- [ ] End_Time.json の完全インポートテスト
- [ ] パフォーマンス測定
- [ ] ドキュメントの更新

## リスク管理

### 技術的リスク

| リスク | 影響度 | 対策 |
|-------|--------|------|
| SerializedObjectによるChartData更新の失敗 | 高 | 代替手段としてリフレクションを準備 |
| 大量ノーツでのメモリ不足 | 中 | バッチ処理とストリーミング読み込み |
| JSON形式の非互換性 | 中 | 柔軟なパーサーと詳細なエラーメッセージ |
| Unity Editorの応答停止 | 低 | 非同期処理とプログレスバー表示 |

### 運用リスク

| リスク | 影響度 | 対策 |
|-------|--------|------|
| 誤ったファイルのインポート | 低 | フォーマット自動検出と確認ダイアログ |
| データの上書き | 中 | Undo機能とバックアップ推奨 |
| 不完全なデータのインポート | 中 | 詳細な検証とエラーレポート |

## 今後の拡張

### 追加インポート形式
1. **osu!形式**: `.osu`ファイルのサポート
2. **StepMania形式**: `.sm`ファイルのサポート
3. **CSV形式**: シンプルな表形式データ

### 機能拡張
1. **バッチインポート**: 複数ファイルの一括処理
2. **インポート履歴**: 最近インポートしたファイルの記録
3. **カスタムマッピング**: フィールドマッピングのカスタマイズ
4. **プレビュー再生**: インポート前の譜面プレビュー

## まとめ

本実装計画に従って段階的に開発を進めることで、リスクを最小限に抑えながら、NotesEditor形式の譜面データをJirouプロジェクトにインポートする機能を実現します。各フェーズでテストを実施し、品質を確保しながら開発を進めます。