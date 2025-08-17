using Jirou.Core;

namespace Jirou.Editor.Import
{
    /// <summary>
    /// 譜面インポーターのインターフェース
    /// </summary>
    public interface IChartImporter
    {
        /// <summary>
        /// フォーマット名を取得
        /// </summary>
        string GetFormatName();
        
        /// <summary>
        /// このインポーターが対応可能か判定
        /// </summary>
        bool CanImport(string content);
        
        /// <summary>
        /// インポート実行
        /// </summary>
        ChartData Import(string content);
    }
}