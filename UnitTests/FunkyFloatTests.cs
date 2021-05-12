using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WasatchNET;

namespace UnitTests
{
    [TestClass]
    public class FunkyFloatTests
    {
        [TestMethod]
        public void fromFloatTest19()
        {
            float f = 1.9f;
            ushort word = FunkyFloat.fromFloat(f);
            Assert.AreEqual(0x01e6, word);
        }

        [TestMethod]
        public void fromFloatTest099()
        {
            float f = 0.99f;
            ushort word = FunkyFloat.fromFloat(f);
            Assert.AreEqual(253, word);
        }

        [TestMethod]
        public void fromWordTest()
        {
            ushort word = 0x01e9;
            float f = FunkyFloat.toFloat(word);
            Assert.IsTrue(f > 1.8 && f < 2.0);
        }
    }
}
