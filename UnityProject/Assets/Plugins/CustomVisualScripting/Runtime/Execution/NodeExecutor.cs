using System;
using System.Collections.Generic;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Runtime.Components;

namespace CustomVisualScripting.Runtime.Execution
{
    public static class NodeExecutor
    {
        public static string Execute(NodeData node, Dictionary<string, object> inputs, Dictionary<string, object> variables, VisualScriptBehaviour context)
        {
            switch (node.Type)
            {
                case NodeType.VariableDeclaration:
                case NodeType.VariableAssignment:
                    object val = inputs.ContainsKey("value") ? inputs["value"] : GetParsedValue(node.Value, node.ValueType);
                    variables[node.Value] = val;
                    return GetNextNodeId(node, "next");

                case NodeType.IfStatement:
                    bool condition = inputs.ContainsKey("condition") && inputs["condition"] is bool b ? b : false;
                    return GetNextNodeId(node, condition ? "true" : "false");

                case NodeType.TransformPositionSet:
                    Vector3 pos = inputs.ContainsKey("value") && inputs["value"] is Vector3 v ? v : Vector3.zero;
                    context.transform.position = pos;
                    return GetNextNodeId(node, "next");

                case NodeType.DebugLog:
                    object msg = inputs.ContainsKey("message") ? inputs["message"] : "";
                    Debug.Log(msg);
                    return GetNextNodeId(node, "next");
            }
            return null;
        }

        public static object EvaluateValue(NodeData node, Dictionary<string, object> inputs, Dictionary<string, object> variables, VisualScriptBehaviour context)
        {
            switch (node.Type)
            {
                case NodeType.VariableInt: return int.TryParse(node.Value, out int iv) ? iv : 0;
                case NodeType.VariableFloat: return float.TryParse(node.Value.TrimEnd('f', 'F'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fv) ? fv : 0f;
                case NodeType.VariableString: return node.Value.Trim('"');
                case NodeType.VariableBool: return bool.TryParse(node.Value, out bool bv) && bv;
                
                case NodeType.VariableRead:
                    return variables.ContainsKey(node.Value) ? variables[node.Value] : null;

                case NodeType.MathAdd:
                    return ExecuteMath(node, inputs, (a, b) => a + b);
                case NodeType.MathSubtract:
                    return ExecuteMath(node, inputs, (a, b) => a - b);
                case NodeType.MathMultiply:
                    return ExecuteMath(node, inputs, (a, b) => a * b);
                case NodeType.MathDivide:
                    return ExecuteMath(node, inputs, (a, b) => a / b);

                case NodeType.CompareGreater:
                    return ExecuteComparison(node, inputs, (a, b) => a > b);
                case NodeType.CompareLess:
                    return ExecuteComparison(node, inputs, (a, b) => a < b);
                case NodeType.CompareEqual:
                    return ExecuteComparison(node, inputs, (a, b) => Math.Abs(a - b) < 0.0001f);

                case NodeType.Vector3Create:
                    float x = inputs.ContainsKey("x") ? Convert.ToSingle(inputs["x"]) : 0f;
                    float y = inputs.ContainsKey("y") ? Convert.ToSingle(inputs["y"]) : 0f;
                    float z = inputs.ContainsKey("z") ? Convert.ToSingle(inputs["z"]) : 0f;
                    return new Vector3(x, y, z);

                case NodeType.TransformPositionRead:
                    return context.transform.position;
            }
            return null;
        }

        private static string GetNextNodeId(NodeData node, string portName)
        {
            if (node.ExecutionFlow.TryGetValue(portName, out string nextId))
            {
                return nextId;
            }
            return null;
        }

        private static object GetParsedValue(string value, string type)
        {
            if (string.IsNullOrEmpty(value)) return null;

            switch (type)
            {
                case "int": return int.TryParse(value, out int i) ? (object)i : null;
                case "float": return float.TryParse(value.TrimEnd('f', 'F'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f) ? (object)f : null;
                case "bool": return bool.TryParse(value, out bool b) ? (object)b : null;
                case "string": return value.Trim('"');
            }
            return null;
        }

        private static float ExecuteMath(NodeData node, Dictionary<string, object> inputs, Func<float, float, float> op)
        {
            float left = inputs.ContainsKey("left") ? Convert.ToSingle(inputs["left"]) : 0f;
            float right = inputs.ContainsKey("right") ? Convert.ToSingle(inputs["right"]) : 0f;
            return op(left, right);
        }

        private static bool ExecuteComparison(NodeData node, Dictionary<string, object> inputs, Func<float, float, bool> op)
        {
            float left = inputs.ContainsKey("left") ? Convert.ToSingle(inputs["left"]) : 0f;
            float right = inputs.ContainsKey("right") ? Convert.ToSingle(inputs["right"]) : 0f;
            return op(left, right);
        }
    }
}