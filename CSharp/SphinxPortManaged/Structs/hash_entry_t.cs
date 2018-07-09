using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class hash_entry_t
    {
        public Pointer<byte> key;        /** Key string, null if this is an empty slot.
					    NOTE that the key must not be changed once the entry
					    has been made. */
        public uint len;         /** Key-length; the key string does not have to be a C-style null
					    terminated string; it can have arbitrary binary bytes */
        public object val;          /** Value associated with above key */
        public Pointer<hash_entry_t> next;   /** For collision resolution */
    };
}
