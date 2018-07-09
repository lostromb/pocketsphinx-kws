using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    /**
         * The hash table structures.
         * Each hash table is identified by a hash_table_t structure.  hash_table_t.table is
         * pre-allocated for a user-controlled max size, and is initially empty.  As new
         * entries are created (using hash_enter()), the empty entries get filled.  If multiple
         * keys hash to the same entry, new entries are allocated and linked together in a
         * linear list.
         */
    public class hash_table_t
    {
        public Pointer<hash_entry_t> table;    /**Primary hash table, excluding entries that collide */
        public int size;     /** Primary hash table size, (is a prime#); NOTE: This is the
				    number of primary entries ALLOCATED, NOT the number of valid
				    entries in the table */
        public int inuse;        /** Number of valid entries in the table. */
        public int nocase;       /** Whether case insensitive for key comparisons */
    }
}
