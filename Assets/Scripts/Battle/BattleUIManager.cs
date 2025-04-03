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

            // 跳过起始点（如果已经是正确位置）
            if ((Vector2)obj.GetTransform().position == BattleManager.Instance.GetBlockWorldLocationByGridLocation(path[0]))
            {
                currentPathIndex++;
            }

            while (currentPathIndex < path.Count)
            {
                targetPosition = BattleManager.Instance.GetBlockWorldLocationByGridLocation(path[currentPathIndex]);

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
                obj.UpdatePawnLocation(targetPosition, path[currentPathIndex]);
                BattleManager.Instance.ModifyEnergy(-1);

                // 更新地块事件 不期望在这里实现，再定夺
                //BlockInfo currentBlock = BattleManager.Instance.GetBlockByGridLocation(path[currentPathIndex]);
                //currentBlock.OnStepOn();

                currentPathIndex++;

                // 每个路径点之间的间隔（可选）
                yield return new WaitForSeconds(0.1f);
            }

            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.MOVE_FINISH, obj);
            currentMoveCoroutine = null;
        }
    }

    /// <summary>
    /// 虽然叫UIManager但干的大概是battlemgr的活，battlemanager用来搞选关和进关这种逻辑吧
    /// </summary>
    public partial class BattleManager : ppCore.Common.Singleton<BattleManager>, IManager
    {
        // 定义方向：上、右、下、左（四方向移动）
        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(0, 1),   // 上
            new Vector2Int(1, 0),   // 右
            new Vector2Int(0, -1),  // 下
            new Vector2Int(-1, 0)   // 左
        };

        // Config
        public Sprite[] diceSprites; // 存储骰子图片的数组

        private GameObject viewRoot;
        private GameObject eventGoRoot;
        private GameObject pawnGoRoot;

        private GameObject blockGoTemplete;
        private GameObject playerGoTemplete;
        private GameObject enemyGoTemplete;

        // BattleState
        public BattleStage _battleStage; // 当前战斗状态

        // TileMap
        private Tilemap _tilemap; // 地图tilemap component
        private BoundsInt _bounds;
        public BlockInfo[,] gridData; // 二维数组存储数据
        private List<Vector2Int> walkableBlocks = new List<Vector2Int>(); // 可移动区域缓存
        private List<Vector2Int> currentShowPath = new List<Vector2Int>(); // 当前为显示UI的格子
        private List<EnemyPawn> enemyPawns; // 地图上所有敌方信息
        public PlayerPawn _playerPawn; // 玩家Pawn

        // Dice
        public float rollDuration = 0.5f; // 骰子滚动的持续时间
        public float rollInterval = 0.02f; // 骰子图片切换的时间间隔
        public float rollEnd = 0.0f; // 骰子最终时间
        public float lastRollTime = 0.0f; // 上次切换时间

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

        #region ------------------------------------------------------------------ GameStage 战斗状态 ------------------------------------------------------------------

        /// <summary>
        /// 切换战斗状态
        /// </summary>
        /// <param name="battleStage"></param>
        public void SetBattleStage(BattleStage battleStage)
        {
            _battleStage = battleStage;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_GAME_STAGE, battleStage);
        }
        #endregion

        #region ------------------------------------------------------------------TileMap 包括地图和部分地图内容判断------------------------------------------------------------------
        /// <summary>
        /// 初始化地图
        /// </summary>
        protected void InitMap()
        {
            _tilemap = GameObject.Find("walkableFloor").GetComponent<Tilemap>();
            _bounds = _tilemap.cellBounds;

            InitBlockData();
            //RefreshBlockEvents(0); // todo 读表查配置 初始化地图当前怪物和Actor
        }

        /// <summary>
        /// 初始化BlockUI
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
        /// 通过gridLocation获取block信息
        /// </summary>
        /// <param name="gridLoc"></param>
        /// <returns></returns>
        public BlockInfo GetBlockByGridLocation(Vector2Int gridLoc)
        {
            return gridData[gridLoc.x, gridLoc.y];
        }

        /// <summary>
        /// 通过grid坐标获取world坐标信息
        /// </summary>
        /// <param name="gridLoc"></param>
        /// <returns></returns>
        public Vector2 GetBlockWorldLocationByGridLocation(Vector2Int gridLoc)
        {
            return gridData[gridLoc.x, gridLoc.y]._worldlocation;
        }

        /// <summary>
        /// 通过grid坐标获取地图Pawn信息
        /// </summary>
        /// <param name="gridLoc"></param>
        /// <returns></returns>
        public MapPawn GetPawnByGridLocation(Vector2Int gridLoc)
        {
            return gridData[gridLoc.x, gridLoc.y].pawn;
        }

        /// <summary>
        /// 根据Pawn获取Block信息
        /// </summary>
        /// <param name="mapPawn"></param>
        /// <returns></returns>
        public BlockInfo GetPawnBlock(MapPawn mapPawn)
        {
            return gridData[mapPawn._gridPosition.x, mapPawn._gridPosition.y];
        }

        /// <summary>
        /// 刷新地图事件
        /// </summary>
        /// <param name="LevelID"></param>
        protected void RefreshBlockEvents(int LevelID)
        {
            // todo 根据levelID获取地块event配置，根据event配置生成
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
        /// 从集合中获取指定数量的不重复随机下标
        /// </summary>
        /// <param name="sourceCount">原集合的元素总数</param>
        /// <param name="requestCount">需要获取的随机数量</param>
        /// <returns>随机下标列表 (可直接用于 source[index] 操作)</returns>
        public static List<int> GetRandomIndexes(int sourceCount, int requestCount)
        {
            List<int> indexes = new List<int>(sourceCount);
            List<int> result = new List<int>();

            // 生成初始下标序列 0,1,2...n-1
            for (int i = 0; i < sourceCount; i++)
            {
                indexes.Add(i);
            }

            // 计算实际可返回的数量
            int safeCount = Mathf.Min(requestCount, sourceCount);

            // Fisher-Yates 洗牌算法（部分洗牌优化）
            for (int i = 0; i < safeCount; i++)
            {
                int swapIndex = UnityEngine.Random.Range(i, indexes.Count);

                // 交换位置
                (indexes[i], indexes[swapIndex]) = (indexes[swapIndex], indexes[i]);

                // 记录结果
                result.Add(indexes[i]);
            }

            return result;
        }

        /// <summary>
        /// 判断某位置是否合法
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
        /// 获取两点间的最短路径
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<Vector2Int> FindShortestPath(Vector2Int start, Vector2Int end)
        {
            // 边界检查
            if (!IsValidPosition(start) || !IsValidPosition(end))
            {
                Debug.LogError("起点或终点非法！");
                return new List<Vector2Int>();
            }

            int width = gridData.GetLength(0);
            int height = gridData.GetLength(1);

            // 记录父节点用于回溯路径
            Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            bool[,] visited = new bool[width, height];

            // 初始化起点
            queue.Enqueue(start);
            visited[start.x, start.y] = true;

            // BFS核心逻辑
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                // 到达终点
                if (current == end)
                {
                    return ReconstructPath(parentMap, start, end);
                }

                // 探索四个方向
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

            // 无可用路径
            return new List<Vector2Int>();
        }

        /// <summary>
        /// 回溯路径 见FindShortestPath
        /// </summary>
        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parentMap, Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int current = end;

            // 从终点回溯到起点
            while (current != start)
            {
                path.Add(current);
                current = parentMap[current];
            }
            path.Add(start);

            // 反转得到正序路径
            path.Reverse();
            return path;
        }

        /// <summary>
        /// 画一个从玩家到target的路径
        /// </summary>
        /// <param name="targetLoc"></param>
        private void OnDrawRoad(Vector2Int targetLoc)
        {
            OnDrawRoad(targetLoc, _playerPawn._gridPosition);
        }

        /// <summary>
        /// 画某两个位置的路径
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
        /// 判断两个地块是否相邻（上下左右四个方向）
        /// </summary>
        public bool IsAdjacent(BlockInfo a, BlockInfo b)
        {
            // 空值检查
            if (a == null || b == null) return false;

            // 坐标差计算
            int deltaX = Mathf.Abs(a._gridLocation.x - b._gridLocation.x);
            int deltaY = Mathf.Abs(a._gridLocation.y - b._gridLocation.y);

            // 判断逻辑：横纵坐标差之和为1
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
        /// 判断当前是否是攻击动作
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
        /// 监听地块点击事件，触发攻击/移动/施法等不同操作
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

            // 42随便写的
            _playerPawn = new PlayerPawn(walkableBlocks[42], GameObject.Instantiate(playerGoTemplete, pawnGoRoot.transform), 100f, 10f, 15f, 100);
            _playerPawn.UpdatePawnLocation(GetBlockWorldLocationByGridLocation(walkableBlocks[42]), walkableBlocks[42]);
            UpdatePawnAnim(_playerPawn, "Idle");
        }

        #endregion

        #region ------------------------------------------------------------------PawnControl 地图Pawn更新------------------------------------------------------------------
        /// <summary>
        /// sourcePawn攻击targetBlock位置的单位
        /// </summary>
        /// <param name="sourcePawn"></param>
        /// <param name="targetBlock"></param>
        private void TryAttack(MapPawn sourcePawn, BlockInfo targetBlock)
        {

        }

        /// <summary>
        /// sourcePawn走到targetBlock
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

            // 走到战斗为止
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
        /// 判断两个pawn是否需要作战
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

        /* 废案，，，
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
