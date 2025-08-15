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
        private ChartData chartData;
        private bool showStatistics = true;
        private bool showValidation = false;
        private List<string> validationErrors = new List<string>();
        
        void OnEnable()
        {
            chartData = (ChartData)target;
        }
        
        public override void OnInspectorGUI()
        {
            // デフォルトインスペクターを描画
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            // ツールボタン
            DrawToolButtons();
            
            EditorGUILayout.Space();
            
            // 統計情報
            showStatistics = EditorGUILayout.Foldout(showStatistics, "統計情報", true);
            if (showStatistics)
            {
                DrawStatistics();
            }
            
            // バリデーション結果
            showValidation = EditorGUILayout.Foldout(showValidation, "バリデーション", true);
            if (showValidation)
            {
                DrawValidation();
            }
        }
        
        private void DrawToolButtons()
        {
            EditorGUILayout.LabelField("ツール", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("ノーツをソート"))
            {
                Undo.RecordObject(chartData, "Sort Notes");
                chartData.SortNotesByTime();
                EditorUtility.SetDirty(chartData);
            }
            
            if (GUILayout.Button("譜面を検証"))
            {
                bool isValid = chartData.ValidateChart(out validationErrors);
                showValidation = true;
                
                if (isValid)
                {
                    EditorUtility.DisplayDialog("検証結果", "譜面データは正常です", "OK");
                }
            }
            
            if (GUILayout.Button("デバッグ情報出力"))
            {
                chartData.PrintDebugInfo();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("すべてのノーツをクリア"))
            {
                if (EditorUtility.DisplayDialog("確認", 
                    "すべてのノーツを削除しますか？", "削除", "キャンセル"))
                {
                    Undo.RecordObject(chartData, "Clear All Notes");
                    chartData.notes.Clear();
                    EditorUtility.SetDirty(chartData);
                }
            }
            
            if (GUILayout.Button("テストノーツを追加"))
            {
                AddTestNotes();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStatistics()
        {
            EditorGUI.indentLevel++;
            
            var stats = chartData.GetStatistics();
            
            EditorGUILayout.LabelField("総ノーツ数:", stats.totalNotes.ToString());
            EditorGUILayout.LabelField("Tapノーツ:", stats.tapNotes.ToString());
            EditorGUILayout.LabelField("Holdノーツ:", stats.holdNotes.ToString());
            
            EditorGUILayout.LabelField("レーン分布:");
            EditorGUI.indentLevel++;
            for (int i = 0; i < 4; i++)
            {
                float percentage = stats.totalNotes > 0 ? 
                    (float)stats.notesByLane[i] / stats.totalNotes * 100f : 0f;
                    
                EditorGUILayout.LabelField(
                    $"レーン {i} ({NoteData.LaneKeys[i]}):", 
                    $"{stats.notesByLane[i]} ({percentage:F1}%)");
            }
            EditorGUI.indentLevel--;
            
            EditorGUILayout.LabelField("譜面長:", 
                $"{stats.chartLengthSeconds:F1}秒 ({stats.chartLengthBeats:F1}ビート)");
            EditorGUILayout.LabelField("平均NPS:", $"{stats.averageNPS:F2}");
            
            if (stats.averageInterval > 0)
            {
                EditorGUILayout.LabelField("平均間隔:", $"{stats.averageInterval:F3}ビート");
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawValidation()
        {
            EditorGUI.indentLevel++;
            
            if (validationErrors.Count == 0)
            {
                EditorGUILayout.HelpBox("エラーはありません", MessageType.Info);
            }
            else
            {
                foreach (var error in validationErrors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void AddTestNotes()
        {
            Undo.RecordObject(chartData, "Add Test Notes");
            
            // 4ビートごとに各レーンにノーツを配置
            for (int beat = 0; beat < 16; beat += 4)
            {
                for (int lane = 0; lane < 4; lane++)
                {
                    var note = new NoteData
                    {
                        noteType = (beat % 8 == 0 && lane % 2 == 0) ? 
                            NoteType.Hold : NoteType.Tap,
                        laneIndex = lane,
                        timeToHit = beat + lane * 0.5f,
                        holdDuration = 2.0f,
                        visualScale = 1.0f,
                        noteColor = Color.white
                    };
                    
                    chartData.notes.Add(note);
                }
            }
            
            chartData.SortNotesByTime();
            EditorUtility.SetDirty(chartData);
            
            Debug.Log($"テストノーツを{16}個追加しました");
        }
    }
}
#endif