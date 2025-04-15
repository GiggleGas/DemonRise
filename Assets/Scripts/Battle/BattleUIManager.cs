using ppCore.Manager;
using Stateless;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static Unity.Collections.AllocatorManager;
using static Unity.VisualScripting.Member;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;


namespace PDR
{
    public enum DiceStateChange
    {
        Restart,
        Change,
        GetResult
    }

    public enum BattleState
    {
        Initialize,
        PlayerTurn,
        MonsterTurn,
        Victory,
        Defeat
    }

    public enum BattleSubState
    {
        None,
        WaitingForAction,
        HoldingCard,
        Rolling,

        UpdatingMove,
        UpdateBattle,
    }

    public enum TeamType
    { 
        None,
        Friend,
        Enemy
    }

    public struct AttackStruct
    {
        public List<MapPawn> attackTargets;
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
            new Vector2Int(-1, 0),   // ��
            new Vector2Int(-1, 1),  // ����
            new Vector2Int(1, 1),    // ����
            new Vector2Int(-1, -1),  // ����
            new Vector2Int(1, -1)    // ����
        };

        // Config
        public Sprite[] diceSprites; // �洢����ͼƬ������

        private GameObject viewRoot;
        private GameObject eventGoRoot;
        private GameObject pawnGoRoot;

        private GameObject blockGoTemplete;
        private GameObject playerGoTemplete;

        // BattleState
        public int currentLevel = 0;
        public int maxLevel = 20;
        public int dicePerLevel = 10; // ÿ�ظ���������
        public int energyPerRound = 1; // ÿ�غϻ���ж�����
        public int remainingDice = 0; // ��ǰʣ����������
        public int energy = 0; // ��ǰ�ж�ֵ
        public BattleState _battleState; // ��ǰս��״̬
        public BattleSubState _battleSubState; // ��ս��״̬

        // TileMap
        private Tilemap _tilemap; // ��ͼtilemap component
        private BoundsInt _bounds;
        public BlockInfo[,] gridData; // ��ά����洢����
        private List<Vector2Int> walkableBlocks = new List<Vector2Int>(); // ���ƶ����򻺴�
        private List<Vector2Int> currentShowPath = new List<Vector2Int>(); // ��ǰΪ��ʾUI�ĸ���
        private List<Vector2Int> currentWarningBlocks = new List<Vector2Int>(); // ��ǰԤ������
        public PlayerPawn _playerPawn; // ���Pawn

        // Dice
        public float rollDuration = 0.5f; // ���ӹ����ĳ���ʱ��
        public float rollInterval = 0.02f; // ����ͼƬ�л���ʱ����
        public float rollEnd = 0.0f; // ��������ʱ��
        public float lastRollTime = 0.0f; // �ϴ��л�ʱ��

        // card
        public CardContainer playerCards;
        public int handMaxNum;
        public CardBase currentChooseCard; 
        private List<Vector2Int> currentCardValidBlocks = new List<Vector2Int>(); // ��ǰ���ƿ��÷�Χ
        public BlockInfo pickedBlock; // ���ܵ���ͷ�block
        public List<GameObject> fires = new List<GameObject>();

        // attack
        public Dictionary<MapPawn, AttackStruct> _attackList;

