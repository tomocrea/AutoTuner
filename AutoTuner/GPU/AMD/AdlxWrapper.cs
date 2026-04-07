using System;
using System.Collections.Generic;
using System.Text;

namespace AutoTuner.GPU.AMD
{

    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;

    //https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose
    //https://learn.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-10.0
    internal class AdlxWrapper : IDisposable
    {
        //https://www.w3schools.com/cs/cs_properties.php
        private ADLXHelper help;
        private IADLXSystem sys;
        public IADLXGPUTuningServices tuningService { get; private set; }
        public IADLXGPUList gpuList { get; private set; }
        public IADLXPerformanceMonitoringServices perfService { get; private set; }

        //initialisation with constructor
        public AdlxWrapper()
        {
            //protected ADLXHelper help;
            //initialise adlx
            help = new ADLXHelper();
            ADLX_RESULT res = help.Initialize();
            if (res != ADLX_RESULT.ADLX_OK)
            {
                throw new Exception($"Error initialising ADLX helper: {res}");
            }

            //get system services
            sys = help.GetSystemServices();
            if (sys == null)
            {
                throw new Exception($"Error getting system services: {res}");
            }

            //get gpu tuning services
            SWIGTYPE_p_p_adlx__IADLXGPUTuningServices swigGpuTuningService = ADLX.new_gpuTuningP_Ptr();
            res = sys.GetGPUTuningServices(swigGpuTuningService);
            tuningService = ADLX.gpuTuningP_Ptr_value(swigGpuTuningService);
            //delete swig type variable https://www.swig.org/Doc4.3/Library.html#Library_nn4
            ADLX.delete_gpuTuningP_Ptr(swigGpuTuningService);
            if (res != ADLX_RESULT.ADLX_OK || tuningService == null)
            {
                throw new Exception($"Error getting GPU tuning service: {res}");
            }

            //get gpu list
            SWIGTYPE_p_p_adlx__IADLXGPUList swigGpuList = ADLX.new_IADLXGPUListP_Ptr();
            res = sys.GetGPUs(swigGpuList);
            gpuList = ADLX.IADLXGPUListP_Ptr_value(swigGpuList);
            //delete swig type variable
            ADLX.delete_IADLXGPUListP_Ptr(swigGpuList);
            if (res != ADLX_RESULT.ADLX_OK || gpuList == null)
            {
                throw new Exception($"Error getting GPU list: {res}");
            }

            //get monitoring, necessary to ensure safety
            //for IADLXGPUMetrics
            SWIGTYPE_p_p_adlx__IADLXPerformanceMonitoringServices swigPerf = ADLX.new_performanceMonitoringServicesP_Ptr();
            res = sys.GetPerformanceMonitoringServices(swigPerf);
            perfService = ADLX.performanceMonitoringServicesP_Ptr_value(swigPerf);
            ADLX.delete_performanceMonitoringServicesP_Ptr(swigPerf);

            if (res != ADLX_RESULT.ADLX_OK || perfService == null)
            {
                throw new Exception("Error getting Performance Monitoring service.");
            }
        }

        //get list of all gpus
        public List<IADLXGPU> GetGPUs()
        {
            List<IADLXGPU> newGpuList = new List<IADLXGPU>();

            if (gpuList == null)
            {
                return newGpuList;
            }

            for (uint i = 0; i < gpuList.Size(); i++)
            {
                newGpuList.Add(GetGPU(i));
            }
            return newGpuList;
        }

        //get specific gpu obj
        public IADLXGPU GetGPU(uint index = 0)
        {
            if (index >= gpuList.Size())
            {
                throw new IndexOutOfRangeException();
            }

            SWIGTYPE_p_p_adlx__IADLXGPU swigGpu = ADLX.new_IADLXGPUP_Ptr();
            ADLX_RESULT res = gpuList.At(index, swigGpu);
            IADLXGPU gpu = ADLX.IADLXGPUP_Ptr_value(swigGpu);
            ADLX.delete_IADLXGPUP_Ptr(swigGpu);
            if (res != ADLX_RESULT.ADLX_OK || gpu == null)
            {
                throw new Exception("Failed to retrieve GPU.");
            }

            return gpu;
        }

