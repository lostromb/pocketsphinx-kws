using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SphinxPortManaged;
using SphinxPortManaged.CPlusPlus;

namespace SphinxPortTests
{
    [TestClass]
    public class CPlusPlusTests
    {
        [TestMethod]
        public void TestPointerToBoolean()
        {
            Pointer<int> ptr = default(Pointer<int>);
            Assert.IsFalse(ptr.IsNonNull);
            ptr = PointerHelpers.Malloc<int>(10);
            Assert.IsTrue(ptr.IsNonNull);
            ptr.Free();
            Assert.IsFalse(ptr.IsNonNull);
        }

        [TestMethod]
        public void TestPointerDeref()
        {
            Pointer<int> ptr = PointerHelpers.Malloc<int>(10);
            Assert.AreEqual(0, +ptr);
            ptr = PointerHelpers.Malloc<int>(0);
            try
            {
                int x = +ptr;
                Assert.Fail("Should have thrown exception");
            }
            catch (Exception) { }
        }

        [TestMethod]
        public void TestPointerPoint()
        {
            Pointer<int> ptr = PointerHelpers.Malloc<int>(10);
            for (int c = 0; c < 10; c++)
            {
                ptr[c] = c;
            }
            Pointer<int> ptr2 = ptr + 6;
            Assert.AreEqual(6, +ptr2);
            ptr2 = ptr.Point(6);
            Assert.AreEqual(6, +ptr2);
            ptr2 = ptr2 - 3;
            Assert.AreEqual(3, +ptr2);
            Pointer<int> ptr3 = ptr + 3;
            Assert.AreEqual(ptr2, ptr3);
        }

        [TestMethod]
        public void TestPointerIncrement()
        {
            Pointer<int> ptr = PointerHelpers.Malloc<int>(10);
            Pointer<int> ptr2 = ptr;
            for (int c = 0; c < 10; c++, ptr2++)
            {
                ptr[c] = c;
                Assert.AreEqual(c, ptr2.Deref);
            }
        }

        [TestMethod]
        public void TestVoidPointerByteToByte()
        {
            Pointer<byte> pointer = PointerHelpers.Malloc<byte>(80);
            Pointer<byte> voidPointer = pointer.ReinterpretCast<byte>();
            pointer[0] = 99;
            pointer[1] = 128;
            Assert.AreEqual(99, voidPointer[0]);
            Assert.AreEqual(128, voidPointer[1]);
            pointer[20] = 53;
            pointer[21] = 240;
            voidPointer = voidPointer + 20;
            Assert.AreEqual(53, voidPointer[0]);
            Assert.AreEqual(240, voidPointer[1]);
        }

        [TestMethod]
        public void TestVoidPointerByteToSbyte()
        {
            Pointer<byte> pointer = PointerHelpers.Malloc<byte>(80);
            Pointer<sbyte> voidPointer = pointer.ReinterpretCast<sbyte>();
            pointer[0] = 99;
            pointer[1] = 128;
            Assert.AreEqual((sbyte)99, voidPointer[0]);
            Assert.AreEqual((sbyte)-128, voidPointer[1]);
            pointer[20] = 53;
            pointer[21] = 240;
            voidPointer = voidPointer + 20;
            Assert.AreEqual((sbyte)53, voidPointer[0]);
            Assert.AreEqual((sbyte)-16, voidPointer[1]);
        }

        //[TestMethod]
        //public void TestVoidPointerShort()
        //{
        //    Pointer<byte> pointer = PointerHelpers.Malloc<byte>(80);
        //    VoidPointer<short> voidPointer = new VoidPointer<short>(pointer, 0);
        //    pointer[0] = 99;
        //    pointer[1] = 128;
        //    Assert.AreEqual(99, voidPointer[0]);
        //    Assert.AreEqual(128, voidPointer[1]);
        //    pointer[20] = 53;
        //    pointer[21] = 240;
        //    voidPointer = voidPointer + 20;
        //    Assert.AreEqual(53, voidPointer[0]);
        //    Assert.AreEqual(240, voidPointer[1]);
        //}

        //[TestMethod]
        //public void TestVoidPointerUShort()
        //{
        //    Pointer<byte> pointer = PointerHelpers.Malloc<byte>(80);
        //    VoidPointer<ushort> voidPointer = new VoidPointer<ushort>(pointer, 0);
        //    pointer[0] = 99;
        //    pointer[1] = 128;
        //    Assert.AreEqual(99, voidPointer[0]);
        //    Assert.AreEqual(128, voidPointer[1]);
        //    pointer[20] = 53;
        //    pointer[21] = 240;
        //    voidPointer = voidPointer + 20;
        //    Assert.AreEqual(53, voidPointer[0]);
        //    Assert.AreEqual(240, voidPointer[1]);
        //}

