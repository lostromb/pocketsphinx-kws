using SphinxPortManaged.CPlusPlus;
using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class psphinx_trigger
    {
        public static trigger_adapter trigger_create(string modelDir, string dictionaryFile, bool verboseLogging)
        {
            //printf("            creating recognizer\n");

            Pointer<ps_decoder_t> ps = PointerHelpers.NULL<ps_decoder_t>();
            Pointer < cmd_ln_t> config = PointerHelpers.NULL<cmd_ln_t>();
            
            config = cmd_ln.cmd_ln_init(config, pocketsphinx.ps_args(), 1,
                "-hmm", modelDir,
                "-dict", dictionaryFile,
                "-verbose", "y");

            ps = pocketsphinx.ps_init(config);

            cmd_ln.cmd_ln_free_r(config);

            trigger_adapter adapter = new trigger_adapter();
            adapter.ps = ps;
            adapter.user_is_speaking = false;
            adapter.last_hyp = PointerHelpers.Malloc<byte>(512);
            adapter.last_hyp[0] = 0;

            return adapter;
        }

        public static int trigger_reconfigure(trigger_adapter adapter, Pointer<byte> keyfile)
        {
            Pointer<ps_decoder_t> ps = adapter.ps;

            //printf("            reconfiguring %s\n", keyfile);

            if (pocketsphinx.ps_set_kws(ps, cstring.ToCString("keyword_search"), keyfile) != 0)
            {
                return -1;
            }

            if (pocketsphinx.ps_set_search(ps, cstring.ToCString("keyword_search")) != 0)
            {
                return -1;
            }

            return 0;
        }

        public static int trigger_start_processing(trigger_adapter adapter)
        {
            //printf("            process start\n");
            
            Pointer<ps_decoder_t> ps = adapter.ps;

            adapter.utt_started = true;
            return pocketsphinx.ps_start_utt(ps); // todo use ps_start_stream?
        }

        public static int trigger_stop_processing(trigger_adapter adapter)
        {
            //printf("            process stop\n");

            Pointer < ps_decoder_t> ps = adapter.ps;

            if (adapter.utt_started)
            {
                pocketsphinx.ps_end_utt(ps);
                adapter.utt_started = false;
                if (adapter.last_hyp.IsNonNull)
                {
                    adapter.last_hyp[0] = (byte)'\0';
                }
            }

            return 0;
        }

        public static bool trigger_process_samples(trigger_adapter adapter, Pointer<short> samples, int numSamples)
        {
            Pointer < ps_decoder_t> ps = adapter.ps;

            pocketsphinx.ps_process_raw(ps, samples, (uint)numSamples, 0, 0);
            byte in_speech = pocketsphinx.ps_get_in_speech(ps);
            if (in_speech != 0 && !adapter.user_is_speaking)
            {
                adapter.user_is_speaking = true;
            }

            bool returnVal = false;
            
            BoxedValueInt score = new BoxedValueInt();
            Pointer<byte> hyp = pocketsphinx.ps_get_hyp(ps, score);

            if (hyp.IsNonNull)
            {
                //printf("            tenative hyp %s\n", hyp);
                if (!adapter.triggered)
                {
                    returnVal = true;
                    adapter.triggered = true;
                    uint hypsize = cstring.strlen(hyp);
                    cstring.strncpy(adapter.last_hyp, hyp, hypsize);
                    adapter.last_hyp[hypsize] = 0;
                    //printf("            adapter last hyp is %s\n", hyp);
                }
            }

            if (in_speech == 0 && adapter.user_is_speaking)
            {
                /* speech .Deref. silence transition, time to start new utterance  */
                pocketsphinx.ps_end_utt(ps);
                adapter.utt_started = false;

                hyp = pocketsphinx.ps_get_hyp(ps, score);

                if (hyp.IsNonNull)
                {
                    //printf("            final hyp %s\n", hyp);
                    if (!adapter.triggered)
                    {
                        returnVal = true;
                        adapter.triggered = true;
                        uint hypsize = cstring.strlen(hyp);
                        cstring.strncpy(adapter.last_hyp, hyp, hypsize);
                        adapter.last_hyp[hypsize] = 0;
                        //printf("            adapter last hyp is %s\n", hyp);
                    }
                }

                if (pocketsphinx.ps_start_utt(ps) < 0)
                {
                    //printf("            failed to restart utterance\n");
                }
                adapter.utt_started = true;

                adapter.user_is_speaking = false;
                adapter.triggered = false;
                //printf("Ready....\n");
            }

            return returnVal;
        }

        public static void trigger_get_last_hyp(trigger_adapter adapter, Pointer<byte> buffer)
        {
            cstring.strncpy(buffer, adapter.last_hyp, 512);
        }

        public static int trigger_free(trigger_adapter adapter)
        {
            pocketsphinx.ps_free(adapter.ps);
            return 0;
        }
    }
}
