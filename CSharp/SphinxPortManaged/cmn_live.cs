using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class cmn_live
    {
		public static void cmn_live_shiftwin(Pointer<cmn_t> cm)
		{
			float sf;
			int i;

			err.E_INFO("Update from < ");
			for (i = 0; i < cm.Deref.veclen; i++)
                err.E_INFOCONT(string.Format("{0:F2}  ", (cm.Deref.cmn_mean[i])));
			err.E_INFOCONT(">\n");

			sf = (1.0f) / cm.Deref.nframe;
			for (i = 0; i < cm.Deref.veclen; i++)
				cm.Deref.cmn_mean[i] = cm.Deref.sum[i] / cm.Deref.nframe; /* sum[i] * sf */

															  /* Make the accumulation decay exponentially */
			if (cm.Deref.nframe >= cmn.CMN_WIN_HWM) {
				sf = cmn.CMN_WIN * sf;
				for (i = 0; i < cm.Deref.veclen; i++)
					cm.Deref.sum[i] = (cm.Deref.sum[i] * sf);
				cm.Deref.nframe = cmn.CMN_WIN;
			}

            err.E_INFO("Update to   < ");
			for (i = 0; i < cm.Deref.veclen; i++)
                err.E_INFOCONT(string.Format("{0:F2}  ", (cm.Deref.cmn_mean[i])));
            err.E_INFOCONT(">\n");
		}

		public static void cmn_live_update(Pointer<cmn_t> cm)
		{
			float sf;
			int i;

			if (cm.Deref.nframe <= 0)
				return;

			err.E_INFO("Update from < ");
			for (i = 0; i < cm.Deref.veclen; i++)
                err.E_INFOCONT(string.Format("{0:F2}  ", (cm.Deref.cmn_mean[i])));
            err.E_INFOCONT(">\n");

			/* Update mean buffer */
			sf = (1.0f) / cm.Deref.nframe;
			for (i = 0; i < cm.Deref.veclen; i++)
				cm.Deref.cmn_mean[i] = cm.Deref.sum[i] / cm.Deref.nframe; /* sum[i] * sf; */

															  /* Make the accumulation decay exponentially */
			if (cm.Deref.nframe > cmn.CMN_WIN_HWM) {
				sf = cmn.CMN_WIN * sf;
				for (i = 0; i < cm.Deref.veclen; i++)
					cm.Deref.sum[i] = (cm.Deref.sum[i] * sf);
				cm.Deref.nframe = cmn.CMN_WIN;
			}

            err.E_INFO("Update to   < ");
			for (i = 0; i < cm.Deref.veclen; i++)
                err.E_INFOCONT(string.Format("{0:F2}  ", (cm.Deref.cmn_mean[i])));
            err.E_INFOCONT(">\n");
		}

		public static void cmn_live_run(Pointer<cmn_t> cm, Pointer<Pointer<float>> incep, int varnorm, int nfr)
		{
			int i, j;

			if (nfr <= 0)
				return;

			if (varnorm != 0)
				err.E_FATAL
				("Variance normalization not implemented in live mode decode\n");

			for (i = 0; i < nfr; i++) {

				/* Skip zero energy frames */
				if (incep[i][0] < 0)
					continue;

				for (j = 0; j < cm.Deref.veclen; j++) {
					cm.Deref.sum[j] += incep[i][j];
                    incep[i].Set(j, incep[i][j] - cm.Deref.cmn_mean[j]);
				}

				++cm.Deref.nframe;
			}

			/* Shift buffer down if we have more than CMN_WIN_HWM frames */
			if (cm.Deref.nframe > cmn.CMN_WIN_HWM)
				cmn_live_shiftwin(cm);
		}

    }
}