        //get adlx's auto tuning implementation
        public IADLXGPUAutoTuning? GetAutoTuning(IADLXGPU gpu) //nullable to allow return null if auto tuning is unsupported
        {
            //check if auto tuning is supported by gpu
            //use out bool due to %apply bool *OUTPUT { adlx_bool * };
            //SWIGTYPE_p_bool swigbool = ADLX.new_boolP();
            ADLX_RESULT res = tuningService.IsSupportedAutoTuning(gpu, out bool supported);
            //bool supported = ADLX.boolP_value(swigbool);
            //ADLX.delete_boolP(swigbool);
            if (supported != true)
            {
                //throw new Exception($"ADLX auto tuning not supported {res}");
                return null;
            }

            //interface object (tuningService.GetAutoTuning only accepts interface objects for 2nd param)
            SWIGTYPE_p_p_adlx__IADLXInterface swigInterface = ADLX.new_IADLXInterfaceP_Ptr();
            res = tuningService.GetAutoTuning(gpu, swigInterface);
            IADLXInterface interfaceObj = ADLX.IADLXInterfaceP_Ptr_value(swigInterface);
            ADLX.delete_IADLXInterfaceP_Ptr(swigInterface);
            if (res != ADLX_RESULT.ADLX_OK || interfaceObj == null)
            {
                throw new Exception($"Error getting ADLX auto tuning interface {res}");
            }

            //https://stackoverflow.com/questions/2458025/how-to-properly-downcast-in-c-sharp-with-a-swig-generated-interface
            //downcast superclass interface to auto tuning
            IntPtr pointer = IADLXInterface.getCPtr(interfaceObj).Handle;
            IADLXGPUAutoTuning autoTuning = new IADLXGPUAutoTuning(pointer, false);
            if (autoTuning == null)
            {
                throw new Exception($"Error getting ADLX auto tuning {res}");
            }
            return autoTuning;
        }

        //fan tuning, similar to auto tuning
        public IADLXManualFanTuning? GetManualFanTuning(IADLXGPU gpu)
        {
            //check if auto tuning is supported by gpu
            //SWIGTYPE_p_bool swigbool = ADLX.new_boolP();
            tuningService.IsSupportedManualFanTuning(gpu, out bool supported);
            //bool supported = ADLX.boolP_value(swigbool);
            //ADLX.delete_boolP(swigbool);
            if (supported != true)
            {
                return null;
            }

            //interface object (only accepts interface objects for 2nd param)
            SWIGTYPE_p_p_adlx__IADLXInterface swigInterface = ADLX.new_IADLXInterfaceP_Ptr();
            ADLX_RESULT res = tuningService.GetManualFanTuning(gpu, swigInterface);
            IADLXInterface interfaceObj = ADLX.IADLXInterfaceP_Ptr_value(swigInterface);
            ADLX.delete_IADLXInterfaceP_Ptr(swigInterface);
            if (res != ADLX_RESULT.ADLX_OK || interfaceObj == null)
            {
                throw new Exception($"Error getting ADLX fan tuning interface {res}");
            }

            //https://stackoverflow.com/questions/2458025/how-to-properly-downcast-in-c-sharp-with-a-swig-generated-interface
            //downcast superclass interface to fan tuning
            IntPtr pointer = IADLXInterface.getCPtr(interfaceObj).Handle;
            IADLXManualFanTuning fanTuning = new IADLXManualFanTuning(pointer, false);
            if (fanTuning == null)
            {
                throw new Exception($"Error getting ADLX fan tuning {res}");
            }
            return fanTuning;
        }

        //power tuning, similar to fan tuning
        public IADLXManualPowerTuning? GetManualPowerTuning(IADLXGPU gpu)
        {
            //check if power tuning is supported by gpu
            //SWIGTYPE_p_bool swigbool = ADLX.new_boolP();
            tuningService.IsSupportedManualPowerTuning(gpu, out bool supported);
            //bool supported = ADLX.boolP_value(swigbool);
            //ADLX.delete_boolP(swigbool);
            if (supported != true)
            {
                return null;
            }

            //interface object (only accepts interface objects for 2nd param)
            SWIGTYPE_p_p_adlx__IADLXInterface swigInterface = ADLX.new_IADLXInterfaceP_Ptr();
            ADLX_RESULT res = tuningService.GetManualPowerTuning(gpu, swigInterface);
            IADLXInterface interfaceObj = ADLX.IADLXInterfaceP_Ptr_value(swigInterface);
            ADLX.delete_IADLXInterfaceP_Ptr(swigInterface);
            if (res != ADLX_RESULT.ADLX_OK || interfaceObj == null)
            {
                throw new Exception($"Error getting ADLX power tuning interface {res}");
            }

            //https://stackoverflow.com/questions/2458025/how-to-properly-downcast-in-c-sharp-with-a-swig-generated-interface
            //downcast superclass interface to power tuning
            IntPtr pointer = IADLXInterface.getCPtr(interfaceObj).Handle;
            IADLXManualPowerTuning powerTuning = new IADLXManualPowerTuning(pointer, false);
            if (powerTuning == null)
            {
                throw new Exception($"Error getting ADLX power tuning {res}");
            }
            return powerTuning;
        }

