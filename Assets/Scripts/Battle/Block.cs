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