# AutoTuner: An Automated GPU Overclocking/Undervolting Utility
![Demo](https://github.com/user-attachments/assets/a9e5923e-2231-465c-b429-7e0d07272a4a)
AutoTuner is a C# application that automates the risky and time consuming task of tuning GPUs. It uses a custom coarse-to-fine algorithm and stress test to safely find optimal tuning values.  
Testing resulted in a 10.3% performance increase and a 13.2% reduction in power consumption.

## Features:
- Algorithm: Tuning is performed by the coarse-to-fine algorithm, similar to the process of manual tuning, to efficiently find the point of instability.
- Stress Test: ComputeSharp is used for a matrix multiplication stress test, simulating sustained and transient workloads to trigger instability. Further work is being done to expand the stress tests to simulate more use cases.
- State Management: The program updates the tuning state and modifies hardware values based on that state. The state is saved as a JSON file so it can persist through driver timeouts and crashes.
- Real-Time Monitoring: A background thread queries sensors to monitor temperatures and avoid thermal throttling, other metrics are also monitored such as clock speeds, power usage, and total GPU usage.
- Hardware Abstraction Layer (HAL): Uses the adapter pattern to implement hardware SDKs. Currently the ADLX SDK is implemented for AMD GPUs, future adapters to support different manufacturers are planned.

## Results
The following test results were taken using an AMD 9070XT.
| Metric                      | Stock configuration |  Auto tuned configuration (Undervolted)    |
|-----------------------------|---------------------|--------------------------------------------|
| Average clock speed         | 2905MHz             | 3167MHz                                    |
| Matrix multiplication time  | 32ms                | 29ms                                       |
| Average total board power   | 303W                | 263W                                       |
| Average temperature         | 61.33°C             | 60°C                                       |

## Prerequisites
- Windows 64-bit, Compatible hardware  
- ADLX 1.4+, SWIG 4.4.1+, Visual Studio 2026+

## Build instructions
1. Clone the repo
2. Download the ADLX SDK and SWIG to be used for the ADLXCSharpBind project
3. Move the ADLX SDK folder to the ADLXCSharpBind folder
4. Build the solution in Visual Studio 2026+

## Roadmap
- Current work is being done (see stresstest branch) to improve the stress test by adding different workloads to more accurately find instability, such as VRAM testing
- Implement SDKs from different manufacturers such as NVAPI for NVIDIA GPUs
- Support more operating systems, such as Linux
- Possibly extend capabilities to include other components, such as CPUs

## Disclaimers
This software modifies low level hardware tuning values such as clock speeds, voltages and power limits. Please use this software at your own risk. The author is not responsible for any hardware damage, crashes, or instability.  
This project is an independent, unofficial utility. It is not affiliated with, endorsed by, or sponsored by Advanced Micro Devices, Inc. (AMD). "AMD" and "Radeon" are registered trademarks of Advanced Micro Devices, Inc.

## Dependencies:
- [ComputeSharp](https://github.com/Sergio0694/ComputeSharp)
- [ScottPlot](https://github.com/scottplot/scottplot)
