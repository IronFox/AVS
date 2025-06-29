﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVS
{
    public class ControlPanelButton : HandTarget, IHandTarget
    {
        private System.Action myClickFunction;
        private System.Action myHoverFunction;
        public void Init(System.Action clickFunc, System.Action hoverFunc)
        {
            myClickFunction = clickFunc;
            myHoverFunction = hoverFunc;
        }

        void IHandTarget.OnHandClick(GUIHand hand)
        {
            myClickFunction();
        }

        void IHandTarget.OnHandHover(GUIHand hand)
        {
            myHoverFunction();
        }
    }
}
