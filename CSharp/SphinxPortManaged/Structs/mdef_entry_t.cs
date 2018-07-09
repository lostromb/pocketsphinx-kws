using SphinxPortManaged.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.Structs
{
    public class mdef_entry_t
    {
        public int ssid;
        public int tmat;
        
        //union {
        //	/**< CI phone information - attributes (just "filler" for now) */
        //	struct {

        //        uint8 filler;
        //        uint8 reserved[3];
        //    }
        //    ci;
        //	/**< CD phone information - context info. */
        //	struct {

        //        uint8 wpos;
        //    uint8 ctx[3]; /**< quintphones will require hacking */
        //}
        //cd;
        //} info;

        private byte _info_0;
        private Pointer<byte> _info_1 = PointerHelpers.Malloc<byte>(3);

        public byte info_ci_filler
        {
            get
            {
                return _info_0;
            }
            set
            {
                _info_0 = value;
            }
        }

        public Pointer<byte> info_ci_reserved
        {
            get
            {
                return _info_1;
            }
            set
            {
                _info_1 = value;
            }
        }

        public byte info_cd_wpos
        {
            get
            {
                return _info_0;
            }
            set
            {
                _info_0 = value;
            }
        }
        
        public Pointer<byte> info_cd_ctx
        {
            get
            {
                return _info_1;
            }
            set
            {
                _info_1 = value;
            }
        }
    }
}
