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
        private static Conductor instance;
        
        /// <summary>
        /// Conductorのシングルトンインスタンス
        /// </summary>
        public static Conductor Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<Conductor>();
                    if (instance == null)
                    {
                        Debug.LogError("Conductorが見つかりません！シーンにConductorを配置してください。");
                    }
                }
                return instance;
            }
        }

        [Header("楽曲設定")]
        [Tooltip("楽曲のBPM（Beats Per Minute）")]
        public float songBpm = 120f;
        
        [Tooltip("最初のビートまでのオフセット時間（秒）")]
        public float firstBeatOffset = 0f;
        
        [Tooltip("楽曲再生用のAudioSource")]
        public AudioSource songSource;

        [Header("ノーツ移動設定")]
        [Tooltip("ノーツの移動速度（Z軸距離/ビート）")]
        public float noteSpeed = 10.0f;
        
        [Tooltip("ノーツ生成位置のZ座標")]
        public float spawnZ = 20.0f;
        
        [Tooltip("判定ラインのZ座標")]
        public float hitZ = 0.0f;

        [Header("デバッグ設定")]
        [Tooltip("デバッグログを有効にする")]
        public bool enableDebugLog = false;

        // 楽曲開始時のdspTime
        private double dspSongTime;
        
        // 1ビートあたりの秒数
        private float secPerBeat;
        
        // 楽曲が再生中かどうか
        private bool isPlaying = false;

        /// <summary>
        /// 現在の楽曲再生位置（秒）
        /// </summary>
        public float songPositionInSeconds
        {
            get
            {
                if (!isPlaying) return 0f;
                return (float)(AudioSettings.dspTime - dspSongTime - firstBeatOffset);
            }
        }

        /// <summary>
        /// 現在の楽曲再生位置（ビート）
        /// </summary>
        public float songPositionInBeats
        {
            get
            {
                if (!isPlaying || secPerBeat <= 0) return 0f;
                return songPositionInSeconds / secPerBeat;
            }
        }

        /// <summary>
        /// 楽曲が再生中かどうか
        /// </summary>
        public bool IsPlaying => isPlaying;

        void Awake()
        {
            // シングルトンパターンの実装
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LogDebug("Conductorインスタンスが作成されました");
            }
            else if (instance != this)
            {
                LogDebug("重複するConductorインスタンスを破棄します");
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // 必須コンポーネントのチェック
            if (songSource == null)
            {
                songSource = GetComponent<AudioSource>();
                if (songSource == null)
                {
                    Debug.LogError("AudioSourceが見つかりません！Conductorと同じGameObjectに追加してください。");
                }
            }
            
            // BPMの妥当性チェック
            if (songBpm <= 0)
            {
                Debug.LogWarning($"不正なBPM値: {songBpm}。デフォルト値120に設定します。");
                songBpm = 120f;
            }
            
            // 初期計算
            secPerBeat = 60.0f / songBpm;
            LogDebug($"初期化完了: BPM={songBpm}, 1ビート={secPerBeat}秒");
        }


        /// <summary>
        /// 楽曲の再生を開始
        /// </summary>
        public void StartSong()
        {
            if (!ValidateAudioSource()) return;
            
            // BPMから1ビートあたりの秒数を計算
            secPerBeat = 60.0f / songBpm;
            
            // 開始時刻を記録
            dspSongTime = AudioSettings.dspTime;
            
            // 楽曲を再生
            songSource.Play();
            isPlaying = true;
            
            LogDebug($"楽曲開始: BPM={songBpm}, オフセット={firstBeatOffset}秒");
        }

        /// <summary>
        /// 楽曲の再生を停止
        /// </summary>
        public void StopSong()
        {
            if (songSource != null && songSource.isPlaying)
            {
                songSource.Stop();
                isPlaying = false;
                LogDebug("楽曲停止");
            }
        }

        /// <summary>
        /// 楽曲を一時停止
        /// </summary>
        public void PauseSong()
        {
            if (songSource != null && songSource.isPlaying)
            {
                songSource.Pause();
                isPlaying = false;
                LogDebug("楽曲一時停止");
            }
        }

        /// <summary>
        /// 楽曲の再生を再開
        /// </summary>
        public void ResumeSong()
        {
            if (songSource != null && !songSource.isPlaying)
            {
                songSource.UnPause();
                isPlaying = true;
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
            float beatsPassed = songPositionInBeats - noteBeat;
            
            // Z座標を計算（奥から手前へ移動）
            float zPosition = spawnZ - (beatsPassed * noteSpeed);
            
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
            return songPositionInBeats >= spawnBeat;
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
            float distance = Mathf.Abs(noteZ - hitZ);
            
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
            float beatsRemaining = targetBeat - songPositionInBeats;
            return beatsRemaining * secPerBeat;
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
            
            songBpm = newBpm;
            secPerBeat = 60.0f / songBpm;
            LogDebug($"BPM変更: {newBpm}");
        }


        /// <summary>
        /// AudioSourceの検証
        /// </summary>
        private bool ValidateAudioSource()
        {
            if (songSource == null)
            {
                Debug.LogError("AudioSourceが設定されていません！");
                return false;
            }
            
            if (songSource.clip == null)
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
            if (enableDebugLog)
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
                      $"BPM: {songBpm}", style);
            GUI.Label(new Rect(20, 55, 230, 20), 
                      $"Time: {songPositionInSeconds:F2}s", style);
            GUI.Label(new Rect(20, 75, 230, 20), 
                      $"Beat: {songPositionInBeats:F2}", style);
            GUI.Label(new Rect(20, 95, 230, 20), 
                      $"Playing: {isPlaying}", style);
            GUI.Label(new Rect(20, 115, 230, 20), 
                      $"Note Speed: {noteSpeed}", style);
        }

        void OnDrawGizmos()
        {
            // スポーンラインの可視化
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector3(0, 0.5f, spawnZ), 
                                new Vector3(10, 0.1f, 0.1f));
            
            // スポーンライン位置のラベル
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(new Vector3(5, 1, spawnZ), "Spawn Line");
            
            // 判定ラインの可視化
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(0, 0.5f, hitZ), 
                                new Vector3(10, 0.1f, 0.1f));
            
            // 判定ライン位置のラベル
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(new Vector3(5, 1, hitZ), "Hit Line");
            
            // 移動経路の可視化（4レーン）
            float[] laneX = { -3f, -1f, 1f, 3f };
            Gizmos.color = Color.yellow;
            
            foreach (float x in laneX)
            {
                Gizmos.DrawLine(new Vector3(x, 0.5f, spawnZ), 
                                new Vector3(x, 0.5f, hitZ));
            }
            
            // Z軸方向の矢印
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(0, 2, spawnZ), 
                            new Vector3(0, 2, hitZ));
            Gizmos.DrawCone(new Vector3(0, 2, hitZ), 
                            Quaternion.LookRotation(Vector3.back), 0.5f);
        }
#endif
    }
}