        [TestMethod]
        public void TestVoidPointerByteToULong()
        {
            Pointer<byte> pointer = PointerHelpers.Malloc<byte>(80);
            Pointer<ulong> voidPointer = pointer.Point(20).ReinterpretCast<ulong>();
            pointer[20] = 80;
            pointer[21] = 226;
            pointer[22] = 182;
            pointer[23] = 173;
            pointer[24] = 76;
            pointer[25] = 82;
            pointer[26] = 0;
            pointer[27] = 0;
            Assert.AreEqual(90489285435984UL, voidPointer[0]);
            Assert.AreEqual(0UL, voidPointer[1]);
            voidPointer[2] = 67823904875254UL;
            Assert.AreEqual(246, pointer[36]);
            Assert.AreEqual(226, pointer[37]);
            Assert.AreEqual(193, pointer[38]);
            Assert.AreEqual(123, pointer[39]);
            Assert.AreEqual(175, pointer[40]);
            Assert.AreEqual(61, pointer[41]);
            Assert.AreEqual(0, pointer[42]);
            Assert.AreEqual(0, pointer[43]);
        }

        //[TestMethod]
        //public void TestVoidPointerFloatToByte()
        //{
        //    float testVal = 1.54323923f;
        //    Pointer<float> pointer = PointerHelpers.Malloc<float>(80);
        //    Pointer<byte> voidPointer = pointer.Point(20).ReinterpretCast<byte>();
        //    pointer[20] = testVal;
        //    byte[] compare = BitConverter.GetBytes(testVal);
        //    Assert.AreEqual(compare[0], voidPointer[0]);
        //    Assert.AreEqual(compare[1], voidPointer[1]);
        //    Assert.AreEqual(compare[2], voidPointer[2]);
        //    Assert.AreEqual(compare[3], voidPointer[3]);
        //    Assert.AreEqual(0, voidPointer[4]);
        //}

        [TestMethod]
        public void TestPointerReallocLarger()
        {
            Pointer<int> pointer = PointerHelpers.Malloc<int>(4);
            for (int c = 0; c < 4; c++)
            {
                pointer[c] = c;
            }

            pointer = pointer.Realloc(8);
            for (int c = 4; c < 8; c++)
            {
                pointer[c] = c;
            }
            for (int c = 0; c < 8; c++)
            {
                Assert.AreEqual(c, pointer[c]);
            }
        }

        [TestMethod]
        public void TestPointerReallocSmaller()
        {
            Pointer<int> pointer = PointerHelpers.Malloc<int>(8);
            for (int c = 0; c < 8; c++)
            {
                pointer[c] = c;
            }

            pointer = pointer.Realloc(4);

            for (int c = 0; c < 4; c++)
            {
                Assert.AreEqual(c, pointer[c]);
            }
        }

        [TestMethod]
        public void TestPointerReallocMalloc()
        {
            Pointer<int> pointer = PointerHelpers.NULL<int>();
            pointer = pointer.Realloc(4);

            for (int c = 0; c < 4; c++)
            {
                pointer[c] = c;
            }
            for (int c = 0; c < 4; c++)
            {
                Assert.AreEqual(c, pointer[c]);
            }
        }

        [TestMethod]
        public void TestPointerReallocFree()
        {
            Pointer<int> pointer = PointerHelpers.Malloc<int>(8);
            for (int c = 0; c < 8; c++)
            {
                pointer[c] = c;
            }

            pointer = pointer.Realloc(0);

            try
            {
                pointer[0].GetHashCode();
                Assert.Fail();
            }
            catch (NullReferenceException) { }
        }

        [TestMethod]
        public void TestScanfDN()
        {
            int d;
            int n;
            int r;

            r = stdio.sscanf_d_n(cstring.ToCString(" -4534.9%289#  "), out d, out n);
            Assert.AreEqual(-4534, d);
            Assert.AreEqual(6, n);
            Assert.AreEqual(1, r);

            r = stdio.sscanf_d_n(cstring.ToCString(" Arggg  "), out d, out n);
            Assert.AreEqual(0, r);

            r = stdio.sscanf_d_n(cstring.ToCString("+327667  "), out d, out n);
            Assert.AreEqual(327667, d);
            Assert.AreEqual(7, n);
            Assert.AreEqual(1, r);
        }

        [TestMethod]
        public void TestScanfF()
        {
            double d;
            int r;

            r = stdio.sscanf_f(cstring.ToCString("  +9.90234e5 "), out d);
            Assert.AreEqual(990234, d, 1);
            Assert.AreEqual(1, r);

            r = stdio.sscanf_f(cstring.ToCString(" Arggg  "), out d);
            Assert.AreEqual(0, r);

            r = stdio.sscanf_f(cstring.ToCString("-1234567"), out d);
            Assert.AreEqual(-1234567, d);
            Assert.AreEqual(1, r);
        }

        [TestMethod]
        public void TestScanfSN()
        {
            Pointer<byte> d = PointerHelpers.Malloc<byte>(50);
            int len;
            int r;

            r = stdio.sscanf_s_n(cstring.ToCString("  Three  "), d, out len);
            Assert.AreEqual("Three", cstring.FromCString(d));
            Assert.AreEqual(7, len);
            Assert.AreEqual(1, r);

            r = stdio.sscanf_s_n(cstring.ToCString("      "), d, out len);
            Assert.AreEqual(0, r);

            r = stdio.sscanf_s_n(cstring.ToCString("1"), d, out len);
            Assert.AreEqual("1", cstring.FromCString(d));
            Assert.AreEqual(1, len);
            Assert.AreEqual(1, r);
        }
    }
}
