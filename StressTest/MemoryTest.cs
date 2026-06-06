using System;
using System.Collections.Generic;
using System.Text;
using ComputeSharp;

namespace StressTest
{
    //VRAM memory stress testing
    //inspiration from:
    //https://github.com/GpuZelenograd/memtest_vulkan
    //https://github.com/ComputationalRadiationPhysics/cuda_memtest
    internal class MemoryTest
    {
        /*Moving inversion test
        * MoveInvWrite writes pattern1 to memory locations
        * MoveInvReadWrite checks all contain pattern1 still, then writes pattern2 (inverse of pattern1) aka flips all bits
        * MoveInvRead checks pattern2 is correct in all locations
        */
        //similar to https://github.com/ComputationalRadiationPhysics/cuda_memtest/blob/dev/tests.cpp kernel_move_inv functions
        [ThreadGroupSize(DefaultThreadGroupSizes.XYZ)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct MoveInvWrite(ReadWriteBuffer<uint> buffer, uint pattern) : IComputeShader
        {
            public void Execute()
            {
                int i = ThreadIds.X;
                if (i < buffer.Length)
                {
                    buffer[i] = pattern;
                }
            }
        }

        [ThreadGroupSize(DefaultThreadGroupSizes.XYZ)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct MoveInvReadWrite(ReadWriteBuffer<uint> buffer, uint pattern1, uint pattern2, ReadWriteBuffer<int> result) : IComputeShader
        {
            public void Execute()
            {
                int i = ThreadIds.X;
                if (i < buffer.Length && buffer[i] != pattern1)
                {
                    //add instead of max helps track how many errors if needed
                    Hlsl.InterlockedAdd(ref result[0], 1);
                }
                buffer[i] = pattern2;
            }
        }

        //similar to MatrixCompareAccelerated
        [ThreadGroupSize(DefaultThreadGroupSizes.XYZ)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct MoveInvRead(ReadWriteBuffer<uint> buffer, uint pattern, ReadWriteBuffer<int> result) : IComputeShader
        {
            public void Execute()
            {
                int i = ThreadIds.X;
                if(i < buffer.Length && buffer[i] != pattern) 
                {
                    Hlsl.InterlockedAdd(ref result[0], 1);
                }
            }
        }

        /*Modulo 20 test
        * ModWrite writes pattern1 to every 20th location
        * and writes inverse (pattern2) to other cells
        * then ModRead checks the pattern1 locations
        */
        [ThreadGroupSize(DefaultThreadGroupSizes.XYZ)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct ModWrite(ReadWriteBuffer<uint> buffer, uint offset, uint pattern1, uint pattern2) : IComputeShader
        {
            public void Execute()
            {
                int i = ThreadIds.X;
                if (i < buffer.Length)
                {
                    if (i % 20 == offset)
                    {
                        buffer[i] = pattern1;
                    }
                    else
                    {
                        buffer[i] = pattern2;
                    }
                }
            }
        }

        [ThreadGroupSize(DefaultThreadGroupSizes.XYZ)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct ModRead(ReadWriteBuffer<uint> buffer, uint offset, uint pattern, ReadWriteBuffer<int> result) : IComputeShader
        {
            public void Execute()
            {
                int i = ThreadIds.X;
                if (i < buffer.Length)
                {
                    if (i % 20 == offset && buffer[i] != pattern)
                    {
                        Hlsl.InterlockedAdd(ref result[0], 1);
                    }
                }
            }
        }
    }
}
