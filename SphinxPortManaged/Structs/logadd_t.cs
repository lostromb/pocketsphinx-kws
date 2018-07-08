using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class logadd_t
    {
        /** Table, in unsigned integers of (width) bytes. */
        public Pointer<byte> table_uint8;
        public Pointer<ushort> table_ushort; // LOGAN modified - these are pointers that reinterpret the data inside of table_uint8.
        public Pointer<uint> table_uint32; // OPT these could be changed into a proper union type later on for performance
        /** Number of elements in (table).  This is never smaller than 256 (important!) */
        public uint table_size;
        /** Width of elements of (table). */
        public byte width;
        /** Right shift applied to elements in (table). */
        public sbyte shift;
    }
}
