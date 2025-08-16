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
        [SerializeField] private NoteType _noteType = NoteType.Tap;
        
        [Tooltip("レーン番号（0-3）")]
        [Range(0, 3)]
        [SerializeField] private int _laneIndex = 0;
        
        [Tooltip("ヒットタイミング（ビート単位）")]
        [Min(0f)]
        [SerializeField] private float _timeToHit = 0f;
        
        [Header("Holdノーツ専用")]
        [Tooltip("Holdノーツの長さ（ビート単位）")]
        [Min(0f)]
        [SerializeField] private float _holdDuration = 0f;
        
        [Header("視覚調整")]
        [Tooltip("ノーツの大きさ倍率")]
        [Range(0.5f, 2.0f)]
        [SerializeField] private float _visualScale = 1.0f;
        
        [Tooltip("ノーツの色")]
        [SerializeField] private Color _noteColor = Color.white;
        
        [Header("オプション")]
        [Tooltip("カスタムヒット音")]
        [SerializeField] private AudioClip _customHitSound;
        
        [Tooltip("カスタムヒットエフェクト")]
        [SerializeField] private GameObject _customHitEffect;
        
        [Tooltip("基本スコア値")]
        [Min(1)]
        [SerializeField] private int _baseScore = 100;
        
        [Tooltip("スコア倍率")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _scoreMultiplier = 1.0f;
        
        // 静的定数
        public static readonly float[] LaneXPositions = { -3f, -1f, 1f, 3f };
        public static readonly KeyCode[] LaneKeys = 
        { 
            KeyCode.D, 
            KeyCode.F, 
            KeyCode.J, 
            KeyCode.K 
        };
        
        // プロパティ
        public NoteType NoteType
        {
            get => _noteType;
            set => _noteType = value;
        }
        
        public int LaneIndex
        {
            get => _laneIndex;
            set => _laneIndex = Mathf.Clamp(value, 0, 3);
        }
        
        public float TimeToHit
        {
            get => _timeToHit;
            set => _timeToHit = Mathf.Max(0f, value);
        }
        
        public float HoldDuration
        {
            get => _holdDuration;
            set => _holdDuration = Mathf.Max(0f, value);
        }
        
        public float VisualScale
        {
            get => _visualScale;
            set => _visualScale = Mathf.Clamp(value, 0.5f, 2.0f);
        }
        
        public Color NoteColor
        {
            get => _noteColor;
            set => _noteColor = value;
        }
        
        public AudioClip CustomHitSound
        {
            get => _customHitSound;
            set => _customHitSound = value;
        }
        
        public GameObject CustomHitEffect
        {
            get => _customHitEffect;
            set => _customHitEffect = value;
        }
        
        public int BaseScore
        {
            get => _baseScore;
            set => _baseScore = Mathf.Max(1, value);
        }
        
        public float ScoreMultiplier
        {
            get => _scoreMultiplier;
            set => _scoreMultiplier = Mathf.Clamp(value, 0.1f, 10f);
        }
        
        /// <summary>
        /// レーンインデックスからX座標を取得
        /// </summary>
        public float GetLaneXPosition()
        {
            if (_laneIndex >= 0 && _laneIndex < LaneXPositions.Length)
            {
                return LaneXPositions[_laneIndex];
            }
            Debug.LogWarning($"無効なレーンインデックス: {_laneIndex}");
            return 0f;
        }
        
        /// <summary>
        /// ノーツの終了タイミングを取得（Holdノーツ用）
        /// </summary>
        public float GetEndTime()
        {
            return _noteType == NoteType.Hold ? _timeToHit + _holdDuration : _timeToHit;
        }
        
        /// <summary>
        /// データの妥当性をチェック
        /// </summary>
        public bool Validate(out string error)
        {
            error = "";
            
            if (_laneIndex < 0 || _laneIndex > 3)
            {
                error = $"無効なレーンインデックス: {_laneIndex}";
                return false;
            }
            
            if (_timeToHit < 0)
            {
                error = $"負のタイミング値: {_timeToHit}";
                return false;
            }
            
            if (_noteType == NoteType.Hold && _holdDuration <= 0)
            {
                error = $"Holdノーツの長さが不正: {_holdDuration}";
                return false;
            }
            
            if (_visualScale <= 0)
            {
                error = $"不正なスケール値: {_visualScale}";
                return false;
            }
            
            return true;
        }
    }
}