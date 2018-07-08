using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.CPlusPlus
{
    /// <summary>
    /// Allows pointers to read short, int32, int64 values from an underlying byte[] array
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UpcastingMemoryBlockAccess<T> : IMemoryBlockAccess<T> where T : struct
    {
        private MemoryBlock<byte> _array;

        private int _elemSize;
        private int _byteOffset;

        public UpcastingMemoryBlockAccess(MemoryBlock<byte> array, int absoluteByteOffset = 0)
        {
            _array = array;
            _byteOffset = absoluteByteOffset;

            if (!PointerHelpers.IsPrimitiveIntegerType(typeof(T)))
            {
                throw new ArgumentException("UpcastingMemoryBlockAccess can only cast to primitive integer types");
            }

            _elemSize = PointerHelpers.GetElementSize(typeof(T));
        }

        public T this[int index]
        {
            get
            {
                T returnVal;
                if (typeof(T) == typeof(byte))
                {
                    returnVal = (T)(object)(byte)_array.Data[(index * _elemSize) + _byteOffset];
                }
                else if (typeof(T) == typeof(sbyte))
                {
                    returnVal = (T)(object)(sbyte)_array.Data[(index * _elemSize) + _byteOffset];
                }
                else if (typeof(T) == typeof(short))
                {
                    returnVal = (T)(object)(short)BitConverter.ToInt16(_array.Data, (index * _elemSize) + _byteOffset);
                }
                else if (typeof(T) == typeof(ushort))
                {
                    returnVal = (T)(object)(ushort)BitConverter.ToUInt16(_array.Data, (index * _elemSize) + _byteOffset);
                }
                else if (typeof(T) == typeof(int))
                {
                    returnVal = (T)(object)(int)BitConverter.ToInt32(_array.Data, (index * _elemSize) + _byteOffset);
                }
                else if (typeof(T) == typeof(uint))
                {
                    returnVal = (T)(object)(uint)BitConverter.ToUInt32(_array.Data, (index * _elemSize) + _byteOffset);
                }
                else if (typeof(T) == typeof(long))
                {
                    returnVal = (T)(object)(long)BitConverter.ToInt64(_array.Data, (index * _elemSize) + _byteOffset);
                }
                else if (typeof(T) == typeof(ulong))
                {
                    returnVal = (T)(object)(ulong)BitConverter.ToUInt64(_array.Data, (index * _elemSize) + _byteOffset);
                }
                else if (typeof(T) == typeof(float))
                {
                    returnVal = (T)(object)(float)BitConverter.ToSingle(_array.Data, (index * _elemSize) + _byteOffset);
                }
                else if (typeof(T) == typeof(double))
                {
                    returnVal = (T)(object)(double)BitConverter.ToDouble(_array.Data, (index * _elemSize) + _byteOffset);
                }
                else
                {
                    throw new Exception("Invalid void pointer type");
                }

                return returnVal;
            }

            set
            {
                byte[] scratch;
                if (typeof(T) == typeof(byte))
                {
                    scratch = new byte[_elemSize];
                    scratch[0] = (byte)(object)value;
                }
                else if (typeof(T) == typeof(sbyte))
                {
                    scratch = new byte[_elemSize];
                    scratch[0] = (byte)(object)value;
                }
                else if (typeof(T) == typeof(short))
                {
                    scratch = BitConverter.GetBytes((short)(object)value);
                }
                else if (typeof(T) == typeof(ushort))
                {
                    scratch = BitConverter.GetBytes((ushort)(object)value);
                }
                else if (typeof(T) == typeof(int))
                {
                    scratch = BitConverter.GetBytes((int)(object)value);
                }
                else if (typeof(T) == typeof(uint))
                {
                    scratch = BitConverter.GetBytes((uint)(object)value);
                }
                else if (typeof(T) == typeof(long))
                {
                    scratch = BitConverter.GetBytes((long)(object)value);
                }
                else if (typeof(T) == typeof(ulong))
                {
                    scratch = BitConverter.GetBytes((ulong)(object)value);
                }
                else if (typeof(T) == typeof(float))
                {
                    scratch = BitConverter.GetBytes((float)(object)value);
                }
                else if (typeof(T) == typeof(double))
                {
                    scratch = BitConverter.GetBytes((double)(object)value);
                }
                else
                {
                    throw new Exception("Invalid void pointer type");
                }

                Buffer.BlockCopy(scratch, 0, _array.Data, (index * _elemSize) + _byteOffset, _elemSize);
            }
        }

        public T this[uint index]
        {
            get
            {
                return this[(int)index];
            }

            set
            {
                this[(int)index] = value;
            }
        }

        public void Free()
        {
            _array.Free();
        }

        public void Realloc(int newSize)
        {
            throw new NotImplementedException();
        }
    }
}
