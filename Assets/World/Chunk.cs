

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


[RequireComponent( typeof(MeshCollider), typeof(Renderer), typeof(MeshFilter))]
public class Chunk : MonoBehaviour
{
    [HideInInspector]public ChunkHundler handler;
    public Mesh mesh;
    private MeshCollider meshCollider;
    //private MeshFilter meshFilter;
    //[HideInInspector]public  MeshRenderer meshRenderer;
    public Vector3 position;
    public List<Chunk> unDrawChunk;
    public OctoTree octoree;
    public int state = 0;
    [HideInInspector]public WorldController world;
    [HideInInspector]public Material material;
    [HideInInspector]public MeshFilter meshFilter;
    [HideInInspector]public MeshRenderer meshRenderer;
    [HideInInspector]public int[] chunkData;

    [HideInInspector]public int3 chunkSize;
    [HideInInspector]public int numBlocksFromChunk;
 
    //public bool drawChunk;
    
    public void Init( int3 chunkSize, Material mat , WorldController worldController)
    {
        world = worldController;
        unDrawChunk = new List<Chunk>();
        meshCollider = GetComponent<MeshCollider>();
        numBlocksFromChunk = chunkSize.x * chunkSize.y * chunkSize.z;
        chunkData = new int[(chunkSize.x+2) * (chunkSize.y+2) * (chunkSize.z+2)];
        this.chunkSize = chunkSize;
        mesh = new Mesh();
        material = Instantiate(mat);
        meshCollider.sharedMesh = mesh;
        mesh.indexFormat = IndexFormat.UInt16;
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = material;

    }

    private void OnApplicationQuit ()=> DestroyImmediate(gameObject);

    public void ChunkClear()
    {
        meshRenderer.enabled = false;
        mesh.Clear();
        meshCollider.sharedMesh = null;
        StopAllCoroutines();
        world.chunksDestroy.Add(this);
        
        for (int i = 0; i < unDrawChunk.Count; i++)
            unDrawChunk[i].ChunkClear();
        
        unDrawChunk.Clear();
    }
    
    public IEnumerator UnDraw()
    {
        state = 2;
        while (!meshRenderer.enabled )
        {
            if(unDrawChunk.Count==0)
                yield break;
            yield return new WaitForEndOfFrame();
        }

       
        for (int i = 0; i < unDrawChunk.Count; i++)
            unDrawChunk[i].ChunkClear();
        
        unDrawChunk.Clear();

    }

    public IEnumerator UnDrawTop()
    {
        state = 1;
        yield return new WaitUntil(()=>octoree.AllChunkDrawInBranch(true)
                                       || !octoree.CanInTree(octoree.rectDistance.GetResize(octoree.rectDistance.size*2)));
        yield return new WaitForSeconds(1f);
        ChunkClear();
    }

    public void SetDataInChunk(float3 lod , ref ChunkHundler.GetPolyFromArrayCompute polyDat)
    {
       
        material.SetFloat("_SizeVoxelX", lod.x);
        material.SetFloat("_SizeVoxelY", lod.y);
        material.SetFloat("_SizeVoxelZ", lod.z);
        
        Mesh.ApplyAndDisposeWritableMeshData(polyDat.meshDataArray, mesh, 
            MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds);
        
        mesh.bounds = new Bounds((polyDat.boundMin + ((polyDat.boundMax - polyDat.boundMin) * 0.5f))/lod, ((polyDat.boundMax - polyDat.boundMin))/lod);
        
        StartCoroutine(BakeMeshTask());
        
        meshCollider.sharedMesh = mesh;
        meshFilter.sharedMesh = mesh;
    }

    IEnumerator BakeMeshTask()
    {
    
        var baker = new BakeMesh();
        baker.inst = mesh.GetInstanceID();


        var jobHandle = baker.Schedule();

        yield return new WaitUntil(() => jobHandle.IsCompleted);
          
        
        jobHandle.Complete();
    }
    
    [BurstCompile]
    struct BakeMesh : IJob
    {
        [ReadOnly]public int inst;
        public void Execute()
        {
            Physics.BakeMesh(inst, false);
        }
    }

  
}
