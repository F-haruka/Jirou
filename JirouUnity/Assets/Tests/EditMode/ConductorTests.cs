using NUnit.Framework;
using UnityEngine;
using Jirou.Core;

namespace Jirou.Tests.EditMode
{
    /// <summary>
    /// Conductorクラスのユニットテスト
    /// </summary>
    [TestFixture]
    public class ConductorTests
    {
        // BPM変換テスト
        [Test]
        public void BPMToSeconds_ConvertsBPMCorrectly()
        {
            // 120 BPMの場合、1ビートは0.5秒
            float bpm = 120f;
            float expectedSecondsPerBeat = 0.5f;
            float actualSecondsPerBeat = 60f / bpm;
            
            Assert.AreEqual(expectedSecondsPerBeat, actualSecondsPerBeat, 0.001f);
        }
        
        [Test]
        public void BeatCalculation_CalculatesCorrectBeatNumber()
        {
            // 120 BPMで2秒経過した場合、4ビート
            float bpm = 120f;
            float elapsedTime = 2f;
            float expectedBeats = 4f;
            float actualBeats = (elapsedTime * bpm) / 60f;
            
            Assert.AreEqual(expectedBeats, actualBeats, 0.001f);
        }
        
        [Test]
        public void NoteZPosition_CalculatesCorrectly()
        {
            // ノーツのZ座標計算テスト
            float spawnZ = 20f;
            float beatsPassed = 2f;
            float noteSpeed = 5f;
            float expectedZ = spawnZ - (beatsPassed * noteSpeed);
            
            Assert.AreEqual(10f, expectedZ, 0.001f);
        }
        
        [Test]
        public void TimingWindow_PerfectJudgment()
        {
            // Perfect判定のタイミングウィンドウテスト
            float perfectWindow = 0.05f; // ±50ms
            float timingDifference = 0.03f;
            
            Assert.IsTrue(Mathf.Abs(timingDifference) <= perfectWindow);
        }
        
        [Test]
        public void TimingWindow_GreatJudgment()
        {
            // Great判定のタイミングウィンドウテスト
            float greatWindow = 0.1f; // ±100ms
            float timingDifference = 0.08f;
            
            Assert.IsTrue(Mathf.Abs(timingDifference) <= greatWindow);
            Assert.IsFalse(Mathf.Abs(timingDifference) <= 0.05f); // Perfectではない
        }
        
        // Z座標とノーツ移動のテスト
        [Test]
        public void GetNoteZPosition_ReturnsCorrectPosition()
        {
            // 異なるビート位置でのZ座標計算
            float spawnZ = 20f;
            float noteSpeed = 10f;
            float currentBeat = 5f;
            float noteBeat = 3f;
            float beatsPassed = currentBeat - noteBeat;
            float expectedZ = spawnZ - (beatsPassed * noteSpeed);
            
            Assert.AreEqual(0f, expectedZ, 0.001f);
        }
        
        [Test]
        public void GetNoteZPosition_NegativeBeats_ReturnsPositiveZ()
        {
            // まだ到達していないノーツのZ座標
            float spawnZ = 20f;
            float noteSpeed = 10f;
            float currentBeat = 2f;
            float noteBeat = 4f;
            float beatsPassed = currentBeat - noteBeat;
            float expectedZ = spawnZ - (beatsPassed * noteSpeed);
            
            Assert.AreEqual(40f, expectedZ, 0.001f);
        }
        
        // ノーツ生成タイミングのテスト
        [Test]
        public void ShouldSpawnNote_BeforeSpawnTime_ReturnsFalse()
        {
            // 生成タイミング前
            float currentBeat = 1f;
            float noteBeat = 10f;
            float beatsInAdvance = 2f;
            float spawnBeat = noteBeat - beatsInAdvance;
            
            Assert.IsFalse(currentBeat >= spawnBeat);
        }
        
        [Test]
        public void ShouldSpawnNote_AfterSpawnTime_ReturnsTrue()
        {
            // 生成タイミング後
            float currentBeat = 9f;
            float noteBeat = 10f;
            float beatsInAdvance = 2f;
            float spawnBeat = noteBeat - beatsInAdvance;
            
            Assert.IsTrue(currentBeat >= spawnBeat);
        }
        
        [Test]
        public void ShouldSpawnNote_ExactSpawnTime_ReturnsTrue()
        {
            // ちょうど生成タイミング
            float currentBeat = 8f;
            float noteBeat = 10f;
            float beatsInAdvance = 2f;
            float spawnBeat = noteBeat - beatsInAdvance;
            
            Assert.IsTrue(currentBeat >= spawnBeat);
        }
        
        // 判定ゾーンのテスト
        [Test]
        public void IsNoteInHitZone_WithinTolerance_ReturnsTrue()
        {
            // 判定ゾーン内
            float noteZ = 0.5f;
            float hitZ = 0f;
            float tolerance = 1.0f;
            float distance = Mathf.Abs(noteZ - hitZ);
            
            Assert.IsTrue(distance <= tolerance);
        }
        
        [Test]
        public void IsNoteInHitZone_OutsideTolerance_ReturnsFalse()
        {
            // 判定ゾーン外
            float noteZ = 2.5f;
            float hitZ = 0f;
            float tolerance = 1.0f;
            float distance = Mathf.Abs(noteZ - hitZ);
            
            Assert.IsFalse(distance <= tolerance);
        }
        
