using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class dict2pid_t
    {
        public int refcount;
        public Pointer<bin_mdef_t> mdef;
        public Pointer<dict_t> dict;
        public Pointer<Pointer<Pointer<ushort>>> ldiph_lc;
        public Pointer<Pointer<xwdssid_t>> rssid;
        public Pointer<Pointer<Pointer<ushort>>> lrdiph_rc;
        public Pointer<Pointer<xwdssid_t>> lrssid;
    }
}
