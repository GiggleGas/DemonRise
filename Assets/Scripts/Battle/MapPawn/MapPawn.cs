using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;

namespace PDR
{
    /// <summary>
    /// ��ͼPawn���࣬һ���ڵ�ͼ�ϵ�item�������Ϊ��׼
    /// </summary>
    public class MapPawn
    {
        public GameObject _gameObject;                  // ��������Go
        public Vector2Int _gridLocation;                // λ��
        public PawnDisplayComp _pawnDisplayComp;        // �������
        public Animator _animator;                      // �������

        public TeamType _teamType;
        public int _moveRange;
        public int _attackRange;
        public bool IsValid;

        public MapPawn(BlockInfo block, GameObject gameObject, TeamType teamType, int moveRange, int attackRange)
        {
            _gameObject = gameObject;
            _gameObject.GetComponent<Transform>().position = block._worldlocation;
            _gridLocation = block._gridLocation;
            _pawnDisplayComp = _gameObject.GetComponent<PawnDisplayComp>();
            _animator = _gameObject.GetComponent<Animator>();
            _teamType = teamType;
            _moveRange = moveRange;
            _attackRange = attackRange;
            block.pawn = this;
            IsValid = true;
        }

        public virtual void PostInitialize()
        {
            PlayContinuousAnimation("Idle");
            UpdateGo();
        }

        public void UpdatePawnBlock(BlockInfo block, BlockInfo oldBlock)
        {
            oldBlock.pawn = null;
            _gridLocation = block._gridLocation;
            _gameObject.GetComponent<Transform>().position = block._worldlocation;
            block.pawn = this;
        }

        public Transform GetTransform() { return _gameObject.transform; }

        /// <summary>
        /// ���ų����Զ�������idle��move��
        /// </summary>
        /// <param name="animation"></param>
        public void PlayContinuousAnimation(string animation)
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_PLAY_CONTINUOUS_ANIMATION, this, animation);
        }

        /// <summary>
        /// ����һ���Զ���
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="time"></param>
        /// <param name="crossFade"></param>
        public void PlayOnceAnimation(string animation, float time = 1.0f, float crossFade = 0.2f)
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_PLAY_ONCE_ANIMATION, this, animation, time);
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

        public void DestroySelf()
        {
            // todo �����
            BattleManager.Instance.GetBlockByGridLocation(_gridLocation).pawn = null;
            _gameObject.SetActive(false);
        }

        // ����UI
        virtual protected void UpdateGo() { }

        // x���Ϊfalse, x��СΪtrue
        public void UpdateRot(float deltaX)
        {
            _gameObject.GetComponent<SpriteRenderer>().flipX = deltaX < 0;
        }

        virtual public void OnAnimFinish(string animation)
        {

        }
    }
}