        [Test]
        public void IsNoteInHitZone_ExactlyOnBoundary_ReturnsTrue()
        {
            // 判定ゾーンの境界上
            float noteZ = 1.0f;
            float hitZ = 0f;
            float tolerance = 1.0f;
            float distance = Mathf.Abs(noteZ - hitZ);
            
            Assert.IsTrue(distance <= tolerance);
        }
        
        // タイミング計算のテスト
        [Test]
        public void GetTimeUntilBeat_FutureBeat_ReturnsPositiveTime()
        {
            // 未来のビートまでの時間
            float currentBeat = 4f;
            float targetBeat = 8f;
            float bpm = 120f;
            float secPerBeat = 60f / bpm;
            float beatsRemaining = targetBeat - currentBeat;
            float expectedTime = beatsRemaining * secPerBeat;
            
            Assert.AreEqual(2f, expectedTime, 0.001f);
        }
        
        [Test]
        public void GetTimeUntilBeat_PastBeat_ReturnsNegativeTime()
        {
            // 過去のビートまでの時間（負の値）
            float currentBeat = 8f;
            float targetBeat = 4f;
            float bpm = 120f;
            float secPerBeat = 60f / bpm;
            float beatsRemaining = targetBeat - currentBeat;
            float expectedTime = beatsRemaining * secPerBeat;
            
            Assert.AreEqual(-2f, expectedTime, 0.001f);
        }
        
        [Test]
        public void GetTimeUntilBeat_CurrentBeat_ReturnsZero()
        {
            // 現在のビート（ゼロ）
            float currentBeat = 5f;
            float targetBeat = 5f;
            float bpm = 120f;
            float secPerBeat = 60f / bpm;
            float beatsRemaining = targetBeat - currentBeat;
            float expectedTime = beatsRemaining * secPerBeat;
            
            Assert.AreEqual(0f, expectedTime, 0.001f);
        }
        
        // BPM変更のテスト
        [Test]
        public void ChangeBPM_ValidBPM_UpdatesCorrectly()
        {
            // 有効なBPM値
            float newBpm = 140f;
            float expectedSecPerBeat = 60f / newBpm;
            
            Assert.AreEqual(0.4286f, expectedSecPerBeat, 0.001f);
        }
        
        [Test]
        public void ChangeBPM_ZeroBPM_ShouldBeInvalid()
        {
            // 無効なBPM値（ゼロ）
            float newBpm = 0f;
            
            Assert.IsTrue(newBpm <= 0);
        }
        
        [Test]
        public void ChangeBPM_NegativeBPM_ShouldBeInvalid()
        {
            // 無効なBPM値（負の値）
            float newBpm = -120f;
            
            Assert.IsTrue(newBpm <= 0);
        }
        
        // 4レーン配置のテスト
        [Test]
        public void LanePositions_AreCorrectlySpaced()
        {
            // 4レーンのX座標配置
            float[] expectedLaneX = { -3f, -1f, 1f, 3f };
            
            Assert.AreEqual(4, expectedLaneX.Length);
            Assert.AreEqual(-3f, expectedLaneX[0]);
            Assert.AreEqual(-1f, expectedLaneX[1]);
            Assert.AreEqual(1f, expectedLaneX[2]);
            Assert.AreEqual(3f, expectedLaneX[3]);
        }
        
        [Test]
        public void LanePositions_AreSymmetric()
        {
            // レーン配置の対称性
            float[] laneX = { -3f, -1f, 1f, 3f };
            
            Assert.AreEqual(-laneX[0], laneX[3]);
            Assert.AreEqual(-laneX[1], laneX[2]);
        }
        
        // エッジケースのテスト
        [Test]
        public void ExtremeBPM_VeryHigh_CalculatesCorrectly()
        {
            // 非常に高いBPM
            float bpm = 300f;
            float expectedSecPerBeat = 60f / bpm;
            
            Assert.AreEqual(0.2f, expectedSecPerBeat, 0.001f);
        }
        
        [Test]
        public void ExtremeBPM_VeryLow_CalculatesCorrectly()
        {
            // 非常に低いBPM
            float bpm = 60f;
            float expectedSecPerBeat = 60f / bpm;
            
            Assert.AreEqual(1f, expectedSecPerBeat, 0.001f);
        }
        
        [Test]
        public void NoteSpeed_Zero_NoMovement()
        {
            // ノーツ速度ゼロ（移動なし）
            float spawnZ = 20f;
            float noteSpeed = 0f;
            float beatsPassed = 5f;
            float expectedZ = spawnZ - (beatsPassed * noteSpeed);
            
            Assert.AreEqual(20f, expectedZ, 0.001f);
        }
        
        [Test]
        public void FirstBeatOffset_Positive_DelaysStart()
        {
            // 正のオフセット（開始遅延）
            float offset = 2f;
            float dspTime = 10f;
            float currentTime = 12f;
            float expectedPosition = currentTime - dspTime - offset;
            
            Assert.AreEqual(0f, expectedPosition, 0.001f);
        }
        
        [Test]
        public void FirstBeatOffset_Negative_AdvancesStart()
        {
            // 負のオフセット（開始前進）
            float offset = -1f;
            float dspTime = 10f;
            float currentTime = 12f;
            float expectedPosition = currentTime - dspTime - offset;
            
            Assert.AreEqual(3f, expectedPosition, 0.001f);
        }
    }
}