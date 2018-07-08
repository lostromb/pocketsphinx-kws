using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.CPlusPlus
{
    public class DowncastingMemoryBlockAccess<T> : IMemoryBlockAccess<byte> where T : struct
    {
        private MemoryBlock<T> _array;
        private int _elemSize;

        public DowncastingMemoryBlockAccess(MemoryBlock<T> array)
        {
            _array = array;

            if (!PointerHelpers.IsPrimitiveIntegerType(typeof(T)))
            {
                throw new ArgumentException("DowncastingMemoryBlockAccess can only cast to primitive integer types");
            }

            _elemSize = PointerHelpers.GetElementSize(typeof(T));
        }

        public byte this[int index]
        {
            get
            {
                byte[] range;
                if (typeof(T) == typeof(float))
                {
                    range = BitConverter.GetBytes((float)(object)_array.Data[index / _elemSize]);
                }
                if (typeof(T) == typeof(double))
                {
                    range = BitConverter.GetBytes((double)(object)_array.Data[index / _elemSize]);
                }
                else
                {
                    throw new Exception("Invalid void pointer type");
                }

                return range[index % _elemSize];
            }

            set
            {
                byte[] x = new byte[] { value };
                Buffer.BlockCopy(x, 0, _array.Data, index, 1);
            }
        }

        public byte this[uint index]
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
