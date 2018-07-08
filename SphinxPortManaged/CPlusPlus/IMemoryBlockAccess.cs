using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.CPlusPlus
{
    public interface IMemoryBlockAccess<T>
    {
        T this[uint index] { get; set; }
        T this[int index] { get; set; }

        void Free();
        void Realloc(int newSize);
    }
}
