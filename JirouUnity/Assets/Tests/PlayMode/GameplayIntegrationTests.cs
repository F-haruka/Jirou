using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Jirou.Core;

namespace Jirou.Tests
{
    public class GameplayIntegrationTests
    {
        private GameObject testGameObject;
        
        [SetUp]
        public void Setup()
        {
            testGameObject = new GameObject("TestObject");
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.Destroy(testGameObject);
            }
        }
        
        [UnityTest]
        public IEnumerator NoteMovement_MovesForwardOverTime()
        {
            // ノーツが時間経過とともに前進するテスト
            var noteObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            noteObject.transform.position = new Vector3(0, 0.5f, 20f);
            
            float initialZ = noteObject.transform.position.z;
            float moveSpeed = 5f;
            
            // 1秒待機
            float startTime = Time.time;
            while (Time.time - startTime < 1f)
            {
                noteObject.transform.position += Vector3.back * moveSpeed * Time.deltaTime;
                yield return null;
            }
            
            float finalZ = noteObject.transform.position.z;
            
            // Z座標が減少していることを確認（手前に移動）
            Assert.Less(finalZ, initialZ);
            
            // 期待される移動距離に近いことを確認
            float expectedDistance = moveSpeed * 1f;
            float actualDistance = initialZ - finalZ;
            Assert.AreEqual(expectedDistance, actualDistance, 0.5f);
            
            Object.Destroy(noteObject);
        }
        
        [UnityTest]
        public IEnumerator JudgmentLine_DetectsNoteAtCorrectPosition()
        {
            // 判定ラインでノーツを検出するテスト
            var judgmentLine = new GameObject("JudgmentLine");
            judgmentLine.transform.position = Vector3.zero;
            
            var note = GameObject.CreatePrimitive(PrimitiveType.Cube);
            note.transform.position = new Vector3(0, 0.5f, 5f);
            
            // ノーツを判定ラインまで移動
            while (note.transform.position.z > 0.1f)
            {
                note.transform.position += Vector3.back * 10f * Time.deltaTime;
                yield return null;
            }
            
            // 判定ライン付近にあることを確認
            Assert.LessOrEqual(Mathf.Abs(note.transform.position.z), 0.5f);
            
            Object.Destroy(judgmentLine);
            Object.Destroy(note);
        }
        
        [UnityTest]
        public IEnumerator LanePositioning_MaintainsCorrectXPosition()
        {
            // 4レーンの正しいX座標配置テスト
            float[] lanePositions = { -3f, -1f, 1f, 3f };
            
            foreach (float xPos in lanePositions)
            {
                var note = GameObject.CreatePrimitive(PrimitiveType.Cube);
                note.transform.position = new Vector3(xPos, 0.5f, 10f);
                
                // X座標が維持されることを確認
                yield return new WaitForSeconds(0.1f);
                Assert.AreEqual(xPos, note.transform.position.x, 0.001f);
                
                Object.Destroy(note);
            }
        }
        
        [UnityTest]
        public IEnumerator AudioSync_MaintainsConsistentTiming()
        {
            // オーディオ同期の一貫性テスト
            float startDspTime = (float)AudioSettings.dspTime;
            
            yield return new WaitForSeconds(1f);
            
            float endDspTime = (float)AudioSettings.dspTime;
            float elapsed = endDspTime - startDspTime;
            
            // 経過時間が約1秒であることを確認
            Assert.AreEqual(1f, elapsed, 0.1f);
        }
        
        // Conductorクラスの統合テスト
        [UnityTest]
        public IEnumerator Conductor_Initialization_CreatesInstance()
        {
            // Conductorオブジェクトを作成
            var conductorObject = new GameObject("TestConductor");
            var conductor = conductorObject.AddComponent<Conductor>();
            
            // 初期化待機
            yield return null;
            
            // インスタンスが作成されていることを確認
            Assert.IsNotNull(Conductor.Instance);
            Assert.AreEqual(conductor, Conductor.Instance);
            
            Object.Destroy(conductorObject);
        }
        
