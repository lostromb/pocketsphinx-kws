using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class xwdssid_t
    {
        public Pointer<ushort> ssid;
        public Pointer<short> cimap;
        public int n_ssid;
    }
}
