using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public enum BlockType
    {
        Walkable,
        Obstacle
    }

    public class BlockInfo
    {
        public int x;
        public int y;
        public Vector2 location;
        public BlockType type;
        public BlockInfo(int x, int y, Vector2 loc, BlockType t = BlockType.Walkable)
        {
            this.x = x;
            this.y = y;
            location = loc;
            type = t;
        }
    }

    public class Block
    {
        public BlockInfo info;

        // �������ø���
        public void OnStepOn()
        {
            // todo.. 
        }

        // ����뿪�ø���
        public void OnStepOff()
        {
            // todo.. 
        }
    }
}