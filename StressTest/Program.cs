using ComputeSharp;
using StressTest;
using System;

enum TestMode { transient, sustained, expected };
class Program
{
    static void Main(string[] args)
    {
        string gpuName = "";
        TestMode mode = TestMode.expected;
        int iterations = 0;

        //https://www.geeksforgeeks.org/c-sharp/c-sharp-command-line-arguments/
        //loop through arguments and find name or mode arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-name" && i + 1 < args.Length)
            {
                gpuName = args[i + 1];
                i++;
            }
            else if (args[i] == "-mode" && i + 1 < args.Length)
            {
                switch (args[i + 1].ToLower())
                {
                    case "transient":
                        mode = TestMode.transient;
                        break;
                    case "sustained":
                        mode = TestMode.sustained;
                        break;
                    case "expected":
                        mode = TestMode.expected;
                        break;
                    default:
                        Console.WriteLine("Mode not found");
                        Environment.Exit(1);
                        break;
                }
                i++;
            }
            else if(args[i] == "-iterations" && i + 1 < args.Length)
            {
                iterations = int.Parse(args[i + 1]);
                i++;
            }
        }
        if (gpuName == "")
        {
            Console.WriteLine("GPU name not found, use -name GPUName");
            //https://stackoverflow.com/questions/155610/how-do-i-specify-the-exit-code-of-a-console-application-in-net
            Environment.Exit(1);
        }

        //https://github.com/Sergio0694/ComputeSharp/wiki

        //query gpus with predicate, matching string 
        //https://www.geeksforgeeks.org/c-sharp/how-to-compare-strings-in-c-sharp/
        //https://www.geeksforgeeks.org/c-sharp/c-sharp-string-contains-method/
        //https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings
        //Predicate<GraphicsDeviceInfo> predicate = graphicsDeviceInfo => graphicsDeviceInfo.Name.Contains(gpuName, StringComparison.OrdinalIgnoreCase);
        IEnumerable<GraphicsDevice> devices = GraphicsDevice.EnumerateDevices();

        GraphicsDevice? gpu = null;
        foreach(GraphicsDevice device in devices)
        {
            if (device.Name.Contains(gpuName, StringComparison.OrdinalIgnoreCase))
            {
                gpu = device;
                break;
            }
        }

        if (gpu == null)
        {
            Console.WriteLine("Couldn't find a gpu matching: " + gpuName);
            Console.WriteLine("Found gpus are:");
            foreach (GraphicsDevice device in devices)
            {
                Console.WriteLine(device.Name);
            }
            Environment.Exit(2);
        }
        else
        {
            Console.WriteLine("Found gpu: " + gpu.Name);
        }

        float[] a;
        float[] b;

        //https://developer.nvidia.com/blog/how-to-write-high-performance-matrix-multiply-in-nvidia-cuda-tile/
        int m = 4096;
        int ka = 4096;
        int kb = 4096;
        int n = 4096;

        if(mode == TestMode.expected)
        {
            Console.WriteLine("Generating expected output");
            //use the same seed to ensure any errors are instability
            a = MatrixMultiply.GenerateMatrix(m,ka,25);
            b = MatrixMultiply.GenerateMatrix(kb,n,75);
        }
        else
        {
            a = MatrixMultiply.ReadMatrixFromFile(MatrixMultiply.inputPathA);
            b = MatrixMultiply.ReadMatrixFromFile(MatrixMultiply.inputPathB);
        }
        MatrixMultiply.RunMatrixMultiply(mode, gpu, a, b, m, ka, kb, n, iterations);
    }
}