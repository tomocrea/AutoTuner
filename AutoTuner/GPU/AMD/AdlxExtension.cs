using System;
using System.Collections.Generic;
using System.Text;

namespace AutoTuner.GPU.AMD
{
    internal static class AdlxExtension
    {
        //extension allows adding functionality to adlx interface, method to release and dispose in one
        //https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods

        //to be able to release and dispose to remove object from memory and use dispose

        public static void Destroy(this IADLXInterface adlxObject)
        {
            //check if object exists in c# (wrapper)
            if (adlxObject == null)
            {
                return;
            }

            //try finally for disposable
            //https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-idisposable#the-tryfinally-block
            try
            {
                //check if object exists in c++ (memory)
                if (IADLXInterface.getCPtr(adlxObject).Handle != IntPtr.Zero)
                {
                    adlxObject.Release();
                }
            }
            finally
            {
                adlxObject.Dispose();
            }
        }
    }
}
