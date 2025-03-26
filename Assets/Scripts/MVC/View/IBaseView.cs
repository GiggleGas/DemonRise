using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public interface IBaseView
    {
        BaseController _controller { get; set; }

        bool IsInit();

        bool IsOpen();

        bool InitUI();

        bool InitData();

        void Open(params object[] args);

        void Close(params object[] args);

        void Destroy();

        // ִ�б�ģ�����
        void ApplyFunc(string eventName, params System.Object[] args);

        void ApplyControllerFunc(int controllerKey, string eventName, params System.Object[] args);

        void SetVisible(bool bVisible);

        int ViewID { get; set; }
    }
}
