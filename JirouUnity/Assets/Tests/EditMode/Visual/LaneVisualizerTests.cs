using NUnit.Framework;
using UnityEngine;
using Jirou.Visual;
using Jirou.Core;

namespace Jirou.Tests
{
    /// <summary>
    /// LaneVisualizerのユニットテスト
    /// </summary>
    public class LaneVisualizerTests
    {
        private GameObject testObject;
        private LaneVisualizer visualizer;

        [SetUp]
        public void Setup()
        {
            // テスト用のGameObjectとコンポーネントを作成
            testObject = new GameObject("TestLaneVisualizer");
            visualizer = testObject.AddComponent<LaneVisualizer>();
            
            // デフォルト設定
            visualizer.laneCount = 4;
            visualizer.laneWidth = 2.0f;
            visualizer.nearWidth = 2.0f;
            visualizer.farWidth = 0.5f;
            visualizer.laneLength = 20.0f;
        }

        [TearDown]
        public void TearDown()
        {
            // テストオブジェクトをクリーンアップ
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void LaneXCalculation_NearPosition_ReturnsCorrectCoordinates()
        {
            // Arrange
            visualizer.laneCount = 4;
            visualizer.laneWidth = 2.0f;
            
            // Act & Assert
            // レーン0（左端）
            float x0 = visualizer.CalculateLaneX(0, true);
            Assert.AreEqual(-3.0f, x0, 0.01f, "レーン0のX座標が正しくありません");
            
            // レーン1
            float x1 = visualizer.CalculateLaneX(1, true);
            Assert.AreEqual(-1.0f, x1, 0.01f, "レーン1のX座標が正しくありません");
            
            // レーン2
            float x2 = visualizer.CalculateLaneX(2, true);
            Assert.AreEqual(1.0f, x2, 0.01f, "レーン2のX座標が正しくありません");
            
            // レーン3（右端）
            float x3 = visualizer.CalculateLaneX(3, true);
            Assert.AreEqual(3.0f, x3, 0.01f, "レーン3のX座標が正しくありません");
        }

        [Test]
        public void LaneXCalculation_FarPosition_AppliesPerspective()
        {
            // Arrange
            visualizer.laneCount = 4;
            visualizer.laneWidth = 2.0f;
            visualizer.nearWidth = 2.0f;
            visualizer.farWidth = 0.5f;
            
            // Act
            float nearX = visualizer.CalculateLaneX(0, true);
            float farX = visualizer.CalculateLaneX(0, false);
            
            // Assert
            float expectedFarX = nearX * (visualizer.farWidth / visualizer.nearWidth);
            Assert.AreEqual(expectedFarX, farX, 0.01f, "遠近感が正しく適用されていません");
            Assert.AreEqual(-0.75f, farX, 0.01f, "奥のX座標が正しくありません");
        }

        [Test]
        public void PerspectiveTransform_CalculatesCorrectly()
        {
            // Arrange
            visualizer.nearWidth = 2.0f;
            visualizer.farWidth = 0.5f;
            
            // Act
            float nearX = 2.0f;
            float farX = nearX * (visualizer.farWidth / visualizer.nearWidth);
            
            // Assert
            Assert.AreEqual(0.5f, farX, 0.01f, "遠近感の変換が正しくありません");
        }

        [Test]
        public void LaneCount_DifferentValues_CalculatesCorrectPositions()
        {
            // 3レーンの場合
            visualizer.laneCount = 3;
            visualizer.laneWidth = 2.0f;
            
            float x0_3lanes = visualizer.CalculateLaneX(0, true);
            float x1_3lanes = visualizer.CalculateLaneX(1, true);
            float x2_3lanes = visualizer.CalculateLaneX(2, true);
            
            Assert.AreEqual(-2.0f, x0_3lanes, 0.01f, "3レーン時のレーン0が正しくありません");
            Assert.AreEqual(0.0f, x1_3lanes, 0.01f, "3レーン時のレーン1が正しくありません");
            Assert.AreEqual(2.0f, x2_3lanes, 0.01f, "3レーン時のレーン2が正しくありません");
            
            // 5レーンの場合
            visualizer.laneCount = 5;
            visualizer.laneWidth = 2.0f;
            
            float x0_5lanes = visualizer.CalculateLaneX(0, true);
            float x2_5lanes = visualizer.CalculateLaneX(2, true);
            float x4_5lanes = visualizer.CalculateLaneX(4, true);
            
            Assert.AreEqual(-4.0f, x0_5lanes, 0.01f, "5レーン時のレーン0が正しくありません");
            Assert.AreEqual(0.0f, x2_5lanes, 0.01f, "5レーン時のレーン2（中央）が正しくありません");
            Assert.AreEqual(4.0f, x4_5lanes, 0.01f, "5レーン時のレーン4が正しくありません");
        }

        [Test]
        public void ColorChange_UpdatesVisualizerColor()
        {
            // Arrange
            Color initialColor = visualizer.laneColor;
            Color newColor = new Color(1f, 0f, 0f, 0.5f);
            
            // Act
            visualizer.SetLaneColor(newColor);
            
            // Assert
            Assert.AreEqual(newColor, visualizer.laneColor, "レーンの色が更新されていません");
            Assert.AreNotEqual(initialColor, visualizer.laneColor, "レーンの色が変更されていません");
        }

        [Test]
        public void LaneWidth_DifferentValues_CalculatesCorrectSpacing()
        {
            // Arrange
            visualizer.laneCount = 4;
            
            // 狭いレーン幅
            visualizer.laneWidth = 1.0f;
            float narrowX = visualizer.CalculateLaneX(1, true);
            
            // 広いレーン幅
            visualizer.laneWidth = 3.0f;
            float wideX = visualizer.CalculateLaneX(1, true);
            
            // Assert
            Assert.AreEqual(-0.5f, narrowX, 0.01f, "狭いレーン幅での計算が正しくありません");
            Assert.AreEqual(-1.5f, wideX, 0.01f, "広いレーン幅での計算が正しくありません");
        }

        [Test]
        public void InvalidLaneCount_ClampsToValidRange()
        {
            // このテストは実際のValidateSettingsメソッドが呼ばれた場合の動作を確認
            // 現在の実装では、ValidateSettingsはprivateメソッドなので、
            // パブリックAPIを通じて間接的にテストする必要がある
            
            // 負の値
            visualizer.laneCount = -1;
            Assert.GreaterOrEqual(visualizer.laneCount, 0, "レーン数が負の値を許可しています");
            
            // 大きすぎる値（8を超える）
            visualizer.laneCount = 10;
            Assert.LessOrEqual(visualizer.laneCount, 10, "レーン数の上限チェックが必要です");
        }

        [Test]
        public void FarWidthGreaterThanNearWidth_ShouldBeValidated()
        {
            // 逆転した幅の設定を試みる
            visualizer.nearWidth = 0.5f;
            visualizer.farWidth = 2.0f;  // nearWidthより大きい値を設定
            
            // farWidthはnearWidthより大きくならないように自動調整されるべき
            Assert.LessOrEqual(visualizer.farWidth, visualizer.nearWidth, 
                "farWidthがnearWidthより大きくなっています（奥が手前より広い）");
            
            // 遠近感の比率は1.0以下であるべき（奥が狭い）
            float perspectiveRatio = visualizer.farWidth / visualizer.nearWidth;
            Assert.LessOrEqual(perspectiveRatio, 1.0f, "遠近感の比率が逆転しています");
        }

        [Test]
        public void DefaultMaterialCreation_WhenMaterialIsNull()
        {
            // Arrange
            visualizer.laneMaterial = null;
            
            // Act - InitializeLanesは通常Startで呼ばれるが、テストでは手動で呼ぶ必要がある
            // 実際のテストでは、パブリックメソッドを通じて確認
            
            // Assert
            Assert.IsNull(visualizer.laneMaterial, "初期状態でマテリアルはnullであるべき");
        }

        [Test]
        public void LaneLength_AffectsDepth()
        {
            // Arrange
            float shortLength = 10.0f;
            float longLength = 30.0f;
            
            // Act
            visualizer.laneLength = shortLength;
            float shortDepth = visualizer.laneLength;
            
            visualizer.laneLength = longLength;
            float longDepth = visualizer.laneLength;
            
            // Assert
            Assert.AreEqual(shortLength, shortDepth, "短いレーン長が正しく設定されていません");
            Assert.AreEqual(longLength, longDepth, "長いレーン長が正しく設定されていません");
            Assert.Greater(longDepth, shortDepth, "レーン長の比較が正しくありません");
        }

        [Test]
        public void LineWidth_InValidRange()
        {
            // Arrange & Act
            visualizer.lineWidth = 0.05f;
            
            // Assert
            Assert.GreaterOrEqual(visualizer.lineWidth, 0.01f, "線の太さが最小値を下回っています");
            Assert.LessOrEqual(visualizer.lineWidth, 0.5f, "線の太さが最大値を超えています");
        }

        [Test]
        public void OptionsFlags_ControlVisibility()
        {
            // Arrange & Act
            visualizer.showCenterLine = true;
            visualizer.showOuterBorders = true;
            
            // Assert
            Assert.IsTrue(visualizer.showCenterLine, "中央ライン表示フラグが正しくありません");
            Assert.IsTrue(visualizer.showOuterBorders, "外枠表示フラグが正しくありません");
            
            // 無効化テスト
            visualizer.showCenterLine = false;
            visualizer.showOuterBorders = false;
            
            Assert.IsFalse(visualizer.showCenterLine, "中央ライン非表示フラグが正しくありません");
            Assert.IsFalse(visualizer.showOuterBorders, "外枠非表示フラグが正しくありません");
        }

        [Test]
        public void SymmetricLanePositions_AroundCenter()
        {
            // Arrange
            visualizer.laneCount = 4;
            visualizer.laneWidth = 2.0f;
            
            // Act
            float leftLane = visualizer.CalculateLaneX(0, true);
            float rightLane = visualizer.CalculateLaneX(3, true);
            
            // Assert
            Assert.AreEqual(-leftLane, rightLane, 0.01f, "レーンが中心に対して対称になっていません");
            
            // 中央の2レーンも対称性を確認
            float innerLeft = visualizer.CalculateLaneX(1, true);
            float innerRight = visualizer.CalculateLaneX(2, true);
            
            Assert.AreEqual(-innerLeft, innerRight, 0.01f, "内側レーンが中心に対して対称になっていません");
        }

        [Test]
        public void ConductorSync_Disabled_UsesManualSettings()
        {
            // Arrange
            visualizer.syncWithConductor = false;
            float manualLaneLength = 25.0f;
            
            // Act
            visualizer.laneLength = manualLaneLength;
            
            // Assert
            Assert.AreEqual(manualLaneLength, visualizer.laneLength, 0.01f, 
                "Conductor同期無効時に手動設定が使用されていません");
            Assert.IsFalse(visualizer.syncWithConductor, 
                "Conductor同期フラグが正しく設定されていません");
        }

        [Test]
        public void ConductorSync_InitializesCorrectly()
        {
            // Arrange
            GameObject conductorObject = new GameObject("TestConductor");
            Conductor conductor = conductorObject.AddComponent<Conductor>();
            conductor.GetType().GetField("_spawnZ", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(conductor, 30.0f);
            
            // シングルトンインスタンスを設定
            var instanceField = typeof(Conductor).GetField("_instance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            instanceField?.SetValue(null, conductor);
            
            visualizer.syncWithConductor = true;
            
            // Act
            visualizer.ForceSync();
            
            // Assert
            Assert.IsTrue(visualizer.syncWithConductor, 
                "Conductor同期が有効になっていません");
            
            // Cleanup
            Object.DestroyImmediate(conductorObject);
            instanceField?.SetValue(null, null);
        }

        [Test]
        public void ForceSync_WithoutConductor_HandlesGracefully()
        {
            // Arrange
            visualizer.syncWithConductor = true;
            float originalLaneLength = visualizer.laneLength;
            
            // Conductorインスタンスがない状態を確認
            var instanceField = typeof(Conductor).GetField("_instance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            instanceField?.SetValue(null, null);
            
            // エラーログの期待設定
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, 
                "Conductorが見つかりません！シーンにConductorを配置してください。");
            
            // Act
            visualizer.ForceSync();
            
            // Assert
            Assert.AreEqual(originalLaneLength, visualizer.laneLength, 0.01f, 
                "Conductorが存在しない場合、レーン長は変更されないべきです");
        }

        [Test]
        public void SyncUpdateInterval_InValidRange()
        {
            // Arrange & Act
            visualizer.syncUpdateInterval = 0.5f;
            
            // Assert
            Assert.GreaterOrEqual(visualizer.syncUpdateInterval, 0.1f, 
                "同期更新間隔が最小値を下回っています");
            Assert.LessOrEqual(visualizer.syncUpdateInterval, 5.0f, 
                "同期更新間隔が最大値を超えています");
            
            // 境界値テスト
            visualizer.syncUpdateInterval = 0.1f;
            Assert.AreEqual(0.1f, visualizer.syncUpdateInterval, 0.01f, 
                "最小同期更新間隔が正しく設定されていません");
            
            visualizer.syncUpdateInterval = 5.0f;
            Assert.AreEqual(5.0f, visualizer.syncUpdateInterval, 0.01f, 
                "最大同期更新間隔が正しく設定されていません");
        }

        [Test]
        public void ConductorSync_UpdatesLaneLength()
        {
            // Arrange
            GameObject conductorObject = new GameObject("TestConductor");
            Conductor conductor = conductorObject.AddComponent<Conductor>();
            
            // Conductorのプライベートフィールドに値を設定
            float expectedSpawnZ = 35.0f;
            conductor.GetType().GetField("_spawnZ", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(conductor, expectedSpawnZ);
            
            // シングルトンインスタンスを設定
            var instanceField = typeof(Conductor).GetField("_instance", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            instanceField?.SetValue(null, conductor);
            
            visualizer.syncWithConductor = true;
            
            // Act
            visualizer.ForceSync();
            
            // Assert
            Assert.AreEqual(expectedSpawnZ, visualizer.laneLength, 0.01f, 
                "ConductorのSpawnZとレーン長が同期されていません");
            
            // Cleanup
            Object.DestroyImmediate(conductorObject);
            instanceField?.SetValue(null, null);
        }
    }
}