using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Jirou.Core;

namespace Jirou.Editor.Import
{
    /// <summary>
    /// 譜面インポート管理クラス
    /// </summary>
    public static class ChartImportManager
    {
        private static readonly List<IChartImporter> importers = new List<IChartImporter>();
        
        static ChartImportManager()
        {
            // インポーターを登録
            RegisterImporter(new NotesEditorJsonImporter());
            // 将来的に他のインポーターも追加
        }
        
        /// <summary>
        /// インポーターを登録
        /// </summary>
        public static void RegisterImporter(IChartImporter importer)
        {
            if (importer != null && !importers.Contains(importer))
            {
                importers.Add(importer);
                Debug.Log($"インポーター登録: {importer.GetFormatName()}");
            }
        }
        
        /// <summary>
        /// インポートを試行
        /// </summary>
        public static bool TryImport(string content, out ChartData chartData, out string error)
        {
            chartData = null;
            error = null;
            
            // 対応可能なインポーターを探す
            foreach (var importer in importers)
            {
                if (importer.CanImport(content))
                {
                    try
                    {
                        Debug.Log($"{importer.GetFormatName()}形式として認識しました");
                        chartData = importer.Import(content);
                        return true;
                    }
                    catch (Exception e)
                    {
                        error = $"{importer.GetFormatName()}のインポート失敗:\n{e.Message}";
                        Debug.LogError(error);
                    }
                }
            }
            
            // 対応するインポーターが見つからない場合
            error = "対応するインポート形式が見つかりませんでした。\n" +
                   $"対応形式: {string.Join(", ", GetSupportedFormats())}";
            return false;
        }
        
        /// <summary>
        /// サポートされている形式を取得
        /// </summary>
        public static string[] GetSupportedFormats()
        {
            return importers.Select(i => i.GetFormatName()).ToArray();
        }
        
        /// <summary>
        /// フォーマットを自動検出
        /// </summary>
        public static string DetectFormat(string content)
        {
            foreach (var importer in importers)
            {
                if (importer.CanImport(content))
                {
                    return importer.GetFormatName();
                }
            }
            return "Unknown";
        }
    }
}