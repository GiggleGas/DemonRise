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
        public string type;
        public Sprite sprite;
    }

    public spriteStruct[] sweetPrefabs;
    public Dictionary<string, Sprite> spriteConfig;

    public GameObject blockUIPrefab;
    public GameObject heroGoPrefab;

    public void Awake()
    {
        spriteConfig = new Dictionary<string, Sprite>();
        for (int i = 0; i < sweetPrefabs.Length; i++)
        {
            if (!spriteConfig.ContainsKey(sweetPrefabs[i].type))
            {
                spriteConfig.Add(sweetPrefabs[i].type, sweetPrefabs[i].sprite);
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
