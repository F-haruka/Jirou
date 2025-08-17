using System.Diagnostics;
using NUnit.Framework;
using Jirou.Editor.Import;
using Debug = UnityEngine.Debug;

namespace Jirou.Tests.Editor
{
    [TestFixture]
    public class ImportPerformanceTests
    {
        [Test]
        public void ImportLargeFile_パフォーマンス測定()
        {
            // 大量のノーツを含むテストデータを生成
            var testData = GenerateLargeTestData(1000);
            string json = UnityEngine.JsonUtility.ToJson(testData);
            
            var stopwatch = Stopwatch.StartNew();
            
            Jirou.Core.ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(json, out chartData, out error);
            
            stopwatch.Stop();
            
            Assert.IsTrue(success);
            Assert.AreEqual(1000, chartData.Notes.Count);
            
            Debug.Log($"1000ノーツのインポート時間: {stopwatch.ElapsedMilliseconds}ms");
            
            // パフォーマンス基準: 1000ノーツで1秒以内
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000);
            
            UnityEngine.Object.DestroyImmediate(chartData);
        }
        
        [Test]
        public void ImportSmallFile_パフォーマンス測定()
        {
            // 少量のノーツを含むテストデータを生成
            var testData = GenerateLargeTestData(10);
            string json = UnityEngine.JsonUtility.ToJson(testData);
            
            var stopwatch = Stopwatch.StartNew();
            
            Jirou.Core.ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(json, out chartData, out error);
            
            stopwatch.Stop();
            
            Assert.IsTrue(success);
            Assert.AreEqual(10, chartData.Notes.Count);
            
            Debug.Log($"10ノーツのインポート時間: {stopwatch.ElapsedMilliseconds}ms");
            
            // パフォーマンス基準: 10ノーツで100ms以内
            Assert.Less(stopwatch.ElapsedMilliseconds, 100);
            
            UnityEngine.Object.DestroyImmediate(chartData);
        }
        
        [Test]
        public void ImportMediumFile_パフォーマンス測定()
        {
            // 中量のノーツを含むテストデータを生成
            var testData = GenerateLargeTestData(100);
            string json = UnityEngine.JsonUtility.ToJson(testData);
            
            var stopwatch = Stopwatch.StartNew();
            
            Jirou.Core.ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(json, out chartData, out error);
            
            stopwatch.Stop();
            
            Assert.IsTrue(success);
            Assert.AreEqual(100, chartData.Notes.Count);
            
            Debug.Log($"100ノーツのインポート時間: {stopwatch.ElapsedMilliseconds}ms");
            
            // パフォーマンス基準: 100ノーツで200ms以内
            Assert.Less(stopwatch.ElapsedMilliseconds, 200);
            
            UnityEngine.Object.DestroyImmediate(chartData);
        }
        
        private NotesEditorData GenerateLargeTestData(int noteCount)
        {
            var data = new NotesEditorData
            {
                name = "Performance Test",
                BPM = 120,
                offset = 0,
                notes = new System.Collections.Generic.List<NotesEditorNote>()
            };
            
            for (int i = 0; i < noteCount; i++)
            {
                data.notes.Add(new NotesEditorNote
                {
                    LPB = 4,
                    num = i * 4,
                    block = i % 4,
                    type = 1,
                    notes = new System.Collections.Generic.List<object>()
                });
            }
            
            return data;
        }
        
        [Test]
        public void ImportWithComplexHolds_パフォーマンス測定()
        {
            // Holdノーツを含む複雑なテストデータを生成
            var testData = GenerateComplexTestData(200);
            string json = UnityEngine.JsonUtility.ToJson(testData);
            
            var stopwatch = Stopwatch.StartNew();
            
            Jirou.Core.ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(json, out chartData, out error);
            
            stopwatch.Stop();
            
            Assert.IsTrue(success);
            Assert.AreEqual(200, chartData.Notes.Count);
            
            Debug.Log($"200個の複雑なノーツのインポート時間: {stopwatch.ElapsedMilliseconds}ms");
            
            // パフォーマンス基準: 200個の複雑なノーツで500ms以内
            Assert.Less(stopwatch.ElapsedMilliseconds, 500);
            
            UnityEngine.Object.DestroyImmediate(chartData);
        }
        
        private NotesEditorData GenerateComplexTestData(int noteCount)
        {
            var data = new NotesEditorData
            {
                name = "Complex Performance Test",
                BPM = 150,
                offset = 2000,
                notes = new System.Collections.Generic.List<NotesEditorNote>()
            };
            
            for (int i = 0; i < noteCount; i++)
            {
                // TapとHoldを交互に配置
                bool isHold = i % 2 == 0;
                var note = new NotesEditorNote
                {
                    LPB = 4,
                    num = i * 4,
                    block = i % 4,
                    type = isHold ? 2 : 1,
                    notes = new System.Collections.Generic.List<object>()
                };
                
                // Holdノーツの場合、終了位置を追加
                if (isHold)
                {
                    note.notes.Add(new NotesEditorNote
                    {
                        LPB = 4,
                        num = (i + 1) * 4,
                        block = i % 4,
                        type = 2,
                        notes = new System.Collections.Generic.List<object>()
                    });
                }
                
                data.notes.Add(note);
            }
            
            return data;
        }
    }
}