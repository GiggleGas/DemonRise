using Microsoft.Unity.VisualStudio.Editor;
using ppCore.Manager;
using Stateless;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using TMPro;
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

    /// <summary>
    /// 虽然叫UIManager但干的大概是battlemgr的活，battlemanager用来搞选关和进关这种逻辑吧
    /// </summary>
    [Manager(ManagerPriority.Delay)]
    public class BattleUIManager : ppCore.Common.Singleton<BattleUIManager>, IManager
    {
        public Sprite[] diceSprites; // 存储骰子图片的数组
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

            diceSprites = new Sprite[6];
            for (int i = 0; i < 6; i++)
            {
                diceSprites[i] = Resources.Load<Sprite>($"Pics/diceRed{i + 1}");
            }

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
            else if(_battleStage == BattleStage.UpdatingMove)
            {
                SmoothMovement();
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

        #endregion

        #region PlayerState
        public PlayerState _playerState;
        private GameObject playerGO;
        private Animator playerAnimator;
        private string currentAnimation;

        private void InitPlayer()
        {
            if (walkableBlocks.Count == 0)
            {
                Debug.LogError("No valid spawn positions!");
                return;
            }

            playerGO = GameObject.Find("playerGO").gameObject;
            _playerState = new PlayerState(100f, 10f, 15f, 100, walkableBlocks[42]);
            UpdatePlayerPosition(_playerState._blockInfo, true);
            playerAnimator = playerGO.GetComponent<Animator>();
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
                playerGO.GetComponent<Transform>().position = new Vector3(targetPosition.x, targetPosition.y);
            }
        }


        void TryMove(int dx, int dy)
        {
            int newX = _playerState._blockInfo.x + dx;
            int newY = _playerState._blockInfo.y + dy;

            if (IsValidPosition(newX, newY))
            {
                _playerState._blockInfo.OnStepOff();
                _playerState._blockInfo = gridData[newX, newY];
                UpdateEnergy(energy - 1);
                UpdatePlayerAnim("Jump");
                SetBattleStage(BattleStage.UpdatingMove);
            }
        }

        private float moveSpeed = 10.0f;
        void SmoothMovement()
        {
            Vector2 moveTarget = _playerState._blockInfo.location;
            Vector2 currentV2 = Vector2.Lerp(
                playerGO.GetComponent<Transform>().position,
                moveTarget,
                Time.deltaTime * moveSpeed
            );

            if ((currentV2 - moveTarget).magnitude < 0.01f)
            {
                playerGO.GetComponent<Transform>().position = new Vector3(moveTarget.x, moveTarget.y);
                OnStopMove();
            }
            else
            {
                playerGO.GetComponent<Transform>().position = new Vector3(currentV2.x, currentV2.y);
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

            GameObject battleEventGo = GameObject.Find("battleEvent").gameObject;
            GameObject healEventGo = GameObject.Find("healEvent").gameObject;
            GameObject eventGoRoot = GameObject.Find("eventGoRoot").gameObject;
            List<int> randomIndexes = GetRandomIndexes(walkableBlocks.Count, healingNum + battleNum);
            foreach (int index in randomIndexes) 
            {
                if(healingNum > 0)
                {
                    walkableBlocks[index].eventType = BlockEventType.Healing;
                    walkableBlocks[index].eventGo = GameObject.Instantiate(healEventGo, eventGoRoot.transform);
                    //walkableBlocks[index].eventGo.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"Pics/diceRed{1}");
                    walkableBlocks[index].eventGo.GetComponent<Transform>().position = new Vector3(walkableBlocks[index].location.x, walkableBlocks[index].location.y);
                    healingNum--;
                }
                else
                {
                    walkableBlocks[index].eventType = BlockEventType.Battle;
                    walkableBlocks[index].eventGo = GameObject.Instantiate(battleEventGo, eventGoRoot.transform);
                    //walkableBlocks[index].eventGo.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"Pics/diceRed{2}");
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
