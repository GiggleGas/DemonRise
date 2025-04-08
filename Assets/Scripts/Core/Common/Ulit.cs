using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public static class Utilit
    {
        public static Transform FindInChildren(this GameObject go, string name)
        {
            foreach (Transform x in go.GetComponentsInChildren<Transform>())
            {
                if (x.gameObject.name == name)
                {
                    return x;
                }
            }
            return null;
        }
    }
}