using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Debug
{
    [System.Serializable, NodeMenuItem("Debug/Debug Log")]
    public class DebugLogNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.DebugLog;

        [Input("message")]
        public object message;

        [Output("output")]
        public object output;

        public override string name => "Debug Log";

        protected override void Process()
        {
            output = message;
            if (message != null)
            {
                UnityEngine.Debug.Log($"[VS] {message}");
            }
        }
    }
}