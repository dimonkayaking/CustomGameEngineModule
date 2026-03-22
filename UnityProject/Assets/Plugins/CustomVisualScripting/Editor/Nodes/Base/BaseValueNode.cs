using System;
using UnityEngine;
using GraphProcessor;

namespace CustomVisualScripting.Editor.Nodes.Base
{
    [Serializable]
    public abstract class BaseValueNode : BaseNode
    {
        [Output("value")]
        public object value;

        public abstract object GetValue();
    }
}