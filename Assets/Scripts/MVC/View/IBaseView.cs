using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public interface IBaseView
    {
        bool IsInit();

        bool IsOpen();

        void InitUI();

        void InitData();

        void Open(params object[] args);

        void Close(params object[] args);

        void Destroy();

        void SetVisible(bool bVisible);

        string ViewID { get; set; }
    }
}
