using System;
using System.Collections.Generic;
using System.Linq;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Runtime.Execution
{
    public class NodeExecutor
    {
        private readonly Dictionary<string, object> _variables = new Dictionary<string, object>();

        public object ExecuteNode(NodeData node, Dictionary<string, object> context, GraphData graph = null)
        {
            if (node == null) return null;

            try
            {
                return node.Type switch
                {
                    NodeType.LiteralInt => int.Parse(node.Value),
                    NodeType.LiteralFloat => float.Parse(node.Value),
                    NodeType.LiteralBool => bool.Parse(node.Value),
                    NodeType.LiteralString => node.Value,
                    NodeType.MathAdd => MathBin(node, graph, context, (a, b) => Add(a, b)),
                    NodeType.MathSubtract => MathBin(node, graph, context, (a, b) => Sub(a, b)),
                    NodeType.MathMultiply => MathBin(node, graph, context, (a, b) => Mul(a, b)),
                    NodeType.MathDivide => MathBin(node, graph, context, (a, b) => Div(a, b)),
                    NodeType.MathModulo => MathBin(node, graph, context, (a, b) => Mod(a, b)),
                    NodeType.CompareEqual => CmpBin(node, graph, context, (a, b) => Equals(a, b)),
                    NodeType.CompareNotEqual => CmpBin(node, graph, context, (a, b) => !Equals(a, b)),
                    NodeType.CompareGreater => CmpNum(node, graph, context, (a, b) => ToDouble(a) > ToDouble(b)),
                    NodeType.CompareLess => CmpNum(node, graph, context, (a, b) => ToDouble(a) < ToDouble(b)),
                    NodeType.CompareGreaterOrEqual => CmpNum(node, graph, context, (a, b) => ToDouble(a) >= ToDouble(b)),
                    NodeType.CompareLessOrEqual => CmpNum(node, graph, context, (a, b) => ToDouble(a) <= ToDouble(b)),
                    NodeType.LogicalAnd => LogicBin(node, graph, context, (a, b) => ToBool(a) && ToBool(b)),
                    NodeType.LogicalOr => LogicBin(node, graph, context, (a, b) => ToBool(a) || ToBool(b)),
                    NodeType.LogicalNot => !ToBool(GetPort(node, graph, context, "input")),
                    NodeType.ConsoleWriteLine => ExecConsoleWriteLine(node, graph, context),
                    NodeType.IntParse => ExecIntParse(node, graph, context),
                    NodeType.FloatParse => ExecFloatParse(node, graph, context),
                    NodeType.ToStringConvert => ExecToString(node, graph, context),
                    NodeType.MathfAbs => UnityEngine.Mathf.Abs(ToSingle(GetPort(node, graph, context, "input"))),
                    NodeType.MathfMax => UnityEngine.Mathf.Max(
                        ToSingle(GetPort(node, graph, context, "inputA")),
                        ToSingle(GetPort(node, graph, context, "inputB"))),
                    NodeType.MathfMin => UnityEngine.Mathf.Min(
                        ToSingle(GetPort(node, graph, context, "inputA")),
                        ToSingle(GetPort(node, graph, context, "inputB"))),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[NodeExecutor] Ошибка выполнения узла {node.Id}: {ex.Message}");
                return null;
            }
        }

        private static object MathBin(NodeData node, GraphData graph, Dictionary<string, object> ctx, Func<object, object, object> op)
        {
            var a = GetPort(node, graph, ctx, "inputA");
            var b = GetPort(node, graph, ctx, "inputB");
            return op(a, b);
        }

        private static object CmpBin(NodeData node, GraphData graph, Dictionary<string, object> ctx, Func<object, object, bool> op)
        {
            var a = GetPort(node, graph, ctx, "left");
            var b = GetPort(node, graph, ctx, "right");
            return op(a, b);
        }

        private static object CmpNum(NodeData node, GraphData graph, Dictionary<string, object> ctx, Func<object, object, bool> op)
        {
            var a = GetPort(node, graph, ctx, "left");
            var b = GetPort(node, graph, ctx, "right");
            return op(a, b);
        }

        private static object LogicBin(NodeData node, GraphData graph, Dictionary<string, object> ctx, Func<object, object, bool> op)
        {
            var a = GetPort(node, graph, ctx, "left");
            var b = GetPort(node, graph, ctx, "right");
            return op(a, b);
        }

        private static object Add(object left, object right)
        {
            if (left is int l && right is int r) return l + r;
            if (left is float lf && right is float rf) return lf + rf;
            if (left is int li && right is float rf2) return li + rf2;
            if (left is float lf2 && right is int ri) return lf2 + ri;
            return 0;
        }

        private static object Sub(object left, object right)
        {
            if (left is int l && right is int r) return l - r;
            if (left is float lf && right is float rf) return lf - rf;
            if (left is int li && right is float rf2) return li - rf2;
            if (left is float lf2 && right is int ri) return lf2 - ri;
            return 0;
        }

        private static object Mul(object left, object right)
        {
            if (left is int l && right is int r) return l * r;
            if (left is float lf && right is float rf) return lf * rf;
            if (left is int li && right is float rf2) return li * rf2;
            if (left is float lf2 && right is int ri) return lf2 * ri;
            return 0;
        }

        private static object Div(object left, object right)
        {
            if (ToDouble(right) == 0) return 0;
            if (left is int l && right is int r) return l / r;
            if (left is float lf && right is float rf) return lf / rf;
            return Convert.ToSingle(ToDouble(left) / ToDouble(right));
        }

        private static object Mod(object left, object right)
        {
            if (ToDouble(right) == 0) return 0;
            if (left is int l && right is int r) return l % r;
            return Convert.ToSingle(ToDouble(left) % ToDouble(right));
        }

        private static double ToDouble(object v) => v switch
        {
            int i => i,
            float f => f,
            double d => d,
            _ => 0
        };

        private static bool ToBool(object v) => v switch
        {
            bool b => b,
            int i => i != 0,
            _ => false
        };

        private static float ToSingle(object v) => v switch
        {
            float f => f,
            int i => i,
            double d => (float)d,
            _ => 0f
        };

        private static object? ExecConsoleWriteLine(NodeData node, GraphData graph, Dictionary<string, object> ctx)
        {
            var msg = GetPort(node, graph, ctx, "message");
            UnityEngine.Debug.Log(msg?.ToString() ?? "");
            return null;
        }

        private static object? ExecIntParse(NodeData node, GraphData graph, Dictionary<string, object> ctx)
        {
            var s = GetPort(node, graph, ctx, "input")?.ToString();
            return int.TryParse(s, out var n) ? n : 0;
        }

        private static object? ExecFloatParse(NodeData node, GraphData graph, Dictionary<string, object> ctx)
        {
            var s = GetPort(node, graph, ctx, "input")?.ToString();
            return float.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var f)
                ? f
                : 0f;
        }

        private static object? ExecToString(NodeData node, GraphData graph, Dictionary<string, object> ctx)
        {
            var v = GetPort(node, graph, ctx, "input");
            return v?.ToString() ?? "";
        }

        private static object GetPort(NodeData node, GraphData graph, Dictionary<string, object> context, string portName)
        {
            if (node.InputConnections != null && node.InputConnections.TryGetValue(portName, out var legacyId))
            {
                if (context != null && context.TryGetValue(legacyId, out var v))
                    return v;
            }

            if (graph?.Edges == null || context == null)
                return null;

            var edge = graph.Edges.FirstOrDefault(e => e.ToNodeId == node.Id && e.ToPort == portName);
            if (edge == null)
                return null;

            return context.TryGetValue(edge.FromNodeId, out var val) ? val : null;
        }

        public void SetVariable(string name, object value)
        {
            _variables[name] = value;
        }

        public object GetVariable(string name)
        {
            return _variables.TryGetValue(name, out var v) ? v : null;
        }
    }
}
