using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Literals
{
    [System.Serializable, NodeMenuItem("Literals/Float")]
    public class FloatNode : BaseValueNode
    {
        public override NodeType NodeType => NodeType.VariableFloat;

        public float floatValue;

        public override string name => "Float";

        public override object GetValue() => floatValue;

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            string val = data.Value.TrimEnd('f', 'F');
            if (float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result))
            {
                floatValue = result;
            }
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";
            return data;
        }
    }
}