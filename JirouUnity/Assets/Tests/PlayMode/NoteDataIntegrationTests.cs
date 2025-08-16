using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Jirou.Core;

namespace Jirou.Tests
{
    public class NoteDataIntegrationTests
    {
        [UnityTest]
        public IEnumerator ChartData_LoadLargeChart_PerformanceTest()
        {
            // 大量ノーツの譜面を作成
            var chart = ScriptableObject.CreateInstance<ChartData>();
            
            // 1000個のノーツを追加
            for (int i = 0; i < 1000; i++)
            {
                chart.Notes.Add(new NoteData
                {
                    NoteType = i % 5 == 0 ? NoteType.Hold : NoteType.Tap,
                    LaneIndex = i % 4,
                    TimeToHit = i * 0.25f,
                    HoldDuration = 1.0f
                });
            }
            
            // パフォーマンス測定
            float startTime = Time.realtimeSinceStartup;
            
            // 各種処理のテスト
            chart.SortNotesByTime();
            var stats = chart.GetStatistics();
            var filtered = chart.GetNotesInTimeRange(100f, 200f);
            
            float elapsedTime = Time.realtimeSinceStartup - startTime;
            
            // 1秒以内に完了すること
            Assert.Less(elapsedTime, 1.0f, 
                $"処理時間が長すぎます: {elapsedTime}秒");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator NotePool_StressTest()
        {
            // ノーツプールのセットアップ
            GameObject poolObject = new GameObject("TestNotePool");
            var poolManager = poolObject.AddComponent<NotePoolManager>();
            
            // プレハブの代わりにダミーオブジェクトを設定
            GameObject tapPrefab = new GameObject("TapNotePrefab");
            GameObject holdPrefab = new GameObject("HoldNotePrefab");
            
            // リフレクションを使ってプライベートフィールドを設定
            var poolManagerType = poolManager.GetType();
            var tapField = poolManagerType.GetField("_tapNotePrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var holdField = poolManagerType.GetField("_holdNotePrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (tapField != null) tapField.SetValue(poolManager, tapPrefab);
            if (holdField != null) holdField.SetValue(poolManager, holdPrefab);
            
            yield return null;  // 初期化待ち
            
            // 大量のノーツを取得・返却
            GameObject[] notes = new GameObject[100];
            
            // 取得テスト
            for (int i = 0; i < notes.Length; i++)
            {
                notes[i] = poolManager.GetNote(NoteType.Tap);
                Assert.IsNotNull(notes[i]);
            }
            
            // 返却テスト
            for (int i = 0; i < notes.Length; i++)
            {
                poolManager.ReturnNote(notes[i], NoteType.Tap);
            }
            
            // 統計情報の確認
            int tapActive, tapPooled, holdActive, holdPooled;
            poolManager.GetPoolStatistics(
                out tapActive, out tapPooled,
                out holdActive, out holdPooled);
                
            Assert.AreEqual(0, tapActive, "アクティブなノーツが残っています");
            Assert.Greater(tapPooled, 0, "プールが空です");
            
            // クリーンアップ
            Object.Destroy(tapPrefab);
            Object.Destroy(holdPrefab);
            Object.Destroy(poolObject);
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator NoteVisual_3DPositionUpdate()
        {
            // Conductorのモック
            GameObject conductorObject = new GameObject("TestConductor");
            var conductor = conductorObject.AddComponent<Conductor>();
            var bpmField = typeof(Conductor).GetField("_songBpm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var speedField = typeof(Conductor).GetField("_noteSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var spawnField = typeof(Conductor).GetField("_spawnZ", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bpmField?.SetValue(conductor, 120f);
            speedField?.SetValue(conductor, 10f);
            spawnField?.SetValue(conductor, 20f);
            
            yield return null;
            
            // ノーツデータ
            var noteData = new NoteData
            {
                LaneIndex = 1,
                TimeToHit = 2.0f
            };
            
            // 位置計算テスト
            conductor.StartSong();
            
            yield return new WaitForSeconds(0.5f);
            
            Vector3 notePos = NoteVisualCalculator.CalculateNoteWorldPosition(
                noteData, conductor.SongPositionInBeats, conductor);
                
            // X座標の確認（レーン1）
            Assert.AreEqual(-1f, notePos.x, 0.01f);
            
            // Z座標が移動していることを確認
            Assert.Less(notePos.z, conductor.SpawnZ);
            
            Object.Destroy(conductorObject);
        }
        
        [UnityTest]
        public IEnumerator ChartData_ValidateAndSort_Integration()
        {
            // テスト用のChartDataを作成
            var chart = ScriptableObject.CreateInstance<ChartData>();
            
            // 順序がバラバラなノーツを追加
            chart.Notes.Add(new NoteData { LaneIndex = 2, TimeToHit = 8f });
            chart.Notes.Add(new NoteData { LaneIndex = 0, TimeToHit = 2f });
            chart.Notes.Add(new NoteData { LaneIndex = 3, TimeToHit = 16f });
            chart.Notes.Add(new NoteData { LaneIndex = 1, TimeToHit = 4f });
            chart.Notes.Add(new NoteData 
            { 
                NoteType = NoteType.Hold, 
                LaneIndex = 0, 
                TimeToHit = 12f,
                HoldDuration = 2f 
            });
            
            // ソート実行
            chart.SortNotesByTime();
            
            // ソート順の確認
            for (int i = 0; i < chart.Notes.Count - 1; i++)
            {
                Assert.LessOrEqual(chart.Notes[i].TimeToHit, chart.Notes[i + 1].TimeToHit,
                    $"ソート順が不正: インデックス{i}と{i+1}");
            }
            
            // バリデーション実行
            System.Collections.Generic.List<string> errors;
            bool isValid = chart.ValidateChart(out errors);
            
            // 基本的なバリデーションは成功するはず
            Assert.IsTrue(isValid || errors.Count == 1, 
                "予期しないバリデーションエラー: " + string.Join(", ", errors));
            
            // 統計情報の取得テスト
            var stats = chart.GetStatistics();
            Assert.AreEqual(5, stats.totalNotes);
            Assert.AreEqual(4, stats.tapNotes);
            Assert.AreEqual(1, stats.holdNotes);
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator NotePosition3D_DistanceCalculation()
        {
            // 各レーンの位置でノーツを作成
            for (int lane = 0; lane < 4; lane++)
            {
                var position = new NotePosition3D(lane, 10f, 0.5f);
                
                // X座標の確認
                float expectedX = NoteData.LaneXPositions[lane];
                Assert.AreEqual(expectedX, position.X, 0.001f,
                    $"レーン{lane}のX座標が不正");
                
                // Y座標の確認
                Assert.AreEqual(0.5f, position.Y, 0.001f);
                
                // Z座標の確認
                Assert.AreEqual(10f, position.Z, 0.001f);
                
                // Vector3変換の確認
                Vector3 vec = position.ToVector3();
                Assert.AreEqual(new Vector3(expectedX, 0.5f, 10f), vec);
                
                // 判定ラインまでの距離計算
                float distance = position.GetDistanceToJudgmentLine(0f);
                Assert.AreEqual(10f, distance, 0.001f);
            }
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator NoteVisualCalculator_ScaleAndAlpha()
        {
            float spawnZ = 20f;
            
            // スケール計算のテスト
            // 奥（spawnZ）で0.5倍
            float scaleAtSpawn = NoteVisualCalculator.CalculateScaleByDistance(spawnZ, spawnZ);
            Assert.AreEqual(0.5f, scaleAtSpawn, 0.01f, "スポーン位置でのスケールが不正");
            
            // 手前（0）で1.5倍
            float scaleAtHit = NoteVisualCalculator.CalculateScaleByDistance(0f, spawnZ);
            Assert.AreEqual(1.5f, scaleAtHit, 0.01f, "判定位置でのスケールが不正");
            
            // 中間地点で1.0倍
            float scaleAtMiddle = NoteVisualCalculator.CalculateScaleByDistance(10f, spawnZ);
            Assert.AreEqual(1.0f, scaleAtMiddle, 0.01f, "中間位置でのスケールが不正");
            
            // アルファ計算のテスト
            // 80%地点より手前は完全不透明
            float alphaAt70Percent = NoteVisualCalculator.CalculateAlphaByDistance(14f, spawnZ);
            Assert.AreEqual(1.0f, alphaAt70Percent, 0.01f, "70%地点でのアルファが不正");
            
            // スポーン地点で完全透明
            float alphaAtSpawn = NoteVisualCalculator.CalculateAlphaByDistance(spawnZ, spawnZ);
            Assert.AreEqual(0f, alphaAtSpawn, 0.01f, "スポーン位置でのアルファが不正");
            
            // 90%地点で半透明
            float alphaAt90Percent = NoteVisualCalculator.CalculateAlphaByDistance(18f, spawnZ);
            Assert.Greater(alphaAt90Percent, 0f, "90%地点でアルファが0");
            Assert.Less(alphaAt90Percent, 1f, "90%地点でアルファが1");
            
            yield return null;
        }
    }
}