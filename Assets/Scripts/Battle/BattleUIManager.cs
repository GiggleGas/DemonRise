using Microsoft.Unity.VisualStudio.Editor;
using ppCore.Manager;
using Stateless;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;


namespace PDR
{
    public enum DiceStateChange
    {
        Restart,
        Change,
        GetResult
    }

    public enum BattleStage
    {
        WaitingForAction,
        Rolling,
        UpdatingMove,
        UpdateBattle,
    }

    public enum TeamType
    { 
        Friend,
        Enemy
    }

    public class MoveComp : MonoBehaviour
    {
        public void RegisterEvents()
        {
            EventMgr.Instance.Register<MapPawn, List<Vector2Int>>(EventType.EVENT_BATTLE_UI, SubEventType.MOVE_GO, TryMoveGO);
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

            // ������ʼ�㣨����Ѿ�����ȷλ�ã�
            if ((Vector2)obj.GetTransform().position == BattleManager.Instance.GetBlockWorldLocationByGridLocation(path[0]))
            {
                currentPathIndex++;
            }

            while (currentPathIndex < path.Count)
            {
                targetPosition = BattleManager.Instance.GetBlockWorldLocationByGridLocation(path[currentPathIndex]);

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
                obj.UpdatePawnLocation(targetPosition, path[currentPathIndex]);
                BattleManager.Instance.ModifyEnergy(-1);

                // ���µؿ��¼� ������������ʵ�֣��ٶ���
                //BlockInfo currentBlock = BattleManager.Instance.GetBlockByGridLocation(path[currentPathIndex]);
                //currentBlock.OnStepOn();

                currentPathIndex++;

                // ÿ��·����֮��ļ������ѡ��
                yield return new WaitForSeconds(0.1f);
            }

            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.MOVE_FINISH, obj);
            currentMoveCoroutine = null;
        }
    }

    /// <summary>
    /// ��Ȼ��UIManager���ɵĴ����battlemgr�Ļbattlemanager������ѡ�غͽ��������߼���
    /// </summary>
    public partial class BattleManager : ppCore.Common.Singleton<BattleManager>, IManager
    {
        // ���巽���ϡ��ҡ��¡����ķ����ƶ���
        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(0, 1),   // ��
            new Vector2Int(1, 0),   // ��
            new Vector2Int(0, -1),  // ��
            new Vector2Int(-1, 0)   // ��
        };

        // Config
        public Sprite[] diceSprites; // �洢����ͼƬ������

        private GameObject viewRoot;
        private GameObject eventGoRoot;
        private GameObject pawnGoRoot;

        private GameObject blockGoTemplete;
        private GameObject playerGoTemplete;
        private GameObject enemyGoTemplete;

        // BattleState
        public BattleStage _battleStage; // ��ǰս��״̬

        // TileMap
        private Tilemap _tilemap; // ��ͼtilemap component
        private BoundsInt _bounds;
        public BlockInfo[,] gridData; // ��ά����洢����
        private List<Vector2Int> walkableBlocks = new List<Vector2Int>(); // ���ƶ����򻺴�
        private List<Vector2Int> currentShowPath = new List<Vector2Int>(); // ��ǰΪ��ʾUI�ĸ���
        private List<EnemyPawn> enemyPawns; // ��ͼ�����ез���Ϣ
        public PlayerPawn _playerPawn; // ���Pawn

        // Dice
        public float rollDuration = 0.5f; // ���ӹ����ĳ���ʱ��
        public float rollInterval = 0.02f; // ����ͼƬ�л���ʱ����
        public float rollEnd = 0.0f; // ��������ʱ��
        public float lastRollTime = 0.0f; // �ϴ��л�ʱ��

        // UI
        public int energy = 0;

        public void OnAwake()
        {
            // RegisterViews
            RegisterViews();

            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.ROLL_THE_DICE, BeginRolling);
            // EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.GAMBLING_VIEW_FINISH_LOAD, OnGamblingFinishLoad);
            EventMgr.Instance.Register<Vector2Int>(EventType.EVENT_BATTLE_UI, SubEventType.DRAW_ROAD, OnDrawRoad);
            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.CLEAR_ROAD, OnClearRoad);
            EventMgr.Instance.Register<Vector2Int>(EventType.EVENT_BATTLE_UI, SubEventType.BLOCK_MOUSE_DOWN, OnClickBlock);
            EventMgr.Instance.Register<MapPawn>(EventType.EVENT_BATTLE_UI, SubEventType.MOVE_FINISH, OnPawnStopMove);

            diceSprites = new Sprite[6];
            for (int i = 0; i < 6; i++)
            {
                diceSprites[i] = Resources.Load<Sprite>($"Pics/diceRed{i + 1}");
            }

            eventGoRoot = GameObject.Find("eventGoRoot").gameObject;
            pawnGoRoot = GameObject.Find("pawnGoRoot").gameObject;
            viewRoot = GameObject.Find("game").gameObject;

            enemyGoTemplete = viewRoot.GetComponent<GameEntry>().enemyGoPrefab;
            blockGoTemplete = viewRoot.GetComponent<GameEntry>().blockUIPrefab;
            playerGoTemplete = viewRoot.GetComponent<GameEntry>().playerGoPrefab;
            MoveComp moveComp = viewRoot.AddComponent<MoveComp>();
            moveComp.RegisterEvents();

            _battleStage = BattleStage.WaitingForAction;
            InitMap();
            InitPlayer();
            InitUI();
        }

        public void OnUpdate()
        {
            HandlePlayerInput();

            if (_battleStage == BattleStage.Rolling)
            {
                RollTheDice();
            }
        }

        #region ------------------------------------------------------------------ GameStage ս��״̬ ------------------------------------------------------------------

        /// <summary>
        /// �л�ս��״̬
        /// </summary>
        /// <param name="battleStage"></param>
        public void SetBattleStage(BattleStage battleStage)
        {
            _battleStage = battleStage;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_GAME_STAGE, battleStage);
        }
        #endregion

        #region ------------------------------------------------------------------TileMap ������ͼ�Ͳ��ֵ�ͼ�����ж�------------------------------------------------------------------
        /// <summary>
        /// ��ʼ����ͼ
        /// </summary>
        protected void InitMap()
        {
            _tilemap = GameObject.Find("walkableFloor").GetComponent<Tilemap>();
            _bounds = _tilemap.cellBounds;

            InitBlockData();
            //RefreshBlockEvents(0); // todo ��������� ��ʼ����ͼ��ǰ�����Actor
        }

        /// <summary>
        /// ��ʼ��BlockUI
        /// </summary>
        private void InitBlockData()
        {
            TileBase[] allTiles = _tilemap.GetTilesBlock(_bounds);

            int width = _bounds.size.x;
            int height = _bounds.size.y;
            gridData = new BlockInfo[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileBase tile = allTiles[x + y * width];
                    if (tile != null)
                    {
                        Vector3Int cellPos = new Vector3Int(
                            _bounds.xMin + x,
                            _bounds.yMin + y,
                            0
                        );
                        Vector2 worldPos = _tilemap.GetCellCenterWorld(cellPos);
                        Vector2Int gridPos = new Vector2Int(x, y);
                        BlockInfo block = new BlockInfo(gridPos, worldPos, BlockType.Walkable);
                        GameObject blockUIGo = GameObject.Instantiate(blockGoTemplete, eventGoRoot.transform);
                        block.blockUI = blockUIGo.AddComponent<BlockUI>();
                        block.blockUI.transform.position = worldPos;
                        block.blockUI.SetGridLocation(gridPos);
                        gridData[x, y] = block;
                        walkableBlocks.Add(gridPos);
                    }
                }
            }
        }

        /// <summary>
        /// ͨ��gridLocation��ȡblock��Ϣ
        /// </summary>
        /// <param name="gridLoc"></param>
        /// <returns></returns>
        public BlockInfo GetBlockByGridLocation(Vector2Int gridLoc)
        {
            return gridData[gridLoc.x, gridLoc.y];
        }

        /// <summary>
        /// ͨ��grid�����ȡworld������Ϣ
        /// </summary>
        /// <param name="gridLoc"></param>
        /// <returns></returns>
        public Vector2 GetBlockWorldLocationByGridLocation(Vector2Int gridLoc)
        {
            return gridData[gridLoc.x, gridLoc.y]._worldlocation;
        }

        /// <summary>
        /// ͨ��grid�����ȡ��ͼPawn��Ϣ
        /// </summary>
        /// <param name="gridLoc"></param>
        /// <returns></returns>
        public MapPawn GetPawnByGridLocation(Vector2Int gridLoc)
        {
            return gridData[gridLoc.x, gridLoc.y].pawn;
        }

        /// <summary>
        /// ����Pawn��ȡBlock��Ϣ
        /// </summary>
        /// <param name="mapPawn"></param>
        /// <returns></returns>
        public BlockInfo GetPawnBlock(MapPawn mapPawn)
        {
            return gridData[mapPawn._gridPosition.x, mapPawn._gridPosition.y];
        }

        /// <summary>
        /// ˢ�µ�ͼ�¼�
        /// </summary>
        /// <param name="LevelID"></param>
        protected void RefreshBlockEvents(int LevelID)
        {
            // todo ����levelID��ȡ�ؿ�event���ã�����event��������
            enemyPawns = new List<EnemyPawn>();
            int enemyNum = 10;
            List<int> randomIndexes = GetRandomIndexes(walkableBlocks.Count, enemyNum);
            foreach (int index in randomIndexes)
            {
                if (enemyNum > 0)
                {
                    EnemyPawn enemyPawn = new EnemyPawn(walkableBlocks[index], GameObject.Instantiate(enemyGoTemplete, pawnGoRoot.transform), 100f, 10f, 15f, 100);
                    enemyPawn.UpdatePawnLocation(GetBlockWorldLocationByGridLocation(walkableBlocks[index]), walkableBlocks[index]);
                    UpdatePawnAnim(enemyPawn, "Idle");
                    enemyPawns.Add(enemyPawn);
                    enemyNum--;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// �Ӽ����л�ȡָ�������Ĳ��ظ�����±�
        /// </summary>
        /// <param name="sourceCount">ԭ���ϵ�Ԫ������</param>
        /// <param name="requestCount">��Ҫ��ȡ���������</param>
        /// <returns>����±��б� (��ֱ������ source[index] ����)</returns>
        public static List<int> GetRandomIndexes(int sourceCount, int requestCount)
        {
            List<int> indexes = new List<int>(sourceCount);
            List<int> result = new List<int>();

            // ���ɳ�ʼ�±����� 0,1,2...n-1
            for (int i = 0; i < sourceCount; i++)
            {
                indexes.Add(i);
            }

            // ����ʵ�ʿɷ��ص�����
            int safeCount = Mathf.Min(requestCount, sourceCount);

            // Fisher-Yates ϴ���㷨������ϴ���Ż���
            for (int i = 0; i < safeCount; i++)
            {
                int swapIndex = UnityEngine.Random.Range(i, indexes.Count);

                // ����λ��
                (indexes[i], indexes[swapIndex]) = (indexes[swapIndex], indexes[i]);

                // ��¼���
                result.Add(indexes[i]);
            }

            return result;
        }

        /// <summary>
        /// �ж�ĳλ���Ƿ�Ϸ�
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool IsValidPosition(Vector2Int position)
        {
            if (position.x < 0 || position.x >= gridData.GetLength(0)) return false;
            if (position.y < 0 || position.y >= gridData.GetLength(1)) return false;
            return gridData[position.x, position.y] != null &&
                   gridData[position.x, position.y]._type == BlockType.Walkable;
        }
        
        /// <summary>
        /// ��ȡ���������·��
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<Vector2Int> FindShortestPath(Vector2Int start, Vector2Int end)
        {
            // �߽���
            if (!IsValidPosition(start) || !IsValidPosition(end))
            {
                Debug.LogError("�����յ�Ƿ���");
                return new List<Vector2Int>();
            }

            int width = gridData.GetLength(0);
            int height = gridData.GetLength(1);

            // ��¼���ڵ����ڻ���·��
            Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            bool[,] visited = new bool[width, height];

            // ��ʼ�����
            queue.Enqueue(start);
            visited[start.x, start.y] = true;

            // BFS�����߼�
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                // �����յ�
                if (current == end)
                {
                    return ReconstructPath(parentMap, start, end);
                }

                // ̽���ĸ�����
                foreach (Vector2Int dir in Directions)
                {
                    Vector2Int newLoc = current + dir;

                    if (IsValidPosition(newLoc) &&
                        !visited[newLoc.x, newLoc.y])
                    {
                        Vector2Int neighbor = newLoc;
                        visited[newLoc.x, newLoc.y] = true;
                        parentMap[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // �޿���·��
            return new List<Vector2Int>();
        }

        /// <summary>
        /// ����·�� ��FindShortestPath
        /// </summary>
        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parentMap, Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int current = end;

            // ���յ���ݵ����
            while (current != start)
            {
                path.Add(current);
                current = parentMap[current];
            }
            path.Add(start);

            // ��ת�õ�����·��
            path.Reverse();
            return path;
        }

        /// <summary>
        /// ��һ������ҵ�target��·��
        /// </summary>
        /// <param name="targetLoc"></param>
        private void OnDrawRoad(Vector2Int targetLoc)
        {
            OnDrawRoad(targetLoc, _playerPawn._gridPosition);
        }

        /// <summary>
        /// ��ĳ����λ�õ�·��
        /// </summary>
        /// <param name="targetLoc"></param>
        /// <param name="sourceLoc"></param>
        private void OnDrawRoad(Vector2Int targetLoc, Vector2Int sourceLoc)
        {
            if (_battleStage == BattleStage.UpdatingMove)
            {
                return;
            }
            currentShowPath = FindShortestPath(sourceLoc, targetLoc);
            for (int i = 0; i < currentShowPath.Count; i++)
            {
                if (i == 0 || i == currentShowPath.Count - 1)
                {
                    continue;
                }
                GetBlockByGridLocation(currentShowPath[i]).blockUI.ShowPath(true);
            }
        }

        /// <summary>
        /// �ж������ؿ��Ƿ����ڣ����������ĸ�����
        /// </summary>
        public bool IsAdjacent(BlockInfo a, BlockInfo b)
        {
            // ��ֵ���
            if (a == null || b == null) return false;

            // ��������
            int deltaX = Mathf.Abs(a._gridLocation.x - b._gridLocation.x);
            int deltaY = Mathf.Abs(a._gridLocation.y - b._gridLocation.y);

            // �ж��߼������������֮��Ϊ1
            return (deltaX == 1 && deltaY == 0) ||
                   (deltaY == 1 && deltaX == 0);
        }
        #endregion

        #region ------------------------------------------------------------------ PlayerInput ------------------------------------------------------------------
        private void HandlePlayerInput()
        {
            if(ShouldHandlePlayerInput() && energy == 0)
            {
                HandleDiceInput();
            }
        }

        private bool ShouldHandlePlayerInput()
        {
            return _battleStage == BattleStage.WaitingForAction;
        }

        private void HandleDiceInput()
        {
            if (Input.GetKeyDown(KeyCode.Space)) BeginRolling();
        }

        /// <summary>
        /// �жϵ�ǰ�Ƿ��ǹ�������
        /// </summary>
        /// <param name="sourceBlock"></param>
        /// <param name="targetBlock"></param>
        /// <returns></returns>
        private bool IsAttackAction(BlockInfo sourceBlock, BlockInfo targetBlock)
        {
            if (IsAdjacent(sourceBlock, targetBlock) && NeedAttack(sourceBlock.pawn, targetBlock.pawn))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// �����ؿ����¼�����������/�ƶ�/ʩ���Ȳ�ͬ����
        /// </summary>
        /// <param name="blockGridLocation"></param>
        private void OnClickBlock(Vector2Int blockGridLocation)
        {
            if(_battleStage != BattleStage.WaitingForAction)
            {
                return;
            }

            BlockInfo playerBlock = GetPawnBlock(_playerPawn);
            BlockInfo targetBlock = GetBlockByGridLocation(blockGridLocation);
            if(IsAttackAction(targetBlock, playerBlock))
            {
                TryAttack(_playerPawn, targetBlock);
            }
            else
            {
                TryMovePawn(_playerPawn, targetBlock);
            }
        }
        #endregion

        #region ------------------------------------------------------------------ PlayerState ------------------------------------------------------------------
        private void InitPlayer()
        {
            if (walkableBlocks.Count == 0)
            {
                Debug.LogError("No valid spawn positions!");
                return;
            }

            // 42���д��
            _playerPawn = new PlayerPawn(walkableBlocks[42], GameObject.Instantiate(playerGoTemplete, pawnGoRoot.transform), 100f, 10f, 15f, 100);
            _playerPawn.UpdatePawnLocation(GetBlockWorldLocationByGridLocation(walkableBlocks[42]), walkableBlocks[42]);
            UpdatePawnAnim(_playerPawn, "Idle");
        }

        #endregion

        #region ------------------------------------------------------------------PawnControl ��ͼPawn����------------------------------------------------------------------
        /// <summary>
        /// sourcePawn����targetBlockλ�õĵ�λ
        /// </summary>
        /// <param name="sourcePawn"></param>
        /// <param name="targetBlock"></param>
        private void TryAttack(MapPawn sourcePawn, BlockInfo targetBlock)
        {

        }

        /// <summary>
        /// sourcePawn�ߵ�targetBlock
        /// </summary>
        /// <param name="sourcePawn"></param>
        /// <param name="targetBlock"></param>
        private void TryMovePawn(MapPawn sourcePawn, BlockInfo targetBlock)
        {
            BlockInfo sourceBlock = GetPawnBlock(sourcePawn);
            List<Vector2Int> pathList = FindShortestPath(sourceBlock._gridLocation, targetBlock._gridLocation);
            if (pathList.Count == 0 || pathList.Count - 1 > energy)
            {
                return;
            }
            OnClearRoad();

            // �ߵ�ս��Ϊֹ
            List<Vector2Int> safeBlocks = new List<Vector2Int>();
            foreach (Vector2Int curBlock in pathList)
            {
                if(!NeedAttack(sourcePawn, GetPawnByGridLocation(curBlock)))
                {
                    safeBlocks.Add(curBlock);
                }
            }

            SetBattleStage(BattleStage.UpdatingMove);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.MOVE_GO, (MapPawn)_playerPawn, safeBlocks);

        }

        private void OnPawnStopMove(MapPawn pawn)
        {
            UpdatePawnAnim(pawn, "Idle");
            BlockInfo currentBlock = GetBlockByGridLocation(pawn._gridPosition);
            currentBlock.OnStepOn();
            SetBattleStage(BattleStage.WaitingForAction);
        }

        /// <summary>
        /// �ж�����pawn�Ƿ���Ҫ��ս
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private bool NeedAttack(MapPawn source, MapPawn target)
        {
            return false;
        }

        private void UpdatePawnAnim(MapPawn mapPawn, string animation, float crossFade = 0.2f)
        {
            mapPawn.PlayAnimation(animation, crossFade);
        }
        #endregion

        #region ------------------------------------------------------------------ UI ------------------------------------------------------------------
        private void RegisterViews()
        {
            ViewManager.Instance.Register(ViewType.BattleMainView, new ViewInfo()
            {
                viewName = "BattleMainView",
                parentTransform = ViewManager.Instance.canvasTransform,
                order = 0,
            });

            ViewManager.Instance.Register(ViewType.GamblingView, new ViewInfo()
            {
                viewName = "GamblingView",
                parentTransform = ViewManager.Instance.canvasTransform,
                order = 1,
            });
        }


        protected void InitUI()
        {
            OpenBattleMainView();
            UpdateEnergy();
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_PLAYER_PAWN, _playerPawn);
        }

        private void OpenBattleMainView()
        {
            ViewManager.Instance.Open(ViewType.BattleMainView);
        }

        private void UpdateEnergy(int newEnergy = 0)
        {
            energy = newEnergy;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_ENERGY, energy);
        }

        private void OnClearRoad()
        {
            foreach (Vector2Int gridLoc in walkableBlocks)
            {
                GetBlockByGridLocation(gridLoc).blockUI.ShowPath(false);
            }
            currentShowPath.Clear();
        }
        #endregion

        #region ------------------------------------------------------------------ Dice ------------------------------------------------------------------

        private void UpdateDiceState(DiceStateChange diceStateChange)
        {
            int randomIndex = UnityEngine.Random.Range(0, 6);
            switch (diceStateChange)
            {
                case DiceStateChange.Restart:
                    ResetDiceState();
                    EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_STATE, true);
                    break;
                case DiceStateChange.GetResult:
                    ResetDiceState();
                    EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_SPRITE, randomIndex);
                    UpdateEnergy(randomIndex + 1);
                    break;
                case DiceStateChange.Change:
                    EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_SPRITE, randomIndex);
                    break;
                default:
                    break;
            }
        }

        private void RollTheDice()
        {
            if (Time.time > rollEnd)
            {
                UpdateDiceState(DiceStateChange.GetResult);
            }
            else if (Time.time - lastRollTime > rollInterval)
            {
                UpdateDiceState(DiceStateChange.Change);
                lastRollTime = Time.time;
            }
        }

        private void BeginRolling()
        {
            SetBattleStage(BattleStage.Rolling);
            rollEnd = Time.time + rollDuration;
            lastRollTime = Time.time;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_STATE, false);
        }

        private void ResetDiceState()
        {
            rollEnd = 0.0f;
            lastRollTime = 0.0f;
            SetBattleStage(BattleStage.WaitingForAction);
        }


        public void ModifyEnergy(int value)
        {
            UpdateEnergy(energy + value);
        }
        #endregion

        /* �ϰ�������
        #region Gambling
        protected void OnGamblingFinishLoad()
        {
            OnEnterGambling(_playerPawn._blockInfo);
        }

        public void OnEnterGambling(BlockInfo blockInfo)
        {
            InitPlayerData();
            InitEnemyData();
        }

        private void OpenGamblingView()
        {
            ViewManager.Instance.Open(ViewType.GamblingView);
        }

        private void InitPlayerData()
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_GAMBLING_PLAYER_VIEW, _playerPawn);
        }

        private void InitEnemyData()
        {

        }
        #endregion
        */
    }
}
