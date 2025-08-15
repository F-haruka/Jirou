#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Jirou.Core;

namespace Jirou.Editor
{
    /// <summary>
    /// 譜面編集ウィンドウ
    /// </summary>
    public class ChartEditorWindow : EditorWindow
    {
        [MenuItem("Jirou/Chart Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<ChartEditorWindow>("Chart Editor");
            window.minSize = new Vector2(400, 600);
        }
        
        private ChartData currentChart;
        private Vector2 scrollPosition;
        private int selectedNoteIndex = -1;
        
        // フィルタ設定
        private bool filterEnabled = false;
        private int filterLane = -1;  // -1 = すべて
        private NoteType filterType = NoteType.Tap;
        private bool filterTypeEnabled = false;
        
        void OnGUI()
        {
            DrawHeader();
            
            if (currentChart == null)
            {
                EditorGUILayout.HelpBox("ChartDataを選択してください", MessageType.Info);
                return;
            }
            
            DrawChartInfo();
            DrawTools();
            DrawFilter();
            DrawNotesList();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("譜面エディタ", EditorStyles.boldLabel);
            
            currentChart = EditorGUILayout.ObjectField(
                "Chart Data", 
                currentChart, 
                typeof(ChartData), 
                false) as ChartData;
                
            EditorGUILayout.Space();
        }
        
        private void DrawChartInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("譜面情報", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"曲名: {currentChart.songName}");
            EditorGUILayout.LabelField($"BPM: {currentChart.bpm}");
            EditorGUILayout.LabelField($"難易度: {currentChart.difficultyName} (Lv.{currentChart.difficulty})");
            EditorGUILayout.LabelField($"ノーツ数: {currentChart.notes.Count}");
            
            var stats = currentChart.GetStatistics();
            EditorGUILayout.LabelField($"譜面長: {stats.chartLengthSeconds:F1}秒");
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTools()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ツール", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("新規ノーツ追加"))
            {
                AddNewNote();
            }
            
            if (GUILayout.Button("ソート"))
            {
                currentChart.SortNotesByTime();
                EditorUtility.SetDirty(currentChart);
            }
            
            if (GUILayout.Button("検証"))
            {
                List<string> errors;
                if (currentChart.ValidateChart(out errors))
                {
                    EditorUtility.DisplayDialog("成功", "譜面は正常です", "OK");
                }
                else
                {
                    string errorMsg = string.Join("\n", errors);
                    EditorUtility.DisplayDialog("エラー", errorMsg, "OK");
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("JSONエクスポート"))
            {
                ExportToJSON();
            }
            
            if (GUILayout.Button("JSONインポート"))
            {
                ImportFromJSON();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFilter()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            filterEnabled = EditorGUILayout.Toggle("フィルタ有効", filterEnabled);
            
            if (filterEnabled)
            {
                EditorGUI.indentLevel++;
                
                filterLane = EditorGUILayout.IntPopup(
                    "レーン", 
                    filterLane,
                    new string[] { "すべて", "レーン0 (D)", "レーン1 (F)", "レーン2 (J)", "レーン3 (K)" },
                    new int[] { -1, 0, 1, 2, 3 });
                    
                filterTypeEnabled = EditorGUILayout.Toggle("タイプフィルタ", filterTypeEnabled);
                if (filterTypeEnabled)
                {
                    filterType = (NoteType)EditorGUILayout.EnumPopup("ノーツタイプ", filterType);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawNotesList()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"ノーツリスト ({GetFilteredNotes().Count}件)", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            
            var filteredNotes = GetFilteredNotes();
            
            for (int i = 0; i < filteredNotes.Count; i++)
            {
                var note = filteredNotes[i];
                int originalIndex = currentChart.notes.IndexOf(note);
                
                EditorGUILayout.BeginHorizontal();
                
                // 選択状態の表示
                bool isSelected = (originalIndex == selectedNoteIndex);
                if (isSelected)
                {
                    GUI.backgroundColor = Color.cyan;
                }
                
                // ノーツ情報
                string noteInfo = $"[{originalIndex}] {note.noteType} - " +
                                 $"レーン{note.laneIndex} - " +
                                 $"{note.timeToHit:F2}ビート";
                                 
                if (note.noteType == NoteType.Hold)
                {
                    noteInfo += $" (長さ: {note.holdDuration:F2})";
                }
                
                if (GUILayout.Button(noteInfo, EditorStyles.miniButton))
                {
                    selectedNoteIndex = originalIndex;
                }
                
                // 削除ボタン
                if (GUILayout.Button("削除", GUILayout.Width(40)))
                {
                    Undo.RecordObject(currentChart, "Delete Note");
                    currentChart.notes.RemoveAt(originalIndex);
                    EditorUtility.SetDirty(currentChart);
                    selectedNoteIndex = -1;
                }
                
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            // 選択中のノーツの詳細編集
            if (selectedNoteIndex >= 0 && selectedNoteIndex < currentChart.notes.Count)
            {
                DrawNoteDetails(currentChart.notes[selectedNoteIndex]);
            }
        }
        
        private void DrawNoteDetails(NoteData note)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ノーツ詳細", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            note.noteType = (NoteType)EditorGUILayout.EnumPopup("タイプ", note.noteType);
            note.laneIndex = EditorGUILayout.IntSlider("レーン", note.laneIndex, 0, 3);
            note.timeToHit = EditorGUILayout.FloatField("タイミング（ビート）", note.timeToHit);
            
            if (note.noteType == NoteType.Hold)
            {
                note.holdDuration = EditorGUILayout.FloatField("長さ（ビート）", note.holdDuration);
            }
            
            note.visualScale = EditorGUILayout.Slider("スケール", note.visualScale, 0.5f, 2.0f);
            note.noteColor = EditorGUILayout.ColorField("色", note.noteColor);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(currentChart);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private List<NoteData> GetFilteredNotes()
        {
            if (!filterEnabled)
            {
                return currentChart.notes;
            }
            
            var filtered = new List<NoteData>();
            
            foreach (var note in currentChart.notes)
            {
                bool passLaneFilter = (filterLane == -1 || note.laneIndex == filterLane);
                bool passTypeFilter = (!filterTypeEnabled || note.noteType == filterType);
                
                if (passLaneFilter && passTypeFilter)
                {
                    filtered.Add(note);
                }
            }
            
            return filtered;
        }
        
        private void AddNewNote()
        {
            Undo.RecordObject(currentChart, "Add Note");
            
            var newNote = new NoteData
            {
                noteType = NoteType.Tap,
                laneIndex = 0,
                timeToHit = 0f,
                visualScale = 1.0f,
                noteColor = Color.white
            };
            
            currentChart.notes.Add(newNote);
            EditorUtility.SetDirty(currentChart);
            
            selectedNoteIndex = currentChart.notes.Count - 1;
        }
        
        private void ExportToJSON()
        {
            string path = EditorUtility.SaveFilePanel(
                "Export Chart to JSON",
                Application.dataPath,
                currentChart.songName + ".json",
                "json");
                
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    // ChartDataをJSONエクスポート用の構造に変換
                    var exportData = new ChartExportData
                    {
                        songName = currentChart.songName,
                        artist = currentChart.artist,
                        bpm = currentChart.bpm,
                        difficulty = currentChart.difficulty,
                        difficultyName = currentChart.difficultyName,
                        firstBeatOffset = currentChart.firstBeatOffset,
                        chartAuthor = currentChart.chartAuthor,
                        chartVersion = currentChart.chartVersion,
                        notes = new List<NoteExportData>()
                    };
                    
                    // ノーツデータをエクスポート形式に変換
                    foreach (var note in currentChart.notes)
                    {
                        var exportNote = new NoteExportData
                        {
                            type = note.noteType.ToString(),
                            lane = note.laneIndex,
                            time = note.timeToHit,
                            duration = note.holdDuration,
                            scale = note.visualScale,
                            color = ColorToHex(note.noteColor),
                            score = note.baseScore,
                            multiplier = note.scoreMultiplier
                        };
                        exportData.notes.Add(exportNote);
                    }
                    
                    // JSONに変換して保存
                    string json = JsonUtility.ToJson(exportData, true);
                    System.IO.File.WriteAllText(path, json);
                    
                    Debug.Log($"譜面をエクスポートしました: {path}");
                    EditorUtility.DisplayDialog("成功", 
                        $"JSONファイルをエクスポートしました\n{path}", "OK");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"エクスポート失敗: {e.Message}");
                    EditorUtility.DisplayDialog("エラー", 
                        $"エクスポートに失敗しました\n{e.Message}", "OK");
                }
            }
        }
        
        private void ImportFromJSON()
        {
            string path = EditorUtility.OpenFilePanel(
                "Import Chart from JSON",
                Application.dataPath,
                "json");
                
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    // JSONファイルを読み込み
                    string json = System.IO.File.ReadAllText(path);
                    var importData = JsonUtility.FromJson<ChartExportData>(json);
                    
                    if (importData == null)
                    {
                        throw new System.Exception("JSONデータの解析に失敗しました");
                    }
                    
                    // 現在の譜面データを更新
                    Undo.RecordObject(currentChart, "Import Chart from JSON");
                    
                    currentChart.songName = importData.songName;
                    currentChart.artist = importData.artist;
                    currentChart.bpm = importData.bpm;
                    currentChart.difficulty = importData.difficulty;
                    currentChart.difficultyName = importData.difficultyName;
                    currentChart.firstBeatOffset = importData.firstBeatOffset;
                    currentChart.chartAuthor = importData.chartAuthor;
                    currentChart.chartVersion = importData.chartVersion;
                    
                    // ノーツデータをクリアして新規追加
                    currentChart.notes.Clear();
                    
                    foreach (var importNote in importData.notes)
                    {
                        var note = new NoteData
                        {
                            noteType = (NoteType)System.Enum.Parse(typeof(NoteType), importNote.type),
                            laneIndex = importNote.lane,
                            timeToHit = importNote.time,
                            holdDuration = importNote.duration,
                            visualScale = importNote.scale,
                            noteColor = HexToColor(importNote.color),
                            baseScore = importNote.score,
                            scoreMultiplier = importNote.multiplier
                        };
                        currentChart.notes.Add(note);
                    }
                    
                    // ノーツをソート
                    currentChart.SortNotesByTime();
                    EditorUtility.SetDirty(currentChart);
                    
                    Debug.Log($"譜面をインポートしました: {path}");
                    EditorUtility.DisplayDialog("成功", 
                        $"JSONファイルをインポートしました\n{path}\n{importData.notes.Count}個のノーツを読み込みました", "OK");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"インポート失敗: {e.Message}");
                    EditorUtility.DisplayDialog("エラー", 
                        $"インポートに失敗しました\n{e.Message}", "OK");
                }
            }
        }
        
        // 色をHEX文字列に変換
        private string ColorToHex(Color color)
        {
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);
            int a = Mathf.RoundToInt(color.a * 255);
            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }
        
        // HEX文字列を色に変換
        private Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Color.white;
                
            hex = hex.Replace("#", "");
            
            if (hex.Length < 6)
                return Color.white;
                
            try
            {
                int r = System.Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = System.Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = System.Convert.ToInt32(hex.Substring(4, 2), 16);
                int a = hex.Length >= 8 ? System.Convert.ToInt32(hex.Substring(6, 2), 16) : 255;
                
                return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
            }
            catch
            {
                return Color.white;
            }
        }
    }
    
    // JSONエクスポート/インポート用のデータ構造
    [System.Serializable]
    public class ChartExportData
    {
        public string songName;
        public string artist;
        public float bpm;
        public int difficulty;
        public string difficultyName;
        public float firstBeatOffset;
        public string chartAuthor;
        public string chartVersion;
        public List<NoteExportData> notes;
    }
    
    [System.Serializable]
    public class NoteExportData
    {
        public string type;
        public int lane;
        public float time;
        public float duration;
        public float scale;
        public string color;
        public int score;
        public float multiplier;
    }
}
#endif