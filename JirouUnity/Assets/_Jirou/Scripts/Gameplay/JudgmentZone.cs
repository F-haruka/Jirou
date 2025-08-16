using System.Collections.Generic;
using UnityEngine;
using Jirou.Core;

namespace Jirou.Gameplay
{
    /// <summary>
    /// 各レーンの判定ゾーンを管理するコンポーネント
    /// ノーツの判定範囲の検出と判定結果の計算を行う
    /// </summary>
    public class JudgmentZone : MonoBehaviour
    {
        [Header("Judgment Ranges")]
        [SerializeField] private float perfectRange = 1.0f;  // Perfect判定のZ範囲（広めに設定）
        [SerializeField] private float greatRange = 2.0f;    // Great判定のZ範囲  
        [SerializeField] private float goodRange = 3.0f;     // Good判定のZ範囲
        
        [Header("Lane Settings")]
        [SerializeField] private int laneIndex;  // このゾーンが担当するレーンのインデックス（0-3）
        
        // プライベートフィールド
        private List<NoteController> notesInZone = new List<NoteController>();
        
        // プロパティ
        public int LaneIndex => laneIndex;
        
        /// <summary>
        /// Z座標が0に最も近いノーツを返す
        /// </summary>
        public NoteController GetClosestNote()
        {
            NoteController closest = null;
            float minDistance = float.MaxValue;
            
            // リストをクリーンアップ（null参照を削除）
            notesInZone.RemoveAll(note => note == null);
            
            // デバッグ用：ゾーン内のノーツ数を出力
            if (notesInZone.Count > 0)
            {
                Debug.Log($"[JudgmentZone] GetClosestNote - Lane: {laneIndex}, Notes in zone: {notesInZone.Count}");
            }
            
            foreach (var note in notesInZone)
            {
                // 既に判定済みまたは非アクティブなノーツはスキップ
                if (note.isJudged || !note.gameObject.activeInHierarchy) continue;
                
                // Z座標から判定ラインまでの距離を計算
                float distance = Mathf.Abs(note.transform.position.z);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = note;
                }
            }
            
            if (closest != null)
            {
                Debug.Log($"[JudgmentZone] Found closest note - Lane: {laneIndex}, Z: {closest.transform.position.z:F2}, Distance: {minDistance:F2}");
            }
            
            return closest;
        }
        
        /// <summary>
        /// ノーツのZ座標から判定タイプを返す
        /// </summary>
        public JudgmentType JudgeHit(NoteController note)
        {
            if (note == null) return JudgmentType.Miss;
            
            float distance = Mathf.Abs(note.transform.position.z);
            
            // デバッグログで実際の距離を出力
            Debug.Log($"[JudgmentZone] JudgeHit - Lane: {laneIndex}, NoteZ: {note.transform.position.z:F2}, Distance: {distance:F2}, Perfect: {perfectRange}, Great: {greatRange}, Good: {goodRange}");
            
            if (distance <= perfectRange)
            {
                return JudgmentType.Perfect;
            }
            else if (distance <= greatRange)
            {
                return JudgmentType.Great;
            }
            else if (distance <= goodRange)
            {
                return JudgmentType.Good;
            }
            else
            {
                return JudgmentType.Miss;
            }
        }
        
        /// <summary>
        /// 文字列からJudgmentTypeへの変換（互換性のため）
        /// </summary>
        public string JudgeHit(float zPosition)
        {
            float distance = Mathf.Abs(zPosition);
            
            if (distance <= perfectRange) return "Perfect";
            else if (distance <= greatRange) return "Great";
            else if (distance <= goodRange) return "Good";
            else return "Miss";
        }
        
        /// <summary>
        /// ノーツがゾーンに入った時の処理
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            var note = other.GetComponent<NoteController>();
            if (note != null && note.laneIndex == laneIndex)
            {
                notesInZone.Add(note);
                Debug.Log($"[JudgmentZone] Note entered zone - Lane: {laneIndex}, Z: {note.transform.position.z:F2}");
            }
        }
        
        /// <summary>
        /// ノーツがゾーンから出た時の処理
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            var note = other.GetComponent<NoteController>();
            if (note != null)
            {
                notesInZone.Remove(note);
                Debug.Log($"[JudgmentZone] Note exited zone - Lane: {laneIndex}");
            }
        }
        
        /// <summary>
        /// デバッグ用：ゾーン内のノーツ数を取得
        /// </summary>
        public int GetNotesInZoneCount()
        {
            // nullを除外してカウント
            notesInZone.RemoveAll(note => note == null);
            return notesInZone.Count;
        }
        
        /// <summary>
        /// 判定ゾーンをリセット
        /// </summary>
        public void ResetZone()
        {
            notesInZone.Clear();
        }
        
        void Start()
        {
            // BoxColliderの設定を確認
            var collider = GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }
            
            // Colliderの設定
            collider.isTrigger = true;
            // 奥行きを大きくして、ノーツを確実に検出できるようにする
            // 判定範囲（goodRange = 1.5f）の約3倍のサイズを確保
            collider.size = new Vector3(1.5f, 2f, 10f);  // 幅1.5、高さ2、奥行き10
            collider.center = Vector3.zero;
            
            // Rigidbodyの確認と設定（トリガー検出に必要）
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;  // 物理演算は不要
                rb.useGravity = false;
                Debug.Log($"[JudgmentZone] Added Rigidbody - Lane: {laneIndex}");
            }
            
            Debug.Log($"[JudgmentZone] Initialized - Lane: {laneIndex}, Position: {transform.position}");
        }
        
        // エディタ用のギズモ表示
        void OnDrawGizmos()
        {
            // 判定ゾーンの可視化
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            
            // Perfect範囲
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, perfectRange * 2f));
            
            // Great範囲
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, greatRange * 2f));
            
            // Good範囲
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, goodRange * 2f));
        }
    }
}