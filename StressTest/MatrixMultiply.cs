using ComputeSharp;
using ComputeSharp.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace StressTest
{
    //https://github.com/Sergio0694/ComputeSharp/wiki 3. Getting started
    //matrix multiplication  
    //https://github.com/Sergio0694/ComputeSharp/tree/main/samples/ComputeSharp.Sample

    //https://github.com/m4rs-mt/ILGPU/blob/master/Samples/MatrixMultiply/Program.cs
    class MatrixMultiply
    {
        public static string inputPathA = "expectedInputA.bin";
        public static string inputPathB = "expectedInputB.bin";
        public static string outputPath = "expectedOutput.bin";

        //a*b = e (expected result)
        public static void RunMatrixMultiply(TestMode mode, GraphicsDevice gpu, float[] flatA, float[] flatB, int m, int ka, int kb, int n, int iterations)
        {

            float[]? expectedInputA = null;
            float[]? expectedInputB = null;
            float[]? expectedOutput = null;

            if (mode != TestMode.expected)
            {
                try
                {
                    expectedInputA = ReadMatrixFromFile(inputPathA);
                    expectedInputB = ReadMatrixFromFile(inputPathB);
                    expectedOutput = ReadMatrixFromFile(outputPath);

                    if (!MatrixEqual(flatA, expectedInputA) || !MatrixEqual(flatB, expectedInputB))
                    {
                        Console.WriteLine("Inputs dont match expected inputs, first generate them");
                        Environment.Exit(3);
                    }
                }
                catch
                {
                    Console.WriteLine("Could not read expected matrix files, generate expected output for matrices first");
                    Environment.Exit(3);
                }
            }

            Console.WriteLine($"multiplying [{m}x{ka}]x[{kb}x{n}]");

            //a column height must be the same as b row width
            if (ka != kb)
            {
                throw new ArgumentException("Can't multiply matrices: ka != kb");
            }

            float[] writeFlat = new float[m * n];

            using ReadWriteBuffer<float> bufferA = gpu.AllocateReadWriteBuffer(flatA);
            using ReadWriteBuffer<float> bufferB = gpu.AllocateReadWriteBuffer(flatB);
            using ReadWriteBuffer<float> writeBuffer = gpu.AllocateReadWriteBuffer(writeFlat);
            using ReadWriteBuffer<int> compareResult = gpu.AllocateReadWriteBuffer(new int[1] { 0 });

            //run specific mode
            switch (mode)
            {
                case(TestMode.sustained):
                    RunSustained(gpu, bufferA, bufferB, writeBuffer, m, ka, kb, n, expectedOutput, iterations, compareResult);
                    break;

                case(TestMode.expected):
                    RunExpected(gpu, bufferA, bufferB, writeBuffer, m, ka, kb, n);
                    break;

                case(TestMode.transient):
                    RunTransient(gpu, bufferA, bufferB, writeBuffer, m, ka, kb, n, expectedOutput, iterations, compareResult);
                    break;

                default:
                    Console.WriteLine("Provide a mode"); 
                    break;
            }
        }

        //keeps running matrix multiplication until program stopped
        static void RunSustained(GraphicsDevice gpu, ReadWriteBuffer<float> bufferA, ReadWriteBuffer<float> bufferB, ReadWriteBuffer<float> writeBuffer, int m, int ka, int kb, int n, float[] expectedOutput, int iterations, ReadWriteBuffer<int> compareResult)
        {
            using ReadWriteBuffer<float> expectedBuffer = gpu.AllocateReadWriteBuffer(expectedOutput);
            Stopwatch sw = new Stopwatch();
            Console.WriteLine("Multiplying matrices sustained");
            long averageTime = 0;
            for (int i = 0; i < iterations; i++)
            {
                sw.Restart();
                //gpu.For(m, n, new MatrixMultiplyAccelerated(bufferA, bufferB, m, ka, kb, n, writeBuffer));
                gpu.For(m, n, new MatrixMultiplyTiled(bufferA, bufferB, m, ka, n, writeBuffer, 1.00f, 0.00f));

                gpu.For(m, n, new MatrixCompareAccelerated(writeBuffer, expectedBuffer, n, compareResult));

                //float[] writeFlat = writeBuffer.ToArray();
                int[] compareFlat = compareResult.ToArray();
                sw.Stop();

                compareResult.CopyFrom(new int[] { 0 });

                if (compareFlat[0] == 0)
                {
                    Console.WriteLine("GPU did not calculate matrix comparison, possible instability");
                    Environment.Exit(5);
                }
                else if (compareFlat[0] == 2)
                {
                    Console.WriteLine("GPU result does not match expected output, possible instability");
                    Environment.Exit(4);
                }
                //Console.WriteLine("Time to multiply matrices: " + sw.ElapsedMilliseconds + "ms");
                averageTime += sw.ElapsedMilliseconds;
            }
            averageTime = averageTime / iterations;
            //taken by RunStressTest's RedirectStandardOutput
            Console.WriteLine("AverageTime:" + averageTime);
        }

        //spikes usage at random intervals
        static void RunTransient(GraphicsDevice gpu, ReadWriteBuffer<float> bufferA, ReadWriteBuffer<float> bufferB, ReadWriteBuffer<float> writeBuffer, int m, int ka, int kb, int n, float[] expectedOutput, int totalIterations, ReadWriteBuffer<int> compareResult)
        {
            using ReadWriteBuffer<float> expectedBuffer = gpu.AllocateReadWriteBuffer(expectedOutput);
            Stopwatch sw = new Stopwatch();
            Random rnd = new Random();
            Console.WriteLine("Multiplying matrices transient");
            long averageTime = 0;
            for (int i = 0; i < totalIterations; i++)
            {
                double transientIteration = rnd.NextDouble();
                int waitTime = rnd.Next(500, 2000);

                sw.Restart();
                if(transientIteration <= 0.9)
                {
                    gpu.For(m, n, new MatrixMultiplyAccelerated(bufferA, bufferB, m, ka, kb, n, writeBuffer));
                    //gpu.For(m, n, new MatrixMultiplyTiled(bufferA, bufferB, m, ka, n, writeBuffer, 1.00f, 0.00f));
                    gpu.For(m, n, new MatrixCompareAccelerated(writeBuffer, expectedBuffer, n, compareResult));
                }
                else
                {
                    sw.Stop();
                    Thread.Sleep(waitTime);
                }
                int[] compareFlat = compareResult.ToArray();
                sw.Stop();

                //clear buffer
                compareResult.CopyFrom(new int[] { 0 });

                //verify result
                if (compareFlat[0] == 0 && transientIteration <= 0.9)
                {
                    Console.WriteLine("GPU did not calculate matrix comparison, possible instability");
                    Environment.Exit(5);
                }
                else if (compareFlat[0] == 2)
                {
                    Console.WriteLine("GPU result does not match expected output, possible instability");
                    Environment.Exit(4);
                }
                //Console.WriteLine("Time to multiply matrices: " + sw.ElapsedMilliseconds + "ms");
                averageTime += sw.ElapsedMilliseconds;
            }
            //calculate average time by mean
            averageTime = averageTime / totalIterations;
            //taken by RunStressTest's RedirectStandardOutput
            Console.WriteLine("AverageTime:" + averageTime);
        }

        static void RunExpected(GraphicsDevice gpu, ReadWriteBuffer<float> bufferA, ReadWriteBuffer<float> bufferB, ReadWriteBuffer<float> writeBuffer, int m, int ka, int kb, int n)
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            gpu.For(m, n, new MatrixMultiplyAccelerated(bufferA, bufferB, m, ka, kb, n, writeBuffer));
            float[] writeFlat = writeBuffer.ToArray();
            sw.Stop();
            Console.WriteLine("Time to multiply: " + sw.ElapsedMilliseconds);
            WriteMatrixToFile(bufferA.ToArray(), inputPathA);
            WriteMatrixToFile(bufferB.ToArray(), inputPathB);
            WriteMatrixToFile(writeFlat, outputPath);
        }

        //uses cpu to calculate expected result to use to verify gpu output
        static float[,] CpuMatrixMultiply(float[,] a, float[,] b)
        {
            int m = a.GetLength(0);
            int ka = a.GetLength(1);
            int kb = b.GetLength(0);
            int n = b.GetLength(1);

            if (ka != kb)
            {
                throw new ArgumentException("Can't multiply matrices: ka != kb");
            }

            //calculation, non accelerated so will use cpu
            float[,] c = new float[m, n];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    c[i, j] = 0;
                    for (int k = 0; k < ka; k++)
                    {
                        c[i, j] += a[i, k] * b[k, j];
                    }
                }
            }
            return c;
        }

        //https://www.geeksforgeeks.org/dsa/emulating-a-2-d-array-using-1-d-array/
        //using for loops faster than a.Cast<float>().ToArray()
        static float[] Flatten(float[,] arr)
        {
            int n = arr.GetLength(0); //rows
            int m = arr.GetLength(1); //columns
            float[] res = new float[m * n];
            int i, j, k = 0;
            for (i = 0; i < n; i++)
            {
                for (j = 0; j < m; j++)
                {
                    k = i * m + j;
                    res[k] = arr[i, j];
                }
            }
            return res;
        }

        //https://stackoverflow.com/questions/21020356/matrix-multiplication-on-cpu-numpy-and-gpu-gnumpy-give-different-results
        //https://developer.nvidia.com/blog/controlling-floating-point-determinism-in-nvidia-cccl/
        //cannot compare cpu and gpu result directly
        //so matrixequal compares 2 flattened matrices, gpu expected against the gpu result
        static bool MatrixEqual(float[] a, float[] b)
        {
            //int mb = b.GetLength(0); //rows
            //int nb = b.GetLength(1); //columns

            if(a.Length != b.Length)
            {
                return false;
            }
            //a and b have same length
            for(int i = 0; i < a.Length; i++)
            {
                if(a[i] != b[i])
                {
                    Console.WriteLine("Matrices not equal: " + a[i] + " != " + b[i]);
                    return false;
                }
            }
            return true;
        }

        //methods to save random matrices and expected result
        //https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.memorymarshal.asbytes?view=net-10.0
        //https://stackoverflow.com/questions/70309525/in-c-how-can-i-reinterpret-byte-as-t-where-t-is-a-struct
        public static void WriteMatrixToFile(float[] array, string path)
        {
            var span = array.AsSpan();
            var bytes = MemoryMarshal.AsBytes(span);
            //https://stackoverflow.com/questions/6153074/how-do-i-write-data-to-a-text-file-in-c
            using (FileStream fs = File.Create(path))
            {
                fs.Write(bytes);
            }
        }

        //https://stackoverflow.com/questions/75155160/c-sharp-how-to-read-a-binary-file-into-int-without-a-binaryreader-loop
        public static float[] ReadMatrixFromFile(string path)
        {
            float[] result;
            //https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.read?view=net-10.0
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                result = new float[fs.Length / 4];
                var span = result.AsSpan();
                var bytes = MemoryMarshal.AsBytes(span);
                int n = fs.Read(bytes);
            }
            return (result);
        }

        public static float[] GenerateMatrix(int rows, int cols, int seed = 50)
        {
            //https://learn.microsoft.com/en-us/dotnet/api/system.random?view=net-10.0
            Random rand = new Random(seed);
            float[,] matrix = new float[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for(int j = 0; j < cols; j++)
                {
                    float num = rand.Next(0,100);
                    matrix[i, j] = num;
                }
            }
            return Flatten(matrix);
        }
    }

    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MatrixMultiplyAccelerated(ReadWriteBuffer<float> a, ReadWriteBuffer<float> b, int m, int ka, int kb, int n, ReadWriteBuffer<float> writeBuffer) : IComputeShader
    {
        //parameters passed
        //matrix a = m (row) x ka (column)
        //matrix b = kb (row) x n (column)
        //https://github.com/m4rs-mt/ILGPU/blob/b4fd8b33086434586f36d672bf939f0c0e95bba2/Samples/MatrixMultiply/Program.cs#L130
        public void Execute()
        {
            int x = ThreadIds.X; //current row in a
            int y = ThreadIds.Y; //current column in b
            float sum = 0.0f;

            // ka == kb

            //https://stackoverflow.com/questions/2151084/map-a-2d-array-onto-a-1d-array
            //https://www.geeksforgeeks.org/dsa/emulating-a-2-d-array-using-1-d-array/
            //https://en.wikipedia.org/wiki/Row-_and_column-major_order
            for (int i = 0; i < ka; i++)
            {
                // row major 2d to 1d array
                // row * width (total) + column
                sum += a[x * ka + i] * b[i * n + y];
            }

            writeBuffer[(x*n) + y] = sum;
        }
    }

    //similar to MatrixMultiplyAccelerated but tiled
    //https://eunomia.dev/others/cuda-tutorial/03-gpu-programming-methods/
    //https://developer.nvidia.com/blog/how-to-write-high-performance-matrix-multiply-in-nvidia-cuda-tile/
    //https://github.com/NVIDIA/cuda-samples/blob/master/Samples/0_Introduction/matrixMul/matrixMul.cu
    [ThreadGroupSize(16, 16, 1)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MatrixMultiplyTiled(ReadWriteBuffer<float> a, ReadWriteBuffer<float> b, int m, int n, int k, ReadWriteBuffer<float> c, float alpha, float beta) : IComputeShader
    {
        //https://github.com/llvm-beanz/linalg-examples/blob/main/gemm.hlsl
        const int TILE_SIZE = 16;

        [GroupShared] static readonly float[] tileA = new float[TILE_SIZE * TILE_SIZE];
        [GroupShared] static readonly float[] tileB = new float[TILE_SIZE * TILE_SIZE];

        public void Execute()
        {
            int row = ThreadIds.Y;
            int col = ThreadIds.X;

            float sum = 0.0f;

            int numTiles = ((k + TILE_SIZE - 1) / TILE_SIZE);

            for (int tileId = 0; tileId < numTiles; tileId++)
            {
                int aRow = row;
                int aCol = tileId * TILE_SIZE + GroupIds.X;

                if (aRow < m && aCol < k)
                {
                    tileA[GroupIds.Y * TILE_SIZE + GroupIds.X] = a[aRow * k + aCol];
                }
                else
                {
                    tileA[GroupIds.Y * TILE_SIZE + GroupIds.X] = 0.0f;
                }

                int bRow = tileId * TILE_SIZE + GroupIds.Y;
                int bCol = col;

                if (bRow < k && bCol < n)
                {
                    tileB[GroupIds.Y * TILE_SIZE + GroupIds.X] = b[bRow * n + bCol];
                }
                else
                {
                    tileB[GroupIds.Y * TILE_SIZE + GroupIds.X] = 0.0f;
                }

                Hlsl.GroupMemoryBarrierWithGroupSync();

                for (int i = 0; i < TILE_SIZE; i++)
                {
                    sum += tileA[GroupIds.Y * TILE_SIZE + i] * tileB[i * TILE_SIZE + GroupIds.X];
                }

                Hlsl.GroupMemoryBarrierWithGroupSync();
            }

            if (row < m && col < n)
            {
                int cIndex = row * n + col;
                float existingC = c[cIndex];
                c[cIndex] = alpha * sum + beta * existingC;
            }
        }
    }

    //compare output faster using gpu to avoid dips and increase utilisation
    //similar to gpu-burn implementation
    //https://github.com/wilicc/gpu-burn/blob/master/compare.cu
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    [GeneratedComputeShaderDescriptor]
    public readonly partial struct MatrixCompareAccelerated(ReadWriteBuffer<float> a, ReadWriteBuffer<float> b, int n, ReadWriteBuffer<int> result) : IComputeShader
    {
        readonly float epsilon = 0.001f;
        public void Execute()
        {
            int x = ThreadIds.X;
            int y = ThreadIds.Y;
            int i = (x * n) + y;
            //first stage change to prove comparison has been attempted
            if(i == 0)
            {
                Hlsl.InterlockedMax(ref result[0], 1);
            }
            //if result outside of margin of error, result = 1 which means maths error
            if(Hlsl.Abs(a[i] - b[i]) > epsilon)
            {
                //https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/interlockedor
                Hlsl.InterlockedMax(ref result[0], 2);
            }
        }
    }
}
