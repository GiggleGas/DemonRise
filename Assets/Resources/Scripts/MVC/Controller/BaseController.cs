using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public class BaseController
    {
        private Dictionary<string, System.Action<object[]>> messages; // 模块事件

        protected BaseModel _baseModel;

        public BaseController()
        {
            messages = new Dictionary<string, System.Action<object[]>>();
        }

        public virtual void OnLoadView(IBaseView view)
        {

        }

        public virtual void OpenView(IBaseView view)
        {
        }

        public virtual void CloseView(IBaseView view)
        {

        }

        // 注册模块事件
        public void RegisterFunc(string eventName, System.Action<object[]> action) 
        {
            if(messages.ContainsKey(eventName))
            {
                messages[eventName] += action;
            }
            else
            {
                messages.Add(eventName, action);
            }
        }

        // 注销模块事件
        public void UnregisterFunc(string eventName)
        {
            if(messages.ContainsKey(eventName))
            {
                messages.Remove(eventName);
            }
        }

        public void ApplyFunc(string eventName, params object[] args)
        {
            if(messages.ContainsKey(eventName))
            {
                messages[eventName].Invoke(args);
            }
            else
            {
                Debug.Log("error:" + eventName);
            }
        }

        public void ApplyControllerFunc(int controllerKey, string eventName, params object[] args)
        {
            ControllerManager.Instance.ApplyFunc(controllerKey, eventName, args);
        }

        public void SetModel(BaseModel model)
        {
            _baseModel = model;
        }

        public BaseModel GetModel()
        {
            return _baseModel;
        }

        public T GetModel<T>() where T : BaseModel
        {
            return GetModel<T>();
        }

        public BaseModel GetControllerModel(int controllerKey)
        {
            return ControllerManager.Instance.GetControllerModel(controllerKey);
        }

        public virtual void Destroy()
        {
            RemoveGlobalEvent();
            RemoveModuleEvent();
        }

        public virtual void InitModuleEvent()
        {

        }

        public virtual void RemoveModuleEvent()
        {

        }
        public virtual void InitGlobalEvent()
        {

        }
        public virtual void RemoveGlobalEvent()
        {

        }
    }
}
