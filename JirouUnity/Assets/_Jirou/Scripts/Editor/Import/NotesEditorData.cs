using System;
using System.Collections.Generic;

namespace Jirou.Editor.Import
{
    /// <summary>
    /// NotesEditor形式のJSONデータ構造
    /// </summary>
    [Serializable]
    public class NotesEditorData
    {
        public string name;           // 曲名
        public int maxBlock;         // 最大ブロック数
        public float BPM;            // BPM
        public int offset;           // オフセット（ミリ秒）
        public List<NotesEditorNote> notes;  // ノーツリスト
    }
    
    /// <summary>
    /// NotesEditor形式のノーツデータ
    /// </summary>
    [Serializable]
    public class NotesEditorNote
    {
        public int LPB;              // Lines Per Beat
        public int num;              // タイミング（LPB単位）
        public int block;            // レーン番号（0-3）
        public int type;             // ノーツタイプ（1=Tap, 2=Hold）
        public List<object> notes;   // Holdノーツの終了位置（通常は空配列）
    }
}