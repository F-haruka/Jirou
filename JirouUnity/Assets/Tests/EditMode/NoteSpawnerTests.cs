using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Jirou.Gameplay;
using Jirou.Core;
using System.Collections.Generic;

namespace Jirou.Tests
{
    /// <summary>
    /// NoteSpawnerコンポーネントのユニットテスト
    /// </summary>
    [TestFixture]
    public class NoteSpawnerTests
    {
        private GameObject testGameObject;
        private NoteSpawner noteSpawner;
        private ChartData testChartData;
        
        [SetUp]
        public void Setup()
        {
            // テスト用GameObjectの作成
            testGameObject = new GameObject("TestNoteSpawner");
            noteSpawner = testGameObject.AddComponent<NoteSpawner>();
            
            // テスト用ChartDataの作成
            testChartData = ScriptableObject.CreateInstance<ChartData>();
            
            // テスト用プレハブの作成（ダミー）
            GameObject tapPrefab = new GameObject("TapNotePrefab");
            GameObject holdPrefab = new GameObject("HoldNotePrefab");
            
            // NoteSpawnerの設定
            noteSpawner.chartData = testChartData;
            noteSpawner.tapNotePrefab = tapPrefab;
            noteSpawner.holdNotePrefab = holdPrefab;
            noteSpawner.laneXPositions = new float[] { -3f, -1f, 1f, 3f };
            noteSpawner.noteY = 0.5f;
            noteSpawner.beatsShownInAdvance = 3.0f;
        }
        
        [TearDown]
        public void TearDown()
        {
            // テスト用オブジェクトのクリーンアップ
            if (testGameObject != null)
                Object.DestroyImmediate(testGameObject);
            
            if (testChartData != null)
                Object.DestroyImmediate(testChartData);
        }
        
        [Test]
        public void NoteSpawner_Initialization_WithValidData_Success()
        {
            // Arrange
            var noteData = new NoteData();
            noteData.NoteType = NoteType.Tap;
            noteData.LaneIndex = 0;
            noteData.TimeToHit = 1.0f;
            testChartData.Notes.Add(noteData);
            
            // Act & Assert
            Assert.IsNotNull(noteSpawner);
            Assert.AreEqual(testChartData, noteSpawner.chartData);
            Assert.AreEqual(4, noteSpawner.laneXPositions.Length);
            Assert.AreEqual(0.5f, noteSpawner.noteY);
            Assert.AreEqual(3.0f, noteSpawner.beatsShownInAdvance);
        }
        
        [Test]
        public void NoteSpawner_Statistics_ReturnsCorrectValues()
        {
            // Arrange
            var note1 = new NoteData();
            note1.NoteType = NoteType.Tap;
            note1.LaneIndex = 0;
            note1.TimeToHit = 1.0f;
            testChartData.Notes.Add(note1);
            
            var note2 = new NoteData();
            note2.NoteType = NoteType.Hold;
            note2.LaneIndex = 1;
            note2.TimeToHit = 2.0f;
            note2.HoldDuration = 1.0f;
            testChartData.Notes.Add(note2);
            
            var note3 = new NoteData();
            note3.NoteType = NoteType.Tap;
            note3.LaneIndex = 2;
            note3.TimeToHit = 3.0f;
            testChartData.Notes.Add(note3);
            
            // Act
            int totalNotes, spawnedNotes, activeNotesCount, remainingNotes;
            noteSpawner.GetStatistics(out totalNotes, out spawnedNotes, out activeNotesCount, out remainingNotes);
            
            // Assert
            Assert.AreEqual(3, totalNotes);
            Assert.AreEqual(0, spawnedNotes); // 初期状態では0
            Assert.AreEqual(0, activeNotesCount);
            Assert.AreEqual(3, remainingNotes);
        }
        
        [Test]
        public void ChartData_SortNotesByTime_SortsCorrectly()
        {
            // Arrange
            var note1 = new NoteData();
            note1.NoteType = NoteType.Tap;
            note1.LaneIndex = 0;
            note1.TimeToHit = 3.0f;
            testChartData.Notes.Add(note1);
            
            var note2 = new NoteData();
            note2.NoteType = NoteType.Hold;
            note2.LaneIndex = 1;
            note2.TimeToHit = 1.0f;
            note2.HoldDuration = 1.0f;
            testChartData.Notes.Add(note2);
            
            var note3 = new NoteData();
            note3.NoteType = NoteType.Tap;
            note3.LaneIndex = 2;
            note3.TimeToHit = 2.0f;
            testChartData.Notes.Add(note3);
            
            // Act
            testChartData.SortNotesByTime();
            
            // Assert
            Assert.AreEqual(1.0f, testChartData.Notes[0].TimeToHit);
            Assert.AreEqual(2.0f, testChartData.Notes[1].TimeToHit);
            Assert.AreEqual(3.0f, testChartData.Notes[2].TimeToHit);
        }
        
        [Test]
        public void ChartData_ValidateChart_DetectsInvalidData()
        {
            // Arrange
            var note1 = new NoteData();
            note1.NoteType = NoteType.Tap;
            note1.LaneIndex = -1; // 無効なレーン
            note1.TimeToHit = 1.0f;
            testChartData.Notes.Add(note1);
            
            var note2 = new NoteData();
            note2.NoteType = NoteType.Hold;
            note2.LaneIndex = 4; // 無効なレーン
            note2.TimeToHit = 2.0f;
            note2.HoldDuration = 1.0f;
            testChartData.Notes.Add(note2);
            
            var note3 = new NoteData();
            note3.NoteType = NoteType.Tap;
            note3.LaneIndex = 1;
            note3.TimeToHit = -1.0f; // 無効なタイミング
            testChartData.Notes.Add(note3);
            
            // Act
            List<string> errors;
            bool isValid = testChartData.ValidateChart(out errors);
            
            // Assert
            Assert.IsFalse(isValid);
            Assert.IsNotNull(errors);
            Assert.Greater(errors.Count, 0);
        }
        
        [Test]
        public void NotePoolManager_SingletonPattern_ReturnsSameInstance()
        {
            // Act
            var instance1 = NotePoolManager.Instance;
            var instance2 = NotePoolManager.Instance;
            
            // Assert
            Assert.IsNotNull(instance1);
            Assert.IsNotNull(instance2);
            Assert.AreEqual(instance1, instance2);
            
            // Cleanup
            if (instance1 != null)
                Object.DestroyImmediate(instance1.gameObject);
        }
        
        [Test]
        public void NoteController_Initialize_SetsDataCorrectly()
        {
            // Arrange
            GameObject noteObject = new GameObject("TestNote");
            NoteController controller = noteObject.AddComponent<NoteController>();
            NoteData testNoteData = new NoteData();
            testNoteData.NoteType = NoteType.Tap;
            testNoteData.LaneIndex = 1;
            testNoteData.TimeToHit = 2.5f;
            
            // Act
            controller.Initialize(testNoteData, null);
            
            // Assert
            Assert.AreEqual(testNoteData, controller.noteData);
            Assert.IsFalse(controller.IsCompleted());
            
            // Cleanup
            Object.DestroyImmediate(noteObject);
        }
        
        [Test]
        public void NoteController_OnHit_MarksAsCompleted()
        {
            // Arrange
            GameObject noteObject = new GameObject("TestNote");
            NoteController controller = noteObject.AddComponent<NoteController>();
            
            // Act
            controller.OnHit();
            
            // Assert
            Assert.IsTrue(controller.IsCompleted());
            
            // Cleanup
            Object.DestroyImmediate(noteObject);
        }
    }
}