        //graphics tuning
        //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-tuples
        public (IADLXManualGraphicsTuning1? tuning1, IADLXManualGraphicsTuning2? tuning2) GetManualGraphicsTuning(IADLXGPU gpu)
        {
            //check if graphics tuning is supported by gpu
            //SWIGTYPE_p_bool swigbool = ADLX.new_boolP();
            tuningService.IsSupportedManualGFXTuning(gpu, out bool supported);
            //bool supported = ADLX.boolP_value(swigbool);
            //ADLX.delete_boolP(swigbool);
            if (supported != true)
            {
                //each object nullable so can return just one or neither
                return (null,null);
            }

            //interface object (only accepts interface objects for 2nd param)
            SWIGTYPE_p_p_adlx__IADLXInterface swigInterface = ADLX.new_IADLXInterfaceP_Ptr();
            ADLX_RESULT res = tuningService.GetManualGFXTuning(gpu, swigInterface);
            IADLXInterface interfaceObj = ADLX.IADLXInterfaceP_Ptr_value(swigInterface);
            ADLX.delete_IADLXInterfaceP_Ptr(swigInterface);
            if (res != ADLX_RESULT.ADLX_OK || interfaceObj == null)
            {
                throw new Exception($"Error getting ADLX graphics tuning interface {res}");
            }

            //https://stackoverflow.com/questions/2458025/how-to-properly-downcast-in-c-sharp-with-a-swig-generated-interface
            //downcast superclass interface to power tuning
            IntPtr pointer = IADLXInterface.getCPtr(interfaceObj).Handle;

            //tuning2 (RDNA ASIC family) or fallback to tuning1 (pre-RDNA ASIC)
            //query which system used, for 1 or 2 https://gpuopen.com/manuals/adlx/adlx-sdk-references/adlx-interfaces/system/iadlxsystem1/
            //IADLXInterface QueryInterface needed, need interface id from graphicstuning2/1 and void** ppInterface
            //https://stackoverflow.com/questions/8494909/c-why-double-pointer-for-out-return-function-parameter
            SWIGTYPE_p_p_void ppInterface = ADLX.new_voidP_Ptr();

            //query if newer tuning2
            //https://learn.microsoft.com/en-us/windows/win32/api/unknwn/nf-unknwn-iunknown-queryinterface(q)
            res = interfaceObj.QueryInterface(IADLXManualGraphicsTuning2.IID(), ppInterface);

            if (res == ADLX_RESULT.ADLX_OK)
            {
                //single void pointer void* to be used for new pointer for tuning2
                SWIGTYPE_p_void pInterface = ADLX.voidP_Ptr_value(ppInterface);
                IntPtr pointer2 = SWIGTYPE_p_void.getCPtr(pInterface).Handle;

                IADLXManualGraphicsTuning2 graphicsTuning2 = new IADLXManualGraphicsTuning2(pointer2, false);
                ADLX.delete_voidP_Ptr(ppInterface);
                interfaceObj.Release();
                interfaceObj.Dispose();
                return (null, graphicsTuning2);
            }
            //else if pre-RDNA ASIC
            else
            {
                IADLXManualGraphicsTuning1 graphicsTuning1 = new IADLXManualGraphicsTuning1(pointer, false);
                ADLX.delete_voidP_Ptr(ppInterface);
                interfaceObj.Release();
                interfaceObj.Dispose();
                return (graphicsTuning1, null);
            }
        }

