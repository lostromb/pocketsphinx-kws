using SphinxPortManaged.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged.CPlusPlus
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string rootDir = "C:\\Code\\Durandal";
            string modelDir = rootDir + "\\Data\\sphinx\\en-us-semi";
            string dictFile = rootDir + "\\Data\\sphinx\\cmudict_SPHINX_40.txt";
            trigger_adapter trigger = psphinx_trigger.trigger_create(modelDir, dictFile, true);

            Pointer<byte> configuration1 = cstring.ToCString("ACTIVATE/3.16227766016838e-13/\nEXECUTE COURSE/3.16227766016838e-13/\n");
            psphinx_trigger.trigger_reconfigure(trigger, configuration1);
            
            // Read input file 1
            byte[] file_bytes = System.IO.File.ReadAllBytes(rootDir + "\\Extensions\\Pocketsphinx\\Test1.raw");
            int samples = file_bytes.Length / 2;
            short[] file = new short[samples];
            Pointer<short> input_file_ptr = new Pointer<short>(new UpcastingMemoryBlockAccess<short>(new MemoryBlock<byte>(file_bytes)), 0);
            input_file_ptr.MemCopyTo(file, 0, samples);
            input_file_ptr = new Pointer<short>(file);
            
            // Send it to the trigger in chunks
            Pointer<byte> hyp = PointerHelpers.Malloc<byte>(512);
            Pointer<byte> lasthyp = PointerHelpers.Malloc<byte>(512);
            psphinx_trigger.trigger_start_processing(trigger);

            for (int cursor = 0; cursor < (samples - 159); cursor += 160)
            {
                psphinx_trigger.trigger_process_samples(trigger, input_file_ptr + cursor, 160);
                psphinx_trigger.trigger_get_last_hyp(trigger, hyp);
                if (cstring.strlen(hyp) != 0 && cstring.strcmp(hyp, lasthyp) != 0)
                {
                    Console.Write("Got trigger {0} at sample number {1}\n", cstring.FromCString(hyp), cursor);
                    cstring.strncpy(lasthyp, hyp, 512);
                }
            }

            psphinx_trigger.trigger_stop_processing(trigger);

            Console.Write("\n\nON TO TEST #2\n\n\n");

            Pointer<byte> configuration2 = cstring.ToCString("COMPUTER/3.16227766016838e-13/\n");
            psphinx_trigger.trigger_reconfigure(trigger, configuration2);

            // Read input file 2
            file_bytes = System.IO.File.ReadAllBytes(rootDir + "\\Extensions\\Pocketsphinx\\Test2.raw");
            samples = file_bytes.Length / 2;
            file = new short[samples];
            input_file_ptr = new Pointer<short>(new UpcastingMemoryBlockAccess<short>(new MemoryBlock<byte>(file_bytes)), 0);
            input_file_ptr.MemCopyTo(file, 0, samples);
            input_file_ptr = new Pointer<short>(file);

            // Send it to the trigger in chunks
            hyp = PointerHelpers.Malloc<byte>(512);
            lasthyp = PointerHelpers.Malloc<byte>(512);
            psphinx_trigger.trigger_start_processing(trigger);

            for (int cursor = 0; cursor < (samples - 159); cursor += 160)
            {
                psphinx_trigger.trigger_process_samples(trigger, input_file_ptr + cursor, 160);
                psphinx_trigger.trigger_get_last_hyp(trigger, hyp);
                if (cstring.strlen(hyp) != 0 && cstring.strcmp(hyp, lasthyp) != 0)
                {
                    Console.Write("Got trigger {0} at sample number {1}\n", cstring.FromCString(hyp), cursor);
                    cstring.strncpy(lasthyp, hyp, 512);
                }
            }

            psphinx_trigger.trigger_stop_processing(trigger);

            psphinx_trigger.trigger_free(trigger);
        }
    }
}
