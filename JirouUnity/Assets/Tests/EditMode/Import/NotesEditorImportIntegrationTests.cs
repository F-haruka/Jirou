using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Jirou.Core;
using Jirou.Editor.Import;

namespace Jirou.Tests.Editor
{
    [TestFixture]
    public class NotesEditorImportIntegrationTests
    {
        private string testDataPath;
        
        [SetUp]
        public void Setup()
        {
            // テストデータのパスを設定
            testDataPath = Path.Combine(Application.dataPath, 
                "_Jirou/Data/ChartsJson/End_Time.json");
        }
        
        [Test]
        public void ImportNotesEditorJson_完全なファイル()
        {
            // ファイルが存在することを確認
            Assert.IsTrue(File.Exists(testDataPath), 
                $"テストファイルが見つかりません: {testDataPath}");
            
            // JSONを読み込み
            string json = File.ReadAllText(testDataPath);
            
            // インポート実行
            ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(json, out chartData, out error);
            
            // 検証
            Assert.IsTrue(success, $"インポート失敗: {error}");
            Assert.IsNotNull(chartData);
            Assert.AreEqual("End_Time", chartData.SongName);
            Assert.AreEqual(180f, chartData.Bpm);
            Assert.Greater(chartData.Notes.Count, 0);
            
            // 後処理
            Object.DestroyImmediate(chartData);
        }
        
        [Test]
        public void ImportNotesEditorJson_ノーツ変換確認()
        {
            string simpleJson = @"{
                ""name"": ""Test"",
                ""BPM"": 120,
                ""offset"": 0,
                ""notes"": [
                    {
                        ""LPB"": 4,
                        ""num"": 8,
                        ""block"": 1,
                        ""type"": 1,
                        ""notes"": []
                    }
                ]
            }";
            
            ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(simpleJson, out chartData, out error);
            
            Assert.IsTrue(success);
            Assert.AreEqual(1, chartData.Notes.Count);
            
            var note = chartData.Notes[0];
            Assert.AreEqual(1, note.LaneIndex);
            Assert.AreEqual(NoteType.Tap, note.NoteType);
            Assert.AreEqual(2f, note.TimeToHit); // 8/4 = 2
            
            Object.DestroyImmediate(chartData);
        }
        
        [Test]
        public void ImportNotesEditorJson_Holdノーツ変換()
        {
            string holdJson = @"{
                ""name"": ""Test"",
                ""BPM"": 120,
                ""offset"": 0,
                ""notes"": [
                    {
                        ""LPB"": 4,
                        ""num"": 0,
                        ""block"": 2,
                        ""type"": 2,
                        ""notes"": [
                            {
                                ""LPB"": 4,
                                ""num"": 16,
                                ""block"": 2,
                                ""type"": 2,
                                ""notes"": []
                            }
                        ]
                    }
                ]
            }";
            
            ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(holdJson, out chartData, out error);
            
            Assert.IsTrue(success);
            Assert.AreEqual(1, chartData.Notes.Count);
            
            var note = chartData.Notes[0];
            Assert.AreEqual(NoteType.Hold, note.NoteType);
            Assert.AreEqual(4f, note.HoldDuration); // (16-0)/4 = 4
            
            Object.DestroyImmediate(chartData);
        }
        
        [Test]
        public void ImportNotesEditorJson_無効なデータ()
        {
            string invalidJson = @"{
                ""name"": ""Test"",
                ""BPM"": -120,
                ""offset"": 0,
                ""notes"": []
            }";
            
            ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(invalidJson, out chartData, out error);
            
            Assert.IsFalse(success);
            Assert.IsNotNull(error);
            Assert.IsNull(chartData);
        }
        
        [Test]
        public void ImportNotesEditorJson_複数のノーツ()
        {
            string multipleNotesJson = @"{
                ""name"": ""Test"",
                ""BPM"": 120,
                ""offset"": 1000,
                ""notes"": [
                    {
                        ""LPB"": 4,
                        ""num"": 0,
                        ""block"": 0,
                        ""type"": 1,
                        ""notes"": []
                    },
                    {
                        ""LPB"": 4,
                        ""num"": 4,
                        ""block"": 1,
                        ""type"": 1,
                        ""notes"": []
                    },
                    {
                        ""LPB"": 4,
                        ""num"": 8,
                        ""block"": 2,
                        ""type"": 1,
                        ""notes"": []
                    },
                    {
                        ""LPB"": 4,
                        ""num"": 12,
                        ""block"": 3,
                        ""type"": 1,
                        ""notes"": []
                    }
                ]
            }";
            
            ChartData chartData;
            string error;
            bool success = ChartImportManager.TryImport(multipleNotesJson, out chartData, out error);
            
            Assert.IsTrue(success);
            Assert.AreEqual(4, chartData.Notes.Count);
            
            // オフセットが正しく変換されているか確認
            Assert.AreEqual(2f, chartData.FirstBeatOffset); // 1000ms at 120BPM = 2 beats
            
            // レーンが正しく設定されているか確認
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(i, chartData.Notes[i].LaneIndex);
            }
            
            Object.DestroyImmediate(chartData);
        }
    }
}