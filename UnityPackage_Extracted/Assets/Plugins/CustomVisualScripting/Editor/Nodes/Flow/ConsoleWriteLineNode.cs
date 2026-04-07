using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/Console.WriteLine")]
    public class ConsoleWriteLineNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.ConsoleWriteLine;

        [Input("message")]
        public object message;

        public string messageText = "";

        public override string name => "Console.WriteLine";

        protected override void Process()
        {
            var text = message?.ToString() ?? messageText ?? "";
            if (!string.IsNullOrEmpty(text))
            {
                UnityEngine.Debug.Log(text);
            }
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            messageText = data.Value ?? "";
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = messageText ?? "";
            return data;
        }
    }
}