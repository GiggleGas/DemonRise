using ppCore.Manager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEntry : MonoBehaviour
{
    public void Awake()
    {
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
