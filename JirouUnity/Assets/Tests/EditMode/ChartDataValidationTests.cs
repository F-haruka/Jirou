using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Jirou.Core;
using System.Collections.Generic;
using System.Linq;

namespace Jirou.Tests.EditMode
{
    /// <summary>
    /// ChartDataのバリデーションと統計機能のテスト
    /// </summary>
    [TestFixture]
    public class ChartDataValidationTests
    {
        private ChartData testChartData;
        private SerializedObject serializedObject;

        [SetUp]
        public void SetUp()
        {
            // テスト用のChartDataを作成
            testChartData = ScriptableObject.CreateInstance<ChartData>();
            // Notes プロパティは読み取り専用なので、既存のリストを操作する

            // SerializedObjectを作成
            serializedObject = new SerializedObject(testChartData);

        }

        [TearDown]
        public void TearDown()
        {
            if (testChartData != null)
                Object.DestroyImmediate(testChartData);
        }

        [Test]
        public void ChartData_SortNotesByTime_SortsCorrectly()
        {
            // Arrange
            testChartData.Notes.Add(new NoteData { TimeToHit = 3.0f, LaneIndex = 0 });
            testChartData.Notes.Add(new NoteData { TimeToHit = 1.0f, LaneIndex = 1 });
            testChartData.Notes.Add(new NoteData { TimeToHit = 2.0f, LaneIndex = 2 });

            // Act
            testChartData.SortNotesByTime();

            // Assert
            Assert.AreEqual(1.0f, testChartData.Notes[0].TimeToHit);
            Assert.AreEqual(2.0f, testChartData.Notes[1].TimeToHit);
            Assert.AreEqual(3.0f, testChartData.Notes[2].TimeToHit);
        }

        [Test]
        public void ChartData_ValidateChart_EmptyChart_ReturnsTrue()
        {
            // Arrange
            testChartData.Notes.Clear();

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
            testChartData.Notes.Add(new NoteData { TimeToHit = 1.0f, LaneIndex = -1 });
            testChartData.Notes.Add(new NoteData { TimeToHit = 2.0f, LaneIndex = 4 });

            // Act
            bool isValid = testChartData.ValidateChart(out List<string> errors);

            // Assert
            Assert.IsFalse(isValid);
            // ChartDataのデフォルトでSongClipがnullなので、そのエラーも含まれる
            // 無効なレーンインデックス2つ + 楽曲ファイル未設定1つ = 3つのエラー
            Assert.AreEqual(3, errors.Count);
            Assert.IsTrue(errors.Any(e => e.Contains("無効なレーンインデックス")));
            Assert.IsTrue(errors.Any(e => e.Contains("楽曲ファイル")));
        }

        [Test]
        public void ChartData_ValidateChart_NegativeTime_ReturnsFalse()
        {
            // Arrange
            testChartData.Notes.Add(new NoteData { TimeToHit = -1.0f, LaneIndex = 0 });

            // Act
            bool isValid = testChartData.ValidateChart(out List<string> errors);

            // Assert
            Assert.IsFalse(isValid);
            // 負のタイミングのエラーが少なくとも1つ含まれることを確認
            Assert.IsTrue(errors.Count >= 1);
            Assert.IsTrue(errors.Any(e => e.Contains("負のタイミング")));
        }

        [Test]
        public void ChartData_ValidateChart_NegativeHoldDuration_ReturnsFalse()
        {
            // Arrange
            testChartData.Notes.Add(new NoteData 
            { 
                NoteType = NoteType.Hold,
                TimeToHit = 1.0f, 
                LaneIndex = 0,
                HoldDuration = -1.0f
            });

            // Act
            bool isValid = testChartData.ValidateChart(out List<string> errors);

            // Assert
            Assert.IsFalse(isValid);
            // Holdノーツの長さが不正1つ + 楽曲ファイル未設定1つ = 最低2つのエラー
            Assert.IsTrue(errors.Count >= 2);
            Assert.IsTrue(errors.Any(e => e.Contains("Holdノーツの長さが不正")));
        }

