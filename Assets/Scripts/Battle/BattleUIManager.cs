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

            //EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.OPEN_BATTLE_MAIN_VIEW, OpenBattleMainView);
            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.ROLL_THE_DICE, BeginRolling);

            InitMap();
            InitPlayer();
            InitUI();
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

        #region PlayerState
        public PlayerState _playerState;
        private GameObject playerGO;
        bool bPlayerMoving = false;
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
            _playerState = new PlayerState(100, 10, 100, walkableBlocks[42]);
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

        private void HandleMovementInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) TryMove(0, 1);  // ��
            if (Input.GetKeyDown(KeyCode.DownArrow)) TryMove(0, -1); // ��
            if (Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(-1, 0); // ��
            if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(1, 0);  // ��
        }

        void TryMove(int dx, int dy)
        {
            int newX = _playerState._blockInfo.x + dx;
            int newY = _playerState._blockInfo.y + dy;

            if (IsValidPosition(newX, newY))
            {
                _playerState._blockInfo.OnStepOff();
                _playerState._blockInfo = gridData[newX, newY];
                bPlayerMoving = true;
                UpdateEnergy(energy - 1);
                UpdatePlayerAnim("Jump");
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
            bPlayerMoving = false;

            _playerState._blockInfo.OnStepOn();
            UpdatePlayerAnim("Idle");

            // todo ����block�¼�
            if (energy == 0)
            {
                UpdateDiceState(DiceStateChange.Restart);
            }
        }
        #endregion

        #region tilemap

        private Tilemap _tilemap;
        private BoundsInt _bounds;
        public BlockInfo[,] gridData; // ��ά����洢����
        public List<BlockInfo> walkableBlocks = new List<BlockInfo>(); // ���ƶ����򻺴�

        protected void InitMap()
        {
            _tilemap = GameObject.Find("walkableFloor").GetComponent<Tilemap>();
            _bounds = _tilemap.cellBounds;

            InitBlockData();
            RefreshBlockEvents(0); // todo ���������

        }

        protected void RefreshBlockEvents(int LevelID)
        {
            // todo ����levelID��ȡ�ؿ�event���ã�����event��������

            int healingNum = 10;
            int battleNum = 10;

            GameObject templeteGo = GameObject.Find("eventObjTemplete").gameObject;
            GameObject eventGoRoot = GameObject.Find("eventGoRoot").gameObject;
            List<int> randomIndexes = GetRandomIndexes(walkableBlocks.Count, healingNum + battleNum);
            foreach (int index in randomIndexes) 
            {
                if(healingNum > 0)
                {
                    walkableBlocks[index].eventType = BlockEventType.Healing;
                    walkableBlocks[index].eventGo = GameObject.Instantiate(templeteGo, eventGoRoot.transform);
                    walkableBlocks[index].eventGo.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"Pics/diceRed{1}");
                    walkableBlocks[index].eventGo.GetComponent<Transform>().position = new Vector3(walkableBlocks[index].location.x, walkableBlocks[index].location.y);
                    healingNum--;
                }
                else
                {
                    walkableBlocks[index].eventType = BlockEventType.Battle;
                    walkableBlocks[index].eventGo = GameObject.Instantiate(templeteGo, eventGoRoot.transform);
                    walkableBlocks[index].eventGo.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"Pics/diceRed{2}");
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

        /*private void OnGameStageChange(BattleStage newBattleStage)
        {
            if(newBattleStage == BattleStage.WaitingForRolling)
            {
                UpdateDiceState(DiceStateChange.Restart);
            }
            else if(newBattleStage == BattleStage.WaitingForAction)
            {
                UpdateEnergy();
            }
        }*/
        #endregion

        #region Dice
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

        public int energy = 0;
        private void UpdateEnergy(int newEnergy = 0)
        {
            energy = newEnergy;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_ENERGY, energy);
        }
        #endregion
    }
}
