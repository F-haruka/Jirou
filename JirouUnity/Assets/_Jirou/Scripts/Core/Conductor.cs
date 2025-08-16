using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jirou.Core
{
    /// <summary>
    /// リズムゲーム「Jirou」の音楽同期とタイミング管理を行うシングルトンクラス
    /// 奥行き型ノーツ移動の計算と高精度な音楽同期を実現
    /// </summary>
    public class Conductor : MonoBehaviour
    {
        // シングルトンインスタンス
        private static Conductor _instance;
        
        /// <summary>
        /// Conductorのシングルトンインスタンス
        /// </summary>
        public static Conductor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<Conductor>();
                    if (_instance == null)
                    {
                        Debug.LogError("Conductorが見つかりません！シーンにConductorを配置してください。");
                    }
                }
                return _instance;
            }
        }

        [Header("楽曲設定")]
        [Tooltip("楽曲のBPM（Beats Per Minute）")]
        [SerializeField] private float _songBpm = 120f;
        
        [Tooltip("最初のビートまでのオフセット時間（秒）")]
        [SerializeField] private float _firstBeatOffset = 0f;
        
        [Tooltip("楽曲再生用のAudioSource")]
        [SerializeField] private AudioSource _songSource;

        [Header("ノーツ移動設定")]
        [Tooltip("ノーツの移動速度（Z軸距離/ビート）")]
        [SerializeField] private float _noteSpeed = 10.0f;
        
        [Tooltip("ノーツ生成位置のZ座標")]
        [SerializeField] private float _spawnZ = 20.0f;
        
        [Tooltip("判定ラインのZ座標")]
        [SerializeField] private float _hitZ = 0.0f;

        [Header("レーン設定")]
        [Tooltip("レーンのX座標配列")]
        [SerializeField] private float[] _laneXPositions = { -3f, -1f, 1f, 3f };
        
        [Tooltip("レーン間の視覚的な幅")]
        [SerializeField] private float _laneVisualWidth = 2.0f;
        
        [Tooltip("ノーツのY座標")]
        [SerializeField] private float _noteY = 0.5f;

        [Header("遠近感設定")]
        [Tooltip("手前（判定ライン）でのスケール")]
        [SerializeField] private float _perspectiveNearScale = 1.0f;
        
        [Tooltip("奥（生成位置）でのスケール")]
        [SerializeField] private float _perspectiveFarScale = 0.7f;

        [Header("デバッグ設定")]
        [Tooltip("デバッグログを有効にする")]
        [SerializeField] private bool _enableDebugLog = false;

        // 楽曲開始時のdspTime
        private double _dspSongTime;
        
        // 1ビートあたりの秒数
        private float _secPerBeat;
        
        // 楽曲が再生中かどうか
        private bool _isPlaying = false;
        
        // キャッシュされたコンポーネント
        private AudioSource _cachedAudioSource;

        /// <summary>
        /// 現在の楽曲再生位置（秒）
        /// </summary>
        public float SongPositionInSeconds
        {
            get
            {
                if (!_isPlaying) return 0f;
                return (float)(AudioSettings.dspTime - _dspSongTime - _firstBeatOffset);
            }
        }

        /// <summary>
        /// 現在の楽曲再生位置（ビート）
        /// </summary>
        public float SongPositionInBeats
        {
            get
            {
                if (!_isPlaying || _secPerBeat <= 0) return 0f;
                return SongPositionInSeconds / _secPerBeat;
            }
        }

        /// <summary>
        /// 楽曲が再生中かどうか
        /// </summary>
        public bool IsPlaying => _isPlaying;
        
        /// <summary>
        /// 楽曲のBPM
        /// </summary>
        public float SongBpm => _songBpm;
        
        /// <summary>
        /// ノーツの移動速度
        /// </summary>
        public float NoteSpeed => _noteSpeed;
        
        /// <summary>
        /// ノーツ生成位置のZ座標
        /// </summary>
        public float SpawnZ => _spawnZ;
        
        /// <summary>
        /// 判定ラインのZ座標
        /// </summary>
        public float HitZ => _hitZ;
        
        /// <summary>
        /// レーンのX座標配列
        /// </summary>
        public float[] LaneXPositions => _laneXPositions;
        
        /// <summary>
        /// レーン間の視覚的な幅
        /// </summary>
        public float LaneVisualWidth => _laneVisualWidth;
        
        /// <summary>
        /// ノーツのY座標
        /// </summary>
        public float NoteY => _noteY;
        
        /// <summary>
        /// 手前（判定ライン）でのスケール
        /// </summary>
        public float PerspectiveNearScale => _perspectiveNearScale;
        
        /// <summary>
        /// 奥（生成位置）でのスケール
        /// </summary>
        public float PerspectiveFarScale => _perspectiveFarScale;
        
        /// <summary>
        /// レーン幅
        /// </summary>
        public float LaneWidth => _laneVisualWidth;
        
        /// <summary>
        /// 楽曲再生位置（ビート）- 小文字プロパティ名対応
        /// </summary>
        public float songPositionInBeats => SongPositionInBeats;
        
        /// <summary>
        /// 楽曲BPM - 小文字プロパティ名対応
        /// </summary>
        public float songBpm 
        { 
            get => _songBpm; 
            set => _songBpm = value; 
        }
        
        /// <summary>
        /// ノーツ生成位置のZ座標 - 小文字プロパティ名対応
        /// </summary>
        public float spawnZ => _spawnZ;
        
        /// <summary>
        /// 判定ラインのZ座標 - 小文字プロパティ名対応
        /// </summary>
        public float hitZ => _hitZ;
        
        /// <summary>
        /// AudioSource - 小文字プロパティ名対応
        /// </summary>
        public AudioSource songSource 
        { 
            get => _cachedAudioSource ?? _songSource;
            set => _songSource = value;
        }

        void Awake()
        {
            // シングルトンパターンの実装
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeComponents();
                LogDebug("Conductorインスタンスが作成されました");
            }
            else if (_instance != this)
            {
                LogDebug("重複するConductorインスタンスを破棄します");
                Destroy(gameObject);
            }
        }

        void Start()
        {
            ValidateSettings();
            CalculateInitialValues();
            LogDebug($"初期化完了: BPM={_songBpm}, 1ビート={_secPerBeat}秒");
        }


        /// <summary>
        /// 楽曲の再生を開始
        /// </summary>
        public void StartSong()
        {
            if (!ValidateAudioSource()) return;
            
            // BPMから1ビートあたりの秒数を計算
            _secPerBeat = 60.0f / _songBpm;
            
            // 開始時刻を記録
            _dspSongTime = AudioSettings.dspTime;
            
            // 楽曲を再生
            _cachedAudioSource.Play();
            _isPlaying = true;
            
            LogDebug($"楽曲開始: BPM={_songBpm}, オフセット={_firstBeatOffset}秒");
        }

        /// <summary>
        /// 楽曲の再生を停止
        /// </summary>
        public void StopSong()
        {
            if (_cachedAudioSource != null && _cachedAudioSource.isPlaying)
            {
                _cachedAudioSource.Stop();
                _isPlaying = false;
                LogDebug("楽曲停止");
            }
        }

        /// <summary>
        /// 楽曲を一時停止
        /// </summary>
        public void PauseSong()
        {
            if (_cachedAudioSource != null && _cachedAudioSource.isPlaying)
            {
                _cachedAudioSource.Pause();
                _isPlaying = false;
                LogDebug("楽曲一時停止");
            }
        }

        /// <summary>
        /// 楽曲の再生を再開
        /// </summary>
        public void ResumeSong()
        {
            if (_cachedAudioSource != null && !_cachedAudioSource.isPlaying)
            {
                _cachedAudioSource.UnPause();
                _isPlaying = true;
                LogDebug("楽曲再開");
            }
        }

        /// <summary>
        /// 指定されたビートタイミングのノーツの現在Z座標を計算
        /// </summary>
        /// <param name="noteBeat">ノーツのビートタイミング</param>
        /// <returns>ノーツのZ座標</returns>
        public float GetNoteZPosition(float noteBeat)
        {
            // 現在のビート位置からノーツビートまでの差分
            float beatsPassed = SongPositionInBeats - noteBeat;
            
            // Z座標を計算（奥から手前へ移動）
            float zPosition = _spawnZ - (beatsPassed * _noteSpeed);
            
            return zPosition;
        }

        /// <summary>
        /// ノーツを生成すべきタイミングかを判定
        /// </summary>
        /// <param name="noteBeat">ノーツのビートタイミング</param>
        /// <param name="beatsInAdvance">事前生成するビート数</param>
        /// <returns>生成すべきならtrue</returns>
        public bool ShouldSpawnNote(float noteBeat, float beatsInAdvance)
        {
            // 先読みビート数を考慮した生成判定
            float spawnBeat = noteBeat - beatsInAdvance;
            
            // 現在のビート位置が生成タイミングを超えたか
            return SongPositionInBeats >= spawnBeat;
        }

        /// <summary>
        /// ノーツが判定ゾーン内にあるかチェック
        /// </summary>
        /// <param name="noteZ">ノーツのZ座標</param>
        /// <param name="tolerance">判定の許容範囲</param>
        /// <returns>判定ゾーン内ならtrue</returns>
        public bool IsNoteInHitZone(float noteZ, float tolerance = 1.0f)
        {
            // 判定ラインとの距離を計算
            float distance = Mathf.Abs(noteZ - _hitZ);
            
            // 許容範囲内かチェック
            return distance <= tolerance;
        }

        /// <summary>
        /// 指定ビートまでの残り時間を取得（秒）
        /// </summary>
        /// <param name="targetBeat">目標ビート</param>
        /// <returns>残り時間（秒）</returns>
        public float GetTimeUntilBeat(float targetBeat)
        {
            float beatsRemaining = targetBeat - SongPositionInBeats;
            return beatsRemaining * _secPerBeat;
        }

        /// <summary>
        /// BPMを変更（楽曲中のBPM変化対応）
        /// </summary>
        /// <param name="newBpm">新しいBPM</param>
        public void ChangeBPM(float newBpm)
        {
            if (newBpm <= 0)
            {
                Debug.LogWarning($"不正なBPM値: {newBpm}");
                return;
            }
            
            _songBpm = newBpm;
            _secPerBeat = 60.0f / _songBpm;
            LogDebug($"BPM変更: {newBpm}");
        }

        /// <summary>
        /// レーン数を取得
        /// </summary>
        public int GetLaneCount()
        {
            return _laneXPositions != null ? _laneXPositions.Length : 0;
        }

        /// <summary>
        /// 指定レーンのX座標を取得
        /// </summary>
        public float GetLaneX(int laneIndex)
        {
            if (laneIndex < 0 || laneIndex >= _laneXPositions.Length)
            {
                Debug.LogWarning($"不正なレーンインデックス: {laneIndex}");
                return 0f;
            }
            return _laneXPositions[laneIndex];
        }

        /// <summary>
        /// Z座標に基づいて遠近感を考慮したレーンX座標を計算
        /// </summary>
        /// <param name="laneIndex">レーンインデックス（0-3）</param>
        /// <param name="zPosition">Z座標（0=判定ライン、SpawnZ=生成位置）</param>
        /// <returns>遠近感を適用したX座標</returns>
        public float GetPerspectiveLaneX(int laneIndex, float zPosition)
        {
            if (laneIndex < 0 || laneIndex >= _laneXPositions.Length)
            {
                Debug.LogError($"Invalid lane index: {laneIndex}");
                return 0f;
            }
            
            float baseX = _laneXPositions[laneIndex];
            float t = Mathf.Clamp01(zPosition / _spawnZ);
            float scale = Mathf.Lerp(_perspectiveNearScale, _perspectiveFarScale, t);
            return baseX * scale;
        }

        /// <summary>
        /// 指定したZ座標でのレーン幅を取得
        /// </summary>
        /// <param name="zPosition">Z座標</param>
        /// <returns>そのZ座標でのレーン幅</returns>
        public float GetLaneWidthAtZ(float zPosition)
        {
            float t = Mathf.Clamp01(zPosition / _spawnZ);
            return _laneVisualWidth * Mathf.Lerp(_perspectiveNearScale, _perspectiveFarScale, t);
        }

        /// <summary>
        /// Z座標に基づいたスケール値を取得
        /// </summary>
        /// <param name="zPosition">Z座標</param>
        /// <returns>そのZ座標でのスケール値</returns>
        public float GetScaleAtZ(float zPosition)
        {
            float t = Mathf.Clamp01(zPosition / _spawnZ);
            return Mathf.Lerp(_perspectiveNearScale, _perspectiveFarScale, t);
        }


        /// <summary>
        /// コンポーネントの初期化
        /// </summary>
        private void InitializeComponents()
        {
            _cachedAudioSource = _songSource != null ? _songSource : GetComponent<AudioSource>();
            
            // AudioSourceが見つからない場合は自動的に追加
            if (_cachedAudioSource == null)
            {
                _cachedAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.LogWarning("[Conductor] AudioSourceが見つからなかったため、自動的に追加しました。");
            }
        }
        
        /// <summary>
        /// 設定の検証
        /// </summary>
        private void ValidateSettings()
        {
            // AudioSourceのチェック
            if (_cachedAudioSource == null)
            {
                Debug.LogError("AudioSourceが見つかりません！Conductorと同じGameObjectに追加してください。");
            }
            
            // BPMの妥当性チェック
            if (_songBpm <= 0)
            {
                Debug.LogWarning($"不正なBPM値: {_songBpm}。デフォルト値120に設定します。");
                _songBpm = 120f;
            }
        }
        
        /// <summary>
        /// 初期値の計算
        /// </summary>
        private void CalculateInitialValues()
        {
            _secPerBeat = 60.0f / _songBpm;
        }
        
        /// <summary>
        /// AudioSourceの検証
        /// </summary>
        private bool ValidateAudioSource()
        {
            if (_cachedAudioSource == null)
            {
                Debug.LogError("AudioSourceが設定されていません！");
                return false;
            }
            
            if (_cachedAudioSource.clip == null)
            {
                Debug.LogError("AudioClipが設定されていません！");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// デバッグログ出力
        /// </summary>
        private void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log($"[Conductor] {message}");
            }
        }


#if UNITY_EDITOR
        void OnGUI()
        {
            if (!Application.isPlaying) return;
            
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = Texture2D.whiteTexture;
            
            // 背景ボックス
            GUI.Box(new Rect(10, 10, 250, 140), "Conductor Debug Info", boxStyle);
            
            // デバッグ情報表示
            GUI.Label(new Rect(20, 35, 230, 20), 
                      $"BPM: {_songBpm}", style);
            GUI.Label(new Rect(20, 55, 230, 20), 
                      $"Time: {SongPositionInSeconds:F2}s", style);
            GUI.Label(new Rect(20, 75, 230, 20), 
                      $"Beat: {SongPositionInBeats:F2}", style);
            GUI.Label(new Rect(20, 95, 230, 20), 
                      $"Playing: {_isPlaying}", style);
            GUI.Label(new Rect(20, 115, 230, 20), 
                      $"Note Speed: {_noteSpeed}", style);
        }

        void OnDrawGizmos()
        {
            // Gizmo表示を完全に無効化
            return;
        }
#endif
    }
}