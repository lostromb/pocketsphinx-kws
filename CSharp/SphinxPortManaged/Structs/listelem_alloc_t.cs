using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class listelem_alloc_t
    {
        public Pointer<Pointer<char>> freelist;            /**< ptr to first element in freelist */
        public Pointer<gnode_t> blocks;             /**< Linked list of blocks allocated. */
        public Pointer<gnode_t> blocksize;          /**< Number of elements in each block */
        public uint elemsize;            /**< Number of (char *) in element */
        public uint blk_alloc;           /**< Number of alloc operations before increasing blocksize */
        public uint n_blocks;
        public uint n_alloc;
        public uint n_freed;
    }
}
