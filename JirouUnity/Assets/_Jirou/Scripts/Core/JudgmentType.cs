using System;

namespace Jirou.Core
{
    /// <summary>
    /// ノーツのタイミング判定結果を表す列挙型
    /// </summary>
    [Serializable]
    public enum JudgmentType
    {
        /// <summary>
        /// 完璧なタイミング（±0.5単位以内）
        /// </summary>
        Perfect = 0,
        
        /// <summary>
        /// 良いタイミング（±1.0単位以内）
        /// </summary>
        Great = 1,
        
        /// <summary>
        /// 普通のタイミング（±1.5単位以内）
        /// </summary>
        Good = 2,
        
        /// <summary>
        /// ミス（タイミングが大きくずれている）
        /// </summary>
        Miss = 3
    }
}