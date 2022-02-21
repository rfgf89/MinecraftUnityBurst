
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Unity.VisualScripting;
using Debug = UnityEngine.Debug;


public class ChunkHundler : MonoBehaviour
{
    public ComputeShader meshingCompute;

    
    public int nmTris;
    [HideInInspector]public int3 sizeComputePartChunk;
    [HideInInspector]public int sizeAllComputePartChunk;
    
    [HideInInspector]public WorldController world;
    [HideInInspector]public Chunk chunk;

    
    private int3 chunkSize;
    
    private int kernelMeshing;

    private ComputeBuffer pointBuffer;
    private ComputeBuffer polygonsBuffer;
    private ComputeBuffer triCountBuffer;
    [HideInInspector]public bool computing;
    
    private int3 sizeNumPacket;
    private int offsetCall;
    private int offsetPacket;
    private NativeArray<Vector3> vertices;
    private NativeArray<int> triangles;
    private NativeArray<Vector2> uv;
    
    private NativeArray<Vector2> uvTable;

    
    public Stopwatch timer;
    private GetPolyFromArrayCompute polyDat;
    private GenerateChunk genDat;
    
    
    void ReleaseBuffers () {
        if (polygonsBuffer != null) {
            pointBuffer.Release ();
            polygonsBuffer.Release ();
            triCountBuffer.Release ();
            polyDat.UVTable.Dispose();
            polyDat.blocksIDTable.Dispose();
            polyDat.blocksGreedy.Dispose();
            genDat.GenTable.Dispose();
        }
    }



    public void HandlerInit(int3 computeSize,int3 chunksSize)
    {
        
        chunkSize = chunksSize;
        sizeComputePartChunk = computeSize;
        
        uvTable = new NativeArray<Vector2>(ChunkData.UVTable, Allocator.Persistent);
    
        kernelMeshing = meshingCompute.FindKernel("Density");

        
        pointBuffer = new ComputeBuffer(((chunkSize.x+2) * (chunkSize.y+2) * (chunkSize.z+2)), sizeof(int), ComputeBufferType.Default, ComputeBufferMode.Immutable);
        polygonsBuffer = new ComputeBuffer(((chunkSize.x+2) * (chunkSize.y+2) * (chunkSize.z+2))/2,sizeof(float)*2, ComputeBufferType.Append);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        //var tt = polygonsBuffer.GetNativeBufferPtr();

       
        sizeNumPacket = new int3(
            (int)Mathf.Ceil((chunkSize.x+2)  / sizeComputePartChunk.x), 
            (int)Mathf.Ceil((chunkSize.y+2) / sizeComputePartChunk.y), 
            (int)Mathf.Ceil((chunkSize.z+2)  / sizeComputePartChunk.z));
        
        sizeAllComputePartChunk = (sizeNumPacket.x) * (sizeNumPacket.y) * (sizeNumPacket.z);

        meshingCompute.SetInt("sizeW", (chunkSize.x+2));
        meshingCompute.SetInt("sizeH", (chunkSize.y+2));
        meshingCompute.SetInt("sizeD", (chunkSize.z+2));
        meshingCompute.SetBuffer(kernelMeshing, "polygons", polygonsBuffer);
        meshingCompute.SetBuffer(kernelMeshing, "points", pointBuffer);

        polyDat = new GetPolyFromArrayCompute(){
            sizeChunk = chunkSize + new int3(2, 2, 2),
            blocksIDTable = new NativeArray<int>(ChunkData.IDTable, Allocator.Persistent),
            UVTable = new NativeArray<Vector2>(uvTable, Allocator.Persistent),
            blocksGreedy =
                new NativeArray<bool>(((chunkSize.x + 2) * (chunkSize.y + 2) * (chunkSize.z + 2)) * 6, Allocator.Persistent)
        };
        genDat = new GenerateChunk()
        {
            sizeChunk = chunkSize + new int3(2, 2, 2),
            GenTable = new NativeArray<int3>(ChunkData.GenTable, Allocator.Persistent)
        };
       
    }
    
    
    public JobHandle GenCurrentChunk(float3 lod)
    {
        
        //Vector2Int offset = new Vector2Int();
        
        //offset.x = -10000;
        //offset.y = -10000;
        //float[] h = new float[(chunkSize[0] + 2) * (chunkSize[2] + 2)];
        //world.noise2D.GenUniformGrid2D(h,(int)(chunk.position.x),(int)(chunk.position.z), 
        //    (int)((chunkSize[0]) + 2), (int)((chunkSize[2]) + 2), (1f/200f)*lod,2);


        //world.noise2D.GenPositionArray2D(h, world.xPos, world.zPos, 
        //    ((chunk.position.x* chunkSize.x)/1000), 
        //     ((chunk.position.z* chunkSize.z)/1000), 1);
        
        unsafe
        {
            //genDat.depthNoise = (float*) UnsafeUtility.AddressOf(ref h[0]);
            genDat.blocksID = (int*) UnsafeUtility.AddressOf(ref chunk.chunkData[0]);
        }

        
        
  
        genDat.offsetChunk = new float3(chunk.position.x* chunkSize.x,chunk.position.y* chunkSize.y,chunk.position.z* chunkSize.z);
        genDat.sizeLod = lod;
        return genDat.Schedule(); 
    }

    
 

