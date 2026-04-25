//https://gpuopen.com/manuals/adlx/programming-with-adlx/adlx-samples/csharp-samples/adlxcsharpbind/
%module(directors="1") ADLX

//C++
%{
#include <Windows.h>
//include adlx interfaces
#include "./ADLX-1.4/SDK/Include/ADLXDefines.h"
#include "./ADLX-1.4/SDK/Include/ADLXStructures.h"
#include "./ADLX-1.4/SDK/Include/ICollections.h"
#include "./ADLX-1.4/SDK/Include/ISystem.h"
#include "./ADLX-1.4/SDK/Include/ILog.h"
#include "./ADLX-1.4/SDK/Include/IGPUTuning.h"
#include "./ADLX-1.4/SDK/Include/IGPUAutoTuning.h"
#include "./ADLX-1.4/SDK/Include/IGPUTuning1.h"
#include "./ADLX-1.4/SDK/Include/IGPUManualGFXTuning.h"
#include "./ADLX-1.4/SDK/Include/IGPUManualFanTuning.h"
#include "./ADLX-1.4/SDK/Include/IGPUManualPowerTuning.h"
#include "./ADLX-1.4/SDK/Include/IGPUManualVRAMTuning.h"
#include "./ADLX-1.4/SDK/Include/IGPUPresetTuning.h"
#include "./ADLX-1.4/SDK/Include/IPerformanceMonitoring.h"
#include "./ADLX-1.4/SDK/Include/IPerformanceMonitoring1.h"
#include "./ADLX-1.4/SDK/Include/IPerformanceMonitoring2.h"

#include "./ADLX-1.4/SDK/ADLXHelper/Windows/Cpp/ADLXHelper.h"

//types
typedef int64_t         adlx_int64;
typedef int32_t         adlx_int32;
typedef int16_t         adlx_int16;
typedef int8_t          adlx_int8;
typedef uint64_t        adlx_uint64;
typedef uint32_t        adlx_uint32;
typedef uint16_t        adlx_uint16;
typedef uint8_t         adlx_uint8;
typedef size_t          adlx_size;
typedef void*           adlx_handle;
typedef double          adlx_double;
typedef float           adlx_float;
typedef void            adlx_void;
typedef  long            adlx_long;
typedef adlx_int32      adlx_int;
typedef unsigned long   adlx_ulong;
typedef adlx_uint32     adlx_uint;
typedef bool            adlx_bool;
typedef wchar_t WCHAR;
typedef WCHAR TCHAR;

//ms
#define ADLX_CORE_LINK          __declspec(dllexport)
#define ADLX_STD_CALL           __stdcall
#define ADLX_CDECL_CALL         __cdecl
#define ADLX_FAST_CALL          __fastcall
#define ADLX_INLINE              __inline
#define ADLX_FORCEINLINE         __forceinline
#define ADLX_NO_VTABLE          __declspec(novtable)

//IID
#define ADLX_DECLARE_IID(X) static ADLX_INLINE const wchar_t* IID()  { return X; }
#define ADLX_IS_IID(X, Y) (!wcscmp (X, Y))
#define ADLX_DECLARE_ITEM_IID(X) static ADLX_INLINE const wchar_t* ITEM_IID()  { return X; }

using namespace adlx;
%}

//SWIG
//types
typedef int64_t         adlx_int64;
typedef int32_t         adlx_int32;
typedef int16_t         adlx_int16;
typedef int8_t          adlx_int8;
typedef uint64_t        adlx_uint64;
typedef uint32_t        adlx_uint32;
typedef uint16_t        adlx_uint16;
typedef uint8_t         adlx_uint8;
typedef size_t          adlx_size;
typedef void*           adlx_handle;
typedef double          adlx_double;
typedef float           adlx_float;
typedef void            adlx_void;
typedef long            adlx_long;
typedef adlx_int32      adlx_int;
typedef unsigned long   adlx_ulong;
typedef adlx_uint32     adlx_uint;
typedef bool            adlx_bool;
typedef wchar_t WCHAR;
typedef WCHAR TCHAR;

