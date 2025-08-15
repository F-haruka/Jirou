using NUnit.Framework;
using UnityEngine;

namespace Jirou.Tests.EditMode
{
    /// <summary>
    /// NotePosition3D構造体のユニットテスト
    /// </summary>
    public class NotePosition3DTests
    {
        [Test]
        public void NotePosition3D_Constructor_SetsCorrectValues()
        {
            // Arrange & Act
            var pos = new Core.NotePosition3D(1, 10f, 0.5f);
            
            // Assert
            Assert.AreEqual(-1f, pos.x, "レーン1のX座標は-1");
            Assert.AreEqual(0.5f, pos.y, "Y座標は0.5");
            Assert.AreEqual(10f, pos.z, "Z座標は10");
        }
        
        [Test]
        public void NotePosition3D_Constructor_UsesDefaultYPosition()
        {
            // Arrange & Act
            var pos = new Core.NotePosition3D(0, 5f);
            
            // Assert
            Assert.AreEqual(-3f, pos.x, "レーン0のX座標は-3");
            Assert.AreEqual(0.5f, pos.y, "デフォルトY座標は0.5");
            Assert.AreEqual(5f, pos.z, "Z座標は5");
        }
        
        [Test]
        public void NotePosition3D_Constructor_HandlesInvalidLaneIndex()
        {
            // Arrange & Act
            var posNegative = new Core.NotePosition3D(-1, 10f);
            var posOverflow = new Core.NotePosition3D(4, 10f);
            
            // Assert
            Assert.AreEqual(0f, posNegative.x, "無効なレーンインデックスではX=0");
            Assert.AreEqual(0f, posOverflow.x, "無効なレーンインデックスではX=0");
        }
        
        [Test]
        public void NotePosition3D_ToVector3_ConvertsCorrectly()
        {
            // Arrange
            var pos = new Core.NotePosition3D(2, 15f, 1f);
            
            // Act
            Vector3 vec = pos.ToVector3();
            
            // Assert
            Assert.AreEqual(new Vector3(1f, 1f, 15f), vec, "Vector3への変換が正しい");
        }
        
        [Test]
        public void NotePosition3D_GetDistanceToJudgmentLine_CalculatesCorrectly()
        {
            // Arrange
            var pos = new Core.NotePosition3D(0, 10f);
            
            // Act & Assert - デフォルト判定ライン（Z=0）
            float distance = pos.GetDistanceToJudgmentLine();
            Assert.AreEqual(10f, distance, "判定ラインまでの距離は10");
            
            // Act & Assert - カスタム判定ライン
            float customDistance = pos.GetDistanceToJudgmentLine(5f);
            Assert.AreEqual(5f, customDistance, "カスタム判定ラインまでの距離は5");
        }
        
        [Test]
        public void NotePosition3D_AllLanes_HaveCorrectXPositions()
        {
            // Arrange & Act & Assert
            for (int i = 0; i < 4; i++)
            {
                var pos = new Core.NotePosition3D(i, 0f);
                float expectedX = Core.NoteData.LaneXPositions[i];
                Assert.AreEqual(expectedX, pos.x, $"レーン{i}のX座標は{expectedX}");
            }
        }
    }
    
    /// <summary>
    /// NoteVisualCalculatorクラスのユニットテスト
    /// </summary>
    public class NoteVisualCalculatorTests
    {
        [Test]
        public void CalculateScaleByDistance_AtSpawnPosition_ReturnsMinScale()
        {
            // Arrange
            float spawnZ = 20f;
            float currentZ = spawnZ;
            
            // Act
            float scale = Core.NoteVisualCalculator.CalculateScaleByDistance(currentZ, spawnZ);
            
            // Assert
            Assert.AreEqual(0.5f, scale, 0.01f, "スポーン位置では0.5倍");
        }
        
        [Test]
        public void CalculateScaleByDistance_AtJudgmentLine_ReturnsMaxScale()
        {
            // Arrange
            float spawnZ = 20f;
            float currentZ = 0f;
            
            // Act
            float scale = Core.NoteVisualCalculator.CalculateScaleByDistance(currentZ, spawnZ);
            
            // Assert
            Assert.AreEqual(1.5f, scale, 0.01f, "判定ラインでは1.5倍");
        }
        
        [Test]
        public void CalculateScaleByDistance_AtMiddle_ReturnsMiddleScale()
        {
            // Arrange
            float spawnZ = 20f;
            float currentZ = 10f;
            
            // Act
            float scale = Core.NoteVisualCalculator.CalculateScaleByDistance(currentZ, spawnZ);
            
            // Assert
            Assert.AreEqual(1.0f, scale, 0.01f, "中間地点では1.0倍");
        }
        
        [Test]
        public void CalculateScaleByDistance_WithBaseScale_AppliesCorrectly()
        {
            // Arrange
            float spawnZ = 20f;
            float currentZ = 10f;
            float baseScale = 2.0f;
            
            // Act
            float scale = Core.NoteVisualCalculator.CalculateScaleByDistance(currentZ, spawnZ, baseScale);
            
            // Assert
            Assert.AreEqual(2.0f, scale, 0.01f, "baseScale2.0の中間地点では2.0倍");
        }
        
