using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class ciphone_t
    {
        public Pointer<byte> name;                 /**< The name of the CI phone */
        public int filler;		/**< Whether a filler phone; if so, can be substituted by
				   silence phone in left or right context position */
    }
}
