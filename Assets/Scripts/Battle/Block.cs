using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public enum BlockType
    {
        Walkable,
        Obstacle,
    }

    public enum BlockEventType
    {
        Healing,
        Battle
    }

    public class BlockInfo
    {
        public int x;
        public int y;
        public Vector2 location;
        public BlockType type;
        public BlockEventType eventType;
        public GameObject eventGo;

        public BlockInfo(int x, int y, Vector2 loc, BlockType t = BlockType.Walkable)
        {
            this.x = x;
            this.y = y;
            location = loc;
            type = t;
        }

        public virtual bool EnterNewView()
        {
            return eventType == BlockEventType.Battle;
        }

        public void OnStepOn()
        {
            GameObject.Destroy(eventGo);
        }

        public void OnStepOff()
        {

        }
    }
    public class BlockBase
    {
        public virtual void OnStepOn()
        {
        }
        public virtual void OnStepOff()
        {
        }
    }
    public class HealBlock : BlockBase
    {
        public int _addValue;

        public HealBlock(int addValue)
        {
            _addValue = addValue;
        }

        override public void OnStepOn()
        {
            base.OnStepOn();
        }

        public override void OnStepOff()
        {
            base.OnStepOff();
        }
    }
}