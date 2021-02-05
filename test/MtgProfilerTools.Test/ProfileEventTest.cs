using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MtgProfilerTools.Test
{
    class ProfileEventTest
    {
        [Test]
        public void ToBytesStartWithName()
        {
            var buffer = new byte[1024];
            string name = "testtesttesttest";
            var item = new ProfileEvent(1, 100L, 234L, 1024L, ProfileEventType.StartMethod, name);
            int len = item.ToBytes(buffer, 0);
            Assert.AreEqual(36 + name.Length, len);
        }

        [Test]
        public void ToBytesStartWithNoName()
        {
            var buffer = new byte[1024];
            string name = string.Empty;
            var item = new ProfileEvent(1, 100L, 234L, 1024L, ProfileEventType.EndMethod, name);
            int len = item.ToBytes(buffer, 0);
            Assert.AreEqual(36 + name.Length, len);
        }
    }
}
