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
        private System.Action<ChartData> onImportCallback;
        
        public static void ShowDialog(string json, System.Action<ChartData> onImport)
        {
            var window = GetWindow<ImportPreviewDialog>("譜面インポート プレビュー");
            window.minSize = new Vector2(500, 400);
            window.jsonContent = json;
            window.onImportCallback = onImport;
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
                if (onImportCallback != null)
                {
                    onImportCallback(previewData);
                }
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