using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class dictword_t
    {
        public Pointer<byte> word;     /**< Ascii word string */
        public Pointer<short> ciphone; /**< Pronunciation */
        public int pronlen;  /**< Pronunciation length */
        public int alt;    /**< Next alternative pronunciation id, NOT_S3WID if none */
        public int basewid;    /**< Base pronunciation id */
    }
}
