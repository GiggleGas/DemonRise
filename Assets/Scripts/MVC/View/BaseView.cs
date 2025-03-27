using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


namespace PDR
{
    public class BaseView : MonoBehaviour, IBaseView
    {
        public string ViewID { get; set; }

        private bool _isInit = false;
        protected Canvas _canvas;

        void Awake()
        {
            _canvas = gameObject.GetComponent<Canvas>();
            OnAwake();
        }

        void Start()
        {
            OnStart();
        }

        protected virtual void OnAwake()
        {

        }

        protected virtual void OnStart()
        {

        }

        public virtual void Close(params object[] args)
        {
            SetVisible(false);
        }

        public virtual void Destroy()
        {
            Destroy(gameObject);
        }

        public virtual void InitData()
        {
            _isInit = true;
        }

        public virtual void InitUI()
        {
        }

        public bool IsInit()
        {
            return _isInit;
        }

        public bool IsOpen()
        {
            return _canvas.enabled == true;
        }

        public virtual void Open(params object[] args)
        {
        }

        public void SetVisible(bool bVisible)
        {
            _canvas.enabled = bVisible;
        }
    }
}
