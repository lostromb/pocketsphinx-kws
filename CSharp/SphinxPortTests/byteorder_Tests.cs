using Microsoft.VisualStudio.TestTools.UnitTesting;
using SphinxPortManaged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortTests
{
    [TestClass]
    public class byteorder_Tests
    {
        [TestMethod]
        public void TestSwapInt32()
        {
            Assert.AreEqual(unchecked((int)0x44332211), byteorder.SWAP_INT32(unchecked((int)0x11223344)));
            Assert.AreEqual(unchecked((uint)0x44332211U), byteorder.SWAP_INT32(unchecked((uint)0x11223344U)));
            Assert.AreEqual(unchecked((int)0xCCDDEEFF), byteorder.SWAP_INT32(unchecked((int)0xFFEEDDCC)));
            Assert.AreEqual(unchecked((uint)0xCCDDEEFFU), byteorder.SWAP_INT32(unchecked((uint)0xFFEEDDCCU)));
        }

        [TestMethod]
        public void TestSwapInt16()
        {
            Assert.AreEqual(unchecked((short)0x2211), byteorder.SWAP_INT16(unchecked((short)0x1122)));
            Assert.AreEqual(unchecked((ushort)0x2211U), byteorder.SWAP_INT16(unchecked((ushort)0x1122U)));
            Assert.AreEqual(unchecked((short)0xEEFF), byteorder.SWAP_INT16(unchecked((short)0xFFEE)));
            Assert.AreEqual(unchecked((ushort)0xEEFFU), byteorder.SWAP_INT16(unchecked((ushort)0xFFEEU)));
        }
    }
}
