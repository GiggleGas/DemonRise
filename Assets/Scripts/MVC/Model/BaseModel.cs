using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public class BaseModel
    {
        public BaseController _controller;

        public BaseModel(BaseController baseController) 
        {
            _controller = baseController;
        }

    }
}
