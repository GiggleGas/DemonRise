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

    public class PlayerState
    {
        public float health;
        public int exp;
        public int location;
        public BlockInfo targetBlock;
    }

    /// <summary>
    /// ��Ȼ��UIManager���ɵĴ����battlemgr�Ļbattlemanager������ѡ�غͽ��������߼���
    /// </summary>
    [Manager(ManagerPriority.Delay)]
    public class BattleUIManager : ppCore.Common.Singleton<BattleUIManager>, IManager
    {
        public void OnAwake()
        {
            ViewManager.Instance.Register(ViewType.BattleMainView, new ViewInfo()
            {
                viewName = "BattleMainView",
                parentTransform = ViewManager.Instance.canvasTransform,
                order = 0,
            });

            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.OPEN_BATTLE_MAIN_VIEW, OpenBattleMainView);
            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.ROLL_THE_DICE, BeginRolling);
            // EventMgr.Instance.Register<BattleStage>(EventType.EVENT_BATTLE, SubEventType.UPDATE_GAME_STAGE, OnGameStageChange);

            InitMap();
            InitUI();
            InitPlayer();
        }

        public void OnUpdate()
        {
            if(bIsRolling)
            {
                RollTheDice();
                return;
            }

            if(bPlayerMoving)
            {
                SmoothMovement();
            }
            else if(energy > 0)
            {
                HandleMovementInput();
            }
            else
            {
                HandleDiceInput();
            }
        }

        private void OpenBattleMainView()
        {
            ViewManager.Instance.Open(ViewType.BattleMainView);
        }


        protected void InitMap()
        {
            _tilemap = GameObject.Find("walkableFloor").GetComponent<Tilemap>();
            _bounds = _tilemap.cellBounds;
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

                        // ��������ϰ��ʾ���߼���
                        //BlockType blockType = Random.value < obstacleProbability ?
                        //    BlockType.Obstacle : BlockType.Walkable;
                        BlockType blockType = BlockType.Walkable;

                        var block = new BlockInfo(x, y, worldPos, blockType);
                        gridData[x, y] = block;

                        if (blockType == BlockType.Walkable)
                        {
                            walkableBlocks.Add(block);
                        }
                    }
                }
            }
        }

        protected void InitUI()
        {
            UpdateEnergy();
        }

        private void OnGameStageChange(BattleStage newBattleStage)
        {
            if(newBattleStage == BattleStage.WaitingForRolling)
            {
                UpdateDiceState(DiceStateChange.Restart);
            }
            else if(newBattleStage == BattleStage.WaitingForAction)
            {
                UpdateEnergy();
            }
        }

        // DICE
        public float rollDuration = 0.5f; // ���ӹ����ĳ���ʱ��
        public float rollInterval = 0.02f; // ����ͼƬ�л���ʱ����
        public float rollEnd = 0.0f; // ��������ʱ��
        public float lastRollTime = 0.0f; // �ϴ��л�ʱ��
        private bool bIsRolling = false;

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
            if(bIsRolling)
            {
                return;
            }
            bIsRolling = true;
            rollEnd = Time.time + rollDuration;
            lastRollTime = Time.time;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_STATE, false);
        }

        private void ResetDiceState()
        {
            rollEnd = 0.0f;
            lastRollTime = 0.0f;
            bIsRolling = false;
        }

        private void HandleDiceInput()
        {
            if (Input.GetKeyDown(KeyCode.Space)) BeginRolling();
        }

        /// Energy
        public int energy = 0;
        private void UpdateEnergy(int newEnergy = 0)
        {
            energy = newEnergy;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_ENERGY, energy);
        }

        /// player 
        public PlayerState _playerState;
        private GameObject playerGO;
        bool bPlayerMoving = false;

        private void InitPlayer()
        {
            if (walkableBlocks.Count == 0)
            {
                Debug.LogError("No valid spawn positions!");
                return;
            }
            playerGO = GameObject.Find("playerGO").gameObject;
            _playerState = new PlayerState();

            // ���ѡ����ƶ�����
            int startIndex = 42;
            _playerState.targetBlock = walkableBlocks[startIndex];
            UpdatePlayerPosition(_playerState.targetBlock.location, true);
        }

        public void UpdatePlayerPosition(Vector2 targetPosition, bool bImmidiate = false)
        {
            if(bImmidiate)
            {
                playerGO.GetComponent<Transform>().position = new Vector3(targetPosition.x, targetPosition.y);
            }
        }

        private void HandleMovementInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) TryMove(0, 1);  // ��
            if (Input.GetKeyDown(KeyCode.DownArrow)) TryMove(0, -1); // ��
            if (Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(-1, 0); // ��
            if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(1, 0);  // ��
        }

        void TryMove(int dx, int dy)
        {
            int newX = _playerState.targetBlock.x + dx;
            int newY = _playerState.targetBlock.y + dy;

            if (IsValidPosition(newX, newY))
            {
                _playerState.targetBlock = gridData[newX, newY];
                // UpdatePlayerPosition(_playerState.targetBlock.location);
                bPlayerMoving = true;
                UpdateEnergy(energy - 1);
            }
        }

        private float moveSpeed = 10.0f;
        void SmoothMovement()
        {
            Vector2 moveTarget = _playerState.targetBlock.location;
            Vector2 currentV2 = Vector2.Lerp(
                playerGO.GetComponent<Transform>().position,
                moveTarget,
                Time.deltaTime * moveSpeed
            );

            if((currentV2 - moveTarget).magnitude < 0.01f)
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
            bPlayerMoving = false;
            // todo ����block�¼�
            if(energy == 0)
            {
                UpdateDiceState(DiceStateChange.Restart);
            }
        }

        /// map
        public Dictionary<int, Block> blockArr;
        private Tilemap _tilemap;
        private BoundsInt _bounds;
        public Vector2Int gridSize;
        public Vector2[,] positionGrid;
        public BlockInfo[,] gridData; // ��ά����洢����
        public List<BlockInfo> walkableBlocks = new List<BlockInfo>(); // ���ƶ����򻺴�


        public bool IsValidPosition(int x, int y)
        {
            if (x < 0 || x >= gridData.GetLength(0)) return false;
            if (y < 0 || y >= gridData.GetLength(1)) return false;
            return gridData[x, y] != null &&
                   gridData[x, y].type == BlockType.Walkable;
        }
    }
}
