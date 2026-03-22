using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Unity
{
    [System.Serializable, NodeMenuItem("Unity/Get Transform Position")]
    public class GetPositionNode : BaseNode
    {
        public override NodeType NodeType => NodeType.TransformPositionRead;

        [Output("position")]
        public Vector3 position;

        public override string name => "Get Position";
    }
}