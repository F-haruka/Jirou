using UnityEngine;
using Jirou.Core;

namespace Jirou.Tests.EditMode
{
    /// <summary>
    /// エディタモードテスト用のヘルパークラス
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// テスト用のConductor設定を作成
        /// </summary>
        public static ConductorTestSettings CreateDefaultConductorSettings()
        {
            return new ConductorTestSettings
            {
                songBpm = 120f,
                firstBeatOffset = 0f,
                noteSpeed = 10f,
                spawnZ = 20f,
                hitZ = 0f
            };
        }
        
        /// <summary>
        /// テスト用のBPM値セットを取得
        /// </summary>
        public static float[] GetTestBPMValues()
        {
            return new float[] { 60f, 90f, 120f, 140f, 180f, 200f, 240f, 300f };
        }
        
        /// <summary>
        /// 判定精度のテスト用タイミングウィンドウを取得
        /// </summary>
        public static JudgmentWindows GetDefaultJudgmentWindows()
        {
            return new JudgmentWindows
            {
                perfectWindow = 0.05f,  // ±50ms
                greatWindow = 0.1f,     // ±100ms
                goodWindow = 0.15f,     // ±150ms
                missWindow = 0.2f       // ±200ms
            };
        }
        
        /// <summary>
        /// 4レーンのX座標配置を取得
        /// </summary>
        public static float[] GetLanePositions()
        {
            return new float[] { -3f, -1f, 1f, 3f };
        }
        
        /// <summary>
        /// ビートからZ座標を計算（Conductorのロジックを再現）
        /// </summary>
        public static float CalculateNoteZPosition(float currentBeat, float noteBeat, float spawnZ, float noteSpeed)
        {
            float beatsPassed = currentBeat - noteBeat;
            return spawnZ - (beatsPassed * noteSpeed);
        }
        
        /// <summary>
        /// BPMから1ビートあたりの秒数を計算
        /// </summary>
        public static float CalculateSecondsPerBeat(float bpm)
        {
            if (bpm <= 0) return 0f;
            return 60f / bpm;
        }
        
        /// <summary>
        /// 判定タイミングの精度を判定
        /// </summary>
        public static JudgmentType GetJudgmentType(float timingDifference, JudgmentWindows windows)
        {
            float absDiff = Mathf.Abs(timingDifference);
            
            if (absDiff <= windows.perfectWindow)
                return JudgmentType.Perfect;
            if (absDiff <= windows.greatWindow)
                return JudgmentType.Great;
            if (absDiff <= windows.goodWindow)
                return JudgmentType.Good;
            if (absDiff <= windows.missWindow)
                return JudgmentType.Miss;
                
            return JudgmentType.Ignore;
        }
        
        /// <summary>
        /// テスト用のノートデータを生成
        /// </summary>
        public static NoteTestData CreateTestNote(int laneIndex, float beat, Core.NoteType type = Core.NoteType.Tap)
        {
            float[] lanes = GetLanePositions();
            if (laneIndex < 0 || laneIndex >= lanes.Length)
            {
                laneIndex = 0;
            }
            
            return new NoteTestData
            {
                laneIndex = laneIndex,
                xPosition = lanes[laneIndex],
                beat = beat,
                noteType = type,
                holdDuration = type == Core.NoteType.Hold ? 1f : 0f
            };
        }
        
        /// <summary>
        /// テスト用の譜面データを生成
        /// </summary>
        public static NoteTestData[] CreateTestChart()
        {
            return new NoteTestData[]
            {
                CreateTestNote(0, 1f, Core.NoteType.Tap),
                CreateTestNote(1, 2f, Core.NoteType.Tap),
                CreateTestNote(2, 3f, Core.NoteType.Hold),
                CreateTestNote(3, 4f, Core.NoteType.Tap),
                CreateTestNote(0, 5f, Core.NoteType.Tap),
                CreateTestNote(2, 6f, Core.NoteType.Hold),
                CreateTestNote(1, 7f, Core.NoteType.Tap),
                CreateTestNote(3, 8f, Core.NoteType.Tap)
            };
        }
    }
    
    /// <summary>
    /// Conductor設定のテスト用構造体
    /// </summary>
    public struct ConductorTestSettings
    {
        public float songBpm;
        public float firstBeatOffset;
        public float noteSpeed;
        public float spawnZ;
        public float hitZ;
    }
    
    /// <summary>
    /// 判定ウィンドウの設定
    /// </summary>
    public struct JudgmentWindows
    {
        public float perfectWindow;
        public float greatWindow;
        public float goodWindow;
        public float missWindow;
    }
    
    /// <summary>
    /// 判定タイプ
    /// </summary>
    public enum JudgmentType
    {
        Perfect,
        Great,
        Good,
        Miss,
        Ignore
    }
    
    // NoteTypeはCore名前空間のものを使用するため、ここでは削除
    
    /// <summary>
    /// テスト用ノートデータ
    /// </summary>
    public struct NoteTestData
    {
        public int laneIndex;
        public float xPosition;
        public float beat;
        public Core.NoteType noteType;
        public float holdDuration;
    }
}