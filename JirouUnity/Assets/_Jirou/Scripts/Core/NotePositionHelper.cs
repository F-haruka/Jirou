using UnityEngine;

namespace Jirou.Core
{
    /// <summary>
    /// ノーツの3D位置を表す構造体
    /// </summary>
    [System.Serializable]
    public struct NotePosition3D
    {
        public float x;  // レーン位置
        public float y;  // 高さ
        public float z;  // 奥行き位置
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NotePosition3D(int laneIndex, float zPosition, float yPosition = 0.5f)
        {
            if (laneIndex >= 0 && laneIndex < NoteData.LaneXPositions.Length)
            {
                x = NoteData.LaneXPositions[laneIndex];
            }
            else
            {
                x = 0f;
                Debug.LogWarning($"無効なレーンインデックス: {laneIndex}");
            }
            
            y = yPosition;
            z = zPosition;
        }
        
        /// <summary>
        /// Unity Vector3への変換
        /// </summary>
        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
        
        /// <summary>
        /// 判定ラインまでの距離を取得
        /// </summary>
        public float GetDistanceToJudgmentLine(float judgmentZ = 0f)
        {
            return Mathf.Abs(z - judgmentZ);
        }
    }
    
    /// <summary>
    /// ノーツの視覚計算ヘルパー
    /// </summary>
    public static class NoteVisualCalculator
    {
        /// <summary>
        /// 距離に基づくスケール計算
        /// </summary>
        public static float CalculateScaleByDistance(float currentZ, float spawnZ, float baseScale = 1.0f)
        {
            if (spawnZ <= 0) return baseScale;
            
            // 奥（spawnZ）で0.5倍、手前（0）で1.5倍にスケーリング
            float distanceRatio = Mathf.Clamp01(currentZ / spawnZ);
            float scaleFactor = Mathf.Lerp(1.5f, 0.5f, distanceRatio);
            
            return baseScale * scaleFactor;
        }
        
        /// <summary>
        /// 距離に基づく透明度計算（フェードイン効果）
        /// </summary>
        public static float CalculateAlphaByDistance(float currentZ, float spawnZ, float fadeStartRatio = 0.8f)
        {
            if (spawnZ <= 0) return 1f;
            
            float fadeStartZ = spawnZ * fadeStartRatio;
            
            if (currentZ > fadeStartZ)
            {
                float fadeRatio = (currentZ - fadeStartZ) / (spawnZ - fadeStartZ);
                return 1f - fadeRatio;
            }
            
            return 1f;
        }
        
        /// <summary>
        /// ノーツのワールド座標を計算
        /// </summary>
        public static Vector3 CalculateNoteWorldPosition(NoteData noteData, float currentBeat, Conductor conductor)
        {
            if (conductor == null)
            {
                Debug.LogError("Conductorが設定されていません");
                return Vector3.zero;
            }
            
            float zPosition = conductor.GetNoteZPosition(noteData.timeToHit);
            var position = new NotePosition3D(noteData.laneIndex, zPosition);
            
            return position.ToVector3();
        }
        
        /// <summary>
        /// Holdノーツの終端位置を計算
        /// </summary>
        public static Vector3 CalculateHoldEndPosition(NoteData noteData, Conductor conductor)
        {
            if (noteData.noteType != NoteType.Hold)
            {
                Debug.LogWarning("HoldノーツではないためCalculateHoldEndPositionをスキップ");
                return Vector3.zero;
            }
            
            if (conductor == null)
            {
                Debug.LogError("Conductorが設定されていません");
                return Vector3.zero;
            }
            
            float endBeat = noteData.timeToHit + noteData.holdDuration;
            float zPosition = conductor.GetNoteZPosition(endBeat);
            var position = new NotePosition3D(noteData.laneIndex, zPosition);
            
            return position.ToVector3();
        }
    }
}