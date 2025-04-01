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

        private Coroutine currentMoveCoroutine; // ��ǰ���е��ƶ�Э��
        public void TryMoveGO(MapPawn pawn, BlockInfo startBlock, BlockInfo targetBlock, List<BlockInfo> path)
        {
            // ��ʼ�ƶ�Э��
            currentMoveCoroutine = StartCoroutine(MoveAlongPath(pawn, path));
        }


        /// <summary>
        /// ��·���ƶ���Э��
        /// </summary>
        /// 
        private IEnumerator MoveAlongPath(MapPawn obj, List<BlockInfo> path)
        {
            int currentPathIndex = 0;
            Vector3 targetPosition;
            float movementSpeed = 5.0f; // �������ƶ��ٶ�

            // ������ʼ�㣨����Ѿ�����ȷλ�ã�
            if ((Vector2)obj.GetTransform().position == path[0].location)
            {
                currentPathIndex++;
            }

            while (currentPathIndex < path.Count)
            {
                targetPosition = path[currentPathIndex].location;

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
                obj.GetTransform().position = targetPosition;
                obj.UpateBlockInfo(path[currentPathIndex]);
                BattleUIManager.Instance.ModifyEnergy(-1);

                // ���µؿ��¼�
                BlockInfo currentBlock = path[currentPathIndex];
                currentBlock.OnStepOn();

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
    [Manager(ManagerPriority.Delay)]
    public class BattleUIManager : ppCore.Common.Singleton<BattleUIManager>, IManager
    {
        // ���巽���ϡ��ҡ��¡����ķ����ƶ���
        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(0, 1),   // ��
            new Vector2Int(1, 0),   // ��
            new Vector2Int(0, -1),  // ��
            new Vector2Int(-1, 0)   // ��
        };

        public Sprite[] diceSprites; // �洢����ͼƬ������

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
            if (Input.GetKeyDown(KeyCode.UpArrow)) TryMove(0, 1);  // ��
            if (Input.GetKeyDown(KeyCode.DownArrow)) TryMove(0, -1); // ��
            if (Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(-1, 0); // ��
            if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(1, 0);  // ��
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
                // todo ����block�¼�
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
        public BlockInfo[,] gridData; // ��ά����洢����
        public List<BlockInfo> walkableBlocks = new List<BlockInfo>(); // ���ƶ����򻺴�

        private List<BlockInfo> currentShowPath = new List<BlockInfo>();

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

        public List<BlockInfo> FindShortestPath(BlockInfo start, BlockInfo end)
        {
            // �߽���
            if (!IsValidPosition(start.x, start.y) || !IsValidPosition(end.x, end.y))
            {
                Debug.LogError("�����յ�Ƿ���");
                return new List<BlockInfo>();
            }

            int width = gridData.GetLength(0);
            int height = gridData.GetLength(1);

            // ��¼���ڵ����ڻ���·��
            Dictionary<BlockInfo, BlockInfo> parentMap = new Dictionary<BlockInfo, BlockInfo>();
            Queue<BlockInfo> queue = new Queue<BlockInfo>();
            bool[,] visited = new bool[width, height];

            // ��ʼ�����
            queue.Enqueue(start);
            visited[start.x, start.y] = true;

            // BFS�����߼�
            while (queue.Count > 0)
            {
                BlockInfo current = queue.Dequeue();

                // �����յ�
                if (current == end)
                {
                    return ReconstructPath(parentMap, start, end);
                }

                // ̽���ĸ�����
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

            // �޿���·��
            return new List<BlockInfo>();
        }
        /// <summary>
        /// ����·��
        /// </summary>
        private List<BlockInfo> ReconstructPath(Dictionary<BlockInfo, BlockInfo> parentMap, BlockInfo start, BlockInfo end)
        {
            List<BlockInfo> path = new List<BlockInfo>();
            BlockInfo current = end;

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
                // �ƶ���ɻָ�״̬
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
        public float rollDuration = 0.5f; // ���ӹ����ĳ���ʱ��
        public float rollInterval = 0.02f; // ����ͼƬ�л���ʱ����
        public float rollEnd = 0.0f; // ��������ʱ��
        public float lastRollTime = 0.0f; // �ϴ��л�ʱ��

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
