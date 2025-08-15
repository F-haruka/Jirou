using NUnit.Framework;
using UnityEngine;
using Jirou.Core;

namespace Jirou.Tests.EditMode
{
    /// <summary>
    /// Conductorクラスの高度なテスト
    /// </summary>
    [TestFixture]
    public class AdvancedConductorTests
    {
        private ConductorTestSettings defaultSettings;
        private JudgmentWindows judgmentWindows;
        
        [SetUp]
        public void Setup()
        {
            defaultSettings = TestHelpers.CreateDefaultConductorSettings();
            judgmentWindows = TestHelpers.GetDefaultJudgmentWindows();
        }
        
        // 複数BPM値での計算テスト
        [Test]
        public void MultipleBPM_CalculationsAreCorrect()
        {
            float[] testBPMs = TestHelpers.GetTestBPMValues();
            
            foreach (float bpm in testBPMs)
            {
                float secPerBeat = TestHelpers.CalculateSecondsPerBeat(bpm);
                float expectedSec = 60f / bpm;
                
                Assert.AreEqual(expectedSec, secPerBeat, 0.001f, 
                    $"BPM {bpm} の計算が正しくありません");
            }
        }
        
        // 譜面全体のテスト
        [Test]
        public void ChartData_AllNotesHaveValidPositions()
        {
            var chart = TestHelpers.CreateTestChart();
            float[] validLanes = TestHelpers.GetLanePositions();
            
            foreach (var note in chart)
            {
                // レーンインデックスが有効範囲内
                Assert.GreaterOrEqual(note.laneIndex, 0);
                Assert.Less(note.laneIndex, validLanes.Length);
                
                // X座標が正しい
                Assert.AreEqual(validLanes[note.laneIndex], note.xPosition);
                
                // ビートが正の値
                Assert.Greater(note.beat, 0f);
                
                // Holdノーツは持続時間を持つ
                if (note.noteType == NoteType.Hold)
                {
                    Assert.Greater(note.holdDuration, 0f);
                }
            }
        }
        
        // ノーツ移動シミュレーション
        [Test]
        public void NoteMovement_SimulationOverTime()
        {
            float spawnZ = defaultSettings.spawnZ;
            float noteSpeed = defaultSettings.noteSpeed;
            float noteBeat = 10f;
            
            // 時間経過によるZ座標の変化をシミュレート
            for (float currentBeat = 0f; currentBeat <= 15f; currentBeat += 0.5f)
            {
                float z = TestHelpers.CalculateNoteZPosition(currentBeat, noteBeat, spawnZ, noteSpeed);
                
                if (currentBeat < noteBeat)
                {
                    // ノーツビート前：Z座標は正の値（奥側）
                    Assert.Greater(z, 0f, $"Beat {currentBeat}: ノーツはまだ奥にあるべき");
                }
                else if (currentBeat == noteBeat)
                {
                    // ノーツビート時：判定ライン付近
                    float expectedZ = spawnZ - (0f * noteSpeed);
                    Assert.AreEqual(expectedZ, z, 0.001f);
                }
                else
                {
                    // ノーツビート後：Z座標は負の値（手前側）
                    Assert.Less(z, spawnZ, $"Beat {currentBeat}: ノーツは手前に移動しているべき");
                }
            }
        }
        
        // 判定精度の総合テスト
        [Test]
        public void JudgmentAccuracy_AllTypesWork()
        {
            // Perfect判定
            float perfectTiming = 0.03f;
            var perfectJudgment = TestHelpers.GetJudgmentType(perfectTiming, judgmentWindows);
            Assert.AreEqual(JudgmentType.Perfect, perfectJudgment);
            
            // Great判定
            float greatTiming = 0.08f;
            var greatJudgment = TestHelpers.GetJudgmentType(greatTiming, judgmentWindows);
            Assert.AreEqual(JudgmentType.Great, greatJudgment);
            
            // Good判定
            float goodTiming = 0.13f;
            var goodJudgment = TestHelpers.GetJudgmentType(goodTiming, judgmentWindows);
            Assert.AreEqual(JudgmentType.Good, goodJudgment);
            
            // Miss判定
            float missTiming = 0.18f;
            var missJudgment = TestHelpers.GetJudgmentType(missTiming, judgmentWindows);
            Assert.AreEqual(JudgmentType.Miss, missJudgment);
            
            // Ignore（範囲外）
            float ignoreTiming = 0.25f;
            var ignoreJudgment = TestHelpers.GetJudgmentType(ignoreTiming, judgmentWindows);
            Assert.AreEqual(JudgmentType.Ignore, ignoreJudgment);
        }
        
        // 負のタイミングでの判定テスト
        [Test]
        public void JudgmentAccuracy_NegativeTiming()
        {
            // 負の値でも絶対値で判定される
            float negativeTiming = -0.04f;
            var judgment = TestHelpers.GetJudgmentType(negativeTiming, judgmentWindows);
            Assert.AreEqual(JudgmentType.Perfect, judgment);
            
            negativeTiming = -0.12f;
            judgment = TestHelpers.GetJudgmentType(negativeTiming, judgmentWindows);
            Assert.AreEqual(JudgmentType.Good, judgment);
        }
        
