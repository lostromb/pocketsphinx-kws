using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class ps_lattice
    {
        public static int ps_lattice_free(Pointer<ps_lattice_t> dag)
        {
            if (dag.IsNull)
                return 0;
            if (--dag.Deref.refcount > 0)
                return dag.Deref.refcount;
            logmath.logmath_free(dag.Deref.lmath);
            dict.dict_free(dag.Deref.dict);
            listelem_alloc.listelem_alloc_free(dag.Deref.latnode_alloc);
            listelem_alloc.listelem_alloc_free(dag.Deref.latlink_alloc);
            listelem_alloc.listelem_alloc_free(dag.Deref.latlink_list_alloc);
            ckd_alloc.ckd_free(dag.Deref.hyp_str);
            ckd_alloc.ckd_free(dag);
            return 0;
        }
    }
}
