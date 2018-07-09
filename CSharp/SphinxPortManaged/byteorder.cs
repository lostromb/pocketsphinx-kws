using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class byteorder
    {
        // TODO TEST THESE

        public static void SWAP_INT16(Pointer<short> x)
        {
            x.Deref = SWAP_INT16(+x);
        }

        public static short SWAP_INT16(short x)
        {
            return (short)SWAP_INT16((ushort)x);
        }

        public static void SWAP_INT16(Pointer<ushort> x)
        {
            x.Deref = SWAP_INT16(+x);
        }

        public static ushort SWAP_INT16(ushort x)
        {
            return (ushort)((0x00ffU & (x >> 8)) | (0xff00U & (x << 8)));
        }

        public static void SWAP_INT32(Pointer<int> x)
        {
            x.Deref = SWAP_INT32(+x);
        }

        public static int SWAP_INT32(int x)
        {
            return (int)SWAP_INT32((uint)x);
        }

        public static void SWAP_INT32(Pointer<uint> x)
        {
            x.Deref = SWAP_INT32(+x);
        }

        public static uint SWAP_INT32(uint x)
        {
            return ((0x000000ffU & (x >> 24)) |
                (0x0000ff00U & (x >> 8)) |
                (0x00ff0000U & (x << 8)) |
                (0xff000000U & (x << 24)));
        }

        public static void SWAP_FLOAT32(Pointer<float> x)
        {
            byte[] bytes = BitConverter.GetBytes(x.Deref);
            byte s;
            s = bytes[0];
            bytes[0] = bytes[3];
            bytes[3] = s;
            s = bytes[1];
            bytes[1] = bytes[2];
            bytes[2] = s;
            x.Deref = BitConverter.ToSingle(bytes, 0);
        }
    }
}
