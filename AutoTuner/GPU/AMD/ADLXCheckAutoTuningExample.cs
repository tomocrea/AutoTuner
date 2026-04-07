using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace AutoTuner.GPU.AMD
{
    internal class ADLXCheckAutoTuningExample
    {
        //This is an example to test and show how interacting with ADLX works using C# bindings
        public void ExampleMain()
        {
            //use using and try/finally for disposable to avoid memory leaks
            //https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-idisposable

            //1. initialise AdlxInterface
            using (AdlxWrapper init = new AdlxWrapper())
            {
                //https://www.swig.org/Doc4.0/SWIGDocumentation.html#CSharp_typemap_examples
                //type pointers from adlx in .i file

                //2. create pointer for gpu name
                //SWIGTYPE_p_p_char ppName = ADLX.new_charP_Ptr();
                try
                {
                    //3. get list of all gpus
                    List<IADLXGPU> gpus = init.GetGPUs();
                    Console.WriteLine("GPUs found: ");
                    //4. loop through gpu list, printing their index and names
                    for (int i = 0; i < gpus.Count; i++)
                    {
                        IADLXGPU gpu = gpus[i];
                        try
                        {
                            gpu.Name(out string s);
                            //string s = ADLX.charP_Ptr_value(ppName);
                            Console.WriteLine("GPU " + i + " " + s);
                        }
                        finally
                        {
                            //release for adlx then dispose for disposable https://www.swig.org/Doc4.0/SWIGDocumentation.html#CSharp_memory_management_member_variables
                            //destroy does both
                            //5. always release and dispose objects (do both with Destroy() from AdlxExtension
                            gpu.Destroy();
                        }
                    }
                }
                finally
                {
                    //ADLX.delete_charP_Ptr(ppName);
                }
                //https://stackoverflow.com/questions/64418624/why-do-i-get-crtisvalidheappointerblock-and-or-is-block-type-validheader-b

                //6. CLI select gpu from list that was just printed
                Console.WriteLine("Select GPU number: ");
                uint gpuNum = Convert.ToUInt32(Console.ReadLine());

                IADLXGPU gpu1 = init.GetGPU(gpuNum);
                try
                {
                    //7. use GetAutoTuning to check if it is supported and print result
                    IADLXGPUAutoTuning auto = init.GetAutoTuning(gpu1);
                    if (auto == null)
                    {
                        Console.WriteLine("Auto tuning unsupported");
                    }
                    else
                    {
                        try
                        {
                            //SWIGTYPE_p_bool ptr = ADLX.new_boolP();
                            try
                            {
                                auto.IsSupportedOverclockGPU(out bool supported);
                                //bool supported = ADLX.boolP_value(ptr);
                                if (supported == true)
                                {
                                    Console.WriteLine("Auto tuning supported");
                                }
                            }
                            finally
                            {
                                //ADLX.delete_boolP(ptr);
                            }
                        }
                        finally
                        {
                            auto.Destroy();
                        }
                    }
                }
                finally
                {
                    gpu1.Destroy();
                }
            }
        }
    }
}
