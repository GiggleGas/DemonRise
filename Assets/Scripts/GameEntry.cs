using ppCore.Manager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PDR;
using System;

public class GameEntry : MonoBehaviour
{
    [System.Serializable]
    public struct spriteStruct
    {
        public int id;
        public Sprite sprite;
    }

    public spriteStruct[] sweetPrefabs;
    public Sprite[] numSprites;
    public Dictionary<int, Sprite> spriteConfig;

    public GameObject blockUIPrefab;
    public GameObject playerGoPrefab;
    public GameObject enemyGoPrefab;
    public GameObject fireExplosion;

    public void Awake()
    {
        spriteConfig = new Dictionary<int, Sprite>();
        for (int i = 0; i < sweetPrefabs.Length; i++)
        {
            if (!spriteConfig.ContainsKey(sweetPrefabs[i].id))
            {
                spriteConfig.Add(sweetPrefabs[i].id, sweetPrefabs[i].sprite);
            }
        }
        GameObject.DontDestroyOnLoad(this);
        Application.targetFrameRate = 60;
        ManagerLauncher.Instance.Init();
    }
    public void Start()
    {
        ManagerLauncher.Instance.Start();
    }
    public void Update()
    {
        ManagerLauncher.Instance.Update();
    }
    public void LateUpdate()
    {
        ManagerLauncher.Instance.LateUpdate();
    }
    public void FixedUpdate()
    {
        ManagerLauncher.Instance.FixedUpdate();
    }
    public void OnDestroy()
    {
        ManagerLauncher.Instance.Destroy();
    }
}
