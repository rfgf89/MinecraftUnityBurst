using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    
    public List<OctoTree> octoLoad = new List<OctoTree>();
    public List<OctoTree> octoDis = new List<OctoTree>();
    private OctoRectTransform loadOcto;
    private DisRectTransform disOcto;
    
    
    [HideInInspector]public List<Chunk> chunksDestroy = new List<Chunk>();
    [HideInInspector]public List<ChunkHundler> chunkHandlers = new List<ChunkHundler>();
    
    [HideInInspector]public List<OctoTree>[] lodUpdateTree;
    //public List<OctoTree> chunkUpdateDatas = new List<OctoTree>();
    [SerializeField]private int3 sizeHandlersPartChunk;

    
    
    [SerializeField] private Transform moveWorldToObject;
    [SerializeField] private Material matWorld;
    
    [SerializeField] private GameObject chunkHandlerObject;
    [SerializeField] private GameObject chunkObject;
    [SerializeField] private Transform chunksPlace;
    [SerializeField] private int numChunkHandler;
    
     public int loadGraphicDistance;
    [SerializeField] private int lodDistance;
    [SerializeField] private int loadPhysicsDistance;
    private List<Vector3> loadList = new List<Vector3>();

    


    public float timeChunk;
    public int numChunk;

    [HideInInspector]public List<float> numTimeChunk = new List<float>();
    


    
    public TextMeshProUGUI text;
    
    public Vector3 position;
    [SerializeField] private int3 chunksSizeWorld;
    public static int3 chunksSize;
    
    private bool loadChunks;
    
    

    private void Awake()
    {
        chunksSize = chunksSizeWorld;
        ChunkData.AddGenTable();
        
        int lodLevels = 0;
        for (float i = loadGraphicDistance; i/2 >= 1.0f; i/=2f)
            lodLevels++;
        
        
        lodUpdateTree = new List<OctoTree>[lodLevels];
        for (int i = 0; i < lodUpdateTree.Length; i++)
            lodUpdateTree[i] = new List<OctoTree>();

        
        
        loadOcto = new OctoRectTransform(0,0,0,loadGraphicDistance,loadGraphicDistance,loadGraphicDistance);
        disOcto = new DisRectTransform(0,0,0,loadGraphicDistance*lodDistance,loadGraphicDistance*lodDistance, loadGraphicDistance*lodDistance);
    }

    
    
    void Start(){
        
        ChunkHundler hand;
        for (int i = 0; i < numChunkHandler; i++)
        {
            hand = Instantiate(chunkHandlerObject).GetComponent<ChunkHundler>();
            chunkHandlers.Add(hand);
            hand.world = this;
            hand.gameObject.transform.SetParent(chunksPlace);
            hand.HandlerInit(sizeHandlersPartChunk, chunksSize);
            
        }

        
     
        Vector2 pos = new Vector2();
        Vector3 posList;
        for (pos.x = 0; pos.x <= 4; pos.x += 1)
        {

            for (pos.y = 0f; pos.y < 6.283f; pos.y += (6.283f/(8*6.283f))/2)
            {
                posList = new Vector3(Mathf.Round(Mathf.Cos(pos.y) * pos.x)-0.5f,0.5f, Mathf.Round(Mathf.Sin(pos.y) * pos.x)-0.5f);
                if (math.distance(posList+Vector3.one/2, position) <= 4)
                {
                    posList *= loadGraphicDistance;
                    if (loadList.Find(x => (x.x == posList.x && x.y == posList.y && x.z == posList.z)) == default)
                        loadList.Add(posList);
                }
            }
        }

        
        for (int i = 0; i < loadList.Count*2; i++)
        {
            OctoTree tree = new OctoTree(loadOcto);
            tree.rectTransform.pos.x = 15;
            tree.rectTransform.pos.y = 0;
            tree.rectTransform.pos.z = 15;
            tree.TreeInit(tree.rectTransform, disOcto);
            octoDis.Add(tree);
        }
        
        UnLoadChunkElips();
        LoadChunkElips();
        
    }

    public IEnumerator AddChunk()
    {
        Chunk ch;
        ch = Instantiate(chunkObject, chunksPlace).GetComponent<Chunk>();
        chunksDestroy.Add(ch);
        ch.Init( chunksSize, matWorld, this);
        ch.meshRenderer.enabled= false;
        yield break;
    }
    void Update()
    {

        float time = 0;
        foreach (var VARIABLE in numTimeChunk)
        {
            time += VARIABLE;
        }
        
        text.text = "MidChunk"+time / numTimeChunk.Count;
        
        
        if (moveWorldToObject != null)
        {
            position.x = math.floor(moveWorldToObject.transform.position.x / chunksSize.x);
            //position.y = math.floor(moveWorldToObject.transform.position.y / chunksSize.y);
            position.z = math.floor(moveWorldToObject.transform.position.z / chunksSize.z);
            disOcto.pos.x = position.x;
            disOcto.pos.z = position.z;
        }

        
        for (int i = lodUpdateTree.Length - 1; i >= 0; i--)
        for (int j = 0; j < lodUpdateTree[i].Count; j++)
            SetChunkUpdate(lodUpdateTree[i], j);
            

    }

    public bool updataDates;
    public void UpdateCurrentChunk(float3 posChunk)
    {
        if (octoDis.Count > 0 && octoLoad.Find(x =>
            x.rectTransform.pos.x == posChunk.x &&
            x.rectTransform.pos.y == 0 &&
            x.rectTransform.pos.z == posChunk.z) == null)
        {
            OctoTree octo;

            octo = octoDis[0];
            octo.rectTransform.pos.x = posChunk.x;
            octo.rectTransform.pos.y = 0;
            octo.rectTransform.pos.z = posChunk.z;
            octo.rectDistance = disOcto;
            octo.UpdateTree(this);
            octoDis.RemoveAt(0);

            octoLoad.Add(octo);
        }
        
                
            
    }