    JobHandle jobHandle;
    public IEnumerator GenerateChunkData(float3 lod)
    {
        
        if (computing)
            yield break;
        computing = true;
        
        
        timer = new Stopwatch();
        timer.Start();
    
        
        
        jobHandle = GenCurrentChunk(lod);
   
        yield return new WaitUntil(() => jobHandle.IsCompleted);
        
        jobHandle.Complete();
         timer.Stop();
        computing = false;
        StartCoroutine(ComputeAsync(lod,true));
    }

    
    public IEnumerator ComputeAsync(float3 lod, bool exit = false)
    {
        if (!exit && computing)
            yield break;
        computing = true;
        //var Task = new Task(GetDataFromComputeShader);
        
        
       
        
        pointBuffer.SetData(chunk.chunkData);

        var pos = 0;
        nmTris = 0;
        var postTris = 0;
        offsetCall = 0;
        offsetPacket = 0;
        polygonsBuffer.SetCounterValue(0);


        int numTris = -1;
        bool gredStart = false;
  
        for (; pos < sizeAllComputePartChunk; pos += 1)
        {
          
            meshingCompute.SetInt("sizeX", (pos & (sizeNumPacket.x - 1)) * sizeComputePartChunk.x);
            meshingCompute.SetInt("sizeY", ((pos - pos / (sizeNumPacket.x * sizeNumPacket.y) * sizeNumPacket.x * sizeNumPacket.y) / sizeNumPacket.x) * sizeComputePartChunk.y);
            meshingCompute.SetInt("sizeZ", pos / (sizeNumPacket.x * sizeNumPacket.y) * sizeComputePartChunk.z);

            meshingCompute.Dispatch(kernelMeshing, sizeComputePartChunk.x, sizeComputePartChunk.y, sizeComputePartChunk.z);


            AsyncGPUReadback.Request(polygonsBuffer, obj =>
            {
                offsetCall++;
                if (offsetCall == sizeAllComputePartChunk)
                {
                    int[] triCountArray = {0};
                    
                    ComputeBuffer.CopyCount(polygonsBuffer, triCountBuffer, 0);
                    triCountBuffer.GetData(triCountArray);
                    numTris = triCountArray[0];
                    
                    if (numTris > 0)
                    {
                        jobHandle = GreedyMeshing(lod, numTris);
                        gredStart = true;
                    }
                }
            });
            offsetPacket++;
            
        }
        yield return new WaitUntil(() =>  ((jobHandle.IsCompleted && gredStart) || numTris == 0)); 

        

        if (numTris > 0)
        {
            
            
            
            jobHandle.Complete();


            chunk.SetDataInChunk(lod, ref polyDat);
            
        }

        
        

        chunk.meshRenderer.enabled = true;

        world.numChunk++;
        world.timeChunk += timer.ElapsedMilliseconds;
        if (world.numTimeChunk.Count > 100)
            world.numTimeChunk.RemoveAt(world.numTimeChunk.Count-1);
        
        world.numTimeChunk.Insert(0,timer.ElapsedMilliseconds);
        
        computing = false;
        AddListDes();

    }


    
    public bool onList = true;
    public bool clear;
    public void AddListDes()
    {

        if (!onList && !computing)
        {
            world.chunkHandlers.Add(this);
            onList = true;
        }

    }
    
    
    public JobHandle GreedyMeshing(float3 lod, int tris)
    {
       
        
        int2[] arr = new int2[tris];
        polygonsBuffer.GetData(arr);
  
        unsafe
        {
            polyDat.mapPolygon = (int2*) UnsafeUtility.AddressOf(ref arr[0]);
            polyDat.blocksID = (int*) UnsafeUtility.AddressOf(ref chunk.chunkData[0]);
        }
        polyDat.posChunk = new int3((int)chunk.transform.position.x,(int)chunk.transform.position.y,(int)chunk.transform.position.z);
        polyDat.boundMin = new float3(chunk.chunkSize.x,chunk.chunkSize.y,chunk.chunkSize.z);  
        polyDat.boundMax = new float3();
        polyDat.lod = lod;
        polyDat.mapPolygonSize = tris;
        polyDat.meshDataArray = Mesh.AllocateWritableMeshData(1);

        return polyDat.Schedule();

    }



    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
    private unsafe struct GenerateChunk : IJob
    {
        [ReadOnly] public int3 sizeChunk;
        [ReadOnly] public int offsetPart;
        [ReadOnly] public float3 offsetChunk;
        [ReadOnly] public float3 sizeLod;
        
