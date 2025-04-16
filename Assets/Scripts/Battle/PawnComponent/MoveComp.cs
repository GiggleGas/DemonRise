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
            EventMgr.Instance.Register<MapPawn, List<Vector2Int>>(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_MOVE, TryMovePawn);
        }

        private Coroutine currentMoveCoroutine; // 当前运行的移动协程
        public void TryMovePawn(MapPawn pawn, List<Vector2Int> path)
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

            // 跳过起始点（如果已经是正确位置）
            if (obj._gridLocation == path[0])
            {
                currentPathIndex++;
            }

            while (currentPathIndex < path.Count)
            {
                BlockInfo curBlock = BattleManager.Instance.GetBlockByGridLocation(obj._gridLocation);
                BlockInfo targetBlock = BattleManager.Instance.GetBlockByGridLocation(path[currentPathIndex]);
                targetPosition = targetBlock._worldlocation;
                obj.UpdateRot(targetPosition.x - curBlock._worldlocation.x);

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
                BattleManager.Instance.ModifyEnergy(-1, obj);

                // 更新地块事件 不期望在这里实现，再定夺
                BlockInfo currentBlock = BattleManager.Instance.GetBlockByGridLocation(path[currentPathIndex]);
                currentBlock.OnStepOn(obj);

                currentPathIndex++;

                // 每个路径点之间的间隔（可选）
                yield return new WaitForSeconds(0.01f);
            }

            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_MOVE_FINISH, obj);
            currentMoveCoroutine = null;
        }
    }
}
