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
                
                // ノーツデータの変換と追加（SerializedObjectを渡す）
                ConvertAndAddNotes(so, data.notes);
                
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
        
        private void ConvertAndAddNotes(SerializedObject so, List<NotesEditorNote> notesEditorNotes)
        {
            // _notesフィールドを取得
            var notesProperty = so.FindProperty("_notes");
            
            // 既存のノーツをクリア
            notesProperty.ClearArray();
            
            // 新しいノーツを追加
            int noteIndex = 0;
            foreach (var notesEditorNote in notesEditorNotes)
            {
                var noteData = ConvertNote(notesEditorNote);
                if (noteData != null)
                {
                    notesProperty.InsertArrayElementAtIndex(noteIndex);
                    var noteProperty = notesProperty.GetArrayElementAtIndex(noteIndex);
                    
                    // NoteDataの各フィールドを設定
                    noteProperty.FindPropertyRelative("_laneIndex").intValue = noteData.LaneIndex;
                    noteProperty.FindPropertyRelative("_noteType").enumValueIndex = (int)noteData.NoteType;
                    noteProperty.FindPropertyRelative("_timeToHit").floatValue = noteData.TimeToHit;
                    noteProperty.FindPropertyRelative("_holdDuration").floatValue = noteData.HoldDuration;
                    noteProperty.FindPropertyRelative("_visualScale").floatValue = noteData.VisualScale;
                    noteProperty.FindPropertyRelative("_noteColor").colorValue = noteData.NoteColor;
                    noteProperty.FindPropertyRelative("_baseScore").intValue = noteData.BaseScore;
                    noteProperty.FindPropertyRelative("_scoreMultiplier").floatValue = noteData.ScoreMultiplier;
                    
                    noteIndex++;
                }
            }
            
            Debug.Log($"インポート完了: {noteIndex}個のノーツを変換しました");
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