        [UnityTest]
        public IEnumerator Conductor_BPMChange_UpdatesCalculations()
        {
            // Conductorオブジェクトを作成
            var conductorObject = new GameObject("TestConductor");
            var conductor = conductorObject.AddComponent<Conductor>();
            // Conductorのフィールドはprivateなのでリフレクションを使用
            var bpmField = typeof(Conductor).GetField("_songBpm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bpmField?.SetValue(conductor, 120f);
            
            yield return null;
            
            // BPMを変更
            conductor.ChangeBPM(140f);
            
            // BPMが更新されていることを確認
            Assert.AreEqual(140f, conductor.SongBpm);
            
            Object.Destroy(conductorObject);
        }
        
        [UnityTest]
        public IEnumerator Conductor_NoteZPosition_UpdatesOverTime()
        {
            // Conductorオブジェクトを作成
            var conductorObject = new GameObject("TestConductor");
            var conductor = conductorObject.AddComponent<Conductor>();
            // Conductorのフィールドを設定
            var bpmField = typeof(Conductor).GetField("_songBpm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var speedField = typeof(Conductor).GetField("_noteSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var spawnField = typeof(Conductor).GetField("_spawnZ", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bpmField?.SetValue(conductor, 120f);
            speedField?.SetValue(conductor, 10f);
            spawnField?.SetValue(conductor, 20f);
            
            // AudioSourceをモック
            var audioSource = conductorObject.AddComponent<AudioSource>();
            audioSource.clip = AudioClip.Create("TestClip", 44100, 1, 44100, false);
            var sourceField = typeof(Conductor).GetField("_songSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            sourceField?.SetValue(conductor, audioSource);
            
            yield return null;
            
            // ノーツの初期位置
            float noteBeat = 4f;
            float initialZ = conductor.GetNoteZPosition(noteBeat);
            
            // Z座標が正しく計算されることを確認
            Assert.Greater(initialZ, 0f);
            
            Object.Destroy(conductorObject);
        }
        
        [UnityTest]
        public IEnumerator Conductor_ShouldSpawnNote_TimingCheck()
        {
            // Conductorオブジェクトを作成
            var conductorObject = new GameObject("TestConductor");
            var conductor = conductorObject.AddComponent<Conductor>();
            var bpmField = typeof(Conductor).GetField("_songBpm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bpmField?.SetValue(conductor, 120f);
            
            yield return null;
            
            // 生成タイミングチェック
            float noteBeat = 10f;
            float beatsInAdvance = 2f;
            
            // 現在のビートが生成タイミング前
            bool shouldSpawn = conductor.ShouldSpawnNote(noteBeat, beatsInAdvance);
            Assert.IsFalse(shouldSpawn);
            
            Object.Destroy(conductorObject);
        }
        
        [UnityTest]
        public IEnumerator Conductor_IsNoteInHitZone_Detection()
        {
            // Conductorオブジェクトを作成
            var conductorObject = new GameObject("TestConductor");
            var conductor = conductorObject.AddComponent<Conductor>();
            var hitZField = typeof(Conductor).GetField("_hitZ", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            hitZField?.SetValue(conductor, 0f);
            
            yield return null;
            
            // 判定ゾーンチェック
            Assert.IsTrue(conductor.IsNoteInHitZone(0.5f, 1.0f));
            Assert.IsFalse(conductor.IsNoteInHitZone(2.0f, 1.0f));
            Assert.IsTrue(conductor.IsNoteInHitZone(1.0f, 1.0f)); // 境界上
            
            Object.Destroy(conductorObject);
        }
        
        [UnityTest]
        public IEnumerator Conductor_GetTimeUntilBeat_Calculation()
        {
            // Conductorオブジェクトを作成
            var conductorObject = new GameObject("TestConductor");
            var conductor = conductorObject.AddComponent<Conductor>();
            var bpmField = typeof(Conductor).GetField("_songBpm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bpmField?.SetValue(conductor, 120f); // 1ビート = 0.5秒
            
            yield return null;
            
            // 残り時間計算
            float targetBeat = 10f;
            float timeUntil = conductor.GetTimeUntilBeat(targetBeat);
            
            // 現在のビートが0の場合、10ビート * 0.5秒 = 5秒
            Assert.Greater(timeUntil, 0f);
            
            Object.Destroy(conductorObject);
        }
        
        [UnityTest]
        public IEnumerator Conductor_SingletonPattern_PreventsMultipleInstances()
        {
            // 最初のConductorを作成
            var conductor1Object = new GameObject("Conductor1");
            var conductor1 = conductor1Object.AddComponent<Conductor>();
            
            yield return null;
            
            // 2つ目のConductorを作成
            var conductor2Object = new GameObject("Conductor2");
            var conductor2 = conductor2Object.AddComponent<Conductor>();
            
            yield return null;
            
            // 最初のインスタンスのみが残る
            Assert.AreEqual(conductor1, Conductor.Instance);
            
            // 2つ目が破棄されることを確認
            yield return null;
            Assert.IsTrue(conductor2Object == null || !conductor2Object.activeInHierarchy);
            
            Object.Destroy(conductor1Object);
        }
        
        [UnityTest]
        public IEnumerator Conductor_InvalidBPM_HandlesGracefully()
        {
            // Conductorオブジェクトを作成
            var conductorObject = new GameObject("TestConductor");
            var conductor = conductorObject.AddComponent<Conductor>();
            var bpmField = typeof(Conductor).GetField("_songBpm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bpmField?.SetValue(conductor, 120f);
            
            yield return null;
            
            // 無効なBPMを設定してもエラーにならない
            conductor.ChangeBPM(0f);
            Assert.AreEqual(120f, conductor.SongBpm); // 変更されない
            
            conductor.ChangeBPM(-100f);
            Assert.AreEqual(120f, conductor.SongBpm); // 変更されない
            
            Object.Destroy(conductorObject);
        }
        
        [UnityTest]
        public IEnumerator Conductor_DontDestroyOnLoad_PersistsAcrossScenes()
        {
            // Conductorオブジェクトを作成
            var conductorObject = new GameObject("PersistentConductor");
            var conductor = conductorObject.AddComponent<Conductor>();
            
            yield return null;
            
            // DontDestroyOnLoadが設定されていることを確認
            Assert.AreEqual(conductor.gameObject.scene.name, "DontDestroyOnLoad");
            
            Object.Destroy(conductorObject);
        }
    }
}