        [Test]
        public void CalculateScaleByDistance_HandlesZeroSpawnZ()
        {
            // Arrange
            float spawnZ = 0f;
            float currentZ = 5f;
            float baseScale = 1.5f;
            
            // Act
            float scale = Core.NoteVisualCalculator.CalculateScaleByDistance(currentZ, spawnZ, baseScale);
            
            // Assert
            Assert.AreEqual(baseScale, scale, "spawnZ=0の場合はbaseScaleを返す");
        }
        
        [Test]
        public void CalculateAlphaByDistance_BeforeFadeStart_ReturnsFullAlpha()
        {
            // Arrange
            float spawnZ = 20f;
            float fadeStartZ = spawnZ * 0.8f;  // 16f
            float currentZ = 15f;  // フェード開始前
            
            // Act
            float alpha = Core.NoteVisualCalculator.CalculateAlphaByDistance(currentZ, spawnZ);
            
            // Assert
            Assert.AreEqual(1.0f, alpha, "フェード開始前は完全不透明");
        }
        
        [Test]
        public void CalculateAlphaByDistance_AtSpawn_ReturnsZeroAlpha()
        {
            // Arrange
            float spawnZ = 20f;
            float currentZ = spawnZ;
            
            // Act
            float alpha = Core.NoteVisualCalculator.CalculateAlphaByDistance(currentZ, spawnZ);
            
            // Assert
            Assert.AreEqual(0f, alpha, 0.01f, "スポーン位置では完全透明");
        }
        
        [Test]
        public void CalculateAlphaByDistance_DuringFade_ReturnsPartialAlpha()
        {
            // Arrange
            float spawnZ = 20f;
            float fadeStartZ = spawnZ * 0.8f;  // 16f
            float currentZ = 18f;  // フェード中（16-20の中間）
            
            // Act
            float alpha = Core.NoteVisualCalculator.CalculateAlphaByDistance(currentZ, spawnZ);
            
            // Assert
            Assert.AreEqual(0.5f, alpha, 0.01f, "フェード中間では半透明");
        }
        
        [Test]
        public void CalculateAlphaByDistance_WithCustomFadeRatio_AppliesCorrectly()
        {
            // Arrange
            float spawnZ = 20f;
            float fadeStartRatio = 0.5f;  // 50%地点からフェード開始
            float currentZ = 15f;  // 75%地点（フェード中）
            
            // Act
            float alpha = Core.NoteVisualCalculator.CalculateAlphaByDistance(currentZ, spawnZ, fadeStartRatio);
            
            // Assert
            Assert.AreEqual(0.5f, alpha, 0.01f, "カスタムフェード比率が適用される");
        }
        
        [Test]
        public void CalculateAlphaByDistance_HandlesZeroSpawnZ()
        {
            // Arrange
            float spawnZ = 0f;
            float currentZ = 5f;
            
            // Act
            float alpha = Core.NoteVisualCalculator.CalculateAlphaByDistance(currentZ, spawnZ);
            
            // Assert
            Assert.AreEqual(1.0f, alpha, "spawnZ=0の場合は常に不透明");
        }
        
        [Test]
        public void CalculateNoteWorldPosition_RequiresConductor()
        {
            // Arrange
            var noteData = new Core.NoteData { laneIndex = 1, timeToHit = 2f };
            
            // Act - Conductorがnullの場合、エラーログが出力されることを期待
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "Conductorが設定されていません");
            Vector3 pos = Core.NoteVisualCalculator.CalculateNoteWorldPosition(noteData, 1f, null);
            
            // Assert
            Assert.AreEqual(Vector3.zero, pos, "Conductorがnullの場合はVector3.zeroを返す");
        }
        
        [Test]
        public void CalculateHoldEndPosition_RequiresHoldNote()
        {
            // Arrange
            var tapNote = new Core.NoteData 
            { 
                noteType = Core.NoteType.Tap,
                laneIndex = 1, 
                timeToHit = 2f 
            };
            
            // Act - TapノーツでCalculateHoldEndPositionを呼ぶと警告ログが出力されることを期待
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, "HoldノーツではないためCalculateHoldEndPositionをスキップ");
            Vector3 pos = Core.NoteVisualCalculator.CalculateHoldEndPosition(tapNote, null);
            
            // Assert
            Assert.AreEqual(Vector3.zero, pos, "TapノーツではVector3.zeroを返す");
        }
        
        [Test]
        public void CalculateHoldEndPosition_RequiresConductor()
        {
            // Arrange
            var holdNote = new Core.NoteData 
            { 
                noteType = Core.NoteType.Hold,
                laneIndex = 1, 
                timeToHit = 2f,
                holdDuration = 2f
            };
            
            // Act - Conductorがnullの場合、エラーログが出力されることを期待
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "Conductorが設定されていません");
            Vector3 pos = Core.NoteVisualCalculator.CalculateHoldEndPosition(holdNote, null);
            
            // Assert
            Assert.AreEqual(Vector3.zero, pos, "ConductorがnullではVector3.zeroを返す");
        }
    }
}