using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;

namespace PDR
{
    public class MapPawn
    {
        public GameObject _gameObject;
        public Vector2Int _gridPosition;
        public AnimControlComp _animControlComp;
        public TeamType _teamType;
        public int _moveRange;
        public int _attackRange;
        public PawnGo _pawnGo;

        public MapPawn(BlockInfo block, GameObject gameObject, TeamType teamType, int moveRange, int attackRange)
        {
            _gameObject = gameObject;
            _gameObject.GetComponent<Transform>().position = block._worldlocation;
            _gridPosition = block._gridLocation;
            _animControlComp = _gameObject.AddComponent<AnimControlComp>();
            _teamType = teamType;
            _pawnGo = _gameObject.GetComponent<PawnGo>();
            _moveRange = moveRange;
            _attackRange = attackRange;
        }

        public virtual void PostInitialize()
        {
            PlayAnimation("Idle");
            UpdateGo();
        }

        public void UpdatePawnBlock(BlockInfo block, BlockInfo oldBlock)
        {
            oldBlock.pawn = null;
            _gridPosition = block._gridLocation;
            _gameObject.GetComponent<Transform>().position = block._worldlocation;
            block.pawn = this;
        }

        public Transform GetTransform() { return _gameObject.transform; }

        public void PlayAnimation(string animation, float crossFade = 0.2f, float time = 0)
        {
            _animControlComp.ChangeAnimation(animation, crossFade, time);
        }

        public void UpdateTeamType(TeamType teamType)
        {
            _teamType = teamType;
        }

        public bool IsSameTeam(MapPawn pawn)
        {
            return pawn != null && pawn._teamType == _teamType;
        }

        public virtual float TakeDamage(MapPawn damageSource, float damageValue)
        {
            return damageValue;
        }

        public virtual float GetAttackValue()
        {
            return 0.0f;
        }

        // ¸üÐÂUI
        virtual protected void UpdateGo() { }
    }
}
