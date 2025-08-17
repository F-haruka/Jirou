using UnityEngine;
using Jirou.Core;

namespace Jirou.Editor.Import
{
    /// <summary>
    /// 譜面データ変換ユーティリティ
    /// </summary>
    public static class ChartConversionUtility
    {
        /// <summary>
        /// ミリ秒オフセットをビート単位に変換
        /// </summary>
        public static float ConvertMillisecondsToBeats(int offsetMs, float bpm)
        {
            if (bpm <= 0) return 0f;
            
            float offsetSeconds = offsetMs / 1000f;
            float beatDuration = 60f / bpm;
            return offsetSeconds / beatDuration;
        }
        
        /// <summary>
        /// LPB単位のタイミングをビート単位に変換
        /// </summary>
        public static float ConvertLPBToBeats(int num, int lpb)
        {
            if (lpb <= 0) return 0f;
            return (float)num / lpb;
        }
        
        /// <summary>
        /// ノーツタイプを変換
        /// </summary>
        public static NoteType ConvertNoteType(int type)
        {
            return type == 2 ? NoteType.Hold : NoteType.Tap;
        }
        
        /// <summary>
        /// Holdノーツの継続時間を計算
        /// </summary>
        public static float CalculateHoldDuration(NotesEditorNote startNote)
        {
            // 現在のJSONフォーマットではHoldノーツの終端位置が含まれていないため
            // デフォルトの長さを返す
            if (startNote.type != 2)
            {
                return 0f;
            }
            
            // Holdノーツのデフォルト長さ（1ビート）
            return 1.0f;
        }
    }
}