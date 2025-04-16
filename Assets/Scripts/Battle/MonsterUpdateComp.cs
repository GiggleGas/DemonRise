using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;
using static Unity.VisualScripting.Member;

namespace PDR
{
    /// <summary>
    /// 负责整个怪物逻辑的更新
    /// </summary>
    public partial class BattleManager
    {
        private GameObject enemyGoTemplete;
        public List<EnemyPawn> enemyPawns = new List<EnemyPawn>(); // 所有敌人
        private Queue<EnemyPawn> enemyQueue = new Queue<EnemyPawn>();
        private EnemyPawn _currentEnemy;

        /// <summary>
        /// 开始敌方回合
        /// </summary>
        private void StartMonsterTurn()
        {
            SetBattleState(BattleState.MonsterTurn, BattleSubState.UpdatingMove);
            for (int i = 0; i < enemyPawns.Count; ++i)
            {
                if (enemyPawns[i].IsValid == false)
                {
                    enemyPawns[i].DestroySelf();
                    enemyPawns.RemoveAt(i);
                    i--;
                }
            }
            enemyQueue.Clear();
            foreach (EnemyPawn enemyPawn in enemyPawns)
            {
                enemyQueue.Enqueue(enemyPawn);
            }

            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.MONSTER_TURN_START);
            ProcessNextAI();
        }

        /// <summary>
        /// 结束敌方回合
        /// </summary>
        private void EndMonsterTurn()
        {
            CheckPlayerStatus();
            StartNewRound();
        }

        /// <summary>
        /// 处理下一个AI
        /// </summary>
        private void ProcessNextAI()
        {
            if(enemyQueue.Count ==  0)
            {
                EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.MONSTER_TURN_FINISH);
                EndMonsterTurn();
                return;
            }
            _currentEnemy = enemyQueue.Dequeue();
            _currentEnemy.BeginUpdate();
            BeginAIAction();
        }

        /// <summary>
        /// AI行为决策
        /// </summary>
        private void BeginAIAction()
        {
            //// 有逻辑问题，走到思路没有敌人会走不出去
            //// 有逻辑问题，走到思路没有敌人会走不出去
            //// 有逻辑问题，走到思路没有敌人会走不出去
            if(_currentEnemy._energy <= 0)
            {
                EndEnemyAction();
            }
            else if (CanAIAttack(_currentEnemy))
            {
                EnemyAttackAction(_currentEnemy, _playerPawn);
            }
            else
            {
                EnemyMoveAction();
            }
        }

        /// <summary>
        /// AI是否可以攻击
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public bool CanAIAttack(MapPawn enemy)
        {
            BlockInfo enemyBlock = GetBlockByGridLocation(enemy._gridLocation);
            return IsAdjacent(enemyBlock, GetBlockByGridLocation(_playerPawn._gridLocation));
        }

        /// <summary>
        /// AI攻击行为
        /// </summary>
        /// <param name="enemyPawn"></param>
        private void EnemyAttackAction(EnemyPawn enemyPawn, MapPawn target)
        {
            enemyPawn._energy -= 1;
            if(_attackList.ContainsKey(enemyPawn))
            {
                _attackList[enemyPawn].attackTargets.Clear();
                _attackList[enemyPawn].attackTargets.Add(target);
            }
            else
            {
                AttackStruct attackStruct = new AttackStruct();
                attackStruct.attackTargets = new List<MapPawn> { target };
                _attackList.Add(enemyPawn, attackStruct);
            }
            enemyPawn.PlayOnceAnimation("Attack", 0.8f);
        }

        /// <summary>
        /// enmey攻击结束
        /// </summary>
        /// <param name="mapPawn"></param>
        private void OnEnemyAttackFinish(MapPawn mapPawn)
        {
            if(mapPawn == null || mapPawn != _currentEnemy )
            {
                return;
            }

            // 实际伤害计算
            if(_attackList.ContainsKey(_currentEnemy))
            {
                AttackStruct attackStruct = _attackList[mapPawn];
                foreach (var target in attackStruct.attackTargets)
                {
                    target.TakeDamage(_currentEnemy, _currentEnemy.GetAttackValue());
                }
                mapPawn.PlayContinuousAnimation("idle");
            }
            BeginAIAction(); // 继续决策
        }

        /// <summary>
        /// enemy移動動作
        /// </summary>
        private void EnemyMoveAction()
        {
            BlockInfo enemyBlock = GetBlockByGridLocation(_currentEnemy._gridLocation);
            List<Vector2Int> movePath = FindShortestPath(enemyBlock._gridLocation, _playerPawn._gridLocation);
            if (movePath.Count <= 0)
            {
                EndEnemyAction();
                return;
            }

            List<Vector2Int> safeBlocks = new List<Vector2Int>();
            for (int i = 1; i < movePath.Count - 1; i++)
            {
                Vector2Int curBlock = movePath[i];
                if (_currentEnemy._energy > 0)
                {
                    safeBlocks.Add(curBlock);
                    _currentEnemy._energy--;
                }
            }

            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_MOVE, (MapPawn)_currentEnemy, safeBlocks);
        }

        /// <summary>
        /// enemy移动结束
        /// </summary>
        public void OnEnemyMoveFinish()
        {
            BeginAIAction(); // 继续决策
        }

        private void EndEnemyAction()
        {
            _currentEnemy = null;
            ProcessNextAI();
        }

        /*
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
                    BlockInfo enemyBlock = BattleManager.Instance.GetBlockByGridLocation(monster._gridLocation);
                    List<Vector2Int> movePath = BattleManager.Instance.FindShortestPath(enemyBlock._gridLocation, BattleManager.Instance._playerPawn._gridLocation);
                    if (movePath.Count <= 0)
                    {
                        continue;
                    }
                    monster.PlayAnimation("Move");
                    List<Vector2Int> savePath = movePath.GetRange(1, monster._moveRange);
                    int currentPathIndex = 0;
                    Vector3 targetPosition;
                    while (currentPathIndex < savePath.Count)
                    {
                        BlockInfo curBlock = BattleManager.Instance.GetBlockByGridLocation(monster._gridLocation);
                        BlockInfo targetBlock = BattleManager.Instance.GetBlockByGridLocation(savePath[currentPathIndex]);
                        targetPosition = targetBlock._worldlocation;
                        monster.UpdateRot(targetPosition.x - curBlock._worldlocation.x);
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
        */
    }
}
