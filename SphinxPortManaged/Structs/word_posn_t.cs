using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public static class word_posn_t
    {
        public const int WORD_POSN_INTERNAL = 0; /**< Internal phone of word */
        public const int WORD_POSN_BEGIN = 1;   /**< Beginning phone of word */
        public const int WORD_POSN_END = 2;      /**< Ending phone of word */
        public const int WORD_POSN_SINGLE = 3;   /**< Single phone word (i.e. begin & end) */
        public const int WORD_POSN_UNDEFINED = 4;	/**< Undefined value, used for initial conditions, etc */
    }
}
