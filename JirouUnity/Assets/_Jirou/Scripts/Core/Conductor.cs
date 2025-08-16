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
        /// コンポーネントの初期化
        /// </summary>
        private void InitializeComponents()
        {
            _cachedAudioSource = _songSource != null ? _songSource : GetComponent<AudioSource>();
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
            // スポーンラインの可視化
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector3(0, 0.5f, _spawnZ), 
                                new Vector3(10, 0.1f, 0.1f));
            
            // スポーンライン位置のラベル
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(new Vector3(5, 1, _spawnZ), "Spawn Line");
            
            // 判定ラインの可視化
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(0, 0.5f, _hitZ), 
                                new Vector3(10, 0.1f, 0.1f));
            
            // 判定ライン位置のラベル
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(new Vector3(5, 1, _hitZ), "Hit Line");
            
            // 移動経路の可視化（4レーン）
            float[] laneX = { -3f, -1f, 1f, 3f };
            Gizmos.color = Color.yellow;
            
            foreach (float x in laneX)
            {
                Gizmos.DrawLine(new Vector3(x, 0.5f, _spawnZ), 
                                new Vector3(x, 0.5f, _hitZ));
            }
            
            // Z軸方向の矢印
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(0, 2, _spawnZ), 
                            new Vector3(0, 2, _hitZ));
            // 矢印の先端を表現するためのワイヤーキューブ
            Gizmos.DrawWireCube(new Vector3(0, 2, _hitZ), 
                                new Vector3(0.5f, 0.5f, 0.5f));
        }
#endif
    }
}