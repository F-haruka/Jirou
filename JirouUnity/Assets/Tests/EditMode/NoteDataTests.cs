using NUnit.Framework;
using UnityEngine;

namespace Jirou.Tests.EditMode
{
    /// <summary>
    /// NoteDataクラスのユニットテスト
    /// </summary>
    public class NoteDataTests
    {
        [Test]
        public void NoteData_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var note = new Core.NoteData();
            
            // Assert
            Assert.AreEqual(Core.NoteType.Tap, note.noteType, "デフォルトのノートタイプはTapであるべき");
            Assert.AreEqual(0, note.laneIndex, "デフォルトのレーンインデックスは0であるべき");
            Assert.AreEqual(0f, note.timeToHit, "デフォルトのタイミングは0であるべき");
            Assert.AreEqual(0f, note.holdDuration, "デフォルトのホールド長は0であるべき");
            Assert.AreEqual(1.0f, note.visualScale, "デフォルトのスケールは1.0であるべき");
            Assert.AreEqual(Color.white, note.noteColor, "デフォルトの色は白であるべき");
            Assert.AreEqual(100, note.baseScore, "デフォルトの基本スコアは100であるべき");
            Assert.AreEqual(1.0f, note.scoreMultiplier, "デフォルトのスコア倍率は1.0であるべき");
        }
        
        [Test]
        public void NoteData_LaneXPosition_ReturnsCorrectValue()
        {
            // Arrange
            var note = new Core.NoteData();
            float[] expectedPositions = { -3f, -1f, 1f, 3f };
            
            // Act & Assert
            for (int i = 0; i < 4; i++)
            {
                note.laneIndex = i;
                float actualPosition = note.GetLaneXPosition();
                Assert.AreEqual(expectedPositions[i], actualPosition, 
                    $"レーン{i}のX座標は{expectedPositions[i]}であるべき");
            }
        }
        
        [Test]
        public void NoteData_LaneXPosition_HandlesInvalidIndex()
        {
            // Arrange
            var note = new Core.NoteData();
            
            // Act & Assert - 無効なインデックスの場合
            note.laneIndex = -1;
            Assert.AreEqual(0f, note.GetLaneXPosition(), "無効なレーンインデックスでは0を返すべき");
            
            note.laneIndex = 4;
            Assert.AreEqual(0f, note.GetLaneXPosition(), "無効なレーンインデックスでは0を返すべき");
        }
        
        [Test]
        public void NoteData_GetEndTime_CalculatesCorrectlyForTapNote()
        {
            // Arrange
            var tapNote = new Core.NoteData
            {
                noteType = Core.NoteType.Tap,
                timeToHit = 4.0f,
                holdDuration = 2.0f  // Tapノーツでは無視される
            };
            
            // Act
            float endTime = tapNote.GetEndTime();
            
            // Assert
            Assert.AreEqual(4.0f, endTime, "TapノーツのendTimeはtimeToHitと同じであるべき");
        }
        
        [Test]
        public void NoteData_GetEndTime_CalculatesCorrectlyForHoldNote()
        {
            // Arrange
            var holdNote = new Core.NoteData
            {
                noteType = Core.NoteType.Hold,
                timeToHit = 4.0f,
                holdDuration = 2.0f
            };
            
            // Act
            float endTime = holdNote.GetEndTime();
            
            // Assert
            Assert.AreEqual(6.0f, endTime, "HoldノーツのendTimeはtimeToHit + holdDurationであるべき");
        }
        
        [Test]
        public void NoteData_Validate_AcceptsValidData()
        {
            // Arrange
            var note = new Core.NoteData
            {
                noteType = Core.NoteType.Tap,
                laneIndex = 2,
                timeToHit = 1.0f,
                visualScale = 1.0f
            };
            
            // Act
            string error;
            bool isValid = note.Validate(out error);
            
            // Assert
            Assert.IsTrue(isValid, "有効なデータはtrueを返すべき");
            Assert.IsEmpty(error, "エラーメッセージは空であるべき");
        }
        
        [Test]
        public void NoteData_Validate_DetectsInvalidLaneIndex()
        {
            // Arrange
            var note = new Core.NoteData();
            string error;
            
            // Act & Assert - 負の値
            note.laneIndex = -1;
            Assert.IsFalse(note.Validate(out error), "負のレーンインデックスは無効");
            Assert.IsTrue(error.Contains("レーンインデックス"), "エラーメッセージにレーンインデックスが含まれるべき");
            
            // Act & Assert - 範囲外
            note.laneIndex = 4;
            Assert.IsFalse(note.Validate(out error), "レーンインデックス4は無効");
            Assert.IsTrue(error.Contains("レーンインデックス"), "エラーメッセージにレーンインデックスが含まれるべき");
        }
        
        [Test]
        public void NoteData_Validate_DetectsNegativeTiming()
        {
            // Arrange
            var note = new Core.NoteData
            {
                laneIndex = 0,
                timeToHit = -1.0f,
                visualScale = 1.0f
            };
            
            // Act
            string error;
            bool isValid = note.Validate(out error);
            
            // Assert
            Assert.IsFalse(isValid, "負のタイミングは無効");
            Assert.IsTrue(error.Contains("タイミング"), "エラーメッセージにタイミングが含まれるべき");
        }
        
        [Test]
        public void NoteData_Validate_DetectsInvalidHoldDuration()
        {
            // Arrange
            var note = new Core.NoteData
            {
                noteType = Core.NoteType.Hold,
                laneIndex = 0,
                timeToHit = 1.0f,
                holdDuration = 0f,  // Holdノーツでは0は無効
                visualScale = 1.0f
            };
            
            // Act
            string error;
            bool isValid = note.Validate(out error);
            
            // Assert
            Assert.IsFalse(isValid, "Holdノーツの長さ0は無効");
            Assert.IsTrue(error.Contains("Hold"), "エラーメッセージにHoldが含まれるべき");
        }
        
        [Test]
        public void NoteData_Validate_DetectsInvalidScale()
        {
            // Arrange
            var note = new Core.NoteData
            {
                laneIndex = 0,
                timeToHit = 1.0f,
                visualScale = 0f  // 0以下は無効
            };
            
            // Act
            string error;
            bool isValid = note.Validate(out error);
            
            // Assert
            Assert.IsFalse(isValid, "スケール0は無効");
            Assert.IsTrue(error.Contains("スケール"), "エラーメッセージにスケールが含まれるべき");
        }
        
        [Test]
        public void NoteData_LaneKeys_MapsCorrectly()
        {
            // Assert
            Assert.AreEqual(KeyCode.D, Core.NoteData.LaneKeys[0], "レーン0はDキー");
            Assert.AreEqual(KeyCode.F, Core.NoteData.LaneKeys[1], "レーン1はFキー");
            Assert.AreEqual(KeyCode.J, Core.NoteData.LaneKeys[2], "レーン2はJキー");
            Assert.AreEqual(KeyCode.K, Core.NoteData.LaneKeys[3], "レーン3はKキー");
        }
    }
}