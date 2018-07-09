using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class dict_t
    {
        public int refcnt;
        public Pointer<bin_mdef_t> mdef;   /**< Model definition used for phone IDs; NULL if none used */
        public Pointer<dictword_t> word;   /**< Array of entries in dictionary */
        public Pointer<hash_table_t> ht;   /**< Hash table for mapping word strings to word ids */
        public int max_words;    /**< #Entries allocated in dict, including empty slots */
        public int n_word;   /**< #Occupied entries in dict; ie, excluding empty slots */
        public int filler_start; /**< First filler word id (read from filler dict) */
        public int filler_end;   /**< Last filler word id (read from filler dict) */
        public int startwid;   /**< FOR INTERNAL-USE ONLY */
        public int finishwid;  /**< FOR INTERNAL-USE ONLY */
        public int silwid; /**< FOR INTERNAL-USE ONLY */
        public int nocase;
    }
}
