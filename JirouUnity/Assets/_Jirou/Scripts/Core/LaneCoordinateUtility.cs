using UnityEngine;

namespace Jirou.Core
{
    /// <summary>
    /// レーン座標計算用ユーティリティクラス
    /// </summary>
    public static class LaneCoordinateUtility
    {
        /// <summary>
        /// レーンインデックスからワールドX座標を取得
        /// </summary>
        public static float GetLaneWorldX(int laneIndex, Conductor conductor)
        {
            if (conductor == null) 
            {
                Debug.LogError("Conductorが見つかりません");
                return 0f;
            }
            
            return conductor.GetLaneX(laneIndex);
        }
        
        /// <summary>
        /// 遠近感を考慮したX座標を計算
        /// </summary>
        public static float GetPerspectiveX(float baseX, float z, float nearWidth, float farWidth, float spawnZ)
        {
            // Z座標に基づいて幅をリニア補間
            float t = z / spawnZ; // 0（手前）から1（奥）
            float widthScale = Mathf.Lerp(nearWidth, farWidth, t);
            return baseX * widthScale;
        }
        
        /// <summary>
        /// ビート時間からワールドZ座標を計算
        /// </summary>
        public static float GetWorldZFromBeat(float beat, Conductor conductor)
        {
            if (conductor == null) return 0f;
            
            float noteSpeed = conductor.NoteSpeed;
            float spawnZ = conductor.SpawnZ;
            
            return spawnZ - (beat * noteSpeed);
        }
    }
}