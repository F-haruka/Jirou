using UnityEngine;
using Jirou.Core;

namespace Jirou.Gameplay
{
    /// <summary>
    /// 個別ノーツの動作を制御するコンポーネント
    /// </summary>
    public class NoteController : MonoBehaviour
    {
        public NoteData noteData { get; private set; }
        public GameObject customHitEffect { get; set; }
        public AudioClip customHitSound { get; set; }
        
        // NoteSpawnerTestSetupで使用される追加プロパティ
        public NoteType noteType = NoteType.Tap;
        public int laneIndex;
        public float timeToHit;
        public float holdDuration;
        public float moveSpeed = 10f;
        public bool isActive { get; private set; }
        public bool isJudged { get; private set; }
        public bool isHolding { get; private set; }
        
        private Conductor conductor;
        private bool isCompleted = false;
        private float initialZ;
        
        public void Initialize(NoteData data, Conductor conductorRef)
        {
            noteData = data;
            conductor = conductorRef;
            initialZ = transform.position.z;
            isCompleted = false;
        }
        
        void Update()
        {
            if (conductor == null || isCompleted) return;
            
            // Z座標の更新
            float newZ = conductor.GetNoteZPosition(noteData.TimeToHit);
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                newZ
            );
            
            // 判定ラインを通過したかチェック
            if (newZ < conductor.hitZ - 2f)
            {
                isCompleted = true;
            }
        }
        
        public bool IsCompleted()
        {
            return isCompleted;
        }
        
        public void OnHit()
        {
            // ヒット時の処理
            isCompleted = true;
            
            // エフェクト生成
            if (customHitEffect != null)
            {
                Instantiate(customHitEffect, transform.position, Quaternion.identity);
            }
            
            // サウンド再生
            if (customHitSound != null)
            {
                AudioSource.PlayClipAtPoint(customHitSound, transform.position);
            }
        }
        
        /// <summary>
        /// ノーツをリセット（プール返却時）
        /// </summary>
        public void Reset()
        {
            isActive = false;
            isJudged = false;
            isHolding = false;
            isCompleted = false;
            
            // 位置とスケールのリセット
            transform.position = Vector3.zero;
            transform.localScale = Vector3.one;
        }
    }
}