        // レーン対称性の検証
        [Test]
        public void LaneSymmetry_Validation()
        {
            float[] lanes = TestHelpers.GetLanePositions();
            
            // 4レーンあることを確認
            Assert.AreEqual(4, lanes.Length);
            
            // 中心からの対称性を検証
            Assert.AreEqual(-lanes[0], lanes[3], 0.001f, "外側レーンが対称でない");
            Assert.AreEqual(-lanes[1], lanes[2], 0.001f, "内側レーンが対称でない");
            
            // レーン間隔の一貫性
            float outerSpacing = lanes[1] - lanes[0];
            float innerSpacing = lanes[2] - lanes[1];
            float outerSpacing2 = lanes[3] - lanes[2];
            
            Assert.AreEqual(outerSpacing, innerSpacing, 0.001f, "レーン間隔が一定でない");
            Assert.AreEqual(innerSpacing, outerSpacing2, 0.001f, "レーン間隔が一定でない");
        }
        
        // スポーン計算の境界値テスト
        [Test]
        public void SpawnCalculation_BoundaryValues()
        {
            float noteBeat = 10f;
            float beatsInAdvance = 2f;
            float spawnBeat = noteBeat - beatsInAdvance;
            
            // 境界値のテスト
            Assert.IsFalse(7.99f >= spawnBeat, "スポーン直前");
            Assert.IsTrue(8.00f >= spawnBeat, "スポーンタイミング");
            Assert.IsTrue(8.01f >= spawnBeat, "スポーン直後");
        }
        
        // BPM変更時の計算精度
        [Test]
        public void BPMChange_MidSong_Calculations()
        {
            // 初期BPM
            float initialBpm = 120f;
            float initialSecPerBeat = TestHelpers.CalculateSecondsPerBeat(initialBpm);
            Assert.AreEqual(0.5f, initialSecPerBeat, 0.001f);
            
            // BPM変更
            float newBpm = 140f;
            float newSecPerBeat = TestHelpers.CalculateSecondsPerBeat(newBpm);
            Assert.AreEqual(60f / 140f, newSecPerBeat, 0.001f);
            
            // 変更前後で異なることを確認
            Assert.AreNotEqual(initialSecPerBeat, newSecPerBeat);
        }
        
        // オフセットありの計算テスト
        [Test]
        public void FirstBeatOffset_AffectsTimingCalculations()
        {
            float offset1 = 0f;
            float offset2 = 1f;
            float offset3 = -0.5f;
            
            float dspTime = 10f;
            float currentTime = 15f;
            
            // オフセットなし
            float position1 = currentTime - dspTime - offset1;
            Assert.AreEqual(5f, position1);
            
            // 正のオフセット（遅延）
            float position2 = currentTime - dspTime - offset2;
            Assert.AreEqual(4f, position2);
            
            // 負のオフセット（前進）
            float position3 = currentTime - dspTime - offset3;
            Assert.AreEqual(5.5f, position3);
        }
        
        // ノート速度のバリエーション
        [Test]
        public void NoteSpeed_Variations()
        {
            float[] speeds = { 5f, 10f, 15f, 20f };
            float noteBeat = 5f;
            float currentBeat = 3f;
            float spawnZ = 20f;
            
            foreach (float speed in speeds)
            {
                float z = TestHelpers.CalculateNoteZPosition(currentBeat, noteBeat, spawnZ, speed);
                float expectedZ = spawnZ - ((currentBeat - noteBeat) * speed);
                
                Assert.AreEqual(expectedZ, z, 0.001f, 
                    $"速度 {speed} でのZ座標計算が正しくない");
                
                // 速度が高いほど移動距離が大きい
                if (speed > 5f)
                {
                    float zSlow = TestHelpers.CalculateNoteZPosition(currentBeat, noteBeat, spawnZ, 5f);
                    Assert.Greater(z, zSlow, "高速の方が奥にあるべき");
                }
            }
        }
        
        // Holdノーツの特別な処理
        [Test]
        public void HoldNotes_SpecialHandling()
        {
            var holdNote = TestHelpers.CreateTestNote(1, 5f, NoteType.Hold);
            var tapNote = TestHelpers.CreateTestNote(1, 5f, NoteType.Tap);
            
            // Holdノーツは持続時間を持つ
            Assert.Greater(holdNote.holdDuration, 0f);
            Assert.AreEqual(0f, tapNote.holdDuration);
            
            // 同じレーン、同じビートでも異なるタイプ
            Assert.AreEqual(holdNote.laneIndex, tapNote.laneIndex);
            Assert.AreEqual(holdNote.beat, tapNote.beat);
            Assert.AreNotEqual(holdNote.noteType, tapNote.noteType);
        }
        
        // エッジケース：極端な値
        [Test]
        public void ExtremeBoundaryValues()
        {
            // 非常に高いBPM
            float veryHighBpm = 999f;
            float veryHighSecPerBeat = TestHelpers.CalculateSecondsPerBeat(veryHighBpm);
            Assert.Greater(veryHighSecPerBeat, 0f);
            Assert.Less(veryHighSecPerBeat, 1f);
            
            // 非常に低いBPM
            float veryLowBpm = 1f;
            float veryLowSecPerBeat = TestHelpers.CalculateSecondsPerBeat(veryLowBpm);
            Assert.AreEqual(60f, veryLowSecPerBeat);
            
            // 非常に遠いZ座標
            float farZ = TestHelpers.CalculateNoteZPosition(0f, 100f, 1000f, 10f);
            Assert.AreEqual(2000f, farZ);
            
            // 非常に近いZ座標
            float nearZ = TestHelpers.CalculateNoteZPosition(100f, 0f, 20f, 10f);
            Assert.AreEqual(-980f, nearZ);
        }
    }
}