using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Jirou.Core;
using Jirou.Editor;
using System.Collections.Generic;

namespace Jirou.Tests.EditMode
{
    /// <summary>
    /// ChartDataEditorクラスのユニットテスト
    /// </summary>
    [TestFixture]
    public class ChartDataEditorTests
    {
        private ChartData testChartData;
        private SerializedObject serializedObject;
        private ChartDataEditor chartDataEditor;

        [SetUp]
        public void SetUp()
        {
            // テスト用のChartDataを作成
            testChartData = ScriptableObject.CreateInstance<ChartData>();
            testChartData.songName = "TestSong";
            testChartData.bpm = 120f;
            testChartData.difficulty = 2;
            testChartData.notes = new List<NoteData>();

            // SerializedObjectを作成
            serializedObject = new SerializedObject(testChartData);

            // ChartDataEditorのインスタンスを作成
            chartDataEditor = (ChartDataEditor)UnityEditor.Editor.CreateEditor(testChartData, typeof(ChartDataEditor));
        }

        [TearDown]
        public void TearDown()
        {
            if (chartDataEditor != null)
                Object.DestroyImmediate(chartDataEditor);
            if (testChartData != null)
                Object.DestroyImmediate(testChartData);
        }

        [Test]
        public void ChartData_SortNotesByTime_SortsCorrectly()
        {
            // Arrange
            testChartData.notes.Add(new NoteData { timeToHit = 3.0f, laneIndex = 0 });
            testChartData.notes.Add(new NoteData { timeToHit = 1.0f, laneIndex = 1 });
            testChartData.notes.Add(new NoteData { timeToHit = 2.0f, laneIndex = 2 });

            // Act
            testChartData.SortNotesByTime();

            // Assert
            Assert.AreEqual(1.0f, testChartData.notes[0].timeToHit);
            Assert.AreEqual(2.0f, testChartData.notes[1].timeToHit);
            Assert.AreEqual(3.0f, testChartData.notes[2].timeToHit);
        }

        [Test]
        public void ChartData_ValidateChart_EmptyChart_ReturnsTrue()
        {
            // Arrange
            testChartData.notes.Clear();

            // Act
            bool isValid = testChartData.ValidateChart(out List<string> errors);

            // Assert
            Assert.IsTrue(isValid);
            Assert.AreEqual(0, errors.Count);
        }

        [Test]
        public void ChartData_ValidateChart_InvalidLaneIndex_ReturnsFalse()
        {
            // Arrange
            testChartData.notes.Add(new NoteData { timeToHit = 1.0f, laneIndex = -1 });
            testChartData.notes.Add(new NoteData { timeToHit = 2.0f, laneIndex = 4 });

            // Act
            bool isValid = testChartData.ValidateChart(out List<string> errors);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Count > 0);
            Assert.IsTrue(errors[0].Contains("無効なレーンインデックス"));
        }

