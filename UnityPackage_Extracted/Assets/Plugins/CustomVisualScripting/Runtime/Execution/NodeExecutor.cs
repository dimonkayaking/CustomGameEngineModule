using System;
using System.Collections.Generic;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Runtime.Execution
{
    public class NodeExecutor
    {
        private readonly Dictionary<string, object> _variables = new Dictionary<string, object>();
        
        public object ExecuteNode(NodeData node, Dictionary<string, object> inputs, Dictionary<string, object> variables = null)
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
                    
                    NodeType.MathAdd => GetFloat(inputs, "inputA") + GetFloat(inputs, "inputB"),
                    NodeType.MathSubtract => GetFloat(inputs, "inputA") - GetFloat(inputs, "inputB"),
                    NodeType.MathMultiply => GetFloat(inputs, "inputA") * GetFloat(inputs, "inputB"),
                    NodeType.MathDivide => GetFloat(inputs, "inputB") != 0 ? GetFloat(inputs, "inputA") / GetFloat(inputs, "inputB") : 0,
                    NodeType.MathModulo => GetFloat(inputs, "inputA") % GetFloat(inputs, "inputB"),
                    
                    NodeType.CompareEqual => Equals(GetObject(inputs, "left"), GetObject(inputs, "right")),
                    NodeType.CompareNotEqual => !Equals(GetObject(inputs, "left"), GetObject(inputs, "right")),
                    NodeType.CompareGreater => GetFloat(inputs, "left") > GetFloat(inputs, "right"),
                    NodeType.CompareLess => GetFloat(inputs, "left") < GetFloat(inputs, "right"),
                    NodeType.CompareGreaterOrEqual => GetFloat(inputs, "left") >= GetFloat(inputs, "right"),
                    NodeType.CompareLessOrEqual => GetFloat(inputs, "left") <= GetFloat(inputs, "right"),
                    
                    NodeType.LogicalAnd => GetBool(inputs, "left") && GetBool(inputs, "right"),
                    NodeType.LogicalOr => GetBool(inputs, "left") || GetBool(inputs, "right"),
                    NodeType.LogicalNot => !GetBool(inputs, "input"),
                    
                    NodeType.FlowIf => GetBool(inputs, "condition"),
                    NodeType.FlowFor => true,
                    NodeType.FlowWhile => GetBool(inputs, "condition"),
                    NodeType.ConsoleWriteLine => ExecuteConsoleWriteLine(inputs),
                    
                    NodeType.IntParse => int.TryParse(GetString(inputs, "input"), out var i) ? i : 0,
                    NodeType.FloatParse => float.TryParse(GetString(inputs, "input"), out var f) ? f : 0f,
                    NodeType.ToStringConvert => GetObject(inputs, "input")?.ToString() ?? "",
                    
                    NodeType.MathfAbs => UnityEngine.Mathf.Abs(GetFloat(inputs, "input")),
                    NodeType.MathfMax => UnityEngine.Mathf.Max(GetFloat(inputs, "inputA"), GetFloat(inputs, "inputB")),
                    NodeType.MathfMin => UnityEngine.Mathf.Min(GetFloat(inputs, "inputA"), GetFloat(inputs, "inputB")),
                    
                    NodeType.UnityVector3 => new UnityEngine.Vector3(GetFloat(inputs, "X"), GetFloat(inputs, "Y"), GetFloat(inputs, "Z")),
                    NodeType.UnityGetPosition => GetGameObject(inputs, "GameObject")?.transform.position ?? UnityEngine.Vector3.zero,
                    NodeType.UnitySetPosition => SetPositionAndReturn(inputs),
                    
                    _ => null
                };
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[NodeExecutor] Ошибка в {node.Type}: {ex.Message}");
                return null;
            }
        }
        
        private object ExecuteConsoleWriteLine(Dictionary<string, object> inputs)
        {
            var msg = GetObject(inputs, "message")?.ToString() ?? "";
            UnityEngine.Debug.Log($"[Console] {msg}");
            return null;
        }
        
        private object SetPositionAndReturn(Dictionary<string, object> inputs)
        {
            var go = GetGameObject(inputs, "GameObject");
            var pos = GetVector3(inputs, "Position");
            if (go != null) go.transform.position = pos;
            return go;
        }
        
        private float GetFloat(Dictionary<string, object> inputs, string key)
        {
            if (inputs.TryGetValue(key, out var val))
            {
                return val switch
                {
                    int i => i,
                    float f => f,
                    double d => (float)d,
                    string s => float.TryParse(s, out var f) ? f : 0f,
                    _ => 0f
                };
            }
            return 0f;
        }
        
        private bool GetBool(Dictionary<string, object> inputs, string key)
        {
            if (inputs.TryGetValue(key, out var val))
            {
                return val switch
                {
                    bool b => b,
                    int i => i != 0,
                    float f => f != 0,
                    _ => false
                };
            }
            return false;
        }
        
        private string GetString(Dictionary<string, object> inputs, string key)
        {
            return inputs.TryGetValue(key, out var val) ? val?.ToString() ?? "" : "";
        }
        
        private object GetObject(Dictionary<string, object> inputs, string key)
        {
            return inputs.TryGetValue(key, out var val) ? val : null;
        }
        
        private UnityEngine.GameObject GetGameObject(Dictionary<string, object> inputs, string key)
        {
            return inputs.TryGetValue(key, out var val) ? val as UnityEngine.GameObject : null;
        }
        
        private UnityEngine.Vector3 GetVector3(Dictionary<string, object> inputs, string key)
        {
            if (inputs.TryGetValue(key, out var val) && val is UnityEngine.Vector3 v)
                return v;
            return UnityEngine.Vector3.zero;
        }
        
        public void SetVariable(string name, object value)
        {
            _variables[name] = value;
        }
        
        public object GetVariable(string name)
        {
            return _variables.TryGetValue(name, out var val) ? val : null;
        }
        
        public void Clear()
        {
            _variables.Clear();
        }
    }
}