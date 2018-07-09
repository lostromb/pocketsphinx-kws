using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class kws_detections
    {
        public static void kws_detections_reset(Pointer<kws_detections_t> detections)
        {
            Pointer<gnode_t> gn;

            if (detections.Deref.detect_list.IsNull)
                return;

            for (gn = detections.Deref.detect_list; gn.IsNonNull; gn = glist.gnode_next(gn))
                ckd_alloc.ckd_free((Pointer<kws_detection_t>)glist.gnode_ptr(gn));
            glist.glist_free(detections.Deref.detect_list);
            detections.Deref.detect_list = PointerHelpers.NULL<gnode_t>();
        }

        public static void kws_detections_add(Pointer<kws_detections_t> detections, Pointer<byte> keyphrase, int sf, int ef, int prob, int ascr)
        {
            Pointer < gnode_t> gn;
            Pointer <kws_detection_t> detection;
            for (gn = detections.Deref.detect_list; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                Pointer < kws_detection_t> det = (Pointer < kws_detection_t>)glist.gnode_ptr(gn);
                if (cstring.strcmp(keyphrase, det.Deref.keyphrase) == 0 && det.Deref.sf < ef && det.Deref.ef > sf)
                {
                    if (det.Deref.prob < prob)
                    {
                        det.Deref.sf = sf;
                        det.Deref.ef = ef;
                        det.Deref.prob = prob;
                        det.Deref.ascr = ascr;
                    }
                    return;
                }
            }

            /* Nothing found */
            detection = ckd_alloc.ckd_calloc_struct<kws_detection_t>(1);
            detection.Deref.sf = sf;
            detection.Deref.ef = ef;
            detection.Deref.keyphrase = keyphrase;
            detection.Deref.prob = prob;
            detection.Deref.ascr = ascr;
            detections.Deref.detect_list = glist.glist_add_ptr(detections.Deref.detect_list, detection);
        }

        public static Pointer<byte> kws_detections_hyp_str(Pointer<kws_detections_t> detections, int frame, int delay)
        {
            Pointer < gnode_t> gn;
            Pointer <byte> c;
            int len;
            Pointer <byte> hyp_str;

            len = 0;
            for (gn = detections.Deref.detect_list; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                Pointer<kws_detection_t> det = (Pointer<kws_detection_t>)glist.gnode_ptr(gn);
                if (det.Deref.ef < frame - delay)
                {
                    len += (int)cstring.strlen(det.Deref.keyphrase) + 1;
                }
            }

            if (len == 0)
            {
                return PointerHelpers.NULL<byte>();
            }

            hyp_str = ckd_alloc.ckd_calloc<byte>(len);
            c = hyp_str;
            bool cMoved = false; // LOGAN modified
            for (gn = detections.Deref.detect_list; gn.IsNonNull; gn = glist.gnode_next(gn))
            {
                Pointer < kws_detection_t> det = (Pointer < kws_detection_t>)glist.gnode_ptr(gn);
                if (det.Deref.ef < frame - delay)
                {
                    det.Deref.keyphrase.MemCopyTo(c, (int)cstring.strlen(det.Deref.keyphrase));
                    c += cstring.strlen(det.Deref.keyphrase);
                    c.Deref = (byte)' ';
                    c++;
                    cMoved = true;
                }
            }

            if (cMoved)
            {
                c--;
                c.Deref = (byte)'\0';
            }

            return hyp_str;
        }
    }
}
