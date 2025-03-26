using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public enum BlockType
    {

    }

    public class BlockInfo
    {
        public int x;
        public int y;
        BlockType type;
    }

    public class Block
    {
        public BlockInfo info;

        // 玩家落入该格子
        public void OnStepOn()
        {
            // todo.. 
        }

        // 玩家离开该格子
        public void OnStepOff()
        {
            // todo.. 
        }
    }
}