/*
    private void OnDrawGizmos()
    {
        for (int i = 0; i < octoLoad.Count; i++)
        {
            Gizmos.DrawWireCube((octoLoad[i].rectTransform.pos+octoLoad[i].rectTransform.size/2)
                                * chunksSize.x, (octoLoad[i].rectTransform.size)* chunksSize.x);
        }
    }*/

    public void SetChunkUpdate(List<OctoTree> octotree,int i)
    {
        
        if (octotree[i].main!=null && octotree[i].isTree())
        {
            if (chunkHandlers.Count > 0)
            {
                var handler = chunkHandlers[chunkHandlers.Count - 1];
                chunkHandlers.RemoveAt(chunkHandlers.Count - 1);

                octotree[i].main.handler = handler;
                handler.chunk = octotree[i].main;
                handler.onList = false;

                handler.chunk.octoree = octotree[i];
                
                octotree[i].main.handler.StartCoroutine(octotree[i].main.handler.GenerateChunkData(new float3(
                    1f / octotree[i].rectTransform.size.x,
                    1f / octotree[i].rectTransform.size.x, 1f / octotree[i].rectTransform.size.z)));
                octotree[i].prosses = false;
                
                
                octotree.RemoveAt(i);
                
            }
        }
        else
        {
            octotree[i].prosses = false;
            octotree.RemoveAt(i);
        }
        
    }


    public async void LoadChunkElips()
    {
  
        for (var i = 0; i < loadList.Count; i++)
            UpdateCurrentChunk(math.floor((loadList[i] + position) / loadGraphicDistance) * loadGraphicDistance);
  
        await Task.Delay(1);
        LoadChunkElips();
    }

    public async void UnLoadChunkElips()
    {
        float dist = 0f;
            for (var i = 0; i < octoLoad.Count; i++)
            {
                
                dist = math.distance(octoLoad[i].rectTransform.pos+octoLoad[i].rectTransformParent.pos + Vector3.one / 2, position) / loadGraphicDistance;
                    if (dist < 5f )
                    {
                        octoLoad[i].rectDistance = disOcto;
                        octoLoad[i].UpdateTree(this);
                        
                    }
                    else
                    {
                        octoLoad[i].ClearTreeInst(chunksDestroy);
                        octoDis.Add(octoLoad[i]);
                        octoLoad.RemoveAt(i);

                    }
                    
                    
            }

            await Task.Delay(1);
        UnLoadChunkElips();


    }
    
    
    
}
