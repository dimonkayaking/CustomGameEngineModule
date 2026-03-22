using System;
using UnityEngine;
using GraphProcessor;

namespace CustomVisualScripting.Editor.Nodes.Base
{
    public struct Flow {}

    [Serializable]
    public abstract class BaseExecutionNode : BaseNode
    {
        [Input("execIn")]
        public Flow execIn;

        [Output("execOut")]
        public Flow execOut;
    }
}