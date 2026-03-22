using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Literals
{
    [System.Serializable, NodeMenuItem("Literals/String")]
    public class StringNode : BaseValueNode
    {
        public override NodeType NodeType => NodeType.VariableString;

        public string stringValue = "";

        public override string name => "String";

        public override object GetValue() => stringValue;

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            stringValue = data.Value.Trim('"');
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = "\"" + stringValue + "\"";
            return data;
        }
    }
}