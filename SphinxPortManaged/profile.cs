using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class profile
    {
        // FIXME reimplement this
        public static void ptmr_start(ptmr_t timer)
        {
            timer.stopwatch.Start();
        }

        public static void ptmr_stop(ptmr_t timer)
        {
            timer.stopwatch.Stop();
        }

        public static void ptmr_reset(ptmr_t timer)
        {
            timer.stopwatch.Reset();
        }

        public static void ptmr_init(ptmr_t timer)
        {
            timer.stopwatch.Reset();
        }
    }
}
