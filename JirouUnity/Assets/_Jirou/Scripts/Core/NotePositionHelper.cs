using UnityEngine;

namespace Jirou.Core
{
    /// <summary>
    /// ノーツの3D位置を表す構造体
    /// </summary>
    [System.Serializable]
    public struct NotePosition3D
    {
        [SerializeField] private float _x;  // レーン位置
        [SerializeField] private float _y;  // 高さ
        [SerializeField] private float _z;  // 奥行き位置
        
        // プロパティ
        public float X => _x;
        public float Y => _y;
        public float Z => _z;
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NotePosition3D(int laneIndex, float zPosition, float yPosition = 0.5f)
        {
            if (IsValidLaneIndex(laneIndex))
            {
                _x = NoteData.LaneXPositions[laneIndex];
            }
            else
            {
                _x = 0f;
                Debug.LogWarning($"無効なレーンインデックス: {laneIndex}");
            }
            
            _y = yPosition;
            _z = zPosition;
        }
        
        /// <summary>
        /// レーンインデックスの妥当性をチェック
        /// </summary>
        private static bool IsValidLaneIndex(int laneIndex)
        {
            return laneIndex >= 0 && laneIndex < NoteData.LaneXPositions.Length;
        }
        
        /// <summary>
        /// Unity Vector3への変換
        /// </summary>
        public Vector3 ToVector3()
        {
            return new Vector3(_x, _y, _z);
        }
        
        /// <summary>
        /// 判定ラインまでの距離を取得
        /// </summary>
        public float GetDistanceToJudgmentLine(float judgmentZ = 0f)
        {
            return Mathf.Abs(_z - judgmentZ);
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
            float distanceRatio = CalculateDistanceRatio(currentZ, spawnZ);
            float scaleFactor = InterpolateScaleFactor(distanceRatio);
            
            return baseScale * scaleFactor;
        }
        
        /// <summary>
        /// 距離比率を計算
        /// </summary>
        private static float CalculateDistanceRatio(float currentZ, float spawnZ)
        {
            return Mathf.Clamp01(currentZ / spawnZ);
        }
        
        /// <summary>
        /// スケールファクタを補間
        /// </summary>
        private static float InterpolateScaleFactor(float distanceRatio)
        {
            return Mathf.Lerp(1.5f, 0.5f, distanceRatio);
        }
        
        /// <summary>
        /// 距離に基づく透明度計算（フェードイン効果）
        /// </summary>
        public static float CalculateAlphaByDistance(float currentZ, float spawnZ, float fadeStartRatio = 0.8f)
        {
            if (spawnZ <= 0) return 1f;
            
            float fadeStartZ = CalculateFadeStartZ(spawnZ, fadeStartRatio);
            
            if (currentZ > fadeStartZ)
            {
                return CalculateFadeAlpha(currentZ, fadeStartZ, spawnZ);
            }
            
            return 1f;
        }
        
        /// <summary>
        /// フェード開始位置を計算
        /// </summary>
        private static float CalculateFadeStartZ(float spawnZ, float fadeStartRatio)
        {
            return spawnZ * fadeStartRatio;
        }
        
        /// <summary>
        /// フェードアルファ値を計算
        /// </summary>
        private static float CalculateFadeAlpha(float currentZ, float fadeStartZ, float spawnZ)
        {
            float fadeRatio = (currentZ - fadeStartZ) / (spawnZ - fadeStartZ);
            return 1f - fadeRatio;
        }
        
        /// <summary>
        /// ノーツのワールド座標を計算
        /// </summary>
        public static Vector3 CalculateNoteWorldPosition(NoteData noteData, float currentBeat, Conductor conductor)
        {
            if (!ValidateConductor(conductor))
            {
                return Vector3.zero;
            }
            
            float zPosition = conductor.GetNoteZPosition(noteData.TimeToHit);
            var position = new NotePosition3D(noteData.LaneIndex, zPosition);
            
            return position.ToVector3();
        }
        
        /// <summary>
        /// Holdノーツの終端位置を計算
        /// </summary>
        public static Vector3 CalculateHoldEndPosition(NoteData noteData, Conductor conductor)
        {
            if (!ValidateHoldNote(noteData))
            {
                return Vector3.zero;
            }
            
            if (!ValidateConductor(conductor))
            {
                return Vector3.zero;
            }
            
            float endBeat = noteData.TimeToHit + noteData.HoldDuration;
            float zPosition = conductor.GetNoteZPosition(endBeat);
            var position = new NotePosition3D(noteData.LaneIndex, zPosition);
            
            return position.ToVector3();
        }
        
        /// <summary>
        /// Holdノーツの検証
        /// </summary>
        private static bool ValidateHoldNote(NoteData noteData)
        {
            if (noteData.NoteType != NoteType.Hold)
            {
                Debug.LogWarning("HoldノーツではないためCalculateHoldEndPositionをスキップ");
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// Conductorの検証
        /// </summary>
        private static bool ValidateConductor(Conductor conductor)
        {
            if (conductor == null)
            {
                Debug.LogError("Conductorが設定されていません");
                return false;
            }
            return true;
        }
    }
}