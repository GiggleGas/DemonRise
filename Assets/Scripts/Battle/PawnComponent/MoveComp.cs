using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace PDR
{
    public class MoveComp : MonoBehaviour
    {
        public void RegisterEvents()
        {
            EventMgr.Instance.Register<MapPawn, List<Vector2Int>>(EventType.EVENT_BATTLE_UI, SubEventType.MOVE_PLAYER, TryMoveGO);
        }

        private Coroutine currentMoveCoroutine; // ��ǰ���е��ƶ�Э��
        public void TryMoveGO(MapPawn pawn, List<Vector2Int> path)
        {
            // ��ʼ�ƶ�Э��
            currentMoveCoroutine = StartCoroutine(MoveAlongPath(pawn, path));
        }

        /// <summary>
        /// ��·���ƶ���Э��
        /// </summary>
        /// 
        private IEnumerator MoveAlongPath(MapPawn obj, List<Vector2Int> path)
        {
            int currentPathIndex = 0;
            Vector3 targetPosition;
            float movementSpeed = 5.0f; // �������ƶ��ٶ�
            obj.PlayAnimation("Move");

            // ������ʼ�㣨����Ѿ�����ȷλ�ã�
            if (obj._gridPosition == path[0])
            {
                currentPathIndex++;
            }

            while (currentPathIndex < path.Count)
            {
                BlockInfo curBlock = BattleManager.Instance.GetBlockByGridLocation(obj._gridPosition);
                BlockInfo targetBlock = BattleManager.Instance.GetBlockByGridLocation(path[currentPathIndex]);
                targetPosition = targetBlock._worldlocation;

                // �ƶ�����һ��·����
                while (Vector3.Distance(obj.GetTransform().position, targetPosition) > 0.01f)
                {
                    obj.GetTransform().position = Vector3.MoveTowards(
                        obj.GetTransform().position,
                        targetPosition,
                        movementSpeed * Time.deltaTime
                    );
                    yield return null;
                }

                // ȷ����ȷ����
                obj.UpdatePawnBlock(targetBlock, curBlock);
                BattleManager.Instance.ModifyEnergy(-1);

                // ���µؿ��¼� ������������ʵ�֣��ٶ���
                //BlockInfo currentBlock = BattleManager.Instance.GetBlockByGridLocation(path[currentPathIndex]);
                //currentBlock.OnStepOn();

                currentPathIndex++;

                // ÿ��·����֮��ļ������ѡ��
                yield return new WaitForSeconds(0.01f);
            }

            obj.PlayAnimation("Idle");
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.PLAYER_MOVE_FINISH, obj);
            currentMoveCoroutine = null;
        }
    }

    public class AIMoveComp : MonoBehaviour
    {
        float movementSpeed = 5.0f; // �������ƶ��ٶ�
        float attackTimeOut = 0.8f;

        public void RegisterEvents()
        {
            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.MOVE_AI, UpdateAITurn);
        }

        public void UpdateAITurn()
        {
            StartCoroutine(ProcessMonsterTurn());
        }

        private IEnumerator ProcessMonsterTurn()
        {
            // �������й���ִ���ж�
            foreach (MapPawn monster in BattleManager.Instance.enemyPawns)
            {
                if (monster == null) continue;
                if (BattleManager.Instance.CanAIAttack(monster))
                {
                    // ������������
                    yield return StartCoroutine(HandleMonsterAttack(monster));
                }
                else 
                {
                    BlockInfo enemyBlock = BattleManager.Instance.GetBlockByGridLocation(monster._gridPosition);
                    List<Vector2Int> movePath = BattleManager.Instance.FindShortestPath(enemyBlock._gridLocation, BattleManager.Instance._playerPawn._gridPosition);
                    if(movePath.Count <= 0)
                    {
                        continue;
                    }
                    monster.PlayAnimation("Move");
                    List<Vector2Int> savePath = movePath.GetRange(1, monster._moveRange);
                    int currentPathIndex = 0;
                    Vector3 targetPosition;
                    while (currentPathIndex < savePath.Count)
                    {
                        BlockInfo curBlock = BattleManager.Instance.GetBlockByGridLocation(monster._gridPosition);
                        BlockInfo targetBlock = BattleManager.Instance.GetBlockByGridLocation(savePath[currentPathIndex]);
                        targetPosition = targetBlock._worldlocation;

                        // �ƶ�����һ��·����
                        while (Vector3.Distance(monster.GetTransform().position, targetPosition) > 0.01f)
                        {
                            monster.GetTransform().position = Vector3.MoveTowards(
                                monster.GetTransform().position,
                                targetPosition,
                                movementSpeed * Time.deltaTime
                            );
                            yield return null;
                        }

                        // ȷ����ȷ����
                        monster.UpdatePawnBlock(targetBlock, curBlock);
                        currentPathIndex++;
                        monster.PlayAnimation("Idle");

                        // ÿ��·����֮��ļ������ѡ��
                        yield return new WaitForSeconds(0.01f);
                    }
                }
                yield return new WaitForSeconds(0.5f); // �ж����
            }
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.AI_TURN_FINISH);
        }

        private IEnumerator HandleMonsterAttack(MapPawn enemy)
        {
            // ������������
            enemy._animControlComp.ChangeAnimation("Attack");

            yield return new WaitForSeconds(attackTimeOut);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.AI_ATTACK_FINISH, enemy);
        }
    }

    public class AnimControlComp : MonoBehaviour
    {
        private Animator animator;
        private MapPawn _sourcePawn;

        private MapPawn _targetPawn;

        protected void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void SetSourcePawn(MapPawn sourcePawn)
        {
            _sourcePawn = sourcePawn;
        }

        public void RegisterAttackContext(MapPawn targetPawn)
        {
            _targetPawn = targetPawn;
        }

        public void ChangeAnimation(string animation, float crossFade = 0.2f, float time = 0.0f)
        {
            if(time >0)
            {
                StartCoroutine(Wait());
            }
            else
            {
                Validate();
            }

            IEnumerator Wait()
            {
                yield return new WaitForSeconds(time - crossFade);
                EndAnimation();
            }

            void Validate()
            {
                animator.CrossFade(animation, crossFade);
            }

            void EndAnimation()
            {
                animator.CrossFade(animation, crossFade);
                EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.PLAYER_ATTACK_FINISH, _sourcePawn, _targetPawn);
            }
        }

        /// <summary>
        /// ���޸ĵ�animation��timeʱ����޸ĵ�afterAnimͬʱ����֪ͨ�����ƹ��������������ԭ��������ͬʱ����֪ͨ
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="afterAnim"></param>
        /// <param name="time"></param>
        /// <param name="crossFade"></param>
        public void ChangeDuringAnimation(string animation, float time = 1.0f, float crossFade = 0.2f)
        {
            animator.CrossFade(animation, crossFade);
            StartCoroutine(Wait());

            IEnumerator Wait()
            {
                yield return new WaitForSeconds(time - crossFade);
                EndAnimation();
            }

            void EndAnimation()
            {
                EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.ANIM_FINISH, _sourcePawn, animation);
            }
        }
    }
}
