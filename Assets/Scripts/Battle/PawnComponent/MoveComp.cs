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

        private Coroutine currentMoveCoroutine; // ��ǰ���е��ƶ�Э��
        public void TryMovePawn(MapPawn pawn, List<Vector2Int> path)
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

            // ������ʼ�㣨����Ѿ�����ȷλ�ã�
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
                BattleManager.Instance.ModifyEnergy(-1, obj);

                // ���µؿ��¼� ������������ʵ�֣��ٶ���
                BlockInfo currentBlock = BattleManager.Instance.GetBlockByGridLocation(path[currentPathIndex]);
                currentBlock.OnStepOn(obj);

                currentPathIndex++;

                // ÿ��·����֮��ļ������ѡ��
                yield return new WaitForSeconds(0.01f);
            }

            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_MOVE_FINISH, obj);
            currentMoveCoroutine = null;
        }
    }
}
