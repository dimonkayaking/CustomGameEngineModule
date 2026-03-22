using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Literals
{
    [System.Serializable, NodeMenuItem("Literals/Bool")]
    public class BoolNode : BaseValueNode
    {
        public override NodeType NodeType => NodeType.VariableBool;

        public bool boolValue;

        public override string name => "Bool";

        public override object GetValue() => boolValue;

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            if (bool.TryParse(data.Value, out bool result))
            {
                boolValue = result;
            }
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = boolValue ? "true" : "false";
            return data;
        }
    }
}