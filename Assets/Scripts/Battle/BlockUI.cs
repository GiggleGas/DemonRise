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

    public class BlockInfo
    {
        public Vector2Int _gridLocation;
        public Vector2 _worldlocation;
        public BlockType _type;
        public BlockUI blockUI;
        public MapPawn pawn;

        public BlockInfo(Vector2Int gridLocation, Vector2 loc, BlockType type = BlockType.Walkable)
        {
            _gridLocation = gridLocation;
            _worldlocation = loc;
            _type = type;
        }

        public void OnStepOn(MapPawn mapPawn)
        {
            // ½±ÀøµØ¿éÊÂ¼þ
            if(blockUI.IsGolden() && mapPawn is PlayerPawn)
            {
                blockUI.SetGolden(false);
                EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.STEP_ON_GOLDEN_BLOCK);
            }
        }

        public void OnStepOff()
        {

        }
    }

    public class BlockUI : MonoBehaviour
    {
        private Vector2Int _gridLocation;
        private GameObject _selected;
        private GameObject _path;
        private GameObject _range;
        private GameObject _warning; 
        private GameObject _golden;
        private GameObject _poison;

        private void Awake()
        {
            _selected = transform.Find("selected").gameObject;
            _path = transform.Find("path").gameObject;
            _range = transform.Find("range").gameObject;
            _warning = transform.Find("warning").gameObject;
            _golden = transform.Find("golden").gameObject;
            _poison = transform.Find("poison").gameObject;
        }

        private void OnMouseOver()
        {
            _selected.SetActive(true);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.DRAW_ROAD, _gridLocation);
        }

        private void OnMouseExit()
        {
            _selected.SetActive(false);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CLEAR_ROAD);
        }

        private void OnMouseDown()
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.BLOCK_MOUSE_DOWN, _gridLocation);
        }

        public void SetGridLocation(Vector2Int gridLocation)
        {
            _gridLocation = gridLocation;
        }

        public void ShowPath(bool value)
        {
            _path.SetActive(value);
        }

        public void ShowRange(bool value)
        {
            _range.SetActive(value);
        }

        public void ShowWarning(bool value)
        {
            _warning.SetActive(value);
        }

        public void SetGolden(bool value)
        {
            _golden.SetActive(value);
        }

        public bool IsGolden()
        {
            return _golden.activeSelf;
        }

        public void ShowPoison(bool value)
        {
            _poison.SetActive(value);
        }
    }

}
