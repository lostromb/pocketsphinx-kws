using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class vector
    {
        public static double vector_sum_norm(Pointer<float> vec, int len)
        {
            double sum, f;
            int i;

            sum = 0.0;
            for (i = 0; i < len; i++)
                sum += vec[i];

            if (sum != 0.0)
            {
                f = 1.0 / sum;
                for (i = 0; i < len; i++)
                    vec[i] = (float)(vec[i] * f);
            }

            return sum;
        }


        public static void vector_floor(Pointer<float> vec, int len, double flr)
        {
            int i;

            for (i = 0; i < len; i++)
                if (vec[i] < flr)
                    vec[i] = (float)flr;
        }


        public static void vector_nz_floor(Pointer<float> vec, int len, double flr)
        {
            int i;

            for (i = 0; i < len; i++)
                if ((vec[i] != 0.0) && (vec[i] < flr))
                    vec[i] = (float)flr;
        }
    }
}
