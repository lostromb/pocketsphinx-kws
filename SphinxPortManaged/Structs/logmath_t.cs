using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class logmath_t
    {
        public logadd_t t;
        public int refcount;
        public double _base;
        public double log_of_base;
        public double log10_of_base;
        public double inv_log_of_base;
        public double inv_log10_of_base;
        public int zero;

        public logmath_t()
        {
            t = new logadd_t();
        }
    }
}