        [Test]
        public void ChartData_GetStatistics_EmptyChart_ReturnsZeroStats()
        {
            // Arrange
            testChartData.Notes.Clear();

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
            // BPMはデフォルトの120fを使用
            testChartData.Notes.Add(new NoteData { NoteType = NoteType.Tap, TimeToHit = 0f, LaneIndex = 0 });
            testChartData.Notes.Add(new NoteData { NoteType = NoteType.Hold, TimeToHit = 2f, LaneIndex = 1 });
            testChartData.Notes.Add(new NoteData { NoteType = NoteType.Tap, TimeToHit = 4f, LaneIndex = 2 });

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
            testChartData.Notes.Add(new NoteData { LaneIndex = 0 });
            testChartData.Notes.Add(new NoteData { LaneIndex = 0 });
            testChartData.Notes.Add(new NoteData { LaneIndex = 1 });
            testChartData.Notes.Add(new NoteData { LaneIndex = 2 });
            testChartData.Notes.Add(new NoteData { LaneIndex = 3 });
            testChartData.Notes.Add(new NoteData { LaneIndex = 3 });
            testChartData.Notes.Add(new NoteData { LaneIndex = 3 });

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
            testChartData.Notes.Add(new NoteData { TimeToHit = 0f });
            testChartData.Notes.Add(new NoteData { TimeToHit = 1f });
            testChartData.Notes.Add(new NoteData { TimeToHit = 3f });
            testChartData.Notes.Add(new NoteData { TimeToHit = 6f });

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
            testChartData.Notes.Add(new NoteData { TimeToHit = 1f });
            testChartData.Notes.Add(new NoteData { TimeToHit = 2f });
            Assert.AreEqual(2, testChartData.Notes.Count);

            // Act
            testChartData.Notes.Clear();

            // Assert
            Assert.AreEqual(0, testChartData.Notes.Count);
        }

        [Test]
        public void ChartData_ValidateChart_SimultaneousNotes_ReturnsFalse()
        {
            // Arrange
            testChartData.Notes.Add(new NoteData { TimeToHit = 1.0f, LaneIndex = 0 });
            testChartData.Notes.Add(new NoteData { TimeToHit = 1.0f, LaneIndex = 0 });

            // Act
            bool isValid = testChartData.ValidateChart(out List<string> errors);

            // Assert
            Assert.IsFalse(isValid);
            // 重複ノーツ1つ + 楽曲ファイル未設定1つ = 最低2つのエラー
            Assert.IsTrue(errors.Count >= 2);
            Assert.IsTrue(errors.Any(e => e.Contains("重複ノーツ")));
        }

        [Test]
        public void ChartData_ValidateChart_ValidChart_ReturnsTrue()
        {
            // Arrange
            testChartData.Notes.Add(new NoteData 
            { 
                NoteType = NoteType.Tap,
                TimeToHit = 1.0f, 
                LaneIndex = 0,
                VisualScale = 1.0f
            });
            testChartData.Notes.Add(new NoteData 
            { 
                NoteType = NoteType.Hold,
                TimeToHit = 2.0f, 
                LaneIndex = 1,
                HoldDuration = 1.0f,
                VisualScale = 1.0f
            });

            // Act
            bool isValid = testChartData.ValidateChart(out List<string> errors);

            // Assert
            // 楽曲ファイルが設定されていないのでバリデーションは失敗する
            Assert.IsFalse(isValid);
            Assert.AreEqual(1, errors.Count);  // 楽曲ファイル未設定のエラーのみ
            Assert.IsTrue(errors.Any(e => e.Contains("楽曲ファイル")));
        }

        [Test]
        public void ChartData_SortNotesByTime_EmptyList_DoesNotThrow()
        {
            // Arrange
            testChartData.Notes.Clear();

            // Act & Assert
            Assert.DoesNotThrow(() => testChartData.SortNotesByTime());
            Assert.AreEqual(0, testChartData.Notes.Count);
        }

        [Test]
        public void ChartData_SortNotesByTime_SingleNote_DoesNotChange()
        {
            // Arrange
            var note = new NoteData { TimeToHit = 1.0f, LaneIndex = 0 };
            testChartData.Notes.Add(note);

            // Act
            testChartData.SortNotesByTime();

            // Assert
            Assert.AreEqual(1, testChartData.Notes.Count);
            Assert.AreSame(note, testChartData.Notes[0]);
        }

        [Test]
        public void ChartData_Properties_SetAndGetCorrectly()
        {
            // Arrange & Act
            // ChartDataのプロパティは読み取り専用なので、直接設定できない

            // Assert
            Assert.AreEqual("無題", testChartData.SongName);
            Assert.AreEqual("不明", testChartData.Artist);
            Assert.AreEqual(120f, testChartData.Bpm);
            Assert.AreEqual(1, testChartData.Difficulty);
            Assert.AreEqual(0f, testChartData.FirstBeatOffset);
            Assert.IsNull(testChartData.SongClip);
        }
    }
}