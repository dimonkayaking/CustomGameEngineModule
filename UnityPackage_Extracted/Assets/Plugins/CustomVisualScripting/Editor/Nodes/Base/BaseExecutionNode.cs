using System;
using GraphProcessor;

namespace CustomVisualScripting.Editor.Nodes.Base
{
    [Serializable]
    public abstract class BaseExecutionNode : CustomBaseNode
    {
        [Input("execIn")]
        public object execIn;

        [Output("execOut")]
        public object execOut;
    }
}