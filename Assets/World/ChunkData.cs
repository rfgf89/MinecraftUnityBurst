using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;
public static class ChunkData 
{
    const int sizeTexture = 16;
    
    public static readonly int[] EdgeTable = {
        3,2,1,0 , 0,1,2,3
    };

    public static readonly float3[] PointTable = {
        new float3(0.0f,1.0f,0.0f),new float3(0.0f,0.0f,0.0f),new float3(1.0f,0.0f,0.0f),new float3(1.0f,1.0f,0.0f),
        new float3(0.0f,0.0f,1.0f),new float3(0.0f,0.0f,0.0f),new float3(0.0f,1.0f,0.0f),new float3(0.0f,1.0f,1.0f),
        new float3(1.0f,0.0f,0.0f),new float3(0.0f,0.0f,0.0f),new float3(0.0f,0.0f,1.0f),new float3(1.0f,0.0f,1.0f),

        new float3(0.0f,1.0f,1.0f),new float3(0.0f,0.0f,1.0f),new float3(1.0f,0.0f,1.0f),new float3(1.0f,1.0f,1.0f),
        new float3(1.0f,0.0f,1.0f),new float3(1.0f,0.0f,0.0f),new float3(1.0f,1.0f,0.0f),new float3(1.0f,1.0f,1.0f),
        new float3(1.0f,1.0f,0.0f),new float3(0.0f,1.0f,0.0f),new float3(0.0f,1.0f,1.0f),new float3(1.0f,1.0f,1.0f)
    };

    public static readonly Vector2[] UVTable;
    
    public static int3[] GenTable;
    
    public static readonly int[] IDTable;
    
    public static readonly float3[] NormalTable = {
        new float3(0.0f,0.0f,-1.0f),
        new float3(-1.0f,0.0f,0.0f),
        new float3(0.0f,-1.0f,0.0f),

        new float3(0.0f,0.0f,1.0f),
        new float3(1.0f,0.0f,0.0f),
        new float3(0.0f,1.0f,0.0f)
    };
    
 
    static ChunkData()
    {
        
        int sizeArr = sizeTexture*sizeTexture;
        float sizeTexturefl = 1f / sizeTexture;
        
        UVTable = new Vector2[sizeArr*4];
        
        for (int i = 0; i < sizeArr; i++)
        {
            
            float y = Mathf.Floor(i * sizeTexturefl);
            float x = ((i - (y / sizeTexturefl)) * sizeTexturefl);
   
            y = 1.0f - (y * sizeTexturefl) - sizeTexturefl;

  
            UVTable[i*4    ] = new float2(x + sizeTexturefl, y);
            UVTable[i*4 + 1] = new float2(x + sizeTexturefl, y + sizeTexturefl);
            UVTable[i*4 + 2] = new float2(x, y + sizeTexturefl);
            UVTable[i*4 + 3]  = new float2(x, y);
              
        }
        
        
        IDTable = new int[sizeArr*6];
        for (int i = 0; i < sizeArr; i++)
        {
            IDTable[i*6    ] = i;
            IDTable[i*6 + 1] = i;
            IDTable[i*6 + 2] = i;
            IDTable[i*6 + 3] = i;
            IDTable[i*6 + 4] = i;
            IDTable[i*6 + 5] = i;
        }
        

        SetBlock(1, 1, 1, 1, 1, 1, 1);
        SetBlock(4, 39, 39, 39, 39, 0, 2);

    }

    public static void AddGenTable()
    {
        GenTable = new int3[(WorldController.chunksSize.x+2)*(WorldController.chunksSize.y+2)*(WorldController.chunksSize.z+2)];
        for (int x = 0; x < WorldController.chunksSize.x+2; x++)
        {
            for (int y = 0; y < WorldController.chunksSize.y+2; y++)
            {
                for (int z = 0; z < WorldController.chunksSize.z+2; z++)
                {
                    GenTable[x+y*(WorldController.chunksSize.x+2)+z*((WorldController.chunksSize.x+2)*(WorldController.chunksSize.y+2))] = new int3(x,y,z);
                }
            }

        }
    }
    static void SetBlock(int idBlock, int left, int right, int top, int down, int front, int back)
    {
        IDTable[idBlock*6    ] = left;
        IDTable[idBlock*6 + 1] = down;
        IDTable[idBlock*6 + 2] = back;
        IDTable[idBlock*6 + 3] = right;
        IDTable[idBlock*6 + 4] = top;
        IDTable[idBlock*6 + 5] = front;
    } 
    
    
    
    public struct Polygon
    {
        public int trisIndex;
    }
    
    public static float modulo (float numeric, float mod) => numeric - (Mathf.Floor(numeric/mod) * mod);
    public static float3 Multiple(this float3 First, float3 Last)
    {
        return new float3(First.x * Last.x, First.y * Last.y, First.z * Last.z);
    }
    
    public static float3 Min(this float3 First, float3 Last)
    {
        return new float3(Math.Min(First.x,Last.x), Math.Min(First.y,Last.y), Math.Min(First.z,Last.z));
    }
    public static float3 Max(ref float3 First, float3 Last)
    {
        First.x = Math.Max(First.x, Last.x);
        First.y = Math.Max(First.y, Last.y);
        First.z = Math.Max(First.z, Last.z);
        return First;
    }
    
    
    
    
}



