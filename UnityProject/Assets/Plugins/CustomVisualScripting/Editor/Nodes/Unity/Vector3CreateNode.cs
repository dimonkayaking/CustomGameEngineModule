using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Unity
{
    [System.Serializable, NodeMenuItem("Unity/Vector3 Create")]
    public class Vector3CreateNode : BaseNode
    {
        public override NodeType NodeType => NodeType.Vector3Create;

        [Input("x")]
        public float x;

        [Input("y")]
        public float y;

        [Input("z")]
        public float z;

        [Output("result")]
        public Vector3 result;

        public override string name => "New Vector3";
    }
}