        //vram tuning, similar to graphics tuning
        public (IADLXManualVRAMTuning1? tuning1, IADLXManualVRAMTuning2? tuning2) GetManualVRAMTuning(IADLXGPU gpu)
        {
            //check if vram tuning is supported by gpu
            //SWIGTYPE_p_bool swigbool = ADLX.new_boolP();
            tuningService.IsSupportedManualVRAMTuning(gpu, out bool supported);
            //bool supported = ADLX.boolP_value(swigbool);
            //ADLX.delete_boolP(swigbool);
            if (supported != true)
            {
                return (null,null);
            }

            //interface object (only accepts interface objects for 2nd param)
            SWIGTYPE_p_p_adlx__IADLXInterface swigInterface = ADLX.new_IADLXInterfaceP_Ptr();
            ADLX_RESULT res = tuningService.GetManualVRAMTuning(gpu, swigInterface);
            IADLXInterface interfaceObj = ADLX.IADLXInterfaceP_Ptr_value(swigInterface);
            ADLX.delete_IADLXInterfaceP_Ptr(swigInterface);
            if (res != ADLX_RESULT.ADLX_OK || interfaceObj == null)
            {
                throw new Exception($"Error getting ADLX vram tuning interface {res}");
            }

            //https://stackoverflow.com/questions/2458025/how-to-properly-downcast-in-c-sharp-with-a-swig-generated-interface
            //downcast superclass interface to power tuning
            IntPtr pointer = IADLXInterface.getCPtr(interfaceObj).Handle;

            //tuning2 (RDNA ASIC family) or fallback to tuning1 (pre-RDNA ASIC)
            //query which system used, for 1 or 2 https://gpuopen.com/manuals/adlx/adlx-sdk-references/adlx-interfaces/system/iadlxsystem1/
            //IADLXInterface QueryInterface needed, need interface id from vramtuning2/1 and void ppInterface
            SWIGTYPE_p_p_void ppInterface = ADLX.new_voidP_Ptr();

            //query if newer tuning2
            res = interfaceObj.QueryInterface(IADLXManualVRAMTuning2.IID(), ppInterface);

            if (res == ADLX_RESULT.ADLX_OK)
            {
                SWIGTYPE_p_void pInterface = ADLX.voidP_Ptr_value(ppInterface);
                IntPtr pointer2 = SWIGTYPE_p_void.getCPtr(pInterface).Handle;

                IADLXManualVRAMTuning2 vramTuning2 = new IADLXManualVRAMTuning2(pointer2, false);
                ADLX.delete_voidP_Ptr(ppInterface);
                interfaceObj.Release();
                interfaceObj.Dispose();
                return (null, vramTuning2);
            }
            //else if pre-RDNA ASIC
            else
            {
                IADLXManualVRAMTuning1 vramTuning1 = new IADLXManualVRAMTuning1(pointer, false);
                ADLX.delete_voidP_Ptr(ppInterface);
                interfaceObj.Release();
                interfaceObj.Dispose();
                return (vramTuning1, null);
            }
        }

        public IADLXGPUMetrics GetGPUMetrics(IADLXGPU gpu)
        {
            SWIGTYPE_p_p_adlx__IADLXGPUMetrics swigMetrics = ADLX.new_gpuMetricsP_Ptr();
            ADLX_RESULT res = perfService.GetCurrentGPUMetrics(gpu, swigMetrics);
            IADLXGPUMetrics metrics = ADLX.gpuMetricsP_Ptr_value(swigMetrics);
            ADLX.delete_gpuMetricsP_Ptr(swigMetrics);

            if (res != ADLX_RESULT.ADLX_OK || metrics == null)
            {
                throw new Exception($"Failed to retrieve GPU metrics: {res}");
            }

            return metrics;
        }

        public IADLXGPUMetricsSupport GetGPUMetricsSupport(IADLXGPU gpu)
        {
            SWIGTYPE_p_p_adlx__IADLXGPUMetricsSupport swigMetricsSupport = ADLX.new_gpuMetricsSupportP_Ptr();
            ADLX_RESULT res = perfService.GetSupportedGPUMetrics(gpu, swigMetricsSupport);
            IADLXGPUMetricsSupport metricsSupport = ADLX.gpuMetricsSupportP_Ptr_value(swigMetricsSupport);
            ADLX.delete_gpuMetricsSupportP_Ptr(swigMetricsSupport);

            if (res != ADLX_RESULT.ADLX_OK || metricsSupport == null)
            {
                throw new Exception($"Failed to retrieve GPU metrics support: {res}");
            }

            return metricsSupport;
        }

        //convert SWIGTYPE char (char** in c++) to string to use in methods
        //e.g. virtual ADLX_RESULT ADLX_STD_CALL Name(const char** name) const = 0;
        //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#108-method-group-conversions
        //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/delegate-operator
        //public string String(Func<SWIGTYPE_p_p_char, ADLX_RESULT> adlxStringConvert)
        //{

        //}

        //ensure no memory leaks
        //dispose to stop memory leaks using IDisposable
        public void Dispose()
        {
            //after program run release and terminate to avoid memory leaks
            if (gpuList != null) 
            {
                gpuList.Release();
                gpuList.Dispose();
            }
            if (tuningService != null)
            {
                tuningService.Release();
                tuningService.Dispose();
            }
            if (sys != null)
            {
                sys.Dispose();
            }
            if(help != null)
            {
                ADLX_RESULT res = help.Terminate();
                help.Dispose();
            }
            if(perfService != null)
            {
                perfService.Release();
                perfService.Dispose();
            }
        }
    }
}