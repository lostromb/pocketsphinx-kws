using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class hash_iter_t
    {
        public Pointer<hash_table_t> ht;  /**< Hash table we are iterating over. */
        public Pointer<hash_entry_t> ent; /**< Current entry in that table. */
        public uint idx;        /**< Index of next bucket to search. */
    };
}
