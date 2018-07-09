using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.CPlusPlus
{
    public class BasicMemoryBlockAccess<T> : IMemoryBlockAccess<T>
    {
        private MemoryBlock<T> _block;

        public BasicMemoryBlockAccess(MemoryBlock<T> block)
        {
            _block = block;
        }

        public T this[int index]
        {
            get
            {
                return _block.Data[index];
            }

            set
            {
                _block.Data[index] = value;
            }
        }

        public T this[uint index]
        {
            get
            {
                return _block.Data[index];
            }

            set
            {
                _block.Data[index] = value;
            }
        }
        
        public void Free()
        {
            _block.Free();
        }

        public void Realloc(int newSize)
        {
            _block.Realloc(newSize);
        }

        public MemoryBlock<T> Block => _block;
    }
}
