using ppCore.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace PDR
{
    public class ViewInfo
    {
        public string viewName;
        public Transform parentTransform;
        public int order;
    }

    [Manager(ManagerPriority.Delay)]
    public class ViewManager : ppCore.Common.Singleton<ViewManager>, IManager
    {
        public Transform canvasTransform;
        public Transform worldTransform;
        public Dictionary<int, IBaseView> _openedViews;
        public Dictionary<int, IBaseView> _cachedView;
        public Dictionary<int, ViewInfo> _views;

        public ViewManager()
        {
            canvasTransform = GameObject.Find("Canvas").transform;
            worldTransform = GameObject.Find("WorldCanvas").transform;
            _openedViews = new Dictionary<int, IBaseView>();
            _cachedView = new Dictionary<int, IBaseView>();
            _views = new Dictionary<int, ViewInfo>();
        }

        public void Register(ViewType key, ViewInfo view)
        {
            Register((int)key, view);
        }

        // ×¢²áView
        public void Register(int key, ViewInfo view)
        {
            if(_views.ContainsKey(key) == false)
            {
                _views.Add(key, view);
            }
        }

        //
        public void Unregister(int key)
        {
            if(_views.ContainsKey(key))
            {
                _views.Remove(key);
            }
        }

        //
        public void RemoveView(int key)
        {
            _views.Remove(key);
            _openedViews.Remove(key);
            _cachedView.Remove(key);
        }

        public bool IsOpen(int key)
        {
            return _openedViews.ContainsKey(key);
        }

        public IBaseView GetView(int key)
        {
            if(_openedViews.ContainsKey(key))
            {
                return _openedViews[key];
            }
            if(_cachedView.ContainsKey(key))
            {
                return _cachedView[key];
            }
            return null;
        }

        public T GetView<T>(ViewType key) where T : class, IBaseView
        {
            return GetView<T>((int)key);
        }
        public T GetView<T>(int key) where T : class, IBaseView
        {
            IBaseView view = GetView(key);
            if(view != null)
            {
                return view as T;
            }
            return null;
        }

        public void Destroy(int key)
        {
            IBaseView oldView = GetView(key);
            if (oldView != null)
            {
                Unregister(key);
                oldView.Destroy();
                _cachedView.Remove(key);
            }
        }

        public void Close(int key, params object[] args)
        {
            if(_openedViews.ContainsKey(key) == false)
            {
                return;
            }

            IBaseView view = GetView(key);
            if(view != null)
            {
                _openedViews.Remove(key);
                view.Close(args);
            }
        }

        public void Open(ViewType key, params object[] args)
        {
            Open((int)key, args);
        }
        public void Open(int key, params object[] args)
        {
            IBaseView view = GetView(key);
            if(view == null)
            {
                string type = ((ViewType)key).ToString();
                ViewInfo info = _views[key];
                GameObject UIPrefab = UnityEngine.Object.Instantiate(Resources.Load($"View/{type}"), info.parentTransform) as GameObject;
                Canvas canvas = UIPrefab.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = UIPrefab.AddComponent<Canvas>();
                }
                if(UIPrefab.GetComponent<GraphicRaycaster>() == null)
                {
                    UIPrefab.AddComponent<GraphicRaycaster>();
                }
                canvas.overrideSorting = true;
                view = UIPrefab.AddComponent(Type.GetType("PDR." + type)) as IBaseView;
                view.ViewID = type;
                _cachedView.Add(key, view);
            }

            if(_openedViews.ContainsKey(key))
            {
                return;
            }
            _openedViews.Add(key, view);

            if(view.IsInit() == false)
            {
                view.InitUI();
                view.InitData();
            }
            view.Open(args);
            view.SetVisible(true);
        }
    }
}
