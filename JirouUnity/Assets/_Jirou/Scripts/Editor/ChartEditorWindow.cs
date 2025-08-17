#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Jirou.Core;
using Jirou.Editor.Import;

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
        
        private ChartData _currentChart;
        private Vector2 _scrollPosition;
        private int _selectedNoteIndex = -1;
        
        // フィルタ設定
        private bool _filterEnabled = false;
        private int _filterLane = -1;  // -1 = すべて
        private NoteType _filterType = NoteType.Tap;
        private bool _filterTypeEnabled = false;
        
        void OnGUI()
        {
            DrawHeader();
            
            if (_currentChart == null)
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
            
            _currentChart = EditorGUILayout.ObjectField(
                "Chart Data", 
                _currentChart, 
                typeof(ChartData), 
                false) as ChartData;
                
            EditorGUILayout.Space();
        }
        
        private void DrawChartInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("譜面情報", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"曲名: {_currentChart.SongName}");
            EditorGUILayout.LabelField($"BPM: {_currentChart.Bpm}");
            EditorGUILayout.LabelField($"難易度: {_currentChart.DifficultyName} (Lv.{_currentChart.Difficulty})");
            EditorGUILayout.LabelField($"ノーツ数: {_currentChart.Notes.Count}");
            
            var stats = _currentChart.GetStatistics();
            EditorGUILayout.LabelField($"譜面長: {stats.chartLengthSeconds:F1}秒");
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTools()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ツール", EditorStyles.boldLabel);
            
            DrawFirstToolRow();
            DrawSecondToolRow();
        }
        
        /// <summary>
        /// ツールの1行目を描画
        /// </summary>
        private void DrawFirstToolRow()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("新規ノーツ追加"))
            {
                AddNewNote();
            }
            
            if (GUILayout.Button("ソート"))
            {
                SortNotes();
            }
            
            if (GUILayout.Button("検証"))
            {
                ValidateChart();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// ツールの2行目を描画
        /// </summary>
        private void DrawSecondToolRow()
        {
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
        
        /// <summary>
        /// ノーツをソート
        /// </summary>
        private void SortNotes()
        {
            _currentChart.SortNotesByTime();
            EditorUtility.SetDirty(_currentChart);
        }
        
        /// <summary>
        /// 譜面を検証
        /// </summary>
        private void ValidateChart()
        {
            List<string> errors;
            if (_currentChart.ValidateChart(out errors))
            {
                EditorUtility.DisplayDialog("成功", "譜面は正常です", "OK");
            }
            else
            {
                string errorMsg = string.Join("\n", errors);
                EditorUtility.DisplayDialog("エラー", errorMsg, "OK");
            }
        }
        
        private void DrawFilter()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            _filterEnabled = EditorGUILayout.Toggle("フィルタ有効", _filterEnabled);
            
            if (_filterEnabled)
            {
                DrawFilterOptions();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// フィルタオプションを描画
        /// </summary>
        private void DrawFilterOptions()
        {
            EditorGUI.indentLevel++;
            
            DrawLaneFilter();
            DrawTypeFilter();
            
            EditorGUI.indentLevel--;
        }
        
        /// <summary>
        /// レーンフィルタを描画
        /// </summary>
        private void DrawLaneFilter()
        {
            _filterLane = EditorGUILayout.IntPopup(
                "レーン", 
                _filterLane,
                new string[] { "すべて", "レーン0 (D)", "レーン1 (F)", "レーン2 (J)", "レーン3 (K)" },
                new int[] { -1, 0, 1, 2, 3 });
        }
        
        /// <summary>
        /// タイプフィルタを描画
        /// </summary>
        private void DrawTypeFilter()
        {
            _filterTypeEnabled = EditorGUILayout.Toggle("タイプフィルタ", _filterTypeEnabled);
            if (_filterTypeEnabled)
            {
                _filterType = (NoteType)EditorGUILayout.EnumPopup("ノーツタイプ", _filterType);
            }
        }
        
        private void DrawNotesList()
        {
            EditorGUILayout.Space();
            var filteredNotes = GetFilteredNotes();
            EditorGUILayout.LabelField($"ノーツリスト ({filteredNotes.Count}件)", EditorStyles.boldLabel);
            
            DrawNotesScrollView(filteredNotes);
            DrawSelectedNoteDetails();
        }
        
        /// <summary>
        /// ノーツスクロールビューを描画
        /// </summary>
        private void DrawNotesScrollView(List<NoteData> filteredNotes)
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
            
            for (int i = 0; i < filteredNotes.Count; i++)
            {
                DrawNoteListItem(filteredNotes[i], i);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// ノーツリストアイテムを描画
        /// </summary>
        private void DrawNoteListItem(NoteData note, int listIndex)
        {
            int originalIndex = _currentChart.Notes.IndexOf(note);
            
            EditorGUILayout.BeginHorizontal();
            
            // 選択状態の表示
            SetSelectionBackground(originalIndex);
            
            // ノーツ情報ボタン
            string noteInfo = CreateNoteInfoString(note, originalIndex);
            if (GUILayout.Button(noteInfo, EditorStyles.miniButton))
            {
                _selectedNoteIndex = originalIndex;
            }
            
            // 削除ボタン
            if (GUILayout.Button("削除", GUILayout.Width(40)))
            {
                DeleteNote(originalIndex);
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 選択状態の背景色を設定
        /// </summary>
        private void SetSelectionBackground(int originalIndex)
        {
            bool isSelected = (originalIndex == _selectedNoteIndex);
            if (isSelected)
            {
                GUI.backgroundColor = Color.cyan;
            }
        }
        
        /// <summary>
        /// ノーツ情報文字列を作成
        /// </summary>
        private string CreateNoteInfoString(NoteData note, int originalIndex)
        {
            string noteInfo = $"[{originalIndex}] {note.NoteType} - " +
                             $"レーン{note.LaneIndex} - " +
                             $"{note.TimeToHit:F2}ビート";
                             
            if (note.NoteType == NoteType.Hold)
            {
                noteInfo += $" (長さ: {note.HoldDuration:F2})";
            }
            
            return noteInfo;
        }
        
        /// <summary>
        /// ノーツを削除
        /// </summary>
        private void DeleteNote(int originalIndex)
        {
            Undo.RecordObject(_currentChart, "Delete Note");
            _currentChart.Notes.RemoveAt(originalIndex);
            EditorUtility.SetDirty(_currentChart);
            _selectedNoteIndex = -1;
        }
        
        /// <summary>
        /// 選択中のノーツの詳細を描画
        /// </summary>
        private void DrawSelectedNoteDetails()
        {
            if (IsNoteSelected())
            {
                DrawNoteDetails(_currentChart.Notes[_selectedNoteIndex]);
            }
        }
        
        /// <summary>
        /// ノーツが選択されているかチェック
        /// </summary>
        private bool IsNoteSelected()
        {
            return _selectedNoteIndex >= 0 && _selectedNoteIndex < _currentChart.Notes.Count;
        }
        
        private void DrawNoteDetails(NoteData note)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ノーツ詳細", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            DrawNoteBasicProperties(note);
            DrawNoteHoldProperties(note);
            DrawNoteVisualProperties(note);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_currentChart);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// ノーツの基本プロパティを描画
        /// </summary>
        private void DrawNoteBasicProperties(NoteData note)
        {
            note.NoteType = (NoteType)EditorGUILayout.EnumPopup("タイプ", note.NoteType);
            note.LaneIndex = EditorGUILayout.IntSlider("レーン", note.LaneIndex, 0, 3);
            note.TimeToHit = EditorGUILayout.FloatField("タイミング（ビート）", note.TimeToHit);
        }
        
        /// <summary>
        /// Holdノーツのプロパティを描画
        /// </summary>
        private void DrawNoteHoldProperties(NoteData note)
        {
            if (note.NoteType == NoteType.Hold)
            {
                note.HoldDuration = EditorGUILayout.FloatField("長さ（ビート）", note.HoldDuration);
            }
        }
        
        /// <summary>
        /// ノーツの視覚プロパティを描画
        /// </summary>
        private void DrawNoteVisualProperties(NoteData note)
        {
            note.VisualScale = EditorGUILayout.Slider("スケール", note.VisualScale, 0.5f, 2.0f);
            note.NoteColor = EditorGUILayout.ColorField("色", note.NoteColor);
        }
        
        private List<NoteData> GetFilteredNotes()
        {
            if (!_filterEnabled)
            {
                return _currentChart.Notes;
            }
            
            var filtered = new List<NoteData>();
            
            foreach (var note in _currentChart.Notes)
            {
                if (PassesAllFilters(note))
                {
                    filtered.Add(note);
                }
            }
            
            return filtered;
        }
        
        /// <summary>
        /// ノーツがすべてのフィルタを通るかチェック
        /// </summary>
        private bool PassesAllFilters(NoteData note)
        {
            bool passLaneFilter = PassesLaneFilter(note);
            bool passTypeFilter = PassesTypeFilter(note);
            
            return passLaneFilter && passTypeFilter;
        }
        
        /// <summary>
        /// レーンフィルタを通るかチェック
        /// </summary>
        private bool PassesLaneFilter(NoteData note)
        {
            return _filterLane == -1 || note.LaneIndex == _filterLane;
        }
        
        /// <summary>
        /// タイプフィルタを通るかチェック
        /// </summary>
        private bool PassesTypeFilter(NoteData note)
        {
            return !_filterTypeEnabled || note.NoteType == _filterType;
        }
        
        private void AddNewNote()
        {
            Undo.RecordObject(_currentChart, "Add Note");
            
            var newNote = CreateDefaultNote();
            
            _currentChart.Notes.Add(newNote);
            EditorUtility.SetDirty(_currentChart);
            
            _selectedNoteIndex = _currentChart.Notes.Count - 1;
        }
        
        /// <summary>
        /// デフォルトノーツを作成
        /// </summary>
        private NoteData CreateDefaultNote()
        {
            var newNote = new NoteData();
            newNote.NoteType = NoteType.Tap;
            newNote.LaneIndex = 0;
            newNote.TimeToHit = 0f;
            newNote.VisualScale = 1.0f;
            newNote.NoteColor = Color.white;
            
            return newNote;
        }
        
        private void ExportToJSON()
        {
            string path = GetExportPath();
                
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var exportData = CreateExportData();
                    WriteJsonToFile(exportData, path);
                    ShowExportSuccessDialog(path);
                }
                catch (System.Exception e)
                {
                    HandleExportError(e);
                }
            }
        }
        
        /// <summary>
        /// エクスポートパスを取得
        /// </summary>
        private string GetExportPath()
        {
            return EditorUtility.SaveFilePanel(
                "Export Chart to JSON",
                Application.dataPath,
                _currentChart.SongName + ".json",
                "json");
        }
        
        /// <summary>
        /// エクスポートデータを作成
        /// </summary>
        private ChartExportData CreateExportData()
        {
            var exportData = new ChartExportData
            {
                songName = _currentChart.SongName,
                artist = _currentChart.Artist,
                bpm = _currentChart.Bpm,
                difficulty = _currentChart.Difficulty,
                difficultyName = _currentChart.DifficultyName,
                firstBeatOffset = _currentChart.FirstBeatOffset,
                chartAuthor = _currentChart.ChartAuthor,
                chartVersion = _currentChart.ChartVersion,
                notes = ConvertNotesToExportFormat()
            };
            
            return exportData;
        }
        
        /// <summary>
        /// ノーツをエクスポート形式に変換
        /// </summary>
        private List<NoteExportData> ConvertNotesToExportFormat()
        {
            var exportNotes = new List<NoteExportData>();
            
            foreach (var note in _currentChart.Notes)
            {
                var exportNote = new NoteExportData
                {
                    type = note.NoteType.ToString(),
                    lane = note.LaneIndex,
                    time = note.TimeToHit,
                    duration = note.HoldDuration,
                    scale = note.VisualScale,
                    color = ColorToHex(note.NoteColor),
                    score = note.BaseScore,
                    multiplier = note.ScoreMultiplier
                };
                exportNotes.Add(exportNote);
            }
            
            return exportNotes;
        }
        
        /// <summary>
        /// JSONをファイルに書き込み
        /// </summary>
        private void WriteJsonToFile(ChartExportData exportData, string path)
        {
            string json = JsonUtility.ToJson(exportData, true);
            System.IO.File.WriteAllText(path, json);
        }
        
        /// <summary>
        /// エクスポート成功ダイアログを表示
        /// </summary>
        private void ShowExportSuccessDialog(string path)
        {
            Debug.Log($"譜面をエクスポートしました: {path}");
            EditorUtility.DisplayDialog("成功", 
                $"JSONファイルをエクスポートしました\n{path}", "OK");
        }
        
        /// <summary>
        /// エクスポートエラーを処理
        /// </summary>
        private void HandleExportError(System.Exception e)
        {
            Debug.LogError($"エクスポート失敗: {e.Message}");
            EditorUtility.DisplayDialog("エラー", 
                $"エクスポートに失敗しました\n{e.Message}", "OK");
        }
        
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
        
        /// <summary>
        /// インポートパスを取得
        /// </summary>
        private string GetImportPath()
        {
            return EditorUtility.OpenFilePanel(
                "Import Chart from JSON",
                Application.dataPath,
                "json");
        }
        
        /// <summary>
        /// JSONファイルからデータを読み込み
        /// </summary>
        private ChartExportData LoadJsonFromFile(string path)
        {
            string json = System.IO.File.ReadAllText(path);
            var importData = JsonUtility.FromJson<ChartExportData>(json);
            
            if (importData == null)
            {
                throw new System.Exception("JSONデータの解析に失敗しました");
            }
            
            return importData;
        }
        
        /// <summary>
        /// インポートされたデータを適用
        /// </summary>
        private void ApplyImportedData(ChartExportData importData)
        {
            Undo.RecordObject(_currentChart, "Import Chart from JSON");
            
            ApplyChartMetadata(importData);
            ImportNotes(importData.notes);
            
            _currentChart.SortNotesByTime();
            EditorUtility.SetDirty(_currentChart);
        }
        
        /// <summary>
        /// 譜面メタデータを適用
        /// </summary>
        private void ApplyChartMetadata(ChartExportData importData)
        {
            // Note: ChartDataがプロパティでラップされているため、直接更新できない
            // プロパティにセッターがある場合はそれを使用し、ない場合はSerializationUtilityを使用
            // ここではシリアライズフィールドを直接更新する方法を使用する必要がある
            Debug.LogWarning("インポート機能は現在ノーツデータのみをサポートしています。譜面メタデータの更新はスキップされました。");
        }
        
        /// <summary>
        /// ノーツデータをインポート
        /// </summary>
        private void ImportNotes(List<NoteExportData> importNotes)
        {
            _currentChart.Notes.Clear();
            
            foreach (var importNote in importNotes)
            {
                var note = ConvertImportNoteToNoteData(importNote);
                _currentChart.Notes.Add(note);
            }
        }
        
        /// <summary>
        /// インポートノートをNoteDataに変換
        /// </summary>
        private NoteData ConvertImportNoteToNoteData(NoteExportData importNote)
        {
            var note = new NoteData();
            
            note.NoteType = (NoteType)System.Enum.Parse(typeof(NoteType), importNote.type);
            note.LaneIndex = importNote.lane;
            note.TimeToHit = importNote.time;
            note.HoldDuration = importNote.duration;
            note.VisualScale = importNote.scale;
            note.NoteColor = HexToColor(importNote.color);
            note.BaseScore = importNote.score;
            note.ScoreMultiplier = importNote.multiplier;
            
            return note;
        }
        
        /// <summary>
        /// インポート成功ダイアログを表示
        /// </summary>
        private void ShowImportSuccessDialog(string path, int noteCount)
        {
            Debug.Log($"譜面をインポートしました: {path}");
            EditorUtility.DisplayDialog("成功", 
                $"JSONファイルをインポートしました\n{path}\n{noteCount}個のノーツを読み込みました", "OK");
        }
        
        /// <summary>
        /// インポートエラーを処理
        /// </summary>
        private void HandleImportError(System.Exception e)
        {
            Debug.LogError($"インポート失敗: {e.Message}");
            EditorUtility.DisplayDialog("エラー", 
                $"インポートに失敗しました\n{e.Message}", "OK");
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