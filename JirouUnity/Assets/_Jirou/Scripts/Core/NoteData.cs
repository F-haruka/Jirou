using System;
using UnityEngine;

namespace Jirou.Core
{
    /// <summary>
    /// ノーツタイプの列挙型
    /// </summary>
    [Serializable]
    public enum NoteType
    {
        Tap = 0,    // 単押しノーツ
        Hold = 1    // 長押しノーツ
    }

    /// <summary>
    /// 個別のノーツデータを表すクラス
    /// </summary>
    [Serializable]
    public class NoteData
    {
        [Header("基本情報")]
        [Tooltip("ノーツの種類")]
        public NoteType noteType = NoteType.Tap;
        
        [Tooltip("レーン番号（0-3）")]
        [Range(0, 3)]
        public int laneIndex = 0;
        
        [Tooltip("ヒットタイミング（ビート単位）")]
        [Min(0f)]
        public float timeToHit = 0f;
        
        [Header("Holdノーツ専用")]
        [Tooltip("Holdノーツの長さ（ビート単位）")]
        [Min(0f)]
        public float holdDuration = 0f;
        
        [Header("視覚調整")]
        [Tooltip("ノーツの大きさ倍率")]
        [Range(0.5f, 2.0f)]
        public float visualScale = 1.0f;
        
        [Tooltip("ノーツの色")]
        public Color noteColor = Color.white;
        
        [Header("オプション")]
        [Tooltip("カスタムヒット音")]
        public AudioClip customHitSound;
        
        [Tooltip("カスタムヒットエフェクト")]
        public GameObject customHitEffect;
        
        [Tooltip("基本スコア値")]
        [Min(1)]
        public int baseScore = 100;
        
        [Tooltip("スコア倍率")]
        [Range(0.1f, 10f)]
        public float scoreMultiplier = 1.0f;
        
        // 静的定数
        public static readonly float[] LaneXPositions = { -3f, -1f, 1f, 3f };
        public static readonly KeyCode[] LaneKeys = 
        { 
            KeyCode.D, 
            KeyCode.F, 
            KeyCode.J, 
            KeyCode.K 
        };
        
        /// <summary>
        /// レーンインデックスからX座標を取得
        /// </summary>
        public float GetLaneXPosition()
        {
            if (laneIndex >= 0 && laneIndex < LaneXPositions.Length)
            {
                return LaneXPositions[laneIndex];
            }
            Debug.LogWarning($"無効なレーンインデックス: {laneIndex}");
            return 0f;
        }
        
        /// <summary>
        /// ノーツの終了タイミングを取得（Holdノーツ用）
        /// </summary>
        public float GetEndTime()
        {
            return noteType == NoteType.Hold ? timeToHit + holdDuration : timeToHit;
        }
        
        /// <summary>
        /// データの妥当性をチェック
        /// </summary>
        public bool Validate(out string error)
        {
            error = "";
            
            if (laneIndex < 0 || laneIndex > 3)
            {
                error = $"無効なレーンインデックス: {laneIndex}";
                return false;
            }
            
            if (timeToHit < 0)
            {
                error = $"負のタイミング値: {timeToHit}";
                return false;
            }
            
            if (noteType == NoteType.Hold && holdDuration <= 0)
            {
                error = $"Holdノーツの長さが不正: {holdDuration}";
                return false;
            }
            
            if (visualScale <= 0)
            {
                error = $"不正なスケール値: {visualScale}";
                return false;
            }
            
            return true;
        }
    }
}