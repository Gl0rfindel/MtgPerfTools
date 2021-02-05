using System;
using NUnit.Framework;

namespace MtgProfilerTools.Test
{
    public class BitOperationsTest
    {
        [Test]
        public void ToBytesInt()
        {
            var bytes = new byte[10];
            int val = BitOperations.ToBytes(513, bytes, 4);
            Assert.AreEqual(8, val);
            Assert.AreEqual(new byte[] { 0, 0, 0, 0, 1, 2, 0, 0, 0, 0}, bytes);
        }

        [Test]
        public void ToBytesIntMax()
        {
            var bytes = new byte[10];
            int val = BitOperations.ToBytes(int.MaxValue, bytes, 5);
            Assert.AreEqual(9, val);
            Assert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 255, 255, 255, 127, 0 }, bytes);
        }

        [Test]
        public void ToBytesIntMin()
        {
            var bytes = new byte[10];
            int val = BitOperations.ToBytes(int.MinValue, bytes, 4);
            Assert.AreEqual(8, val);
            Assert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 0, 0, 128, 0, 0 }, bytes);
        }
    }
}