//ms
#define ADLX_CORE_LINK      __declspec(dllexport)
#define ADLX_STD_CALL       __stdcall
#define ADLX_CDECL_CALL     __cdecl
#define ADLX_FAST_CALL      __fastcall
#define ADLX_INLINE         __inline
#define ADLX_FORCEINLINE    __forceinline
#define ADLX_NO_VTABLE      __declspec(novtable)

//IID
//#define ADLX_DECLARE_IID(X) static ADLX_INLINE const wchar_t* IID() { return X; }
//#define ADLX_IS_IID(X, Y) (!wcscmp (X, Y))
//#define ADLX_DECLARE_ITEM_IID(X) static ADLX_INLINE const wchar_t* ITEM_IID() { return X; }

//swig libraries
%include stdint.i
%include carrays.i
%include windows.i
%include typemaps.i

//tuning listeners
%feature("director") IADLXGPUAutoTuningCompleteListener;
%feature("director") IADLXGPUTuningChangedListenerListener;

//pointers for use in adlx methods as out parameters 
//*OUTPUT means it is the pointer in c++ used for output
//https://www.swig.org/Doc4.2/Arguments.html#Arguments_nn5
//%apply int *output example
//https://www.swig.org/Doc4.2/Typemaps.html#Typemaps_nn13
//more on %apply
//https://www.swig.org/Doc4.2/Typemaps.html#Typemaps_nn48
//type mapping
//https://www.swig.org/Doc4.2/CSharp.html#CSharp_type_mapping
//bool in c++ is a byte, in c# is 4 bytes
//https://stackoverflow.com/questions/32110152/c-sharp-marshalling-bool
//https://learn.microsoft.com/en-us/dotnet/standard/native-interop/customize-struct-marshalling
//ADLXDefines.h: typedef bool adlx_bool;
%apply bool *OUTPUT { adlx_bool * };
%apply int *OUTPUT { adlx_int * };
%apply unsigned int *OUTPUT { adlx_uint * };
%apply double *OUTPUT { adlx_double * };
//char** is a double pointer meaning it is a pointer to a string in c++
//example: getName requires char** param to point to output string
//https://gpuopen.com/manuals/adlx/adlx-sdk-references/adlx-interfaces/gpu/iadlxgpu/name/
//char * should be used as read only, since modifying string data could cause errors
//https://www.swig.org/Doc4.2/SWIG.html#SWIG_nn14
//char ** turned into pp_char since there is not enough info to determine how it is used
//https://www.swig.org/Doc4.2/SWIG.html#SWIG_nn16

//https://stackoverflow.com/questions/56630778/generate-swig-proxy-for-c-function-with-param-char
//snippet modified from:
//https://stackoverflow.com/a/56699201
%typemap(csin,pre="global::System.IntPtr tmp$csinput=global::System.IntPtr.Zero;",
              post="$csinput=global::System.Runtime.InteropServices.Marshal.PtrToStringAnsi(tmp$csinput);") 
              const char **OUTPUT "ref tmp$csinput";
%typemap(cstype) const char **OUTPUT "out string";
%typemap(imtype) const char **OUTPUT "ref global::System.IntPtr"
%apply const char **OUTPUT { const char ** };

//adlx interfaces
%include "./ADLX-1.4/SDK/Include/ADLXDefines.h"
%include "./ADLX-1.4/SDK/Include/ADLXStructures.h"
%include "./ADLX-1.4/SDK/Include/ICollections.h"
%include "./ADLX-1.4/SDK/Include/ISystem.h"
%include "./ADLX-1.4/SDK/Include/ILog.h"
%include "./ADLX-1.4/SDK/Include/IGPUTuning.h"
%include "./ADLX-1.4/SDK/Include/IGPUAutoTuning.h"
%include "./ADLX-1.4/SDK/Include/IGPUTuning1.h"
%include "./ADLX-1.4/SDK/Include/IGPUManualGFXTuning.h"
%include "./ADLX-1.4/SDK/Include/IGPUManualFanTuning.h"
%include "./ADLX-1.4/SDK/Include/IGPUManualPowerTuning.h"
%include "./ADLX-1.4/SDK/Include/IGPUManualVRAMTuning.h"
%include "./ADLX-1.4/SDK/Include/IGPUPresetTuning.h"
%include "./ADLX-1.4/SDK/ADLXHelper/Windows/Cpp/ADLXHelper.h"
%include "./ADLX-1.4/SDK/Include/IPerformanceMonitoring.h"
%include "./ADLX-1.4/SDK/Include/IPerformanceMonitoring1.h"
%include "./ADLX-1.4/SDK/Include/IPerformanceMonitoring2.h"

