using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Jirou.Tests.EditMode
{
    /// <summary>
    /// ChartDataクラスのユニットテスト
    /// </summary>
    public class ChartDataTests
    {
        private Core.ChartData CreateTestChart()
        {
            var chart = ScriptableObject.CreateInstance<Core.ChartData>();
            chart.bpm = 120f;
            chart.songName = "Test Song";
            chart.artist = "Test Artist";
            chart.difficulty = 5;
            chart.difficultyName = "Normal";
            
            // テスト用ノーツを追加
            chart.notes.Add(new Core.NoteData { laneIndex = 0, timeToHit = 0f });
            chart.notes.Add(new Core.NoteData { laneIndex = 1, timeToHit = 1f });
            chart.notes.Add(new Core.NoteData { laneIndex = 2, timeToHit = 2f });
            chart.notes.Add(new Core.NoteData { laneIndex = 3, timeToHit = 3f });
            
            return chart;
        }
        
        [Test]
        public void ChartData_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var chart = ScriptableObject.CreateInstance<Core.ChartData>();
            
            // Assert
            Assert.AreEqual(120f, chart.bpm, "デフォルトBPMは120");
            Assert.AreEqual("無題", chart.songName, "デフォルト曲名は'無題'");
            Assert.AreEqual("不明", chart.artist, "デフォルトアーティスト名は'不明'");
            Assert.AreEqual(1, chart.difficulty, "デフォルト難易度は1");
            Assert.AreEqual("Normal", chart.difficultyName, "デフォルト難易度名は'Normal'");
            Assert.IsNotNull(chart.notes, "ノーツリストは初期化されているべき");
            Assert.AreEqual(0, chart.notes.Count, "初期状態ではノーツは0個");
        }
        
        [Test]
        public void ChartData_SortNotesByTime_SortsCorrectly()
        {
            // Arrange
            var chart = CreateTestChart();
            
            // 順序を意図的に乱す
            var temp = chart.notes[0];
            chart.notes[0] = chart.notes[3];
            chart.notes[3] = temp;
            
            // Act
            chart.SortNotesByTime();
            
            // Assert
            for (int i = 0; i < chart.notes.Count - 1; i++)
            {
                Assert.LessOrEqual(
                    chart.notes[i].timeToHit,
                    chart.notes[i + 1].timeToHit,
                    $"ノーツ[{i}]はノーツ[{i + 1}]より前にあるべき");
            }
        }
        
        [Test]
        public void ChartData_GetNotesInTimeRange_FiltersCorrectly()
        {
            // Arrange
            var chart = CreateTestChart();
            
            // Act
            var filtered = chart.GetNotesInTimeRange(1f, 2f);
            
            // Assert
            Assert.AreEqual(2, filtered.Count, "範囲内のノーツは2個");
            Assert.AreEqual(1f, filtered[0].timeToHit, "最初のノーツは1ビート");
            Assert.AreEqual(2f, filtered[1].timeToHit, "2番目のノーツは2ビート");
        }
        
        [Test]
        public void ChartData_GetNotesInTimeRange_HandlesEmptyRange()
        {
            // Arrange
            var chart = CreateTestChart();
            
            // Act
            var filtered = chart.GetNotesInTimeRange(10f, 20f);
            
            // Assert
            Assert.AreEqual(0, filtered.Count, "範囲外では0個を返すべき");
        }
        
        [Test]
        public void ChartData_GetNoteCountByLane_CountsCorrectly()
        {
            // Arrange
            var chart = CreateTestChart();
            chart.notes.Add(new Core.NoteData { laneIndex = 0, timeToHit = 4f });
            chart.notes.Add(new Core.NoteData { laneIndex = 0, timeToHit = 5f });
            
            // Act
            var counts = chart.GetNoteCountByLane();
            
            // Assert
            Assert.AreEqual(3, counts[0], "レーン0には3個のノーツ");
            Assert.AreEqual(1, counts[1], "レーン1には1個のノーツ");
            Assert.AreEqual(1, counts[2], "レーン2には1個のノーツ");
            Assert.AreEqual(1, counts[3], "レーン3には1個のノーツ");
        }
        
        [Test]
        public void ChartData_GetTotalNoteCount_ReturnsCorrectCount()
        {
            // Arrange
            var chart = CreateTestChart();
            
            // Act
            int count = chart.GetTotalNoteCount();
            
            // Assert
            Assert.AreEqual(4, count, "総ノーツ数は4個");
        }
        
        [Test]
        public void ChartData_GetHoldNoteCount_CountsCorrectly()
        {
            // Arrange
            var chart = CreateTestChart();
            chart.notes.Add(new Core.NoteData 
            { 
                noteType = Core.NoteType.Hold, 
                laneIndex = 1, 
                timeToHit = 4f,
                holdDuration = 2f
            });
            chart.notes.Add(new Core.NoteData 
            { 
                noteType = Core.NoteType.Hold, 
                laneIndex = 2, 
                timeToHit = 6f,
                holdDuration = 1f
            });
            
            // Act
            int holdCount = chart.GetHoldNoteCount();
            
            // Assert
            Assert.AreEqual(2, holdCount, "Holdノーツは2個");
        }
        
        [Test]
        public void ChartData_GetTapNoteCount_CountsCorrectly()
        {
            // Arrange
            var chart = CreateTestChart();  // デフォルトで4個のTapノーツ
            chart.notes.Add(new Core.NoteData 
            { 
                noteType = Core.NoteType.Hold, 
                laneIndex = 1, 
                timeToHit = 4f,
                holdDuration = 2f
            });
            
            // Act
            int tapCount = chart.GetTapNoteCount();
            
            // Assert
            Assert.AreEqual(4, tapCount, "Tapノーツは4個");
        }
        
        [Test]
        public void ChartData_GetChartLengthInBeats_CalculatesCorrectly()
        {
            // Arrange
            var chart = CreateTestChart();
            
            // Holdノーツを追加
            chart.notes.Add(new Core.NoteData
            {
                noteType = Core.NoteType.Hold,
                laneIndex = 0,
                timeToHit = 4f,
                holdDuration = 2f
            });
            
            // Act
            float length = chart.GetChartLengthInBeats();
            
            // Assert
            Assert.AreEqual(6f, length, "譜面長は6ビート（4 + 2）");
        }
        
        [Test]
        public void ChartData_GetChartLengthInBeats_HandlesEmptyChart()
        {
            // Arrange
            var chart = ScriptableObject.CreateInstance<Core.ChartData>();
            
            // Act
            float length = chart.GetChartLengthInBeats();
            
            // Assert
            Assert.AreEqual(0f, length, "空の譜面の長さは0");
        }
        
        [Test]
        public void ChartData_GetChartLengthInSeconds_CalculatesCorrectly()
        {
            // Arrange
            var chart = CreateTestChart();
            chart.bpm = 120f;  // 120 BPM = 2ビート/秒
            
            // Act
            float lengthInSeconds = chart.GetChartLengthInSeconds();
            
            // Assert
            Assert.AreEqual(1.5f, lengthInSeconds, 0.01f, "3ビート÷2ビート/秒 = 1.5秒");
        }
        
        [Test]
        public void ChartData_GetChartLengthInSeconds_HandlesZeroBPM()
        {
            // Arrange
            var chart = CreateTestChart();
            chart.bpm = 0f;
            
            // Act
            float length = chart.GetChartLengthInSeconds();
            
            // Assert
            Assert.AreEqual(0f, length, "BPM0の場合は0を返すべき");
        }
        
        [Test]
        public void ChartData_ValidateChart_AcceptsValidChart()
        {
            // Arrange
            var chart = CreateTestChart();
            chart.songClip = AudioClip.Create("test", 44100, 1, 44100, false);
            
            // Act
            List<string> errors;
            bool isValid = chart.ValidateChart(out errors);
            
            // Assert
            Assert.IsTrue(isValid, "有効な譜面はtrueを返すべき");
            Assert.AreEqual(0, errors.Count, "エラーリストは空であるべき");
        }
        
        [Test]
        public void ChartData_ValidateChart_DetectsInvalidBPM()
        {
            // Arrange
            var chart = CreateTestChart();
            
            // Act & Assert - 負のBPM
            chart.bpm = -1f;
            List<string> errors;
            bool isValid = chart.ValidateChart(out errors);
            
            Assert.IsFalse(isValid, "負のBPMは無効");
            Assert.IsTrue(errors.Any(e => e.Contains("BPM")), "エラーにBPMが含まれるべき");
            
            // Act & Assert - 極端に大きいBPM
            chart.bpm = 1000f;
            isValid = chart.ValidateChart(out errors);
            
            Assert.IsFalse(isValid, "BPM1000は無効");
            Assert.IsTrue(errors.Any(e => e.Contains("BPM")), "エラーにBPMが含まれるべき");
        }
        
        [Test]
        public void ChartData_ValidateChart_DetectsMissingSongClip()
        {
            // Arrange
            var chart = CreateTestChart();
            chart.songClip = null;
            
            // Act
            List<string> errors;
            bool isValid = chart.ValidateChart(out errors);
            
            // Assert
            Assert.IsFalse(isValid, "楽曲ファイルなしは無効");
            Assert.IsTrue(errors.Any(e => e.Contains("楽曲ファイル")), "エラーに楽曲ファイルが含まれるべき");
        }
        
        [Test]
        public void ChartData_ValidateChart_DetectsEmptySongName()
        {
            // Arrange
            var chart = CreateTestChart();
            chart.songName = "";
            
            // Act
            List<string> errors;
            bool isValid = chart.ValidateChart(out errors);
            
            // Assert
            Assert.IsFalse(isValid, "空の曲名は無効");
            Assert.IsTrue(errors.Any(e => e.Contains("曲名")), "エラーに曲名が含まれるべき");
        }
        
        [Test]
        public void ChartData_ValidateChart_DetectsDuplicateNotes()
        {
            // Arrange
            var chart = CreateTestChart();
            
            // 同じレーン、同じタイミングのノーツを追加
            chart.notes.Add(new Core.NoteData { laneIndex = 0, timeToHit = 0f });
            
            // Act
            List<string> errors;
            bool isValid = chart.ValidateChart(out errors);
            
            // Assert
            Assert.IsFalse(isValid, "重複ノーツは無効");
            Assert.IsTrue(errors.Any(e => e.Contains("重複")), "エラーに重複が含まれるべき");
        }
        
        [Test]
        public void ChartData_GetStatistics_CalculatesCorrectly()
        {
            // Arrange
            var chart = CreateTestChart();
            chart.notes.Add(new Core.NoteData 
            { 
                noteType = Core.NoteType.Hold,
                laneIndex = 0,
                timeToHit = 4f,
                holdDuration = 2f
            });
            
            // Act
            var stats = chart.GetStatistics();
            
            // Assert
            Assert.AreEqual(5, stats.totalNotes, "総ノーツ数は5");
            Assert.AreEqual(4, stats.tapNotes, "Tapノーツは4");
            Assert.AreEqual(1, stats.holdNotes, "Holdノーツは1");
            Assert.AreEqual(6f, stats.chartLengthBeats, "譜面長は6ビート");
            Assert.Greater(stats.averageNPS, 0, "平均NPSは正の値");
            Assert.Greater(stats.averageInterval, 0, "平均間隔は正の値");
        }
        
        [Test]
        public void ChartData_GetStatistics_HandlesEmptyChart()
        {
            // Arrange
            var chart = ScriptableObject.CreateInstance<Core.ChartData>();
            
            // Act
            var stats = chart.GetStatistics();
            
            // Assert
            Assert.AreEqual(0, stats.totalNotes, "空の譜面では0ノーツ");
            Assert.AreEqual(0, stats.tapNotes, "Tapノーツは0");
            Assert.AreEqual(0, stats.holdNotes, "Holdノーツは0");
            Assert.AreEqual(0f, stats.chartLengthBeats, "譜面長は0");
            Assert.AreEqual(0f, stats.averageNPS, "平均NPSは0");
            Assert.AreEqual(0f, stats.averageInterval, "平均間隔は0");
        }
    }
}