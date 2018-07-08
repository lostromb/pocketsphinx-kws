using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class gauden_t
    {
        public Pointer<Pointer<Pointer<Pointer<float>>>> mean;    /**< mean[codebook][feature][codeword] vector */
        public Pointer<Pointer<Pointer<Pointer<float>>>> var; /**< like mean; diagonal covariance vector only */
        public Pointer<Pointer<Pointer<float>>> det;  /**< log(determinant) for each variance vector;
			   actually, log(sqrt(2*pi*det)) */
        public Pointer<logmath_t> lmath;   /**< log math computation */
        public int n_mgau;   /**< Number codebooks */
        public int n_feat;   /**< Number feature streams in each codebook */
        public int n_density;    /**< Number gaussian densities in each codebook-feature stream */
        public Pointer<int> featlen;	/**< feature length for each feature */
    }
}
