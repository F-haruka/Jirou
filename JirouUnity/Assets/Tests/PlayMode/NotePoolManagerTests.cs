using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Jirou.Core;

namespace Jirou.Tests.PlayMode
{
    /// <summary>
    /// NotePoolManagerクラスのユニットテスト
    /// </summary>
    [TestFixture]
    public class NotePoolManagerTests
    {
        private GameObject poolManagerObject;
        private NotePoolManager poolManager;
        private GameObject tapNotePrefab;
        private GameObject holdNotePrefab;

        [SetUp]
        public void SetUp()
        {
            // テスト用のプレハブを作成
            tapNotePrefab = new GameObject("TapNotePrefab");
            holdNotePrefab = new GameObject("HoldNotePrefab");

            // NotePoolManagerのGameObjectを作成
            poolManagerObject = new GameObject("TestNotePoolManager");
            poolManager = poolManagerObject.AddComponent<NotePoolManager>();

            // プレハブを設定（リフレクションを使用）
            var tapPrefabField = typeof(NotePoolManager).GetField("_tapNotePrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var holdPrefabField = typeof(NotePoolManager).GetField("_holdNotePrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            tapPrefabField?.SetValue(poolManager, tapNotePrefab);
            holdPrefabField?.SetValue(poolManager, holdNotePrefab);

            // 初期プールサイズを設定
            var initialSizeField = typeof(NotePoolManager).GetField("_initialPoolSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            initialSizeField?.SetValue(poolManager, 10);
        }

        [TearDown]
        public void TearDown()
        {
            // テスト用オブジェクトをクリーンアップ
            if (poolManagerObject != null)
                Object.DestroyImmediate(poolManagerObject);
            if (tapNotePrefab != null)
                Object.DestroyImmediate(tapNotePrefab);
            if (holdNotePrefab != null)
                Object.DestroyImmediate(holdNotePrefab);
        }

        [UnityTest]
        public IEnumerator GetNote_TapNote_ReturnsActiveNote()
        {
            yield return null;  // Awakeを待つ

            // Act
            GameObject note = poolManager.GetNote(NoteType.Tap);

            // Assert
            Assert.IsNotNull(note);
            Assert.IsTrue(note.activeInHierarchy);
            Assert.IsTrue(note.name.Contains("TapNotePrefab"));
        }

        [UnityTest]
        public IEnumerator GetNote_HoldNote_ReturnsActiveNote()
        {
            yield return null;  // Awakeを待つ

            // Act
            GameObject note = poolManager.GetNote(NoteType.Hold);

            // Assert
            Assert.IsNotNull(note);
            Assert.IsTrue(note.activeInHierarchy);
            Assert.IsTrue(note.name.Contains("HoldNotePrefab"));
        }

        [UnityTest]
        public IEnumerator ReturnNote_DeactivatesAndReturnsToPool()
        {
            yield return null;  // Awakeを待つ

            // Arrange
            GameObject note = poolManager.GetNote(NoteType.Tap);
            note.transform.position = new Vector3(5, 5, 5);
            note.transform.rotation = Quaternion.Euler(45, 45, 45);

            // Act
            poolManager.ReturnNote(note, NoteType.Tap);

            // Assert
            Assert.IsFalse(note.activeInHierarchy);
            Assert.AreEqual(Vector3.zero, note.transform.position);
            Assert.AreEqual(Quaternion.identity, note.transform.rotation);
            Assert.AreEqual(Vector3.one, note.transform.localScale);
        }

        [UnityTest]
        public IEnumerator GetNote_AfterReturn_ReusesPooledNote()
        {
            yield return null;  // Awakeを待つ

            // Arrange
            GameObject firstNote = poolManager.GetNote(NoteType.Tap);
            poolManager.ReturnNote(firstNote, NoteType.Tap);

            // Act
            GameObject secondNote = poolManager.GetNote(NoteType.Tap);

            // Assert
            Assert.AreSame(firstNote, secondNote);
            Assert.IsTrue(secondNote.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator GetPoolStatistics_ReturnsCorrectCounts()
        {
            yield return null;  // Awakeを待つ

            // Arrange
            GameObject tapNote1 = poolManager.GetNote(NoteType.Tap);
            GameObject tapNote2 = poolManager.GetNote(NoteType.Tap);
            GameObject holdNote = poolManager.GetNote(NoteType.Hold);
            poolManager.ReturnNote(tapNote2, NoteType.Tap);

            // Act
            poolManager.GetPoolStatistics(out int tapActive, out int tapPooled, 
                                         out int holdActive, out int holdPooled);

            // Assert
            Assert.AreEqual(1, tapActive);  // tapNote1がアクティブ
            Assert.IsTrue(tapPooled > 0);   // プールに返却されたノート
            Assert.AreEqual(1, holdActive);  // holdNoteがアクティブ
            Assert.IsTrue(holdPooled >= 0);  // プールに残っているノート
        }

        [UnityTest]
        public IEnumerator ClearPool_DeactivatesAllNotes()
        {
            yield return null;  // Awakeを待つ

            // Arrange
            GameObject tapNote = poolManager.GetNote(NoteType.Tap);
            GameObject holdNote = poolManager.GetNote(NoteType.Hold);

            // Act
            poolManager.ClearPool();

            // Assert
            Assert.IsFalse(tapNote.activeInHierarchy);
            Assert.IsFalse(holdNote.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator GetNote_EmptyPool_CreatesNewNote()
        {
            yield return null;  // Awakeを待つ

            // Arrange - プールからすべてのノーツを取得
            var notes = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < 15; i++)  // 初期プールサイズより多く取得
            {
                notes.Add(poolManager.GetNote(NoteType.Tap));
            }

            // Act
            GameObject newNote = poolManager.GetNote(NoteType.Tap);

            // Assert
            Assert.IsNotNull(newNote);
            Assert.IsTrue(newNote.activeInHierarchy);
            Assert.IsFalse(notes.Contains(newNote));  // 新しいノート
        }

        [UnityTest]
        public IEnumerator ReturnNote_NullNote_DoesNotThrow()
        {
            yield return null;  // Awakeを待つ

            // Act & Assert - 例外が発生しないことを確認
            Assert.DoesNotThrow(() => poolManager.ReturnNote(null, NoteType.Tap));
        }

        [UnityTest]
        public IEnumerator GetNote_MultipleTimes_AllNotesUnique()
        {
            yield return null;  // Awakeを待つ

            // Act
            GameObject note1 = poolManager.GetNote(NoteType.Tap);
            GameObject note2 = poolManager.GetNote(NoteType.Tap);
            GameObject note3 = poolManager.GetNote(NoteType.Tap);

            // Assert
            Assert.AreNotSame(note1, note2);
            Assert.AreNotSame(note2, note3);
            Assert.AreNotSame(note1, note3);
        }

        [UnityTest]
        public IEnumerator Instance_Singleton_ReturnsSameInstance()
        {
            yield return null;  // Awakeを待つ

            // Act
            var instance1 = NotePoolManager.Instance;
            var instance2 = NotePoolManager.Instance;

            // Assert
            Assert.AreSame(instance1, instance2);
            Assert.AreSame(poolManager, instance1);
        }

        [UnityTest]
        public IEnumerator GetNote_WithoutPrefab_ReturnsNull()
        {
            // Arrange - プレハブをnullに設定
            var newPoolManager = new GameObject("TestNullPrefab").AddComponent<NotePoolManager>();
            
            yield return null;  // Awakeを待つ

            // Act
            GameObject note = newPoolManager.GetNote(NoteType.Tap);

            // Assert
            Assert.IsNull(note);

            // Cleanup
            Object.DestroyImmediate(newPoolManager.gameObject);
        }

        [UnityTest]
        public IEnumerator ReturnNote_MaxPoolSize_DestroysExcessNotes()
        {
            yield return null;  // Awakeを待つ

            // Arrange - maxPoolSizeを小さく設定
            var maxSizeField = typeof(NotePoolManager).GetField("_maxPoolSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            maxSizeField?.SetValue(poolManager, 2);

            // 3つのノーツを取得して返却
            var notes = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < 3; i++)
            {
                notes.Add(poolManager.GetNote(NoteType.Tap));
            }

            // Act
            foreach (var note in notes)
            {
                poolManager.ReturnNote(note, NoteType.Tap);
            }

            yield return null;  // 破棄を待つ

            // Assert
            poolManager.GetPoolStatistics(out _, out int tapPooled, out _, out _);
            Assert.LessOrEqual(tapPooled, 2);  // プールサイズが制限されている
        }

        [UnityTest]
        public IEnumerator ClearPool_AfterGettingNotes_RebuildsPoolCorrectly()
        {
            yield return null;  // Awakeを待つ

            // Arrange
            GameObject tapNote = poolManager.GetNote(NoteType.Tap);
            GameObject holdNote = poolManager.GetNote(NoteType.Hold);
            poolManager.ReturnNote(tapNote, NoteType.Tap);

            // Act
            poolManager.ClearPool();

            // Assert
            poolManager.GetPoolStatistics(out int tapActive, out int tapPooled, 
                                         out int holdActive, out int holdPooled);
            Assert.AreEqual(0, tapActive);
            Assert.AreEqual(0, holdActive);
            Assert.IsTrue(tapPooled > 0);  // プールが再構築されている
        }
    }
}