        [ReadOnly] public NativeArray<int3> GenTable;
        
       
        
        [NativeDisableUnsafePtrRestriction]
        [ReadOnly]public int* blocksID;
   
        
        public void Execute()
        {

            Vector3 pos;
     
            float h1, h2;
            
            for (int i = 0; i < sizeChunk.x*sizeChunk.y*sizeChunk.z; i++)
            {
                pos.x = GenTable[i].x / sizeLod.x;
                pos.y = GenTable[i].y / sizeLod.x;
                pos.z = GenTable[i].z / sizeLod.z;

                h2 = SimplexNoise2D(offsetChunk.x + pos.x, offsetChunk.z + pos.z, 6, 0.45f, 0.002f) * 64f;
                //h2 = FastNoise.fnGenSingle2D(noiseKey, offsetChunk.x + pos.x, offsetChunk.z + pos.z, 2416) * 64f;

                if (pos.y >= h2 + 32f) //h*16f
                {
                    blocksID[i] = 0;
                }
                else
                {
                    h1 = SimplexNoise3D(offsetChunk.x + pos.x, offsetChunk.y + pos.y, offsetChunk.z + pos.z,
                        6,
                        0.45f,
                        0.002f);
                    //h1 = depthNoise[i];
                    if (h1 > 0.4f)
                        blocksID[i] = 0;
                    else if (pos.y >= h2 + 32f - math.ceil(1f * sizeLod.y) / sizeLod.y)
                        blocksID[i] = 4;
                    else if (pos.y >= h2 + 32f - math.ceil(8f * sizeLod.y) / sizeLod.y)
                        blocksID[i] = 2;
                    else
                        blocksID[i] = 1;
                }  
            }
          



            
        }




        
            public float SimplexNoise2D(float x, float y, int octaves, float persistence, float freq) {
                float total = 0;
                float frequency = freq;
                float amplitude = 1.0f;
                for(int i=0;i<octaves;i++) {
                    total += Generate(x * frequency, y * frequency) * amplitude;
			
                    amplitude *= persistence;
                    frequency *= 2;
                }
		
                return total;
            }
            
