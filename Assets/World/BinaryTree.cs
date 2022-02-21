using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;


public struct OctoRectTransform
{
    
    public Vector3 pos;
    public Vector3 size;
    
    public OctoRectTransform(float x1, float y1, float z1, float x2, float y2, float z2)
    {
        pos.x = math.min(x1,x2);
        pos.y = math.min(y1,y2);
        pos.z = math.min(z1,z2);
        
        size.x = math.max(x1,x2) - pos.x;
        size.y = math.max(y1,y2) - pos.y;
        size.z = math.max(z1,z2) - pos.z;
    }

}

public struct DisRectTransform
{
    public Vector3 pos;
    public Vector3 size;
    
    public DisRectTransform(float x1, float y1, float z1, float x2, float y2, float z2) 
    {
        pos.x = x1;
        pos.y = y1;
        pos.z = z1;
        
        size.x = x2;
        size.y = y2;
        size.z = z2;
    }
    
    public DisRectTransform GetResize(Vector3 newSize) => new DisRectTransform(pos.x,pos.y,pos.z,newSize.x,newSize.y,newSize.z);

    public Vector3 GetPos(Vector3 newSize)
    {
        Vector3 tPos;

        
        tPos.x = pos.x-(size.x);
        tPos.y = pos.y-(size.y);
        tPos.z = pos.z-(size.z);
        return tPos;
    }
    
    
    
    
}
[Serializable]
    public class OctoTree 
    {

    public Chunk main;
    public OctoTree parent;
    public OctoTree start;
    public OctoTree[] branchs;
    public OctoRectTransform rectTransform;
    public OctoRectTransform rectTransformParent;
    public DisRectTransform rectDistance;
    public int levelLod = -1;
    
    public OctoTree( OctoRectTransform lod)
    {
        //branchs = new OctoTree[numberOfBrench];
   
        rectTransform = lod;
    }

    public void TreeInit(OctoRectTransform lod,  DisRectTransform rectDist, int lodParent = 0, OctoTree parentTree = null)
    {
        parent = parentTree == null ? this : parentTree;
        rectDistance = rectDist;
        levelLod = lodParent;
        lodParent ++;
        if (lod.size.x/2 >= 1.0f)
        {
            if (start == null)
                lod.pos = Vector3.zero;
            
            branchs = new OctoTree[4];
            branchs[0] = new OctoTree(new OctoRectTransform(lod.pos.x, 0,lod.pos.z,lod.pos.x+lod.size.x / 2,0,lod.pos.z+lod.size.z / 2));
            branchs[0].start = this;
            branchs[0].TreeInit(branchs[0].rectTransform,rectDist.GetResize(rectDist.size / 2),lodParent,parent);

            branchs[1] = new OctoTree(new OctoRectTransform(lod.pos.x+lod.size.x / 2, 0, lod.pos.z, lod.pos.x+lod.size.x, 0, lod.pos.z+lod.size.z / 2));
            branchs[1].start = this;
            branchs[1].TreeInit(branchs[1].rectTransform,rectDist.GetResize(rectDist.size / 2),lodParent,parent);

            branchs[2] = new OctoTree(new OctoRectTransform(lod.pos.x, 0, lod.pos.z+lod.size.z / 2, lod.pos.x+lod.size.x / 2, 0, lod.pos.z+lod.size.z));
            branchs[2].start = this;
            branchs[2].TreeInit(branchs[2].rectTransform,rectDist.GetResize(rectDist.size / 2),lodParent,parent);

            branchs[3] = new OctoTree(new OctoRectTransform(lod.pos.x+lod.size.x / 2, 0, lod.pos.z+lod.size.z / 2, lod.pos.x+lod.size.x, 0, lod.pos.z+lod.size.z));
            branchs[3].start = this;
            branchs[3].TreeInit(branchs[3].rectTransform,rectDist.GetResize(rectDist.size / 2),lodParent,parent);
        }
        
 
        rectTransformParent = parent == this ? new OctoRectTransform(0,0,0,0,0,0) : parent.rectTransform;
    }

    
    public void ClearTree(List<Chunk> chunksDestroy, List<Chunk> destCh = null)
    {
        if (branchs != null)
        {
            branchs[0].ClearTree(chunksDestroy, destCh);
            branchs[1].ClearTree(chunksDestroy, destCh);
            branchs[2].ClearTree(chunksDestroy, destCh);
            branchs[3].ClearTree(chunksDestroy, destCh);
        }

        ClearOneBranch(chunksDestroy, destCh);

    }
    public void ClearTreeInst(List<Chunk> chunksDestroy)
    {
        if (branchs != null)
        {
            branchs[0].ClearTreeInst(chunksDestroy);
            branchs[1].ClearTreeInst(chunksDestroy);
            branchs[2].ClearTreeInst(chunksDestroy);
            branchs[3].ClearTreeInst(chunksDestroy);
        }

        ClearOneBranchInst(chunksDestroy);

    }
    
    public void ClearOneBranch(List<Chunk> chunksDestroy, List<Chunk> destCh = null)
    {
        if (main != null )
        {
            
            if (destCh != null)
            {
                destCh.Add(main);
                for (int i = 0; i < main.unDrawChunk.Count; i++)
                    destCh.Add(main.unDrawChunk[i]);
                
                main.unDrawChunk.Clear();
            }

            if (main.handler != null)
            {
       
                main.handler.AddListDes();
                
            }
            



            main = null;
        }
    }

    
    public void ClearOneBranch(List<Chunk> chunksDestroy)
    {
        if (main != null )
        {
            
            if (main.handler != null)
            {
        
                main.handler.AddListDes();
                
            }
            
            
            main = null;
        }
    }
    
    public void ClearOneBranchInst(List<Chunk> chunksDestroy)
    {
        if (main != null )
        {
            if (main.handler != null)
            {
       
                main.handler.AddListDes();
                
            }
            
            
            for (int i = 0; i < main.unDrawChunk.Count; i++)
                main.unDrawChunk[i].ChunkClear();
            main.unDrawChunk.Clear();
            
            main.ChunkClear();
            main = null;
        }
    }
    
    public bool isTree()
    {
        return branchs == null || !CanInTree(rectDistance);
    }

    public bool AllChunkDrawInBranch(bool first = false)
    {
        bool result = true;
        if (main == null || first)
        {
            if (branchs == null)
                result = false;
            else
            {
                result = branchs[0].AllChunkDrawInBranch() ? result : false;
                result = branchs[1].AllChunkDrawInBranch() ? result : false;
                result = branchs[2].AllChunkDrawInBranch() ? result : false;
                result = branchs[3].AllChunkDrawInBranch() ? result : false;
            }

        }else
        result = main.meshRenderer.enabled;
        
        return result;
    }

    public bool prosses;
    public void UpdateTree(WorldController world, OctoTree chunkPar = null)
    {
      
        if (!isTree())
        {
            if (main != null)
            {
                main.octoree = this;
                main.StartCoroutine(main.UnDrawTop());
                ClearOneBranch(world.chunksDestroy);
            }
            
            OctoTree temp = chunkPar == null ? (main == null ? null : this) : chunkPar;
            
           
            branchs[0].rectDistance.pos = rectDistance.pos;
            branchs[0].UpdateTree(world, temp);
            branchs[1].rectDistance.pos = rectDistance.pos;
            branchs[1].UpdateTree(world, temp);
            branchs[2].rectDistance.pos = rectDistance.pos;
            branchs[2].UpdateTree(world, temp);
            branchs[3].rectDistance.pos = rectDistance.pos;
            branchs[3].UpdateTree(world, temp);
            

        }
        else
        {
            if ( main == null && world.chunkHandlers.Count>0 && levelLod>0 && levelLod<=world.lodUpdateTree.Length)
            {   
                if (world.chunksDestroy.Count == 0)
                    world.StartCoroutine(world.AddChunk());
                    
                //var handler = world.chunkHandlers[world.chunkHandlers.Count-1];
                var tempChunk = world.chunksDestroy[world.chunksDestroy.Count-1];
                world.chunksDestroy.RemoveAt(world.chunksDestroy.Count-1);
                //world.chunkHandlers.RemoveAt(world.chunkHandlers.Count-1);
              
                //tempChunk.handler = handler;
                //handler.chunk = tempChunk;
                //handler.onList = false;
                

                
                ClearTree(world.chunksDestroy, tempChunk.unDrawChunk);
              
                main = tempChunk;

                prosses = true;
                rectTransformParent = parent == this ? new OctoRectTransform(0,0,0,0,0,0) : parent.rectTransform;
                tempChunk.gameObject.transform.position = new Vector3(
                    (rectTransform.pos.x+rectTransformParent.pos.x) * tempChunk.chunkSize.x,
                    0 * tempChunk.chunkSize.y,
                    (rectTransform.pos.z+rectTransformParent.pos.z) * tempChunk.chunkSize.z);
                tempChunk.position = new Vector3((rectTransform.pos.x+rectTransformParent.pos.x), 0,(rectTransform.pos.z+rectTransformParent.pos.z));
                main.StartCoroutine(main.UnDraw());
                
                
                
                
                world.lodUpdateTree[levelLod-1].Add(this);
            
            }     
                
        }



    }
    
    
    public bool CanInTree(DisRectTransform objTransform)
    {
        Vector3 pos = parent == null || parent == this  ? Vector3.zero : parent.rectTransform.pos;
        return (rectTransform.pos.x + pos.x >= objTransform.pos.x-objTransform.size.x && 
                /*objTransform.pos.y-objTransform.size.y > rectTransform.y &&*/
                rectTransform.pos.z + pos.z >= objTransform.pos.z-objTransform.size.z &&
                rectTransform.pos.x + pos.x + rectTransform.size.x <= objTransform.pos.x+objTransform.size.x &&
                /*objTransform.pos.y+objTransform.size.y < rectTransform.y + rectTransform.h &&*/
                rectTransform.pos.z + pos.z + rectTransform.size.z <= objTransform.pos.z+objTransform.size.z);
    }

}
