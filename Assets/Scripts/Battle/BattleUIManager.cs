using Microsoft.Unity.VisualStudio.Editor;
using ppCore.Manager;
using Stateless;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;


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
        Gambling
    }

    public class MoveComp : MonoBehaviour
    {
        public void RegisterEvents()
        {
            EventMgr.Instance.Register<MapPawn, BlockInfo, BlockInfo, List<BlockInfo>>(EventType.EVENT_BATTLE_UI, SubEventType.MOVE_GO, TryMoveGO);
        }

        private Coroutine currentMoveCoroutine; // 当前运行的移动协程
        public void TryMoveGO(MapPawn pawn, BlockInfo startBlock, BlockInfo targetBlock, List<BlockInfo> path)
        {
            // 开始移动协程
            currentMoveCoroutine = StartCoroutine(MoveAlongPath(pawn, path));
        }


        /// <summary>
        /// 沿路径移动的协程
        /// </summary>
        /// 
        private IEnumerator MoveAlongPath(MapPawn obj, List<BlockInfo> path)
        {
            int currentPathIndex = 0;
            Vector3 targetPosition;
            float movementSpeed = 5.0f; // 可配置移动速度

            // 跳过起始点（如果已经是正确位置）
            if ((Vector2)obj.GetTransform().position == path[0].location)
            {
                currentPathIndex++;
            }

            while (currentPathIndex < path.Count)
            {
                targetPosition = path[currentPathIndex].location;

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
                obj.GetTransform().position = targetPosition;
                obj.UpateBlockInfo(path[currentPathIndex]);
                BattleUIManager.Instance.ModifyEnergy(-1);

                // 更新地块事件
                BlockInfo currentBlock = path[currentPathIndex];
                currentBlock.OnStepOn();

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
    [Manager(ManagerPriority.Delay)]
    public class BattleUIManager : ppCore.Common.Singleton<BattleUIManager>, IManager
    {
        // 定义方向：上、右、下、左（四方向移动）
        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(0, 1),   // 上
            new Vector2Int(1, 0),   // 右
            new Vector2Int(0, -1),  // 下
            new Vector2Int(-1, 0)   // 左
        };

        public Sprite[] diceSprites; // 存储骰子图片的数组

        GameObject viewRoot;
        GameObject battleEventGo;
        GameObject healEventGo;
        GameObject eventGoRoot;
        GameObject pawnGoRoot;
        GameObject blockGoTemplete;
        GameObject playerGOTemplete;

        public void OnAwake()
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

            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.ROLL_THE_DICE, BeginRolling);
            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.GAMBLING_VIEW_FINISH_LOAD, OnGamblingFinishLoad);
            EventMgr.Instance.Register<int, int>(EventType.EVENT_BATTLE_UI, SubEventType.DRAW_ROAD, OnDrawRoad);
            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.CLEAR_ROAD, OnClearRoad);
            EventMgr.Instance.Register<int, int>(EventType.EVENT_BATTLE_UI, SubEventType.BLOCK_MOUSE_DOWN, OnClickMapDown);
            EventMgr.Instance.Register<MapPawn>(EventType.EVENT_BATTLE_UI, SubEventType.MOVE_FINISH, OnMoveFinish);

            diceSprites = new Sprite[6];
            for (int i = 0; i < 6; i++)
            {
                diceSprites[i] = Resources.Load<Sprite>($"Pics/diceRed{i + 1}");
            }

            battleEventGo = GameObject.Find("battleEvent").gameObject;
            healEventGo = GameObject.Find("healEvent").gameObject;
            eventGoRoot = GameObject.Find("eventGoRoot").gameObject;
            pawnGoRoot = GameObject.Find("pawnGoRoot").gameObject;
            viewRoot = GameObject.Find("game").gameObject;

            blockGoTemplete = viewRoot.GetComponent<GameEntry>().blockUIPrefab;
            playerGOTemplete = viewRoot.GetComponent<GameEntry>().heroGoPrefab;
            MoveComp moveComp = viewRoot.AddComponent<MoveComp>();
            moveComp.RegisterEvents();

            _battleStage = BattleStage.WaitingForAction;
            InitMap();
            InitPlayer();
            InitUI();
        }

        public void OnUpdate()
        {
            if(bShouldHandlePlayerInput())
            {
                HandlePlayerInput();
            }

            if(_battleStage == BattleStage.Rolling)
            {
                RollTheDice();
            }
        }

        #region GameState
        public BattleStage _battleStage;

        public void SetBattleStage(BattleStage battleStage)
        {
            _battleStage = battleStage;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_GAME_STAGE, battleStage);
        }
        #endregion

        #region PlayerInput

        private void HandlePlayerInput()
        {
            if(bShouldHandlePlayerInput())
            {
                if(energy == 0)
                {
                    HandleDiceInput();
                }
                else
                {
                    HandleMovementInput();
                }
            }
        }

        private bool bShouldHandlePlayerInput()
        {
            return _battleStage == BattleStage.WaitingForAction;
        }

        private void HandleDiceInput()
        {
            if (Input.GetKeyDown(KeyCode.Space)) BeginRolling();
        }

        private void HandleMovementInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) TryMove(0, 1);  // 上
            if (Input.GetKeyDown(KeyCode.DownArrow)) TryMove(0, -1); // 下
            if (Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(-1, 0); // 左
            if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(1, 0);  // 右
        }

        private void OnClickMapDown(int x, int y)
        {
            if(_battleStage != BattleStage.WaitingForAction)
            {
                return;
            }
            List<BlockInfo> pathList = FindShortestPath(_playerState._blockInfo, gridData[x, y]);
            if (pathList.Count == 0 || pathList.Count - 1 > energy)
            {
                return;
            }
            OnClearRoad();
            SetBattleStage(BattleStage.UpdatingMove);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.MOVE_GO, (MapPawn)_playerState, _playerState._blockInfo, gridData[x, y], pathList);
        }
        #endregion

        #region PlayerState
        public PlayerState _playerState;
        private Animator playerAnimator;
        private string currentAnimation;

        private void InitPlayer()
        {
            if (walkableBlocks.Count == 0)
            {
                Debug.LogError("No valid spawn positions!");
                return;
            }

            _playerState = new PlayerState(100f, 10f, 15f, 100, walkableBlocks[42]);
            _playerState._gameObject = GameObject.Instantiate(playerGOTemplete, pawnGoRoot.transform);
            UpdatePlayerPosition(_playerState._blockInfo, true);
            playerAnimator = _playerState._gameObject.GetComponent<Animator>();
            UpdatePlayerAnim("Idle");
        }

        private void UpdatePlayerAnim(string animation, float crossFade = 0.2f)
        {
            currentAnimation = animation;
            playerAnimator.CrossFade(currentAnimation, crossFade);
        }

        public void UpdatePlayerPosition(BlockInfo blockInfo, bool bImmidiate = false)
        {
            UpdatePlayerPosition(blockInfo.location, bImmidiate);
        }

        public void UpdatePlayerPosition(Vector2 targetPosition, bool bImmidiate = false)
        {
            if (bImmidiate)
            {
                _playerState._gameObject.GetComponent<Transform>().position = new Vector3(targetPosition.x, targetPosition.y);
            }
        }

        void TryMove(int dx, int dy)
        {
            int newX = _playerState._blockInfo.x + dx;
            int newY = _playerState._blockInfo.y + dy;

            if (IsValidPosition(newX, newY))
            {
                _playerState._blockInfo.OnStepOff();
                UpdatePlayerAnim("Jump");
                EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.MOVE_GO, _playerState._gameObject, _playerState._blockInfo, gridData[newX, newY],
                    FindShortestPath(_playerState._blockInfo, gridData[newX, newY]));
            }
        }

        private float moveSpeed = 10.0f;
        void SmoothMovement()
        {
            Vector2 moveTarget = _playerState._blockInfo.location;
            Vector2 currentV2 = Vector2.Lerp(
                _playerState._gameObject.GetComponent<Transform>().position,
                moveTarget,
                Time.deltaTime * moveSpeed
            );

            if ((currentV2 - moveTarget).magnitude < 0.01f)
            {
                _playerState._gameObject.GetComponent<Transform>().position = new Vector3(moveTarget.x, moveTarget.y);
                OnStopMove();
            }
            else
            {
                _playerState._gameObject.GetComponent<Transform>().position = new Vector3(currentV2.x, currentV2.y);
            }
        }

        private void OnStopMove()
        {
            UpdatePlayerAnim("Idle");
            if(_playerState._blockInfo.EnterNewView())
            {
                // todo 触发block事件
                _playerState._blockInfo.OnStepOn();
                OpenGamblingView();
                // EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.ENTER_GAMBLING, _playerState._blockInfo);
                SetBattleStage(BattleStage.Gambling);
            }
            else
            {
                SetBattleStage(BattleStage.WaitingForAction);
            }
        }
        #endregion

        #region tilemap

        private Tilemap _tilemap;
        private BoundsInt _bounds;
        public BlockInfo[,] gridData; // 二维数组存储数据
        public List<BlockInfo> walkableBlocks = new List<BlockInfo>(); // 可移动区域缓存

        private List<BlockInfo> currentShowPath = new List<BlockInfo>();

        protected void InitMap()
        {
            _tilemap = GameObject.Find("walkableFloor").GetComponent<Tilemap>();
            _bounds = _tilemap.cellBounds;

            InitBlockData();
            RefreshBlockEvents(0); // todo 读表查配置
        }

        protected void RefreshBlockEvents(int LevelID)
        {
            // todo 根据levelID获取地块event配置，根据event配置生成

            int healingNum = 10;
            int battleNum = 10;

            List<int> randomIndexes = GetRandomIndexes(walkableBlocks.Count, healingNum + battleNum);
            foreach (int index in randomIndexes) 
            {
                if(healingNum > 0)
                {
                    walkableBlocks[index].eventType = BlockEventType.Healing;
                    walkableBlocks[index].eventGo = GameObject.Instantiate(healEventGo, eventGoRoot.transform);
                    walkableBlocks[index].eventGo.GetComponent<Transform>().position = new Vector3(walkableBlocks[index].location.x, walkableBlocks[index].location.y);
                    healingNum--;
                }
                else
                {
                    walkableBlocks[index].eventType = BlockEventType.Battle;
                    walkableBlocks[index].eventGo = GameObject.Instantiate(battleEventGo, eventGoRoot.transform);
                    walkableBlocks[index].eventGo.GetComponent<Transform>().position = new Vector3(walkableBlocks[index].location.x, walkableBlocks[index].location.y);
                    battleNum--;
                }
            }
        }

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
                        BlockType blockType = BlockType.Walkable;

                        var block = new BlockInfo(x, y, worldPos, blockType);
                        GameObject blockUIGo = GameObject.Instantiate(blockGoTemplete, eventGoRoot.transform);
                        block.blockUI = blockUIGo.AddComponent<BlockUI>();
                        block.blockUI.transform.position = worldPos;
                        block.blockUI.SetGridLocation(x, y);
                        gridData[x, y] = block;
                        walkableBlocks.Add(block);
                    }
                }
            }
        }

        public bool IsValidPosition(int x, int y)
        {
            if (x < 0 || x >= gridData.GetLength(0)) return false;
            if (y < 0 || y >= gridData.GetLength(1)) return false;
            return gridData[x, y] != null &&
                   gridData[x, y].type == BlockType.Walkable;
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

        public List<BlockInfo> FindShortestPath(BlockInfo start, BlockInfo end)
        {
            // 边界检查
            if (!IsValidPosition(start.x, start.y) || !IsValidPosition(end.x, end.y))
            {
                Debug.LogError("起点或终点非法！");
                return new List<BlockInfo>();
            }

            int width = gridData.GetLength(0);
            int height = gridData.GetLength(1);

            // 记录父节点用于回溯路径
            Dictionary<BlockInfo, BlockInfo> parentMap = new Dictionary<BlockInfo, BlockInfo>();
            Queue<BlockInfo> queue = new Queue<BlockInfo>();
            bool[,] visited = new bool[width, height];

            // 初始化起点
            queue.Enqueue(start);
            visited[start.x, start.y] = true;

            // BFS核心逻辑
            while (queue.Count > 0)
            {
                BlockInfo current = queue.Dequeue();

                // 到达终点
                if (current == end)
                {
                    return ReconstructPath(parentMap, start, end);
                }

                // 探索四个方向
                foreach (Vector2Int dir in Directions)
                {
                    int newX = current.x + dir.x;
                    int newY = current.y + dir.y;

                    if (IsValidPosition(newX, newY) &&
                        !visited[newX, newY])
                    {
                        BlockInfo neighbor = gridData[newX, newY];
                        visited[newX, newY] = true;
                        parentMap[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // 无可用路径
            return new List<BlockInfo>();
        }
        /// <summary>
        /// 回溯路径
        /// </summary>
        private List<BlockInfo> ReconstructPath(Dictionary<BlockInfo, BlockInfo> parentMap, BlockInfo start, BlockInfo end)
        {
            List<BlockInfo> path = new List<BlockInfo>();
            BlockInfo current = end;

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

        private void OnDrawRoad(int x, int y)
        {
            if (_battleStage == BattleStage.UpdatingMove)
            {
                return;
            }
            currentShowPath = FindShortestPath(_playerState._blockInfo, gridData[x, y]);
            for(int i = 0; i < currentShowPath.Count; i++)
            {
                if (i == 0 || i == currentShowPath.Count - 1)
                {
                    continue;
                }
                currentShowPath[i].blockUI.ShowPath(true);
            }
        }

        private void OnClearRoad()
        {
            foreach (BlockInfo block in currentShowPath)
            {
                block.blockUI.ShowPath(false);
            }
            currentShowPath.Clear();
        }

        private void OnMoveFinish(MapPawn pawn)
        {
            if(pawn == _playerState)
            {
                // 移动完成恢复状态
                SetBattleStage(BattleStage.WaitingForAction);
                UpdatePlayerAnim("Idle");
            }
        }
        #endregion

        #region UI
        private void OpenBattleMainView()
        {
            ViewManager.Instance.Open(ViewType.BattleMainView);
        }

        protected void InitUI()
        {
            OpenBattleMainView();
            UpdateEnergy();
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_PLAYERSTATE, _playerState);
        }
        #endregion

        #region Dice
        public float rollDuration = 0.5f; // 骰子滚动的持续时间
        public float rollInterval = 0.02f; // 骰子图片切换的时间间隔
        public float rollEnd = 0.0f; // 骰子最终时间
        public float lastRollTime = 0.0f; // 上次切换时间

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


        public int energy = 0;
        private void UpdateEnergy(int newEnergy = 0)
        {
            energy = newEnergy;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_ENERGY, energy);
        }

        public void ModifyEnergy(int value)
        {
            UpdateEnergy(energy + value);
        }
        #endregion

        #region Gambling
        protected void OnGamblingFinishLoad()
        {
            OnEnterGambling(_playerState._blockInfo);
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
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_GAMBLING_PLAYER_VIEW, _playerState);
        }

        private void InitEnemyData()
        {

        }
        #endregion
    }
}
