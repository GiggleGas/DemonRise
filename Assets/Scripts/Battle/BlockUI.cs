using PDR;
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
        public BlockUI blockUI;
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

    public class BlockUI : MonoBehaviour
    {
        private int _x;
        private int _y;
        private GameObject selected;
        private GameObject path;

        public void SetGridLocation(int x, int y)
        {
            _x = x;
            _y = y;
        }

        private void Awake()
        {
            selected = transform.Find("selected").gameObject;
            path = transform.Find("path").gameObject;
        }

        private void OnMouseOver()
        {
            selected.SetActive(true);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.DRAW_ROAD, _x, _y);
        }

        private void OnMouseExit()
        {
            selected.SetActive(false);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CLEAR_ROAD);
        }

        private void OnMouseDown()
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.BLOCK_MOUSE_DOWN, _x, _y);
        }

        private void OnMouseUp()
        {
            // EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.BLOCK_MOUSE_UP, _x, _y);
        }

        public void ShowPath(bool value)
        {
            path.SetActive(value);
        }
    }

}
