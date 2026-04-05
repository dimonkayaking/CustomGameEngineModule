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
        public string message;

        public override string name => "Console.WriteLine";

        protected override void Process()
        {
            if (message != null)
            {
                UnityEngine.Debug.Log(message);
            }
        }
    }
}