        public void OnAwake()
        {
            // RegisterViews
            RegisterViews();

            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.ROLL_THE_DICE, BeginRolling);
            // EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.GAMBLING_VIEW_FINISH_LOAD, OnGamblingFinishLoad);
            EventMgr.Instance.Register<Vector2Int>(EventType.EVENT_BATTLE_UI, SubEventType.DRAW_ROAD, OnDrawRoad);
            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.CLEAR_ROAD, OnClearRoad);
            EventMgr.Instance.Register<Vector2Int>(EventType.EVENT_BATTLE_UI, SubEventType.BLOCK_MOUSE_DOWN, OnClickBlock);
            EventMgr.Instance.Register<MapPawn>(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_MOVE_FINISH, OnPawnStopMove);
            EventMgr.Instance.Register<MapPawn>(EventType.EVENT_BATTLE_UI, SubEventType.AI_ATTACK_FINISH, OnAttack);
            EventMgr.Instance.Register<MapPawn, MapPawn>(EventType.EVENT_BATTLE_UI, SubEventType.PLAYER_ATTACK_FINISH, OnAttack);
            EventMgr.Instance.Register<MapPawn, string>(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_PLAY_ANIMATION_FINISH, OnAnimFinish);
            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.AI_TURN_FINISH, OnEnemyMoveFinish);
            EventMgr.Instance.Register<CardBase>(EventType.EVENT_BATTLE_UI, SubEventType.CLICK_CARD, OnSelectCard);
            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.CARD_USE_FINISH, OnCardSkillFinish);
            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.STEP_ON_GOLDEN_BLOCK, OnStepOnGoldenBlock);
            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.SPAWN_FIRE, SpawnFire);


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
            AnimControlComp animControlComp = viewRoot.AddComponent<AnimControlComp>();
            animControlComp.RegisterEvents();

            currentLevel = 0;
            maxLevel = 20;

            InitMap();
            InitPlayer();
            InitUI();
            InitializeCards();
            UpdateGoldenBlock();
            InitializeLevel(currentLevel);
            StartPlayerTurn();
        }

        public void OnUpdate()
        {
            if (_battleState == BattleState.PlayerTurn)
            {
                HandlePlayerInput();
                if (_battleSubState == BattleSubState.Rolling)
                {
                    RollTheDice();
                }
            }

        }

        public GameEntry GetGameEntry()
        {
            return viewRoot.GetComponent<GameEntry>();
        }

        #region ------------------------------------------------------------------ GameStage ս��״̬ ------------------------------------------------------------------

        /// <summary>
        /// ��ʼ�»غ�
        /// </summary>
        private void StartNewRound()
        {
            if (currentLevel < maxLevel || enemyPawns.Count > 0)
            {
                currentLevel++;
                InitializeLevel(currentLevel);
            }

            StartPlayerTurn(); // ���¿�ʼ��һغ�
        }

        /// <summary>
        /// ÿ�λغϿ�ʼʱ����
        /// </summary>
        /// <param name="level"></param>
        private void InitializeLevel(int level)
        {
            // todo ������
            RefreshBlockEvents(level);
            UpdateEnergy(3);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.START_NEW_ROUND, (float)level / (float)maxLevel);
        }

        /// <summary>
        /// ���ݹؿ�ˢ�µ�ͼ�¼�
        /// </summary>
        /// <param name="LevelID"></param>
        protected void RefreshBlockEvents(int level)
        {
            if(level == 1)
            {
                SpawnEnemy(3, new List<int>() { 7, 10, 37, 34 }, 70.0f, 20.0f);
            }
            else if(level == 6)
            {
                SpawnEnemy(4, new List<int>() { 0, 46, 33, 11, 3, 25, 22 }, 100.0f, 50.0f);
            }
            else if(level == 3)
            {
                EnhanceEnemy(20, 10, 0);
            }
            else if(level == 10)
            {
                EnhanceEnemy(50, 15, 5);
            }
        }

        /// <summary>
        /// �ŵ����볡
        /// </summary>
        /// <param name="enemyNum"></param>
        /// <param name="enemyLoc"></param>
        protected void SpawnEnemy(int enemyNum, List<int> enemyLoc, float health, float attack)
        {
            foreach(int index in enemyLoc)
            {
                if (enemyNum <= 0) 
                {
                    return;
                }
                if(walkableBlocks[index] == _playerPawn._gridLocation)
                {
                    continue;
                }
                BlockInfo block = GetBlockByGridLocation(walkableBlocks[index]);
                EnemyPawn enemyPawn = new EnemyPawn(block, GameObject.Instantiate(enemyGoTemplete, pawnGoRoot.transform), TeamType.Enemy, 1, 1, health, health, attack, 100);
                enemyPawn.PostInitialize();
                enemyPawns.Add(enemyPawn);
                enemyNum--;
            }
        }

        /// <summary>
        /// ǿ������
        /// </summary>
        /// <param name="health"></param>
        /// <param name="attack"></param>
        /// <param name="defence"></param>
        protected void EnhanceEnemy(int health, int attack, int defence)
        {
            foreach(EnemyPawn enemyPawn in enemyPawns)
            {
                enemyPawn.Enhance(health, attack, defence);
            }
        }

        /// <summary>
        /// ���½�ɫ�ؿ�
        /// </summary>
        protected void UpdateGoldenBlock()
        {
            List<int> goldenBlocks = new List<int>()
            {
                0,6,26,40,46
            };

            // Fisher-Yates ϴ���㷨������ϴ���Ż���
            for (int i = 0; i < goldenBlocks.Count; i++)
            {
                int swapIndex = UnityEngine.Random.Range(i, goldenBlocks.Count);

                // ����λ��
                (goldenBlocks[i], goldenBlocks[swapIndex]) = (goldenBlocks[swapIndex], goldenBlocks[i]);
            }

            foreach (int index in goldenBlocks)
            {
                if(walkableBlocks[index] == _playerPawn._gridLocation)
                {
                    continue;
                }
                BlockInfo block = GetBlockByGridLocation(walkableBlocks[index]);
                block.blockUI.SetGolden(true);
                return;
            }
        }

        protected void OnStepOnGoldenBlock()
        {
            // todo 3ѡ1
            _playerPawn.UpadateAttackTimes(15.0f, true, true);
            _playerPawn.AddDefence(50.0f);
            UpdateGoldenBlock();
        }

        protected void UpdateDiceNum(int newNum)
        {
            remainingDice = newNum;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_DICE_NUM, remainingDice);
        }

        /// <summary>
        /// ��ʼ��һغ�
        /// </summary>
        void StartPlayerTurn()
        {
            SetBattleState(BattleState.PlayerTurn, BattleSubState.WaitingForAction);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_HAND_DECK, playerCards.DrawCards(handMaxNum));
            UpdateWarningArea();
        }

        /// <summary>
        /// ��ʼ�з��غ� ��MonsterUpdateComp::StartMonsterTurn
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>

        public bool TryAIAttack(MapPawn enemy)
        {
            BlockInfo enemyBlock = GetBlockByGridLocation(enemy._gridLocation);
            List<BlockInfo> attackList = GetBlockInRange(enemyBlock, enemy._attackRange);
            if (attackList.Count == 0)
            {
                return false;
            }
            foreach (BlockInfo attack in attackList)
            {
                if (attack.pawn != null && enemy.IsSameTeam(attack.pawn))
                {
                    // Ŀǰ��û�ٻ���ɶ�ģ��������ж�
                    TryAttack(enemy, attack);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// �����ﱻ�ɵ�ʱ�����
        /// </summary>
        /// <param name="monster"></param>
        public void OnMonsterDefeated(EnemyPawn enemy)
        {
            if(enemy == null)
            {
                return;
            }
            enemy.IsValid = false;
            enemy.DestroySelf();
            if (enemyPawns.Count == 0)
            {
                if (currentLevel >= maxLevel)
                {
                    EndGame(true);
                }
            }
        }


        public void CheckPlayerStatus()
        {
            if (_playerPawn._health <= 0)
            {
                EndGame(false);
            }
        }

        protected void EndGame(bool isVictory)
        {
            SetBattleState(BattleState.Victory, BattleSubState.None);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.END_GAME, isVictory);
        }

        /// <summary>
        /// �л�ս��״̬
        /// </summary>
        /// <param name="BattleState"></param>
        public void SetBattleState(BattleState battleState, BattleSubState battleSubState)
        {
            if (_battleState == BattleState.Victory)
            {
                return;
            }
            _battleState = battleState;
            _battleSubState = battleSubState;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_GAME_STAGE, _battleState, _battleSubState);
        }

        private void UpdateWarningArea()
        {
            foreach (Vector2Int warningBlock in currentWarningBlocks)
            {
                GetBlockByGridLocation(warningBlock).blockUI.ShowWarning(false);
            }
            currentWarningBlocks.Clear();
            foreach (EnemyPawn enemyPawn in enemyPawns)
            {
                if (enemyPawn.IsValid == false)
                {
                    continue;
                }
                if (CanAIAttack(enemyPawn))
                {
                    GetBlockByGridLocation(_playerPawn._gridLocation).blockUI.ShowWarning(true);
                    currentWarningBlocks.Add(_playerPawn._gridLocation);
                }
                else
                {
                    List<Vector2Int> path = FindShortestPath(enemyPawn._gridLocation, _playerPawn._gridLocation);
                    if(path.Count >= 2)
                    {
                        GetBlockByGridLocation(path[1]).blockUI.ShowWarning(true);
                        currentWarningBlocks.Add(path[1]);
                    }
                }
            }
        }
        #endregion

        #region -------------------------------------------------------- Card ս������ --------------------------------------------------------
        public void InitializeCards()
        {
            handMaxNum = 4;
            playerCards = new CardContainer();
            List<CardBase> cardBases = new List<CardBase>()
            {
                new FastMoveCard(1001, 1, 0, 2),
                new AttackCard(1002, 2, 0, 20.0f),
                new AttackCard(1002, 2, 0, 20.0f),
                new DefenceCard(1003, 1, 0, 15.0f),
                new DefenceCard(1003, 1, 0, 15.0f),
                new DefenceCard(1003, 1, 0, 15.0f),
                new FireBallCard(1004, 3, 4, 50.0f)
            };
            playerCards.InitializeDeck(cardBases);
            playerCards.DrawInitialCards(handMaxNum);
        }

        public void OnSelectCard(CardBase card)
        {
            if (_battleState != BattleState.PlayerTurn || _battleSubState != BattleSubState.WaitingForAction || energy < card._cost) 
            {
                return;
            }
            ClearSelectCard();
            currentChooseCard = card;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_SELECTED_CARD, currentChooseCard._id);
            List<List<Vector2Int>> ranges = GetReachablePositions(_playerPawn._gridLocation, card._range);
            for (int i = 0; i <= card._range; ++i) 
            {
                foreach(var rangePos in ranges[i])
                {
                    currentCardValidBlocks.Add(rangePos);
                }
            }

            foreach(var blockLoc in currentCardValidBlocks)
            {
                GetBlockByGridLocation(blockLoc).blockUI.ShowRange(true);
            }
            SetBattleState(BattleState.PlayerTurn, BattleSubState.HoldingCard);
        }

        public void ConsumCard()
        {
            if(currentChooseCard == null)
            {
                Debug.LogError("û���κ�card�����ͷţ�");
                return;
            }
            currentChooseCard.Consume();
            ModifyEnergy(-currentChooseCard._cost);
            EventMgr.Instance.Dispatch<int>(EventType.EVENT_BATTLE_UI, SubEventType.HIDE_USED_CARD, currentChooseCard._id);
            ClearSelectCard();
            CheckEnergy();
        }

        public void OnCardSkillFinish()
        {
            if(_battleState == BattleState.PlayerTurn && _battleSubState == BattleSubState.HoldingCard)
            {
                CheckEnergy();
            }
        }

        public void ClearSelectCard()
        {
            if(currentChooseCard != null)
            {
                EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CANCEAL_SELECT_CARD, currentChooseCard._id);
                EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_SELECTED_CARD, -1);
                currentChooseCard = null;
            }

            foreach (var blockLoc in currentCardValidBlocks)
            {
                GetBlockByGridLocation(blockLoc).blockUI.ShowRange(false);
            }
            currentCardValidBlocks.Clear();
        }

        protected void SpawnFire()
        {
            GameObject fireGoTemplete = viewRoot.GetComponent<GameEntry>().fireExplosion;
            GameObject fireGo = GameObject.Instantiate(fireGoTemplete, pawnGoRoot.transform);
            fireGo.GetComponent<Transform>().position = pickedBlock._worldlocation;
            AnimControlComp animCotrl = fireGo.AddComponent<AnimControlComp>();
            //animCotrl.ChangeDuringAnimation("FireHit", 0.75f);
            fires.Add(fireGo);
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
            return gridData[mapPawn._gridLocation.x, mapPawn._gridLocation.y];
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
        /// �ж�ĳλ���Ƿ����ƶ���ȥ
        /// </summary>
        /// <param name="movePosition"></param>
        /// <returns></returns>
        public bool IsValidMovePosition(Vector2Int movePosition)
        {
            return gridData[movePosition.x, movePosition.y].pawn == null;
        }

        public List<BlockInfo> GetBlockInRange(BlockInfo sourceBlock, int range)
        {
            List<BlockInfo> result = new List<BlockInfo>();
            Vector2Int sourcePos = new Vector2Int(sourceBlock._gridLocation.x, sourceBlock._gridLocation.y);

            // �˷������������б�Խǣ�
            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    Vector2Int checkPos = new Vector2Int(
                        sourcePos.x + dx,
                        sourcePos.y + dy
                    );

                    // ʹ��������֤�������λ����Ч��
                    if (IsValidPosition(checkPos))
                    {
                        result.Add(gridData[checkPos.x, checkPos.y]);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// ��ȡ���������·��
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<Vector2Int> FindShortestPath(Vector2Int start, Vector2Int end, bool eightDir = false)
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
            int maxNum = eightDir ? 8 : 4;

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
                for (int id = 0; id < maxNum; id++)
                {
                    Vector2Int dir = Directions[id];
                    Vector2Int newLoc = current + dir;

                    if (IsValidPosition(newLoc) &&
                        !visited[newLoc.x, newLoc.y])
                    {
                        if (newLoc != end && !IsValidMovePosition(newLoc)) 
                        {
                            continue;
                        }
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
            OnDrawRoad(targetLoc, _playerPawn._gridLocation);
        }

        /// <summary>
        /// ��ĳ����λ�õ�·��
        /// </summary>
        /// <param name="targetLoc"></param>
        /// <param name="sourceLoc"></param>
        private void OnDrawRoad(Vector2Int targetLoc, Vector2Int sourceLoc)
        {
            if (_battleSubState != BattleSubState.WaitingForAction)
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

        /// <summary>
        /// ��ȡ��������0-N���ڿɴ��λ�ü���
        /// </summary>
        /// <param name="start">��ʼ����</param>
        /// <param name="maxSteps">�����</param>
        // <returns>����������Ŀɴ�λ���б�</returns>
        public List<List<Vector2Int>> GetReachablePositions(Vector2Int start, int maxSteps)
        {
            List<List<Vector2Int>> result = new List<List<Vector2Int>>();

            // ������֤
            if (!IsValidPosition(start) || maxSteps < 0)
            {
                Debug.LogError("�Ƿ��������");
                return result;
            }

            // ��ʼ�����ݽṹ
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, int> stepCount = new Dictionary<Vector2Int, int>();

            // ��ʼ�����
            queue.Enqueue(start);
            visited.Add(start);
            stepCount[start] = 0;

            // BFS����
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int currentSteps = stepCount[current];

                // �����ǰ��δ��ʼ���򴴽����б�
                while (result.Count <= currentSteps)
                {
                    result.Add(new List<Vector2Int>());
                }
                result[currentSteps].Add(current);

                // �ﵽ�����ʱֹͣ��չ
                if (currentSteps >= maxSteps) continue;

                // ̽���ĸ�����
                for (int id = 0; id < 4; id++)
                {
                    Vector2Int dir = Directions[id];
                    Vector2Int neighbor = current + dir;

                    if (IsValidPosition(neighbor) &&
                       !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        stepCount[neighbor] = currentSteps + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // ȷ�������б��ȷ���Ҫ��
            while (result.Count < maxSteps + 1)
            {
                result.Add(new List<Vector2Int>());
            }

            return result;
        }


        #endregion

        #region ------------------------------------------------------------------ PlayerInput ------------------------------------------------------------------
        private void HandlePlayerInput()
        {
            if(ShouldHandlePlayerInput())
            {
                HandleDiceInput();
                if (Input.GetMouseButtonDown(1))
                {
                    ClearSelectCard();
                    SetBattleState(BattleState.PlayerTurn, BattleSubState.WaitingForAction);
                }
            }
        }

        private bool ShouldHandlePlayerInput()
        {
            return _battleState == BattleState.PlayerTurn && (_battleSubState == BattleSubState.WaitingForAction || _battleSubState == BattleSubState.HoldingCard);
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
            if(!ShouldHandlePlayerInput())
            {
                return;
            }

            BlockInfo playerBlock = GetPawnBlock(_playerPawn);
            BlockInfo targetBlock = GetBlockByGridLocation(blockGridLocation);
            if(currentChooseCard != null && _battleSubState == BattleSubState.HoldingCard)
            {
                if(currentCardValidBlocks.Contains(blockGridLocation) && currentChooseCard.IsValidToConsume(playerBlock, targetBlock))
                {
                    pickedBlock = targetBlock;
                    ConsumCard();
                }
            }
            else if(_battleSubState == BattleSubState.WaitingForAction)
            {
                if(IsAttackAction(playerBlock, targetBlock))
                {
                    TryAttack(_playerPawn, targetBlock);
                }
                else
                {
                    TryMovePawn(_playerPawn, targetBlock);
                }
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
            BlockInfo block = gridData[walkableBlocks[20].x, walkableBlocks[20].y];
            _playerPawn = new PlayerPawn(block, GameObject.Instantiate(playerGoTemplete, pawnGoRoot.transform), TeamType.Friend, 1, 1, 70f, 15f, 0, 100);
            _playerPawn.PostInitialize();
        }

        #endregion

        #region ------------------------------------------------------------------PawnControl ��ͼPawn����------------------------------------------------------------------
        public void TryAttack(BlockInfo targetBlock)
        {
            TryAttack(_playerPawn, targetBlock);
        }

        /// <summary>
        /// sourcePawn����targetBlockλ�õĵ�λ
        /// </summary>
        /// <param name="sourcePawn"></param>
        /// <param name="targetBlock"></param>
        public void TryAttack(MapPawn sourcePawn, BlockInfo targetBlock)
        {
            sourcePawn._animControlComp.RegisterAttackContext(targetBlock.pawn);
            sourcePawn.PlayAnimation("Attack");
        }

        private void OnAttack(MapPawn sourceGo)
        {
            float actualDamage = _playerPawn.TakeDamage(sourceGo, sourceGo.GetAttackValue());
            sourceGo.PlayAnimation("Idle");
            CheckPlayerStatus();
        }

        private void OnAttack(MapPawn sourceGo, MapPawn targetGo)
        {
            float actualDamage = targetGo.TakeDamage(sourceGo, sourceGo.GetAttackValue());
            EnemyPawn enemy = targetGo as EnemyPawn;
            if (enemy != null && enemy._health <= 0)
            {
                enemy.PlayAnimation("Dead", 1.0f, 0.2f);
            }
            sourceGo.PlayAnimation("Idle");
            if(sourceGo == _playerPawn)
            {
                ModifyEnergy(-1);
                CheckEnergy();
            }
        }

        private void OnAnimFinish(MapPawn sourcePawn, string animation) 
        {
            if(sourcePawn is EnemyPawn && animation == "Attack")
            {
                OnEnemyAttackFinish(sourcePawn);
            }
            else if (animation == "Dead" && sourcePawn != null) 
            {
                OnMonsterDefeated(sourcePawn as EnemyPawn);
            }
            else if(animation == "FireHit")
            {
                foreach(var fire in fires)
                {
                    GameObject.Destroy(fire);
                }
                fires.Clear();
            }
        }

        /// <summary>
        /// sourcePawn�ߵ�targetBlock
        /// </summary>
        /// <param name="sourcePawn"></param>
        /// <param name="targetBlock"></param>
        private void TryMovePawn(MapPawn sourcePawn, BlockInfo targetBlock)
        {
            if(sourcePawn == null || targetBlock.pawn == sourcePawn)
            {
                return;
            }
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

            sourceBlock.pawn = null;
            SetBattleState(BattleState.PlayerTurn, BattleSubState.UpdatingMove);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_MOVE, (MapPawn)_playerPawn, safeBlocks);

        }

        private void OnPawnStopMove(MapPawn pawn)
        {
            pawn.PlayAnimation("Idle");
            if (pawn == _playerPawn)
            {
                CheckEnergy();
            }
            else
            {
                OnEnemyMoveFinish();
            }
        }

        /// <summary>
        /// �ж�����pawn�Ƿ���Ҫ��ս
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private bool NeedAttack(MapPawn source, MapPawn target)
        {
            if(target == null || source == null)
            {
                return false;
            }
            return !source.IsSameTeam(target);
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
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_PLAYER_PAWN, _playerPawn);
        }

        private void OpenBattleMainView()
        {
            ViewManager.Instance.Open(ViewType.BattleMainView);
        }

        public void CheckEnergy()
        {
            if (energy <= 0)
            {
                StartMonsterTurn();
            }
            else
            {
                SetBattleState(BattleState.PlayerTurn, BattleSubState.WaitingForAction);
            }
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
                    break;
                case DiceStateChange.GetResult:
                    ResetDiceState();
                    EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_SPRITE, randomIndex);
                    ModifyEnergy(randomIndex + 1);
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
            if (remainingDice <= 0)
            {
                return;
            }
            // ModifyDiceNum(-1);
            ModifyEnergy(-1);
            SetBattleState(BattleState.PlayerTurn, BattleSubState.Rolling);
            rollEnd = Time.time + rollDuration;
            lastRollTime = Time.time;
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_STATE, false);
        }

        private void ResetDiceState()
        {
            rollEnd = 0.0f;
            lastRollTime = 0.0f;
            SetBattleState(BattleState.PlayerTurn, BattleSubState.WaitingForAction);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_STATE, true);
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
