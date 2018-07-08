using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class cd_tree_t
    {
        public short ctx;
        public short n_down;

        //  union {
		//      int32 pid; /**< Phone ID (leafnode) */
        //      int32 down; /**< Next level of the tree (offset from start of cd_trees) */
        //  } c;
        private int _c;

        public int c_pid
        {
            get
            {
                return _c;
            }
            set
            {
                _c = value;
            }
        }
        
        public int c_down
        {
            get
            {
                return _c;
            }
            set
            {
                _c = value;
            }
        }
    }
}
