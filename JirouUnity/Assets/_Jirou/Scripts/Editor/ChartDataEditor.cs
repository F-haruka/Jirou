#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Jirou.Core;

namespace Jirou.Editor
{
    /// <summary>
    /// ChartDataのカスタムエディタ
    /// </summary>
    [CustomEditor(typeof(ChartData))]
    public class ChartDataEditor : UnityEditor.Editor
    {
        private ChartData _chartData;
        private bool _showStatistics = true;
        private bool _showValidation = false;
        private List<string> _validationErrors = new List<string>();
        
        void OnEnable()
        {
            _chartData = (ChartData)target;
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            DrawToolButtons();
            
            EditorGUILayout.Space();
            DrawStatisticsSection();
            
            DrawValidationSection();
        }
        
        /// <summary>
        /// 統計情報セクションを描画
        /// </summary>
        private void DrawStatisticsSection()
        {
            _showStatistics = EditorGUILayout.Foldout(_showStatistics, "統計情報", true);
            if (_showStatistics)
            {
                DrawStatistics();
            }
        }
        
        /// <summary>
        /// バリデーションセクションを描画
        /// </summary>
        private void DrawValidationSection()
        {
            _showValidation = EditorGUILayout.Foldout(_showValidation, "バリデーション", true);
            if (_showValidation)
            {
                DrawValidation();
            }
        }
        
        private void DrawToolButtons()
        {
            EditorGUILayout.LabelField("ツール", EditorStyles.boldLabel);
            
            DrawFirstRowButtons();
            DrawSecondRowButtons();
        }
        
        /// <summary>
        /// ツールボタンの1行目を描画
        /// </summary>
        private void DrawFirstRowButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("ノーツをソート"))
            {
                SortNotes();
            }
            
            if (GUILayout.Button("譜面を検証"))
            {
                ValidateChart();
            }
            
            if (GUILayout.Button("デバッグ情報出力"))
            {
                _chartData.PrintDebugInfo();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// ツールボタンの2行目を描画
        /// </summary>
        private void DrawSecondRowButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("すべてのノーツをクリア"))
            {
                ClearAllNotes();
            }
            
            if (GUILayout.Button("テストノーツを追加"))
            {
                AddTestNotes();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// ノーツをソート
        /// </summary>
        private void SortNotes()
        {
            Undo.RecordObject(_chartData, "Sort Notes");
            _chartData.SortNotesByTime();
            EditorUtility.SetDirty(_chartData);
        }
        
        /// <summary>
        /// 譜面を検証
        /// </summary>
        private void ValidateChart()
        {
            bool isValid = _chartData.ValidateChart(out _validationErrors);
            _showValidation = true;
            
            if (isValid)
            {
                EditorUtility.DisplayDialog("検証結果", "譜面データは正常です", "OK");
            }
        }
        
        /// <summary>
        /// すべてのノーツをクリア
        /// </summary>
        private void ClearAllNotes()
        {
            if (EditorUtility.DisplayDialog("確認", 
                "すべてのノーツを削除しますか？", "削除", "キャンセル"))
            {
                Undo.RecordObject(_chartData, "Clear All Notes");
                _chartData.Notes.Clear();
                EditorUtility.SetDirty(_chartData);
            }
        }
        
        private void DrawStatistics()
        {
            EditorGUI.indentLevel++;
            
            var stats = _chartData.GetStatistics();
            
            DrawBasicStats(stats);
            DrawLaneDistribution(stats);
            DrawTimingStats(stats);
            
            EditorGUI.indentLevel--;
        }
        
        /// <summary>
        /// 基本統計を描画
        /// </summary>
        private void DrawBasicStats(ChartStatistics stats)
        {
            EditorGUILayout.LabelField("総ノーツ数:", stats.totalNotes.ToString());
            EditorGUILayout.LabelField("Tapノーツ:", stats.tapNotes.ToString());
            EditorGUILayout.LabelField("Holdノーツ:", stats.holdNotes.ToString());
        }
        
        /// <summary>
        /// レーン分布を描画
        /// </summary>
        private void DrawLaneDistribution(ChartStatistics stats)
        {
            EditorGUILayout.LabelField("レーン分布:");
            EditorGUI.indentLevel++;
            
            for (int i = 0; i < 4; i++)
            {
                float percentage = CalculateLanePercentage(stats.notesByLane[i], stats.totalNotes);
                EditorGUILayout.LabelField(
                    $"レーン {i} ({NoteData.LaneKeys[i]}):", 
                    $"{stats.notesByLane[i]} ({percentage:F1}%)");
            }
            
            EditorGUI.indentLevel--;
        }
        
        /// <summary>
        /// レーンのパーセンテージを計算
        /// </summary>
        private float CalculateLanePercentage(int laneCount, int totalNotes)
        {
            return totalNotes > 0 ? (float)laneCount / totalNotes * 100f : 0f;
        }
        
        /// <summary>
        /// タイミング統計を描画
        /// </summary>
        private void DrawTimingStats(ChartStatistics stats)
        {
            EditorGUILayout.LabelField("譜面長:", 
                $"{stats.chartLengthSeconds:F1}秒 ({stats.chartLengthBeats:F1}ビート)");
            EditorGUILayout.LabelField("平均NPS:", $"{stats.averageNPS:F2}");
            
            if (stats.averageInterval > 0)
            {
                EditorGUILayout.LabelField("平均間隔:", $"{stats.averageInterval:F3}ビート");
            }
        }
        
        private void DrawValidation()
        {
            EditorGUI.indentLevel++;
            
            if (_validationErrors.Count == 0)
            {
                EditorGUILayout.HelpBox("エラーはありません", MessageType.Info);
            }
            else
            {
                foreach (var error in _validationErrors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void AddTestNotes()
        {
            Undo.RecordObject(_chartData, "Add Test Notes");
            
            int notesAdded = 0;
            
            // 4ビートごとに各レーンにノーツを配置
            for (int beat = 0; beat < 16; beat += 4)
            {
                for (int lane = 0; lane < 4; lane++)
                {
                    var note = CreateTestNote(beat, lane);
                    _chartData.Notes.Add(note);
                    notesAdded++;
                }
            }
            
            _chartData.SortNotesByTime();
            EditorUtility.SetDirty(_chartData);
            
            Debug.Log($"テストノーツを{notesAdded}個追加しました");
        }
        
        /// <summary>
        /// テストノーツを作成
        /// </summary>
        private NoteData CreateTestNote(int beat, int lane)
        {
            var note = new NoteData();
            
            note.NoteType = DetermineNoteType(beat, lane);
            note.LaneIndex = lane;
            note.TimeToHit = beat + lane * 0.5f;
            note.HoldDuration = 2.0f;
            note.VisualScale = 1.0f;
            note.NoteColor = Color.white;
            
            return note;
        }
        
        /// <summary>
        /// ノーツタイプを決定
        /// </summary>
        private NoteType DetermineNoteType(int beat, int lane)
        {
            return (beat % 8 == 0 && lane % 2 == 0) ? NoteType.Hold : NoteType.Tap;
        }
    }
}
#endif