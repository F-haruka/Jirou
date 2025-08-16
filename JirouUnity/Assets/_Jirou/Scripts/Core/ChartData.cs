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
        [SerializeField] private AudioClip _songClip;
        
        [Tooltip("BPM（Beats Per Minute）")]
        [Range(60f, 300f)]
        [SerializeField] private float _bpm = 120f;
        
        [Tooltip("曲名")]
        [SerializeField] private string _songName = "無題";
        
        [Tooltip("アーティスト名")]
        [SerializeField] private string _artist = "不明";
        
        [Tooltip("プレビュー開始時間（秒）")]
        [Min(0f)]
        [SerializeField] private float _previewTime = 0f;
        
        [Tooltip("最初のビートまでのオフセット（秒）")]
        [SerializeField] private float _firstBeatOffset = 0f;
        
        [Header("難易度情報")]
        [Tooltip("難易度レベル（1-10）")]
        [Range(1, 10)]
        [SerializeField] private int _difficulty = 1;
        
        [Tooltip("難易度名")]
        [SerializeField] private string _difficultyName = "Normal";
        
        [Header("譜面データ")]
        [Tooltip("ノーツリスト")]
        [SerializeField] private List<NoteData> _notes = new List<NoteData>();
        
        [Header("メタデータ")]
        [Tooltip("譜面作成者")]
        [SerializeField] private string _chartAuthor = "";
        
        [Tooltip("譜面バージョン")]
        [SerializeField] private string _chartVersion = "1.0";
        
        [Tooltip("作成日時")]
        [SerializeField] private string _createdDate = "";
        
        [Tooltip("最終更新日時")]
        [SerializeField] private string _lastModified = "";
        
        // プロパティ
        public AudioClip SongClip => _songClip;
        public float Bpm => _bpm;
        public string SongName => _songName;
        public string Artist => _artist;
        public float PreviewTime => _previewTime;
        public float FirstBeatOffset => _firstBeatOffset;
        public int Difficulty => _difficulty;
        public string DifficultyName => _difficultyName;
        public List<NoteData> Notes => _notes;
        public string ChartAuthor => _chartAuthor;
        public string ChartVersion => _chartVersion;
        public string CreatedDate => _createdDate;
        public string LastModified => _lastModified;
        
        /// <summary>
        /// ノーツをタイミング順にソート
        /// </summary>
        public void SortNotesByTime()
        {
            _notes.Sort((a, b) => a.TimeToHit.CompareTo(b.TimeToHit));
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
            
            Debug.Log($"[ChartData] {_notes.Count}個のノーツをソートしました");
        }
        
        /// <summary>
        /// 指定範囲のノーツを取得
        /// </summary>
        public List<NoteData> GetNotesInTimeRange(float startBeat, float endBeat)
        {
            return _notes.FindAll(n => 
                n.TimeToHit >= startBeat && 
                n.TimeToHit <= endBeat);
        }
        
        /// <summary>
        /// レーン別のノーツ数を取得
        /// </summary>
        public int[] GetNoteCountByLane()
        {
            int[] counts = new int[4];
            foreach (var note in _notes)
            {
                if (note.LaneIndex >= 0 && note.LaneIndex < 4)
                {
                    counts[note.LaneIndex]++;
                }
            }
            return counts;
        }
        
        /// <summary>
        /// 総ノーツ数を取得
        /// </summary>
        public int GetTotalNoteCount()
        {
            return _notes.Count;
        }
        
        /// <summary>
        /// Holdノーツの数を取得
        /// </summary>
        public int GetHoldNoteCount()
        {
            return _notes.Count(n => n.NoteType == NoteType.Hold);
        }
        
        /// <summary>
        /// Tapノーツの数を取得
        /// </summary>
        public int GetTapNoteCount()
        {
            return _notes.Count(n => n.NoteType == NoteType.Tap);
        }
        
        /// <summary>
        /// 譜面の長さ（ビート）を取得
        /// </summary>
        public float GetChartLengthInBeats()
        {
            if (_notes.Count == 0) return 0f;
            
            float lastBeat = _notes.Max(n => n.GetEndTime());
            return lastBeat;
        }
        
        /// <summary>
        /// 譜面の長さ（秒）を取得
        /// </summary>
        public float GetChartLengthInSeconds()
        {
            if (_bpm <= 0) return 0f;
            return GetChartLengthInBeats() * (60f / _bpm);
        }
        
        /// <summary>
        /// 楽曲の長さ（秒）を取得
        /// </summary>
        public float GetSongLengthInSeconds()
        {
            if (_songClip == null) return 0f;
            return _songClip.length;
        }
        
        /// <summary>
        /// 譜面データの妥当性をチェック
        /// </summary>
        public bool ValidateChart(out List<string> errors)
        {
            errors = new List<string>();
            bool isValid = true;
            
            // 基本データのバリデーション
            isValid &= ValidateBasicData(errors);
            
            // ノーツデータのバリデーション
            isValid &= ValidateNoteData(errors);
            
            // 譜面長のバリデーション
            isValid &= ValidateChartLength(errors);
            
            return isValid;
        }
        
        /// <summary>
        /// 基本データのバリデーション
        /// </summary>
        private bool ValidateBasicData(List<string> errors)
        {
            bool isValid = true;
            
            // BPMチェック
            if (_bpm <= 0 || _bpm > 999)
            {
                errors.Add($"不正なBPM値: {_bpm}");
                isValid = false;
            }
            
            // 楽曲ファイルチェック
            if (_songClip == null)
            {
                errors.Add("楽曲ファイルが設定されていません");
                isValid = false;
            }
            
            // 曲名チェック
            if (string.IsNullOrEmpty(_songName))
            {
                errors.Add("曲名が設定されていません");
                isValid = false;
            }
            
            return isValid;
        }
        
        /// <summary>
        /// ノーツデータのバリデーション
        /// </summary>
        private bool ValidateNoteData(List<string> errors)
        {
            bool isValid = true;
            HashSet<string> duplicateCheck = new HashSet<string>();
            
            for (int i = 0; i < _notes.Count; i++)
            {
                var note = _notes[i];
                string noteError;
                
                // 個別ノーツのバリデーション
                if (!note.Validate(out noteError))
                {
                    errors.Add($"ノーツ[{i}]: {noteError}");
                    isValid = false;
                }
                
                // 重複チェック（同じタイミング、同じレーン）
                string key = $"{note.LaneIndex}_{note.TimeToHit:F3}";
                if (duplicateCheck.Contains(key))
                {
                    errors.Add($"ノーツ[{i}]: 重複ノーツ（レーン{note.LaneIndex}, タイミング{note.TimeToHit:F2}）");
                    isValid = false;
                }
                duplicateCheck.Add(key);
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 譜面長のバリデーション
        /// </summary>
        private bool ValidateChartLength(List<string> errors)
        {
            bool isValid = true;
            
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
            stats.averageNPS = CalculateAverageNPS(stats.totalNotes, stats.chartLengthSeconds);
            stats.averageInterval = CalculateAverageInterval();
            
            return stats;
        }
        
        /// <summary>
        /// 平均NPSを計算
        /// </summary>
        private float CalculateAverageNPS(int totalNotes, float chartLengthSeconds)
        {
            return chartLengthSeconds > 0 ? totalNotes / chartLengthSeconds : 0f;
        }
        
        /// <summary>
        /// 平均間隔を計算
        /// </summary>
        private float CalculateAverageInterval()
        {
            if (_notes.Count <= 1) return 0f;
            
            float totalInterval = 0f;
            for (int i = 1; i < _notes.Count; i++)
            {
                totalInterval += Mathf.Abs(_notes[i].TimeToHit - _notes[i - 1].TimeToHit);
            }
            return totalInterval / (_notes.Count - 1);
        }
        
        /// <summary>
        /// デバッグ情報を出力
        /// </summary>
        public void PrintDebugInfo()
        {
            Debug.Log("=== Chart Debug Info ===");
            Debug.Log($"曲名: {_songName}");
            Debug.Log($"アーティスト: {_artist}");
            Debug.Log($"BPM: {_bpm}");
            Debug.Log($"難易度: {_difficultyName} (Lv.{_difficulty})");
            
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