            public float SimplexNoise3D(float x, float y, float z, int octaves, float persistence, float freq) {
                float total = 0;
                float frequency = freq;
                float amplitude = 1.0f;
                for(int i=0;i<octaves;i++) {
                    total += Generate(x * frequency, y * frequency, z * frequency) * amplitude;
			
                    amplitude *= persistence;
                    frequency *= 2;
                }
		
                return total;
            }

        
        private float Generate(float x, float y)
        {
            const float F2 = 0.366025403f; // F2 = 0.5*(sqrt(3.0)-1.0)
            const float G2 = 0.211324865f; // G2 = (3.0-Math.sqrt(3.0))/6.0

            float n0, n1, n2; // Noise contributions from the three corners

            // Skew the input space to determine which simplex cell we're in
            var s = (x + y) * F2; // Hairy factor for 2D
            var xs = x + s;
            var ys = y + s;
            var i = FastFloor(xs);
            var j = FastFloor(ys);

            var t = (i + j) * G2;
            var X0 = i - t; // Unskew the cell origin back to (x,y) space
            var Y0 = j - t;
            var x0 = x - X0; // The x,y distances from the cell origin
            var y0 = y - Y0;

            // For the 2D case, the simplex shape is an equilateral triangle.
            // Determine which simplex we are in.
            int i1, j1; // Offsets for second (middle) corner of simplex in (i,j) coords
            if (x0 > y0) { i1 = 1; j1 = 0; } // lower triangle, XY order: (0,0)->(1,0)->(1,1)
            else { i1 = 0; j1 = 1; }      // upper triangle, YX order: (0,0)->(0,1)->(1,1)

            // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
            // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
            // c = (3-sqrt(3))/6

            var x1 = x0 - i1 + G2; // Offsets for middle corner in (x,y) unskewed coords
            var y1 = y0 - j1 + G2;
            var x2 = x0 - 1.0f + 2.0f * G2; // Offsets for last corner in (x,y) unskewed coords
            var y2 = y0 - 1.0f + 2.0f * G2;

            // Wrap the integer indices at 256, to avoid indexing perm[] out of bounds
            var ii = Mod(i, 256);
            var jj = Mod(j, 256);

            // Calculate the contribution from the three corners
            var t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 < 0.0f) n0 = 0.0f;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Grad(PermOriginal[ii + PermOriginal[jj]], x0, y0);
            }

