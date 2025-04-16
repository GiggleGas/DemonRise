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
    /// �������������߼��ĸ���
    /// </summary>
    public partial class BattleManager
    {
        private GameObject enemyGoTemplete;
        public List<EnemyPawn> enemyPawns = new List<EnemyPawn>(); // ���е���
        private Queue<EnemyPawn> enemyQueue = new Queue<EnemyPawn>();
        private EnemyPawn _currentEnemy;

        /// <summary>
        /// ��ʼ�з��غ�
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
        /// �����з��غ�
        /// </summary>
        private void EndMonsterTurn()
        {
            CheckPlayerStatus();
            StartNewRound();
        }

        /// <summary>
        /// ������һ��AI
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
        /// AI��Ϊ����
        /// </summary>
        private void BeginAIAction()
        {
            //// ���߼����⣬�ߵ�˼·û�е��˻��߲���ȥ
            //// ���߼����⣬�ߵ�˼·û�е��˻��߲���ȥ
            //// ���߼����⣬�ߵ�˼·û�е��˻��߲���ȥ
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
        /// AI�Ƿ���Թ���
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public bool CanAIAttack(MapPawn enemy)
        {
            BlockInfo enemyBlock = GetBlockByGridLocation(enemy._gridLocation);
            return IsAdjacent(enemyBlock, GetBlockByGridLocation(_playerPawn._gridLocation));
        }

        /// <summary>
        /// AI������Ϊ
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
        /// enmey��������
        /// </summary>
        /// <param name="mapPawn"></param>
        private void OnEnemyAttackFinish(MapPawn mapPawn)
        {
            if(mapPawn == null || mapPawn != _currentEnemy )
            {
                return;
            }

            // ʵ���˺�����
            if(_attackList.ContainsKey(_currentEnemy))
            {
                AttackStruct attackStruct = _attackList[mapPawn];
                foreach (var target in attackStruct.attackTargets)
                {
                    target.TakeDamage(_currentEnemy, _currentEnemy.GetAttackValue());
                }
                mapPawn.PlayContinuousAnimation("idle");
            }
            BeginAIAction(); // ��������
        }

        /// <summary>
        /// enemy�Ƅӄ���
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
        /// enemy�ƶ�����
        /// </summary>
        public void OnEnemyMoveFinish()
        {
            BeginAIAction(); // ��������
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
        */
    }
}
