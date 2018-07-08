using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class listelem_alloc
    {
        public static void listelem_alloc_free(Pointer<listelem_alloc_t> list)
        {
            if (list.IsNull)
                return;

            // LOGAN modified - type checks forbid us from freeing something that we're not sure is a pointer
            //Pointer<gnode_t> gn;
            //for (gn = list.Deref.blocks; gn.IsNonNull; gn = glist.gnode_next(gn))
            //    ckd_alloc.ckd_free(glist.gnode_ptr(gn));

            glist.glist_free(list.Deref.blocks);
            glist.glist_free(list.Deref.blocksize);
            ckd_alloc.ckd_free(list);
        }
    }
}
