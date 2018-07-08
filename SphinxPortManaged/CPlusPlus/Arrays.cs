
namespace SphinxPortManaged.CPlusPlus
{
    using System;

    public static class Arrays
    {
        public static T[][] InitTwoDimensionalArray<T>(int x, int y)
        {
            T[][] returnVal = new T[x][];
            for (int c = 0; c < x; c++)
            {
                returnVal[c] = new T[y];
            }
            return returnVal;
        }

        public static Pointer<Pointer<T>> InitTwoDimensionalArrayPointer<T>(int x, int y)
        {
            Pointer<Pointer<T>> returnVal = PointerHelpers.Malloc<Pointer<T>>(x);
            for (int c = 0; c < x; c++)
            {
                returnVal[c] = PointerHelpers.Malloc<T>(y);
            }
            return returnVal;
        }

        public static T[][][] InitThreeDimensionalArray<T>(int x, int y, int z)
        {
            T[][][] returnVal = new T[x][][];
            for (int c = 0; c < x; c++)
            {
                returnVal[c] = new T[y][];
                for (int a = 0; a < y; a++)
                {
                    returnVal[c][a] = new T[z];
                }
            }
            return returnVal;
        }

        //OPT: For the most part this method is used to zero-out arrays, which is usually already done by the runtime.

        public static void MemSetByte(byte[] array, byte value)
        {
            for (int c = 0; c < array.Length; c++)
            {
                array[c] = value;
            }
        }

        public static void MemSetInt(int[] array, int value, int length)
        {
            for (int c = 0; c < length; c++)
            {
                array[c] = value;
            }
        }

        public static void MemSetShort(short[] array, short value, int length)
        {
            for (int c = 0; c < length; c++)
            {
                array[c] = value;
            }
        }

        public static void MemSetFloat(float[] array, float value, int length)
        {
            for (int c = 0; c < length; c++)
            {
                array[c] = value;
            }
        }

        public static void MemSetSbyte(sbyte[] array, sbyte value, int length)
        {
            for (int c = 0; c < length; c++)
            {
                array[c] = value;
            }
        }

        public static void MemSetWithOffset<T>(T[] array, T value, int offset, int length)
        {
            for (int c = offset; c < offset + length; c++)
            {
                array[c] = value;
            }
        }

        public static void MemMove<T>(T[] array, int src_idx, int dst_idx, int length)
        {
            if (src_idx == dst_idx || length == 0)
                return;

            // Do regions overlap?
            if (src_idx + length > dst_idx || dst_idx + length > src_idx)
            {
                // Take extra precautions
                if (dst_idx < src_idx)
                {
                    // Copy forwards
                    for (int c = 0; c < length; c++)
                    {
                        array[c + dst_idx] = array[c + src_idx];
                    }
                }
                else
                {
                    // Copy backwards
                    for (int c = length - 1; c >= 0; c--)
                    {
                        array[c + dst_idx] = array[c + src_idx];
                    }
                }
            }
            else
            {
                // Memory regions cannot overlap; just do a fast copy
                Array.Copy(array, src_idx, array, dst_idx, length);
            }
        }

        public static void MemMoveInt(int[] array, int src_idx, int dst_idx, int length)
        {
            if (src_idx == dst_idx || length == 0)
                return;

            // Do regions overlap?
            if (src_idx + length > dst_idx || dst_idx + length > src_idx)
            {
                // Take extra precautions
                if (dst_idx < src_idx)
                {
                    // Copy forwards
                    for (int c = 0; c < length; c++)
                    {
                        array[c + dst_idx] = array[c + src_idx];
                    }
                }
                else
                {
                    // Copy backwards
                    for (int c = length - 1; c >= 0; c--)
                    {
                        array[c + dst_idx] = array[c + src_idx];
                    }
                }
            }
            else
            {
                // Memory regions cannot overlap; just do a fast copy
                Array.Copy(array, src_idx, array, dst_idx, length);
            }
        }

        public static void MemMoveShort(short[] array, int src_idx, int dst_idx, int length)
        {
            if (src_idx == dst_idx || length == 0)
                return;

            // Do regions overlap?
            if (src_idx + length > dst_idx || dst_idx + length > src_idx)
            {
                // Take extra precautions
                if (dst_idx < src_idx)
                {
                    // Copy forwards
                    for (int c = 0; c < length; c++)
                    {
                        array[c + dst_idx] = array[c + src_idx];
                    }
                }
                else
                {
                    // Copy backwards
                    for (int c = length - 1; c >= 0; c--)
                    {
                        array[c + dst_idx] = array[c + src_idx];
                    }
                }
            }
            else
            {
                // Memory regions cannot overlap; just do a fast copy
                Array.Copy(array, src_idx, array, dst_idx, length);
            }
        }
    }
}
