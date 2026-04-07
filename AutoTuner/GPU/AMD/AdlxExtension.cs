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
            if (adlxObject == null)
            {
                return;
            }

            //try finally for disposable
            //https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-idisposable#the-tryfinally-block
            try
            {
                adlxObject.Release();
            }
            finally
            {
                adlxObject.Dispose();
            }
        }
    }
}
