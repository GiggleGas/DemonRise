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

        private Coroutine currentMoveCoroutine; // 当前运行的移动协程
        public void TryMoveGO(MapPawn pawn, List<Vector2Int> path)
        {
            // 开始移动协程
            currentMoveCoroutine = StartCoroutine(MoveAlongPath(pawn, path));
        }

        /// <summary>
        /// 沿路径移动的协程
        /// </summary>
        /// 
        private IEnumerator MoveAlongPath(MapPawn obj, List<Vector2Int> path)
        {
            int currentPathIndex = 0;
            Vector3 targetPosition;
            float movementSpeed = 5.0f; // 可配置移动速度
            obj.PlayAnimation("Move");

            // 跳过起始点（如果已经是正确位置）
            if (obj._gridPosition == path[0])
            {
                currentPathIndex++;
            }

            while (currentPathIndex < path.Count)
            {
                BlockInfo curBlock = BattleManager.Instance.GetBlockByGridLocation(obj._gridPosition);
                BlockInfo targetBlock = BattleManager.Instance.GetBlockByGridLocation(path[currentPathIndex]);
                targetPosition = targetBlock._worldlocation;

                // 移动至下一个路径点
                while (Vector3.Distance(obj.GetTransform().position, targetPosition) > 0.01f)
                {
                    obj.GetTransform().position = Vector3.MoveTowards(
                        obj.GetTransform().position,
                        targetPosition,
                        movementSpeed * Time.deltaTime
                    );
                    yield return null;
                }

                // 确保精确到达
                obj.UpdatePawnBlock(targetBlock, curBlock);
                BattleManager.Instance.ModifyEnergy(-1);

                // 更新地块事件 不期望在这里实现，再定夺
                //BlockInfo currentBlock = BattleManager.Instance.GetBlockByGridLocation(path[currentPathIndex]);
                //currentBlock.OnStepOn();

                currentPathIndex++;

                // 每个路径点之间的间隔（可选）
                yield return new WaitForSeconds(0.01f);
            }

            obj.PlayAnimation("Idle");
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.PLAYER_MOVE_FINISH, obj);
            currentMoveCoroutine = null;
        }
    }

    public class AIMoveComp : MonoBehaviour
    {
        float movementSpeed = 5.0f; // 可配置移动速度
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
            // 遍历所有怪物执行行动
            foreach (MapPawn monster in BattleManager.Instance.enemyPawns)
            {
                if (monster == null) continue;
                if (BattleManager.Instance.CanAIAttack(monster))
                {
                    // 触发攻击动画
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

                        // 移动至下一个路径点
                        while (Vector3.Distance(monster.GetTransform().position, targetPosition) > 0.01f)
                        {
                            monster.GetTransform().position = Vector3.MoveTowards(
                                monster.GetTransform().position,
                                targetPosition,
                                movementSpeed * Time.deltaTime
                            );
                            yield return null;
                        }

                        // 确保精确到达
                        monster.UpdatePawnBlock(targetBlock, curBlock);
                        currentPathIndex++;
                        monster.PlayAnimation("Idle");

                        // 每个路径点之间的间隔（可选）
                        yield return new WaitForSeconds(0.01f);
                    }
                }
                yield return new WaitForSeconds(0.5f); // 行动间隔
            }
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.AI_TURN_FINISH);
        }

        private IEnumerator HandleMonsterAttack(MapPawn enemy)
        {
            // 触发攻击动画
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
        /// 先修改到animation，time时间后修改到afterAnim同时触发通知，类似攻击动画播完调回原来动画，同时发出通知
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