            var t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 < 0.0f) n1 = 0.0f;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Grad(PermOriginal[ii + i1 + PermOriginal[jj + j1]], x1, y1);
            }

            var t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 < 0.0f) n2 = 0.0f;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Grad(PermOriginal[ii + 1 + PermOriginal[jj + 1]], x2, y2);
            }

            // Add contributions from each corner to get the final noise value.
            // The result is scaled to return values in the interval [-1,1].
            return ((40.0f * (n0 + n1 + n2) + 1))/2; // TODO: The scale factor is preliminary!
        }


        private  float Generate(float x, float y, float z)
        {
            // Simple skewing factors for the 3D case
            const float F3 = 0.333333333f;
            const float G3 = 0.166666667f;

            float n0, n1, n2, n3; // Noise contributions from the four corners

            // Skew the input space to determine which simplex cell we're in
            var s = (x + y + z) * F3; // Very nice and simple skew factor for 3D
            var xs = x + s;
            var ys = y + s;
            var zs = z + s;
            var i = FastFloor(xs);
            var j = FastFloor(ys);
            var k = FastFloor(zs);

            var t = (i + j + k) * G3;
            var X0 = i - t; // Unskew the cell origin back to (x,y,z) space
            var Y0 = j - t;
            var Z0 = k - t;
            var x0 = x - X0; // The x,y,z distances from the cell origin
            var y0 = y - Y0;
            var z0 = z - Z0;

            // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
            // Determine which simplex we are in.
            int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
            int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords

            /* This code would benefit from a backport from the GLSL version! */
            if (x0 >= y0)
            {
                if (y0 >= z0)
                { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // X Y Z order
                else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; } // X Z Y order
                else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; } // Z X Y order
            }
            else
            { // x0<y0
                if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; } // Z Y X order
                else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; } // Y Z X order
                else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // Y X Z order
            }

            // A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
            // a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
            // a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
            // c = 1/6.

            var x1 = x0 - i1 + G3; // Offsets for second corner in (x,y,z) coords
            var y1 = y0 - j1 + G3;
            var z1 = z0 - k1 + G3;
            var x2 = x0 - i2 + 2.0f * G3; // Offsets for third corner in (x,y,z) coords
            var y2 = y0 - j2 + 2.0f * G3;
            var z2 = z0 - k2 + 2.0f * G3;
            var x3 = x0 - 1.0f + 3.0f * G3; // Offsets for last corner in (x,y,z) coords
            var y3 = y0 - 1.0f + 3.0f * G3;
            var z3 = z0 - 1.0f + 3.0f * G3;

            // Wrap the integer indices at 256, to avoid indexing perm[] out of bounds
            var ii = Mod(i, 256);
            var jj = Mod(j, 256);
            var kk = Mod(k, 256);

            // Calculate the contribution from the four corners
            var t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
            if (t0 < 0.0f) n0 = 0.0f;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Grad(PermOriginal[ii + PermOriginal[jj + PermOriginal[kk]]], x0, y0, z0);
            }

            var t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
            if (t1 < 0.0f) n1 = 0.0f;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Grad(PermOriginal[ii + i1 + PermOriginal[jj + j1 + PermOriginal[kk + k1]]], x1, y1, z1);
            }

            var t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
            if (t2 < 0.0f) n2 = 0.0f;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Grad(PermOriginal[ii + i2 + PermOriginal[jj + j2 + PermOriginal[kk + k2]]], x2, y2, z2);
            }

            var t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
            if (t3 < 0.0f) n3 = 0.0f;
            else
            {
                t3 *= t3;
                n3 = t3 * t3 * Grad(PermOriginal[ii + 1 + PermOriginal[jj + 1 + PermOriginal[kk + 1]]], x3, y3, z3);
            }

            // Add contributions from each corner to get the final noise value.
            // The result is scaled to stay just inside [-1,1]
            return 32.0f * (n0 + n1 + n2 + n3); // TODO: The scale factor is preliminary!
        }

      

        private static readonly byte[] PermOriginal = {
            151,160,137,91,90,15,
            131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
            190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
            88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
            77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
            102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
            135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
            5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
            223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
            129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
            251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
            49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
            138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
            151,160,137,91,90,15,
            131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
            190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
            88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
            77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
            102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
            135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
            5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
            223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
            129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
            251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
            49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
            138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180 
        };

        private static int FastFloor(float x)
        {
            return (x > 0) ? ((int)x) : (((int)x) - 1);
        }

        private static int Mod(int x, int m)
        {
            var a = x % m;
            return a < 0 ? a + m : a;
        }

        private static float Grad(int hash, float x)
        {
            var h = hash & 15;
            var grad = 1.0f + (h & 7);   // Gradient value 1.0, 2.0, ..., 8.0
            if ((h & 8) != 0) grad = -grad;         // Set a random sign for the gradient
            return (grad * x);           // Multiply the gradient with the distance
        }

        private static float Grad(int hash, float x, float y)
        {
            var h = hash & 7;      // Convert low 3 bits of hash code
            var u = h < 4 ? x : y;  // into 8 simple gradient directions,
            var v = h < 4 ? y : x;  // and compute the dot product with (x,y).
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -2.0f * v : 2.0f * v);
        }

        private static float Grad(int hash, float x, float y, float z)
        {
            var h = hash & 15;     // Convert low 4 bits of hash code into 12 simple
            var u = h < 8 ? x : y; // gradient directions, and compute dot product.
            var v = h < 4 ? y : h == 12 || h == 14 ? x : z; // Fix repeats at h = 12 to 15
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -v : v);
        }

        private static float Grad(int hash, float x, float y, float z, float t)
        {
            var h = hash & 31;      // Convert low 5 bits of hash code into 32 simple
            var u = h < 24 ? x : y; // gradient directions, and compute dot product.
            var v = h < 16 ? y : z;
            var w = h < 8 ? z : t;
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -v : v) + ((h & 4) != 0 ? -w : w);
        }


        
    }
    
    [BurstCompile(FloatPrecision.Low, FloatMode.Default)]
    public unsafe struct GetPolyFromArrayCompute : IJob
    {
        [ReadOnly]public int3 posChunk;
        [ReadOnly]public int3 sizeChunk;
        [ReadOnly]public float3 lod;
        
    
        [ReadOnly]public NativeArray<int> blocksIDTable;
        public NativeArray<bool> blocksGreedy;

       
        [NativeDisableUnsafePtrRestriction]
        [ReadOnly]public int2* mapPolygon;
        [NativeDisableUnsafePtrRestriction]
        [ReadOnly]public int* blocksID;
        
        [ReadOnly]public int mapPolygonSize;
        
        [ReadOnly]public NativeArray<Vector2> UVTable;

        public float3 boundMin;
        public float3 boundMax;
        
        
        public Mesh.MeshDataArray meshDataArray;

  

        static readonly int[] EdgeTable = {
            3,2,1,0, 0,1,2,3
        };
        
        static readonly float3[] PointTable = {
            new float3(0.0f,1.0f,0.0f),new float3(0.0f,0.0f,0.0f),new float3(1.0f,0.0f,0.0f),new float3(1.0f,1.0f,0.0f),
            new float3(0.0f,0.0f,1.0f),new float3(0.0f,0.0f,0.0f),new float3(0.0f,1.0f,0.0f),new float3(0.0f,1.0f,1.0f),
            new float3(1.0f,0.0f,0.0f),new float3(0.0f,0.0f,0.0f),new float3(0.0f,0.0f,1.0f),new float3(1.0f,0.0f,1.0f),

            new float3(0.0f,1.0f,1.0f),new float3(0.0f,0.0f,1.0f),new float3(1.0f,0.0f,1.0f),new float3(1.0f,1.0f,1.0f),
            new float3(1.0f,0.0f,1.0f),new float3(1.0f,0.0f,0.0f),new float3(1.0f,1.0f,0.0f),new float3(1.0f,1.0f,1.0f),
            new float3(1.0f,1.0f,0.0f),new float3(0.0f,1.0f,0.0f),new float3(0.0f,1.0f,1.0f),new float3(1.0f,1.0f,1.0f)
        };

        static readonly int3[] NormalTable = {
            new int3(0,0,-1),
            new int3(-1,0,0),
            new int3(0,-1,0),

            new int3(0,0,1),
            new int3(1,0,0),
            new int3(0,1,0)
        };
        
      
        static readonly int3[] AmbientTable = {
            new int3(0,0,0),new int3(1,1,0), new int3(1,-1,0), new int3(0,1,0), new int3(0,-1,0), new int3(0,-1,0),
            new int3(0,1,0), new int3(0,2,0), new int3(0,0,0), new int3(0,1,0),
            new int3(0,0,0), new int3(0,-1,1), new int3(0,-1,-1), new int3(0,-1,1),

            new int3(0,-1,0), new int3(-1,0,1),new int3(1,0,1), new int3(-1,0,1),
            new int3(0,-1,0), new int3(1,-1,0), new int3(1,0,-1), new int3(1,0,1),
            new int3(0,0,-1), new int3(-1,1,0),new int3(1,1,0), new int3(-1,1,0),
        };
        
        public void Execute()
        {

            NativeList<Vector3> vertices = new NativeList<Vector3>(mapPolygonSize,Allocator.Temp);
            NativeList<int> triangles = new NativeList<int>(mapPolygonSize,Allocator.Temp);
            NativeList<Vector2> uv = new NativeList<Vector2>(mapPolygonSize,Allocator.Temp);
            //NativeList<int> greedyClear = new NativeList<int>(mapPolygonSize*3,Allocator.Temp);
        
            int i, j, face, face1, face2, face3, b, b1, b2, b3, m, dir, workAx1, workAx2, offsetMesh = 0, offsetMesh1 = 1, offsetMesh2 = 2, offsetMesh3 = 3;
 
            
            int3 d, pos, twoPos, startPos, size;
            int st;
            int2 trisIndex;
            bool br;
            int3 one = new int3(1, 1, 1);
            float2 s;
         
            
       

            QuickSort(0, mapPolygonSize-1);

            for (i = 0; i < mapPolygonSize; i++)
            {
                trisIndex = mapPolygon[i];
                
                d.x = ( trisIndex.x        & 1023);
                d.y = ((trisIndex.x >> 10) & 1023);
                d.z = ((trisIndex.x >> 20) & 1023);
                boundMin = math.min(d, boundMin);
                boundMax = math.max(d, boundMax);
    
                
                m = d.x +d.y * sizeChunk.x + d.z * sizeChunk.y * sizeChunk.z;
                vertices.Resize(offsetMesh + 24, NativeArrayOptions.UninitializedMemory );
                triangles.Resize(offsetMesh + 24, NativeArrayOptions.UninitializedMemory );
                uv.Resize(offsetMesh + 24, NativeArrayOptions.UninitializedMemory );
              
                
                for (j = 0; j < 6; j++)
                {
                    face = (trisIndex.y >> (3 * j)) & 7;
                    if(face == 0) continue;
                    face -= 1;
                    if (CheckVoxelGreedy(d, face)) continue;

                    face1 = face*4 + 1;
                    face2 = face1 + 1;
                    face3 = face2 + 1;
                    b = (face / 3)*4;
                    b1 = b + 1;
                    b2 = b1 + 1;
                    b3 = b2 + 1;
                    pos = d;
                    twoPos = d;
                    startPos = d;
                    br = false;

                    dir = face % 3;
                    workAx1 = dir % 3;
                    workAx2 = (dir + 1) % 3;

                    for (; pos[workAx1] < sizeChunk[workAx1]; pos[workAx1]++)
                        if (CheckVoxelGreedy(pos, face) || CheckVoxel(pos) != blocksID[m] || CheckVoxelNormal(pos,face) != 0)
                            break;



                    for (; twoPos[workAx2] < sizeChunk[workAx2]; twoPos[workAx2]++)
                    {
                        for (twoPos[workAx1] = d[workAx1]; twoPos[workAx1] < pos[workAx1]; twoPos[workAx1]++)
                            if (CheckVoxelGreedy(twoPos, face) || CheckVoxel(twoPos) != blocksID[m] || CheckVoxelNormal(twoPos,face) != 0)
                            {
                                twoPos[workAx1] = pos[workAx1];
                                twoPos[workAx2] = twoPos[workAx2];
                                br = true;
                                break;
                            }

                        if (br) break;
                    }

                  
                    
                    st = d[workAx1];

                    for (; startPos[workAx2] < twoPos[workAx2]; startPos[workAx2]++)
                    for (startPos[workAx1] = st; startPos[workAx1] < twoPos[workAx1]; startPos[workAx1]++)
                        SetVoxelGreedy(true, startPos, face);


                    size = math.max(twoPos - d, one);

                    
                    face *= 4;
                    pos = d - one;
                    s = UVTable[blocksIDTable[blocksID[m]*6+j] * 4 + 3];

                    
                    vertices[offsetMesh] = (pos + PointTable[face].Multiple(size))/lod;
                    triangles[offsetMesh] = EdgeTable[b] + offsetMesh;
                    uv[offsetMesh] = s;
        
                    
                    vertices[offsetMesh1] = (pos + PointTable[face1].Multiple(size))/lod;
                    triangles[offsetMesh1] = EdgeTable[b1] + offsetMesh;
                    uv[offsetMesh1] = s;
              
                    
                    vertices[offsetMesh2] = (pos + PointTable[face2].Multiple(size))/lod;
                    triangles[offsetMesh2] = EdgeTable[b2] + offsetMesh;
                    uv[offsetMesh2] = s;
      
                    
                    vertices[offsetMesh3] = (pos + PointTable[face3].Multiple(size))/lod;
                    triangles[offsetMesh3] = EdgeTable[b3] + offsetMesh;
                    uv[offsetMesh3] = s;
         
 
                    
                    offsetMesh += 4;
                    offsetMesh1 = offsetMesh + 1;
                    offsetMesh2 = offsetMesh1 + 1;
                    offsetMesh3 = offsetMesh2 + 1;
                }
            }

      
            vertices.Resize(offsetMesh, NativeArrayOptions.UninitializedMemory );
            triangles.Resize(offsetMesh, NativeArrayOptions.UninitializedMemory );
            uv.Resize(offsetMesh, NativeArrayOptions.UninitializedMemory );

            
            UnsafeUtility.MemClear(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(blocksGreedy),blocksGreedy.Length );
            
            
       
            Mesh.MeshData meshData = meshDataArray[0];

            var vertexAttributeCount = 2;
            var vertexCount = vertices.Length;
       
		
        
            var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(
                vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
        
            vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);
 
            vertexAttributes[1] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2, stream: 1
            );
            


         
            if (offsetMesh > 65535)
            {
                meshData.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);
                NativeArray<int> triangleIndices = meshData.GetIndexData<int>();
                triangleIndices.CopyFrom(triangles);
            }
            else
            {
                meshData.SetIndexBufferParams(vertexCount, IndexFormat.UInt16);
                NativeArray<ushort> triangleIndices = meshData.GetIndexData<ushort>();
                for (i = 0; i < triangles.Length; i++)
                    triangleIndices[i] = (ushort)triangles[i];
            }

            meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
            vertexAttributes.Dispose();

            
            NativeArray<Vector3> verticesInd = meshData.GetVertexData<Vector3>();

            NativeArray<Vector2> uvInd = meshData.GetVertexData<Vector2>(1);
            verticesInd.CopyFrom(vertices);

            uvInd.CopyFrom(uv);
            

            meshData.subMeshCount = 1;
            
            
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount, MeshTopology.Quads));

            vertices.Dispose();
            triangles.Dispose();
            uv.Dispose();
          

        }
        
      
        

        
        public int CheckVoxel(int3 pos){
          
            return blocksID[(pos.x + pos.y * sizeChunk[0] + pos.z * sizeChunk[0] * sizeChunk[1])];
        }
        
 
        
        public int CheckVoxelNormal(int3 pos,int face){
            
            pos += NormalTable[face];
            return blocksID[(pos.x + pos.y * sizeChunk[0] + pos.z * sizeChunk[0] * sizeChunk[1])];
        }
        
        public bool CheckVoxelGreedy(int3 pos,int face){
            return blocksGreedy[(pos.x + pos.y * sizeChunk[0] + pos.z * sizeChunk[0] * sizeChunk[1])*6 + face];
        }
        
        public void SetVoxelGreedy(bool active, int3 pos,int face){
            //if (CheckOutSide(pos,1))
            //    return;
        
             blocksGreedy[(pos.x + pos.y * sizeChunk[0] + pos.z * sizeChunk[0] * sizeChunk[1])*6 + face] = active;
        }

         void QuickSort( int i, int j ) {
            if ( i < j ) {
                int q = Partition(i, j);
                QuickSort( i,q );
                QuickSort(q + 1, j );
            }
           
        }
 
         int Partition( int p, int r ) {
            int x = mapPolygon[ p ].x;
            int i = p - 1;
            int j = r + 1;
            while ( true ) {
                do {
                    j--;
                }
                while ( mapPolygon[ j ].x > x );
                do {
                    i++;
                }
                while ( mapPolygon[ i ].x < x );
                if ( i < j ) {
                    int2 tmp = mapPolygon[ i ];
                    mapPolygon[ i ] = mapPolygon[ j ];
                    mapPolygon[ j ] = tmp;
                }
                else {
                    return j;
                }
            }
        }






    }
    
    public void OnEnable()=> meshingCompute = Instantiate(meshingCompute);

    ~ChunkHundler()
    {
        ReleaseBuffers();
    }
    
}


