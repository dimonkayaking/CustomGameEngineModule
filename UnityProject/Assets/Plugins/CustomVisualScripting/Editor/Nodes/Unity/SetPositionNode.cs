using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Unity
{
    [System.Serializable, NodeMenuItem("Unity/Set Transform Position")]
    public class SetPositionNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.TransformPositionSet;

        [Input("value")]
        public Vector3 value;

        public override string name => "Set Position";
    }
}