using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public class MapPawn
    {
        public GameObject _gameObject;
        public BlockInfo _blockInfo;

        public virtual void UpateBlockInfo(BlockInfo block) { }
        public Transform GetTransform() { return _gameObject.transform; }
    }
}
