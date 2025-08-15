using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Jirou.Core
{
    /// <summary>
    /// 譜面データを管理するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewChart", menuName = "Jirou/Chart Data", order = 1)]
    public class ChartData : ScriptableObject
    {
        [Header("楽曲情報")]
        [Tooltip("楽曲ファイル")]
        public AudioClip songClip;
        
        [Tooltip("BPM（Beats Per Minute）")]
        [Range(60f, 300f)]
        public float bpm = 120f;
        
        [Tooltip("曲名")]
        public string songName = "無題";
        
        [Tooltip("アーティスト名")]
        public string artist = "不明";
        
        [Tooltip("プレビュー開始時間（秒）")]
        [Min(0f)]
        public float previewTime = 0f;
        
        [Tooltip("最初のビートまでのオフセット（秒）")]
        public float firstBeatOffset = 0f;
        
        [Header("難易度情報")]
        [Tooltip("難易度レベル（1-10）")]
        [Range(1, 10)]
        public int difficulty = 1;
        
        [Tooltip("難易度名")]
        public string difficultyName = "Normal";
        
        [Header("譜面データ")]
        [Tooltip("ノーツリスト")]
        public List<NoteData> notes = new List<NoteData>();
        
        [Header("メタデータ")]
        [Tooltip("譜面作成者")]
        public string chartAuthor = "";
        
        [Tooltip("譜面バージョン")]
        public string chartVersion = "1.0";
        
        [Tooltip("作成日時")]
        public string createdDate = "";
        
        [Tooltip("最終更新日時")]
        public string lastModified = "";
        
        /// <summary>
        /// ノーツをタイミング順にソート
        /// </summary>
        public void SortNotesByTime()
        {
            notes.Sort((a, b) => a.timeToHit.CompareTo(b.timeToHit));
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
            
            Debug.Log($"[ChartData] {notes.Count}個のノーツをソートしました");
        }
        
        /// <summary>
        /// 指定範囲のノーツを取得
        /// </summary>
        public List<NoteData> GetNotesInTimeRange(float startBeat, float endBeat)
        {
            return notes.FindAll(n => 
                n.timeToHit >= startBeat && 
                n.timeToHit <= endBeat);
        }
        
        /// <summary>
        /// レーン別のノーツ数を取得
        /// </summary>
        public int[] GetNoteCountByLane()
        {
            int[] counts = new int[4];
            foreach (var note in notes)
            {
                if (note.laneIndex >= 0 && note.laneIndex < 4)
                {
                    counts[note.laneIndex]++;
                }
            }
            return counts;
        }
        
        /// <summary>
        /// 総ノーツ数を取得
        /// </summary>
        public int GetTotalNoteCount()
        {
            return notes.Count;
        }
        
        /// <summary>
        /// Holdノーツの数を取得
        /// </summary>
        public int GetHoldNoteCount()
        {
            return notes.Count(n => n.noteType == NoteType.Hold);
        }
        
        /// <summary>
        /// Tapノーツの数を取得
        /// </summary>
        public int GetTapNoteCount()
        {
            return notes.Count(n => n.noteType == NoteType.Tap);
        }
        
        /// <summary>
        /// 譜面の長さ（ビート）を取得
        /// </summary>
        public float GetChartLengthInBeats()
        {
            if (notes.Count == 0) return 0f;
            
            float lastBeat = notes.Max(n => n.GetEndTime());
            return lastBeat;
        }
        
        /// <summary>
        /// 譜面の長さ（秒）を取得
        /// </summary>
        public float GetChartLengthInSeconds()
        {
            if (bpm <= 0) return 0f;
            return GetChartLengthInBeats() * (60f / bpm);
        }
        
        /// <summary>
        /// 楽曲の長さ（秒）を取得
        /// </summary>
        public float GetSongLengthInSeconds()
        {
            if (songClip == null) return 0f;
            return songClip.length;
        }
        
        /// <summary>
        /// 譜面データの妥当性をチェック
        /// </summary>
        public bool ValidateChart(out List<string> errors)
        {
            errors = new List<string>();
            bool isValid = true;
            
            // BPMチェック
            if (bpm <= 0 || bpm > 999)
            {
                errors.Add($"不正なBPM値: {bpm}");
                isValid = false;
            }
            
            // 楽曲ファイルチェック
            if (songClip == null)
            {
                errors.Add("楽曲ファイルが設定されていません");
                isValid = false;
            }
            
            // 曲名チェック
            if (string.IsNullOrEmpty(songName))
            {
                errors.Add("曲名が設定されていません");
                isValid = false;
            }
            
            // ノーツデータチェック
            HashSet<string> duplicateCheck = new HashSet<string>();
            
            for (int i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                string noteError;
                
                // 個別ノーツのバリデーション
                if (!note.Validate(out noteError))
                {
                    errors.Add($"ノーツ[{i}]: {noteError}");
                    isValid = false;
                }
                
                // 重複チェック（同じタイミング、同じレーン）
                string key = $"{note.laneIndex}_{note.timeToHit:F3}";
                if (duplicateCheck.Contains(key))
                {
                    errors.Add($"ノーツ[{i}]: 重複ノーツ（レーン{note.laneIndex}, タイミング{note.timeToHit:F2}）");
                    isValid = false;
                }
                duplicateCheck.Add(key);
            }
            
            // 譜面長チェック
            float chartLength = GetChartLengthInSeconds();
            float songLength = GetSongLengthInSeconds();
            
            if (songLength > 0 && chartLength > songLength + 5f)
            {
                errors.Add($"譜面が楽曲より長すぎます（譜面: {chartLength:F1}秒, 楽曲: {songLength:F1}秒）");
                isValid = false;
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 譜面の統計情報を取得
        /// </summary>
        public ChartStatistics GetStatistics()
        {
            var stats = new ChartStatistics();
            
            stats.totalNotes = GetTotalNoteCount();
            stats.tapNotes = GetTapNoteCount();
            stats.holdNotes = GetHoldNoteCount();
            stats.notesByLane = GetNoteCountByLane();
            stats.chartLengthBeats = GetChartLengthInBeats();
            stats.chartLengthSeconds = GetChartLengthInSeconds();
            stats.averageNPS = stats.chartLengthSeconds > 0 ? stats.totalNotes / stats.chartLengthSeconds : 0f;
            
            // 密度計算
            if (notes.Count > 1)
            {
                float totalInterval = 0f;
                for (int i = 1; i < notes.Count; i++)
                {
                    totalInterval += Mathf.Abs(notes[i].timeToHit - notes[i - 1].timeToHit);
                }
                stats.averageInterval = totalInterval / (notes.Count - 1);
            }
            
            return stats;
        }
        
        /// <summary>
        /// デバッグ情報を出力
        /// </summary>
        public void PrintDebugInfo()
        {
            Debug.Log("=== Chart Debug Info ===");
            Debug.Log($"曲名: {songName}");
            Debug.Log($"アーティスト: {artist}");
            Debug.Log($"BPM: {bpm}");
            Debug.Log($"難易度: {difficultyName} (Lv.{difficulty})");
            
            var stats = GetStatistics();
            Debug.Log($"総ノーツ数: {stats.totalNotes}");
            Debug.Log($"Tap: {stats.tapNotes}, Hold: {stats.holdNotes}");
            Debug.Log($"レーン分布: [{string.Join(", ", stats.notesByLane)}]");
            Debug.Log($"譜面長: {stats.chartLengthSeconds:F1}秒 ({stats.chartLengthBeats:F1}ビート)");
            Debug.Log($"平均NPS: {stats.averageNPS:F2}");
            Debug.Log("========================");
        }
    }
    
    /// <summary>
    /// 譜面統計情報
    /// </summary>
    [Serializable]
    public class ChartStatistics
    {
        public int totalNotes;
        public int tapNotes;
        public int holdNotes;
        public int[] notesByLane;
        public float chartLengthBeats;
        public float chartLengthSeconds;
        public float averageNPS;  // Notes Per Second
        public float averageInterval;  // ビート単位
    }
}