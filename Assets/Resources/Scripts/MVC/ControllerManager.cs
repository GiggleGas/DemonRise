using ppCore.Common;
using ppCore.Manager;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PDR
{
    [Manager(ManagerPriority.Delay)]
    public class ControllerManager : ppCore.Common.Singleton<ControllerManager>, IManager
    {
        private Dictionary<int, BaseController> _modules;


        public void RegisterController(ControllerType controllerType, BaseController baseController)
        {
            RegisterController((int)controllerType, baseController);
        }

        public void RegisterController(int controllerType, BaseController baseController)
        {
            if(_modules.ContainsKey(controllerType) == false)
            {
                _modules.Add(controllerType, baseController);
            }
        }

        public void UnregisterController(int controllerType)
        {
            if (_modules.ContainsKey(controllerType))
            {
                _modules.Remove(controllerType);
            }
        }

        public void Clear()
        {
            _modules.Clear();
        }

        public void ClearAllModules()
        {
            List<int> keys = _modules.Keys.ToList();
            foreach (int key in keys)
            {
                _modules[key].Destroy();
                _modules.Remove(key);
            }
        }

        public void ApplyFunc(ControllerType controllerType, string eventName, params object[] args)
        {
            ApplyFunc((int)controllerType, eventName, args);
        }

        // 跨模块触发事件
        public void ApplyFunc(int controllerType, string eventName, params object[] args)
        {
            if(_modules.ContainsKey(controllerType))
            {
                _modules[controllerType].ApplyFunc(eventName, args);
            }
        }

        public BaseModel GetControllerModel(ControllerType controllerType)
        {
            return GetControllerModel((int)controllerType);
        }
        public BaseModel GetControllerModel(int controllerType)
        {
            if (_modules.ContainsKey(controllerType))
            {
                return _modules[controllerType].GetModel();
            }
            return null;
        }
    }

}
