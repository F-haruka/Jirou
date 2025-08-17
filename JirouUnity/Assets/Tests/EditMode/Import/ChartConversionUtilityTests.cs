using NUnit.Framework;
using Jirou.Editor.Import;
using Jirou.Core;

namespace Jirou.Tests.Editor
{
    [TestFixture]
    public class ChartConversionUtilityTests
    {
        [Test]
        public void ConvertMillisecondsToBeats_正常な変換()
        {
            // BPM 120の場合、1ビート = 0.5秒 = 500ms
            float result = ChartConversionUtility.ConvertMillisecondsToBeats(1000, 120f);
            Assert.AreEqual(2f, result, 0.001f);
        }
        
        [Test]
        public void ConvertLPBToBeats_正常な変換()
        {
            // num=16, LPB=4 の場合、16/4 = 4ビート
            float result = ChartConversionUtility.ConvertLPBToBeats(16, 4);
            Assert.AreEqual(4f, result, 0.001f);
        }
        
        [Test]
        public void ConvertNoteType_Tapノーツ()
        {
            var result = ChartConversionUtility.ConvertNoteType(1);
            Assert.AreEqual(NoteType.Tap, result);
        }
        
        [Test]
        public void ConvertNoteType_Holdノーツ()
        {
            var result = ChartConversionUtility.ConvertNoteType(2);
            Assert.AreEqual(NoteType.Hold, result);
        }
        
        [Test]
        public void CalculateHoldDuration_正常な計算()
        {
            var startNote = new NotesEditorNote
            {
                LPB = 4,
                num = 8,
                type = 2,
                notes = new System.Collections.Generic.List<object>
                {
                    new NotesEditorNote { LPB = 4, num = 16 }
                }
            };
            
            float result = ChartConversionUtility.CalculateHoldDuration(startNote);
            Assert.AreEqual(2f, result, 0.001f); // (16-8)/4 = 2
        }
        
        [Test]
        public void CalculateHoldDuration_Tapノーツは0を返す()
        {
            var tapNote = new NotesEditorNote
            {
                LPB = 4,
                num = 8,
                type = 1,
                notes = null
            };
            
            float result = ChartConversionUtility.CalculateHoldDuration(tapNote);
            Assert.AreEqual(0f, result, 0.001f);
        }
        
        [Test]
        public void ConvertMillisecondsToBeats_ゼロBPMは0を返す()
        {
            float result = ChartConversionUtility.ConvertMillisecondsToBeats(1000, 0f);
            Assert.AreEqual(0f, result, 0.001f);
        }
        
        [Test]
        public void ConvertLPBToBeats_ゼロLPBは0を返す()
        {
            float result = ChartConversionUtility.ConvertLPBToBeats(16, 0);
            Assert.AreEqual(0f, result, 0.001f);
        }
    }
}