//pointer functions
%include cpointer.i
%pointer_functions(ADLX_DISPLAY_TYPE, displayTypeP);
%pointer_functions(ADLX_DISPLAY_CONNECTOR_TYPE, disConnectTypeP);
%pointer_functions(ADLX_DISPLAY_SCAN_TYPE, disScanTypeP);
%pointer_functions(adlx_size, adlx_sizeP);
%pointer_functions(ADLX_IntRange, adlx_intRangeP);
%pointer_functions(ADLX_MEMORYTIMING_DESCRIPTION, memoryTimingDescriptionP);

//ppointer macro
%define %ppointer_functions(TYPE,NAME)
%{
static TYPE *new_##NAME() { return new TYPE(); }
static TYPE *copy_##NAME(TYPE value) { return new TYPE(value); }
//https://stackoverflow.com/questions/64418624/why-do-i-get-crtisvalidheappointerblock-and-or-is-block-type-validheader-b
static void delete_##NAME(TYPE *obj) { if (obj) delete obj; }
static void NAME ##_assign(TYPE *obj, TYPE value) { *obj = value; }
static TYPE NAME ##_value(TYPE *obj) { return *obj; }
%}

TYPE *new_##NAME();
TYPE *copy_##NAME(TYPE value);
void  delete_##NAME(TYPE *obj);
void  NAME##_assign(TYPE *obj, TYPE value);
TYPE  NAME##_value(TYPE *obj);
%enddef

using namespace adlx;

%ppointer_functions(IADLXGPUTuningServices*, gpuTuningP_Ptr);
%ppointer_functions(IADLXManualGraphicsTuning1*, manualGfxTuning1P_Ptr);
%ppointer_functions(IADLXManualGraphicsTuning2*, manualGfxTuning2P_Ptr);
%ppointer_functions(IADLXManualFanTuning*, manualFanTuningP_Ptr);
%ppointer_functions(IADLXManualVRAMTuning1*, manualVRAMTuning1P_Ptr);
%ppointer_functions(IADLXManualVRAMTuning2*, manualVRAMTuning2P_Ptr);
%ppointer_functions(IADLXManualPowerTuning*, manualPowerTuningP_Ptr);
%ppointer_functions(IADLXGPUList*, IADLXGPUListP_Ptr);
%ppointer_functions(IADLXGPU*, IADLXGPUP_Ptr);
%ppointer_functions(IADLXGPUAutoTuning*, IADLXGPUAutoTuningP_Ptr);
%ppointer_functions(IADLXInterface*, IADLXInterfaceP_Ptr);
%ppointer_functions(void*, voidP_Ptr);
%ppointer_functions(char*, charP_Ptr);
%ppointer_functions(IADLXManualFanTuningState*, manualFanTuningStateP_Ptr);
%ppointer_functions(IADLXManualFanTuningStateList*, manualFanTuningStateListP_Ptr);
%ppointer_functions(IADLXManualTuningState*, manualTuningStateP_Ptr);
%ppointer_functions(IADLXManualTuningStateList*, manualTuningStateListP_Ptr);
%ppointer_functions(IADLXMemoryTimingDescription*, memoryTimingDescriptionP_Ptr);
%ppointer_functions(IADLXMemoryTimingDescriptionList*, memoryTimingDescriptionListP_Ptr);
%ppointer_functions(IADLXGPUMetrics*, gpuMetricsP_Ptr);
%ppointer_functions(IADLXGPUMetrics1*, gpuMetrics1P_Ptr);
%ppointer_functions(IADLXGPUMetricsSupport*, gpuMetricsSupportP_Ptr);
%ppointer_functions(IADLXGPUMetricsSupport1*, gpuMetricsSupport1P_Ptr);
%ppointer_functions(IADLXPerformanceMonitoringServices*, performanceMonitoringServicesP_Ptr);