        [Test]
        public void ChartData_ValidateChart_NegativeTime_ReturnsFalse()
        {
            // Arrange
            testChartData.notes.Add(new NoteData { timeToHit = -1.0f, laneIndex = 0 });

            // Act
            bool isValid = testChartData.ValidateChart(out List<string> errors);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Count > 0);
            Assert.IsTrue(errors[0].Contains("負のタイミング"));
        }

        [Test]
        public void ChartData_ValidateChart_NegativeHoldDuration_ReturnsFalse()
        {
            // Arrange
            testChartData.notes.Add(new NoteData 
            { 
                noteType = NoteType.Hold,
                timeToHit = 1.0f, 
                laneIndex = 0,
                holdDuration = -1.0f
            });

            // Act
            bool isValid = testChartData.ValidateChart(out List<string> errors);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Count > 0);
            Assert.IsTrue(errors[0].Contains("Holdノーツの持続時間が無効"));
        }

        [Test]
        public void ChartData_GetStatistics_EmptyChart_ReturnsZeroStats()
        {
            // Arrange
            testChartData.notes.Clear();

            // Act
            var stats = testChartData.GetStatistics();

            // Assert
            Assert.AreEqual(0, stats.totalNotes);
            Assert.AreEqual(0, stats.tapNotes);
            Assert.AreEqual(0, stats.holdNotes);
            Assert.AreEqual(0f, stats.chartLengthSeconds);
            Assert.AreEqual(0f, stats.chartLengthBeats);
            Assert.AreEqual(0f, stats.averageNPS);
        }

        [Test]
        public void ChartData_GetStatistics_WithNotes_CalculatesCorrectly()
        {
            // Arrange
            testChartData.bpm = 120f;  // 1ビート = 0.5秒
            testChartData.notes.Add(new NoteData { noteType = NoteType.Tap, timeToHit = 0f, laneIndex = 0 });
            testChartData.notes.Add(new NoteData { noteType = NoteType.Hold, timeToHit = 2f, laneIndex = 1 });
            testChartData.notes.Add(new NoteData { noteType = NoteType.Tap, timeToHit = 4f, laneIndex = 2 });

            // Act
            var stats = testChartData.GetStatistics();

            // Assert
            Assert.AreEqual(3, stats.totalNotes);
            Assert.AreEqual(2, stats.tapNotes);
            Assert.AreEqual(1, stats.holdNotes);
            Assert.AreEqual(4f, stats.chartLengthBeats);
            Assert.AreEqual(2f, stats.chartLengthSeconds);  // 4ビート * 0.5秒/ビート
            Assert.AreEqual(1.5f, stats.averageNPS);  // 3ノーツ / 2秒
        }

        [Test]
        public void ChartData_GetStatistics_LaneDistribution_CountsCorrectly()
        {
            // Arrange
            testChartData.notes.Add(new NoteData { laneIndex = 0 });
            testChartData.notes.Add(new NoteData { laneIndex = 0 });
            testChartData.notes.Add(new NoteData { laneIndex = 1 });
            testChartData.notes.Add(new NoteData { laneIndex = 2 });
            testChartData.notes.Add(new NoteData { laneIndex = 3 });
            testChartData.notes.Add(new NoteData { laneIndex = 3 });
            testChartData.notes.Add(new NoteData { laneIndex = 3 });

            // Act
            var stats = testChartData.GetStatistics();

            // Assert
            Assert.AreEqual(2, stats.notesByLane[0]);
            Assert.AreEqual(1, stats.notesByLane[1]);
            Assert.AreEqual(1, stats.notesByLane[2]);
            Assert.AreEqual(3, stats.notesByLane[3]);
        }

        [Test]
        public void ChartData_GetStatistics_AverageInterval_CalculatesCorrectly()
        {
            // Arrange
            testChartData.notes.Add(new NoteData { timeToHit = 0f });
            testChartData.notes.Add(new NoteData { timeToHit = 1f });
            testChartData.notes.Add(new NoteData { timeToHit = 3f });
            testChartData.notes.Add(new NoteData { timeToHit = 6f });

            // Act
            var stats = testChartData.GetStatistics();

            // Assert
            // 間隔: 1, 2, 3 → 平均 = 2
            Assert.AreEqual(2f, stats.averageInterval, 0.01f);
        }

        [Test]
        public void ChartData_ClearNotes_RemovesAllNotes()
        {
            // Arrange
            testChartData.notes.Add(new NoteData { timeToHit = 1f });
            testChartData.notes.Add(new NoteData { timeToHit = 2f });
            Assert.AreEqual(2, testChartData.notes.Count);

            // Act
            testChartData.notes.Clear();

            // Assert
            Assert.AreEqual(0, testChartData.notes.Count);
        }

        [Test]
        public void ChartData_ValidateChart_SimultaneousNotes_ReturnsFalse()
        {
            // Arrange
            testChartData.notes.Add(new NoteData { timeToHit = 1.0f, laneIndex = 0 });
            testChartData.notes.Add(new NoteData { timeToHit = 1.0f, laneIndex = 0 });

            // Act
            bool isValid = testChartData.ValidateChart(out List<string> errors);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Count > 0);
            Assert.IsTrue(errors[0].Contains("同じレーンに同時"));
        }

        [Test]
        public void ChartData_ValidateChart_ValidChart_ReturnsTrue()
        {
            // Arrange
            testChartData.notes.Add(new NoteData 
            { 
                noteType = NoteType.Tap,
                timeToHit = 1.0f, 
                laneIndex = 0,
                visualScale = 1.0f
            });
            testChartData.notes.Add(new NoteData 
            { 
                noteType = NoteType.Hold,
                timeToHit = 2.0f, 
                laneIndex = 1,
                holdDuration = 1.0f,
                visualScale = 1.0f
            });

            // Act
            bool isValid = testChartData.ValidateChart(out List<string> errors);

            // Assert
            Assert.IsTrue(isValid);
            Assert.AreEqual(0, errors.Count);
        }

        [Test]
        public void ChartData_SortNotesByTime_EmptyList_DoesNotThrow()
        {
            // Arrange
            testChartData.notes.Clear();

            // Act & Assert
            Assert.DoesNotThrow(() => testChartData.SortNotesByTime());
            Assert.AreEqual(0, testChartData.notes.Count);
        }

        [Test]
        public void ChartData_SortNotesByTime_SingleNote_DoesNotChange()
        {
            // Arrange
            var note = new NoteData { timeToHit = 1.0f, laneIndex = 0 };
            testChartData.notes.Add(note);

            // Act
            testChartData.SortNotesByTime();

            // Assert
            Assert.AreEqual(1, testChartData.notes.Count);
            Assert.AreSame(note, testChartData.notes[0]);
        }

        [Test]
        public void ChartData_Properties_SetAndGetCorrectly()
        {
            // Arrange & Act
            testChartData.songName = "Test Song";
            testChartData.artistName = "Test Artist";
            testChartData.bpm = 140f;
            testChartData.difficulty = 3;
            testChartData.firstBeatOffsetSeconds = 1.5f;
            testChartData.audioClip = null;

            // Assert
            Assert.AreEqual("Test Song", testChartData.songName);
            Assert.AreEqual("Test Artist", testChartData.artistName);
            Assert.AreEqual(140f, testChartData.bpm);
            Assert.AreEqual(3, testChartData.difficulty);
            Assert.AreEqual(1.5f, testChartData.firstBeatOffsetSeconds);
            Assert.IsNull(testChartData.audioClip);
        }
    }
}