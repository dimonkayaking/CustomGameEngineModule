using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Runtime.Components;

namespace CustomVisualScripting.Runtime.Execution
{
    public class GraphRunner
    {
        private GraphData graph;
        private Dictionary<string, object> variables = new Dictionary<string, object>();
        private string currentNodeId;
        private VisualScriptBehaviour context;

        public GraphRunner(GraphData graphData, VisualScriptBehaviour context)
        {
            this.graph = graphData;
            this.context = context;
        }

        public void Start()
        {
            var allExecTargets = new HashSet<string>();
            foreach (var node in graph.Nodes)
            {
                foreach (var flow in node.ExecutionFlow.Values)
                {
                    allExecTargets.Add(flow);
                }
            }

            var startNodes = graph.Nodes.Where(n => 
                (n.Type == NodeType.VariableDeclaration || 
                 n.Type == NodeType.VariableAssignment ||
                 n.Type == NodeType.IfStatement ||
                 n.Type == NodeType.TransformPositionSet ||
                 n.Type == NodeType.DebugLog) && 
                !allExecTargets.Contains(n.Id)).ToList();

            if (startNodes.Count > 0)
            {
                currentNodeId = startNodes[0].Id;
                ExecuteNext();
            }
        }

        public void Update()
        {
            // For future usage (e.g., coroutines or async steps).
        }

        private void ExecuteNext()
        {
            while (!string.IsNullOrEmpty(currentNodeId))
            {
                var node = graph.Nodes.FirstOrDefault(n => n.Id == currentNodeId);
                if (node == null) break;

                currentNodeId = ExecuteNode(node);
            }
        }

        private string ExecuteNode(NodeData node)
        {
            var inputs = new Dictionary<string, object>();
            
            foreach (var input in node.InputConnections)
            {
                inputs[input.Key] = GetValueFromPort(input.Value, input.Key);
            }

            return NodeExecutor.Execute(node, inputs, variables, context);
        }

        private object GetValueFromPort(string nodeId, string portName)
        {
            var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null) return null;

            var inputs = new Dictionary<string, object>();
            foreach (var input in node.InputConnections)
            {
                inputs[input.Key] = GetValueFromPort(input.Value, input.Key);
            }

            return NodeExecutor.EvaluateValue(node, inputs, variables, context);
        }
    }
}