using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    public class SubGraphPanel : VisualElement
    {
        public event Action OnChanged;

        private readonly string _title;
        private readonly bool _isConditionPanel;
        private GraphData _subGraph;

        private Label _toggleLabel;
        private VisualElement _content;
        private VisualElement _canvas;
        private VisualElement _connectionLayer;
        private VisualElement _nodesLayer;

        private bool _isExpanded = true;

        private string _connectingFromNodeId;
        private string _connectingFromPort;

        private const float NodeWidth = 150f;
        private const float TitleHeight = 22f;
        private const float TypeBarHeight = 14f;
        private const float PortRowHeight = 18f;
        private const float PortRadius = 5f;
        private const float HSpacing = 50f;
        private const float VSpacing = 14f;
        private const float Padding = 14f;

        private readonly Dictionary<string, Vector2> _nodePositions = new();
        private readonly Dictionary<string, VisualElement> _nodeElements = new();
        private readonly Dictionary<string, Dictionary<string, VisualElement>> _portElements = new();

        private int _connectionRetryCount;
        private const int MaxConnectionRetries = 5;

        public SubGraphPanel(string title, GraphData subGraph, bool isConditionPanel)
        {
            _title = title;
            _subGraph = subGraph ?? new GraphData();
            _isConditionPanel = isConditionPanel;

            BuildUI();
            Rebuild();
        }

        public GraphData SubGraph => _subGraph;

        public void SetSubGraph(GraphData subGraph)
        {
            _subGraph = subGraph ?? new GraphData();
            Rebuild();
        }

        private void BuildUI()
        {
            style.marginTop = 4;
            style.marginBottom = 4;
            style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 1;
            style.borderTopColor = style.borderBottomColor = style.borderLeftColor = style.borderRightColor =
                new Color(0.3f, 0.3f, 0.3f);
            style.borderTopLeftRadius = style.borderTopRightRadius =
                style.borderBottomLeftRadius = style.borderBottomRightRadius = 4;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            header.style.paddingLeft = 8;
            header.style.paddingRight = 8;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            header.style.borderTopLeftRadius = 4;
            header.style.borderTopRightRadius = 4;

            _toggleLabel = new Label("\u25BC");
            _toggleLabel.style.width = 16;
            _toggleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _toggleLabel.style.fontSize = 10;
            _toggleLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            header.Add(_toggleLabel);

            var titleLabel = new Label(_title);
            titleLabel.style.flexGrow = 1;
            titleLabel.style.color = Color.white;
            titleLabel.style.fontSize = 11;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(titleLabel);

            var addBtn = new Label("+");
            addBtn.style.width = 20;
            addBtn.style.height = 20;
            addBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            addBtn.style.fontSize = 14;
            addBtn.style.color = new Color(0.5f, 0.9f, 0.5f);
            addBtn.RegisterCallback<MouseDownEvent>(e =>
            {
                ShowAddNodeMenu();
                e.StopPropagation();
            });
            header.Add(addBtn);

            header.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == 0 && e.target != addBtn)
                {
                    ToggleExpanded();
                    e.StopPropagation();
                }
            });

            Add(header);

            _content = new VisualElement();
            _content.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            _content.style.borderBottomLeftRadius = 4;
            _content.style.borderBottomRightRadius = 4;
            _content.style.minHeight = 60;
            _content.style.overflow = Overflow.Hidden;

            _canvas = new VisualElement();
            _canvas.style.position = Position.Relative;
            _canvas.style.minHeight = 60;

            _connectionLayer = new VisualElement();
            _connectionLayer.style.position = Position.Absolute;
            _connectionLayer.style.left = 0;
            _connectionLayer.style.top = 0;
            _connectionLayer.style.right = 0;
            _connectionLayer.style.bottom = 0;
            _connectionLayer.pickingMode = PickingMode.Ignore;
            _canvas.Add(_connectionLayer);

            _nodesLayer = new VisualElement();
            _nodesLayer.style.position = Position.Absolute;
            _nodesLayer.style.left = 0;
            _nodesLayer.style.top = 0;
            _nodesLayer.style.right = 0;
            _nodesLayer.style.bottom = 0;
            _canvas.Add(_nodesLayer);

            _canvas.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == 1)
                {
                    ShowAddNodeMenu();
                    e.StopPropagation();
                }

                if (_connectingFromNodeId != null)
                {
                    CancelConnection();
                    e.StopPropagation();
                }
            });

            _content.Add(_canvas);
            Add(_content);
        }

        private void ToggleExpanded()
        {
            _isExpanded = !_isExpanded;
            _content.style.display = _isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            _toggleLabel.text = _isExpanded ? "\u25BC" : "\u25B6";
        }

        public void Rebuild()
        {
            _nodeElements.Clear();
            _portElements.Clear();
            _nodePositions.Clear();
            _nodesLayer.Clear();
            _connectionLayer.Clear();
            _connectionRetryCount = 0;

            if (_subGraph == null || _subGraph.Nodes.Count == 0)
            {
                var emptyLabel = new Label(_isConditionPanel
                    ? "Добавьте ноды условия"
                    : "Добавьте ноды тела");
                emptyLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                emptyLabel.style.fontSize = 10;
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLabel.style.paddingTop = 20;
                emptyLabel.style.paddingBottom = 20;
                _nodesLayer.Add(emptyLabel);
                _canvas.style.minHeight = 60;
                return;
            }

            ComputeLayout();
            CreateNodeElements();

            ScheduleConnectionDraw();
        }

        private void ScheduleConnectionDraw()
        {
            _canvas.RegisterCallbackOnce<GeometryChangedEvent>(_ => TryDrawConnections());
            schedule.Execute(TryDrawConnections).ExecuteLater(100);
        }

        private void TryDrawConnections()
        {
            _connectionLayer.Clear();

            bool allDrawn = true;
            foreach (var edge in _subGraph.Edges)
            {
                var fromPos = GetPortWorldPos(edge.FromNodeId, edge.FromPort, false);
                var toPos = GetPortWorldPos(edge.ToNodeId, edge.ToPort, true);

                if (fromPos == null || toPos == null)
                {
                    allDrawn = false;
                    continue;
                }

                _connectionLayer.Add(CreateLine(fromPos.Value, toPos.Value));
            }

            if (!allDrawn && _connectionRetryCount < MaxConnectionRetries)
            {
                _connectionRetryCount++;
                schedule.Execute(TryDrawConnections).ExecuteLater(50 * _connectionRetryCount);
            }
        }

        private void ComputeLayout()
        {
            var layers = new List<List<string>>();
            var nodeLayer = new Dictionary<string, int>();
            var inDegree = new Dictionary<string, int>();

            foreach (var node in _subGraph.Nodes)
                inDegree[node.Id] = 0;

            foreach (var edge in _subGraph.Edges)
            {
                if (inDegree.ContainsKey(edge.ToNodeId))
                    inDegree[edge.ToNodeId]++;
            }

            var queue = new Queue<string>();
            foreach (var kvp in inDegree.Where(k => k.Value == 0))
                queue.Enqueue(kvp.Key);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                int maxParentLayer = -1;
                foreach (var edge in _subGraph.Edges.Where(e => e.ToNodeId == current))
                {
                    if (nodeLayer.TryGetValue(edge.FromNodeId, out var pl))
                        maxParentLayer = Mathf.Max(maxParentLayer, pl);
                }

                var layer = maxParentLayer + 1;
                nodeLayer[current] = layer;

                while (layers.Count <= layer)
                    layers.Add(new List<string>());
                layers[layer].Add(current);

                foreach (var edge in _subGraph.Edges.Where(e => e.FromNodeId == current))
                {
                    if (!inDegree.ContainsKey(edge.ToNodeId)) continue;
                    inDegree[edge.ToNodeId]--;
                    if (inDegree[edge.ToNodeId] <= 0)
                        queue.Enqueue(edge.ToNodeId);
                }
            }

            foreach (var node in _subGraph.Nodes)
            {
                if (nodeLayer.ContainsKey(node.Id)) continue;
                if (layers.Count == 0)
                    layers.Add(new List<string>());
                layers[0].Add(node.Id);
                nodeLayer[node.Id] = 0;
            }

            float x = Padding;
            float maxHeight = 0;

            for (int i = 0; i < layers.Count; i++)
            {
                float y = Padding;
                foreach (var nodeId in layers[i])
                {
                    var node = _subGraph.Nodes.First(n => n.Id == nodeId);
                    float h = GetNodeHeight(node);
                    _nodePositions[nodeId] = new Vector2(x, y);
                    y += h + VSpacing;
                }

                maxHeight = Mathf.Max(maxHeight, y);
                x += NodeWidth + HSpacing;
            }

            _canvas.style.minHeight = maxHeight + Padding;
            _canvas.style.minWidth = x + Padding;
        }

        private float GetNodeHeight(NodeData node)
        {
            var inputs = GetInputPorts(node.Type);
            var outputs = GetOutputPorts(node.Type);
            int maxPorts = Mathf.Max(inputs.Count, outputs.Count);
            return TitleHeight + TypeBarHeight + Mathf.Max(maxPorts, 1) * PortRowHeight + 4;
        }

        private void CreateNodeElements()
        {
            foreach (var node in _subGraph.Nodes)
            {
                if (!_nodePositions.TryGetValue(node.Id, out var pos))
                    continue;

                var nodeEl = CreateMiniNode(node, pos);
                _nodesLayer.Add(nodeEl);
                _nodeElements[node.Id] = nodeEl;
            }
        }

        private VisualElement CreateMiniNode(NodeData node, Vector2 pos)
        {
            var color = GetNodeColor(node.Type);
            float height = GetNodeHeight(node);

            var container = new VisualElement();
            container.style.position = Position.Absolute;
            container.style.left = pos.x;
            container.style.top = pos.y;
            container.style.width = NodeWidth;
            container.style.height = height;
            container.style.borderTopLeftRadius = container.style.borderTopRightRadius =
                container.style.borderBottomLeftRadius = container.style.borderBottomRightRadius = 4;
            container.style.overflow = Overflow.Hidden;
            container.style.borderTopWidth = container.style.borderBottomWidth =
                container.style.borderLeftWidth = container.style.borderRightWidth = 1;
            container.style.borderTopColor = container.style.borderBottomColor =
                container.style.borderLeftColor = container.style.borderRightColor = new Color(0.15f, 0.15f, 0.15f);

            var titleBar = new VisualElement();
            titleBar.style.backgroundColor = color;
            titleBar.style.height = TitleHeight;
            titleBar.style.flexDirection = FlexDirection.Row;
            titleBar.style.alignItems = Align.Center;
            titleBar.style.paddingLeft = 6;
            titleBar.style.paddingRight = 2;

            var titleLabel = new Label(FormatNodeTitle(node));
            titleLabel.style.fontSize = 10;
            titleLabel.style.color = Color.white;
            titleLabel.style.flexGrow = 1;
            titleLabel.style.overflow = Overflow.Hidden;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleBar.Add(titleLabel);

            var deleteBtn = new Label("\u00D7");
            deleteBtn.style.fontSize = 12;
            deleteBtn.style.color = new Color(1f, 0.6f, 0.6f);
            deleteBtn.style.width = 16;
            deleteBtn.style.height = 16;
            deleteBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            deleteBtn.RegisterCallback<MouseDownEvent>(e =>
            {
                RemoveNode(node.Id);
                e.StopPropagation();
            });
            titleBar.Add(deleteBtn);
            container.Add(titleBar);

            var typeBar = new VisualElement();
            typeBar.style.backgroundColor = new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f);
            typeBar.style.height = TypeBarHeight;
            typeBar.style.paddingLeft = 6;
            typeBar.style.justifyContent = Justify.Center;

            var typeLabel = new Label(FormatNodeSubtitle(node));
            typeLabel.style.fontSize = 8;
            typeLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
            typeBar.Add(typeLabel);
            container.Add(typeBar);

            var portArea = new VisualElement();
            portArea.style.flexGrow = 1;
            portArea.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            portArea.style.flexDirection = FlexDirection.Row;

            var inputCol = new VisualElement();
            inputCol.style.flexGrow = 1;
            inputCol.style.justifyContent = Justify.Center;
            inputCol.style.paddingLeft = 2;

            _portElements[node.Id] = new Dictionary<string, VisualElement>();

            foreach (var port in GetInputPorts(node.Type))
            {
                var portRow = CreatePortElement(node.Id, port.name, port.displayName, port.typeName, true);
                inputCol.Add(portRow);
            }

            portArea.Add(inputCol);

            var outputCol = new VisualElement();
            outputCol.style.flexGrow = 1;
            outputCol.style.justifyContent = Justify.Center;
            outputCol.style.alignItems = Align.FlexEnd;
            outputCol.style.paddingRight = 2;

            foreach (var port in GetOutputPorts(node.Type))
            {
                var portRow = CreatePortElement(node.Id, port.name, port.displayName, port.typeName, false);
                outputCol.Add(portRow);
            }

            portArea.Add(outputCol);
            container.Add(portArea);

            MakeDraggable(container, node.Id);

            return container;
        }

        private VisualElement CreatePortElement(string nodeId, string portName, string displayName, string typeName, bool isInput)
        {
            var row = new VisualElement();
            row.style.flexDirection = isInput ? FlexDirection.Row : FlexDirection.RowReverse;
            row.style.alignItems = Align.Center;
            row.style.height = PortRowHeight;

            var isExec = portName == "execIn" || portName == "execOut";

            var circle = new VisualElement();
            circle.style.width = PortRadius * 2;
            circle.style.height = PortRadius * 2;
            circle.style.borderTopLeftRadius = circle.style.borderTopRightRadius =
                circle.style.borderBottomLeftRadius = circle.style.borderBottomRightRadius = PortRadius;

            if (isExec)
                circle.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
            else if (isInput)
                circle.style.backgroundColor = GetTypeColor(typeName);
            else
                circle.style.backgroundColor = GetTypeColor(typeName);

            circle.style.borderTopWidth = circle.style.borderBottomWidth =
                circle.style.borderLeftWidth = circle.style.borderRightWidth = 1;
            circle.style.borderTopColor = circle.style.borderBottomColor =
                circle.style.borderLeftColor = circle.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);

            var key = $"{(isInput ? "in" : "out")}:{portName}";
            _portElements[nodeId][key] = circle;

            circle.RegisterCallback<MouseDownEvent>(e =>
            {
                OnPortClicked(nodeId, portName, isInput);
                e.StopPropagation();
            });

            row.Add(circle);

            var labelText = string.IsNullOrEmpty(typeName) ? displayName : $"{displayName} ({typeName})";
            var label = new Label(labelText);
            label.style.fontSize = 8;
            label.style.color = new Color(0.75f, 0.75f, 0.75f);
            label.style.marginLeft = isInput ? 3 : 0;
            label.style.marginRight = isInput ? 0 : 3;
            row.Add(label);

            return row;
        }

        private static Color GetTypeColor(string typeName)
        {
            return typeName switch
            {
                "int" => new Color(0.3f, 0.7f, 1f),
                "float" => new Color(0.3f, 0.9f, 0.5f),
                "bool" => new Color(0.9f, 0.3f, 0.3f),
                "string" => new Color(0.9f, 0.7f, 0.2f),
                "exec" => new Color(0.9f, 0.9f, 0.9f),
                "GameObject" => new Color(0.4f, 0.8f, 0.4f),
                "Vector3" => new Color(0.2f, 0.7f, 0.9f),
                _ => new Color(0.6f, 0.6f, 0.6f)
            };
        }

        private void OnPortClicked(string nodeId, string portName, bool isInput)
        {
            if (isInput && _connectingFromNodeId != null)
            {
                if (_connectingFromNodeId == nodeId) { CancelConnection(); return; }
                AddConnection(_connectingFromNodeId, _connectingFromPort, nodeId, portName);
                _connectingFromNodeId = null;
                _connectingFromPort = null;
            }
            else if (!isInput)
            {
                _connectingFromNodeId = nodeId;
                _connectingFromPort = portName;

                var key = $"out:{portName}";
                if (_portElements.TryGetValue(nodeId, out var ports) && ports.TryGetValue(key, out var el))
                {
                    el.style.backgroundColor = new Color(1f, 1f, 0.3f);
                    var capturedNodeId = nodeId;
                    schedule.Execute(() =>
                    {
                        if (_connectingFromNodeId == capturedNodeId)
                            CancelConnection();
                    }).ExecuteLater(5000);
                }
            }
        }

        private void CancelConnection()
        {
            if (_connectingFromNodeId != null)
            {
                var key = $"out:{_connectingFromPort}";
                if (_portElements.TryGetValue(_connectingFromNodeId, out var ports) &&
                    ports.TryGetValue(key, out var el))
                {
                    el.style.backgroundColor = GetTypeColor(GetPortType(_connectingFromPort));
                }
            }

            _connectingFromNodeId = null;
            _connectingFromPort = null;
        }

        private string GetPortType(string portName)
        {
            return portName switch
            {
                "execIn" or "execOut" => "exec",
                "inputValue" => "value",
                "output" => "value",
                "inputA" or "inputB" => "float",
                "left" or "right" => "float",
                "input" => "float",
                "result" => "bool",
                "message" => "string",
                "GameObject" => "GameObject",
                "Position" or "Vector3" => "Vector3",
                _ => ""
            };
        }

        private void AddConnection(string fromId, string fromPort, string toId, string toPort)
        {
            if (fromId == toId) return;
            if (_subGraph.Edges.Any(e =>
                    e.FromNodeId == fromId && e.FromPort == fromPort &&
                    e.ToNodeId == toId && e.ToPort == toPort))
                return;

            _subGraph.Edges.Add(new EdgeData
            {
                FromNodeId = fromId,
                FromPort = fromPort,
                ToNodeId = toId,
                ToPort = toPort
            });

            Rebuild();
            OnChanged?.Invoke();
        }

        private void RemoveNode(string nodeId)
        {
            _subGraph.Nodes.RemoveAll(n => n.Id == nodeId);
            _subGraph.Edges.RemoveAll(e => e.FromNodeId == nodeId || e.ToNodeId == nodeId);
            Rebuild();
            OnChanged?.Invoke();
        }

        private void CreateConnectionLines()
        {
            _connectionLayer.Clear();
            foreach (var edge in _subGraph.Edges)
            {
                var fromPos = GetPortWorldPos(edge.FromNodeId, edge.FromPort, false);
                var toPos = GetPortWorldPos(edge.ToNodeId, edge.ToPort, true);
                if (fromPos == null || toPos == null) continue;
                _connectionLayer.Add(CreateLine(fromPos.Value, toPos.Value));
            }
        }

        private Vector2? GetPortWorldPos(string nodeId, string portName, bool isInput)
        {
            var key = $"{(isInput ? "in" : "out")}:{portName}";
            if (!_portElements.TryGetValue(nodeId, out var ports) || !ports.TryGetValue(key, out var el))
                return null;

            var rect = el.worldBound;
            var canvasRect = _canvas.worldBound;
            if (rect.width < 1 || canvasRect.width < 1) return null;
            return new Vector2(rect.center.x - canvasRect.x, rect.center.y - canvasRect.y);
        }

        private static VisualElement CreateLine(Vector2 from, Vector2 to)
        {
            float dx = to.x - from.x;
            float dy = to.y - from.y;
            float length = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

            var line = new VisualElement();
            line.style.position = Position.Absolute;
            line.style.width = length;
            line.style.height = 2;
            line.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f);
            line.style.left = from.x;
            line.style.top = from.y - 1;
            line.style.rotate = new Rotate(angle);
            line.style.transformOrigin = new TransformOrigin(0, Length.Percent(50));
            line.pickingMode = PickingMode.Ignore;

            return line;
        }

        private void MakeDraggable(VisualElement element, string nodeId)
        {
            Vector2 startMousePos = Vector2.zero;
            Vector2 startNodePos = Vector2.zero;
            bool isDragging = false;

            element.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button != 0) return;
                isDragging = true;
                startMousePos = e.mousePosition;
                startNodePos = _nodePositions.TryGetValue(nodeId, out var p) ? p : Vector2.zero;
                element.CaptureMouse();
                e.StopPropagation();
            });

            element.RegisterCallback<MouseMoveEvent>(e =>
            {
                if (!isDragging) return;
                var delta = (Vector2)e.mousePosition - startMousePos;
                var newPos = startNodePos + delta;
                newPos.x = Mathf.Max(0, newPos.x);
                newPos.y = Mathf.Max(0, newPos.y);

                _nodePositions[nodeId] = newPos;
                element.style.left = newPos.x;
                element.style.top = newPos.y;

                schedule.Execute(CreateConnectionLines);
                e.StopPropagation();
            });

            element.RegisterCallback<MouseUpEvent>(e =>
            {
                if (!isDragging) return;
                isDragging = false;
                element.ReleaseMouse();
                OnChanged?.Invoke();
                e.StopPropagation();
            });
        }

        private void ShowAddNodeMenu()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Literals/Int"), false, () => AddNode(NodeType.LiteralInt));
            menu.AddItem(new GUIContent("Literals/Float"), false, () => AddNode(NodeType.LiteralFloat));
            menu.AddItem(new GUIContent("Literals/Bool"), false, () => AddNode(NodeType.LiteralBool));
            menu.AddItem(new GUIContent("Literals/String"), false, () => AddNode(NodeType.LiteralString));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Comparison/Equal"), false, () => AddNode(NodeType.CompareEqual));
            menu.AddItem(new GUIContent("Comparison/Not Equal"), false, () => AddNode(NodeType.CompareNotEqual));
            menu.AddItem(new GUIContent("Comparison/Greater"), false, () => AddNode(NodeType.CompareGreater));
            menu.AddItem(new GUIContent("Comparison/Less"), false, () => AddNode(NodeType.CompareLess));
            menu.AddItem(new GUIContent("Comparison/Greater Or Equal"), false, () => AddNode(NodeType.CompareGreaterOrEqual));
            menu.AddItem(new GUIContent("Comparison/Less Or Equal"), false, () => AddNode(NodeType.CompareLessOrEqual));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Logic/And"), false, () => AddNode(NodeType.LogicalAnd));
            menu.AddItem(new GUIContent("Logic/Or"), false, () => AddNode(NodeType.LogicalOr));
            menu.AddItem(new GUIContent("Logic/Not"), false, () => AddNode(NodeType.LogicalNot));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Math/Add"), false, () => AddNode(NodeType.MathAdd));
            menu.AddItem(new GUIContent("Math/Subtract"), false, () => AddNode(NodeType.MathSubtract));
            menu.AddItem(new GUIContent("Math/Multiply"), false, () => AddNode(NodeType.MathMultiply));
            menu.AddItem(new GUIContent("Math/Divide"), false, () => AddNode(NodeType.MathDivide));

            if (!_isConditionPanel)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Flow/Console.WriteLine"), false,
                    () => AddNode(NodeType.ConsoleWriteLine));
            }

            menu.ShowAsContext();
        }

        private void AddNode(NodeType type)
        {
            var id = $"sub_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            var nodeData = new NodeData
            {
                Id = id,
                Type = type,
                Value = GetDefaultValue(type),
                ValueType = GetDefaultValueType(type),
                VariableName = ""
            };

            if (IsLiteral(type))
            {
                nodeData.ExpressionOverride = GetDefaultValue(type);
            }

            _subGraph.Nodes.Add(nodeData);

            Rebuild();
            OnChanged?.Invoke();
        }

        private static string FormatNodeTitle(NodeData node)
        {
            var baseName = GraphSerializer.GetNodeDisplayName(node.Type);
            if (!string.IsNullOrEmpty(node.VariableName))
                return $"{baseName}: {node.VariableName}";
            return baseName;
        }

        private static string FormatNodeSubtitle(NodeData node)
        {
            if (IsLiteral(node.Type))
            {
                var typeStr = !string.IsNullOrEmpty(node.ValueType) ? node.ValueType : "?";
                var valStr = !string.IsNullOrEmpty(node.Value) ? node.Value : "?";
                if (!string.IsNullOrEmpty(node.VariableName))
                    return $"{typeStr} = {valStr}";
                return $"{typeStr}: {valStr}";
            }

            return GetNodeTypeLabel(node.Type);
        }

        private static string GetNodeTypeLabel(NodeType type) => type switch
        {
            NodeType.MathAdd or NodeType.MathSubtract or NodeType.MathMultiply
                or NodeType.MathDivide or NodeType.MathModulo => "float/int → float/int",
            NodeType.CompareEqual or NodeType.CompareNotEqual or NodeType.CompareGreater
                or NodeType.CompareLess or NodeType.CompareGreaterOrEqual
                or NodeType.CompareLessOrEqual => "any → bool",
            NodeType.LogicalAnd or NodeType.LogicalOr => "bool → bool",
            NodeType.LogicalNot => "bool → bool",
            NodeType.ConsoleWriteLine => "exec + string",
            NodeType.IntParse => "string → int",
            NodeType.FloatParse => "string → float",
            NodeType.ToStringConvert => "any → string",
            NodeType.MathfAbs => "float → float",
            NodeType.MathfMax or NodeType.MathfMin => "float, float → float",
            _ => ""
        };

        private static Color GetNodeColor(NodeType type)
        {
            if (ColorUtility.TryParseHtmlString(GraphSerializer.GetNodeColor(type), out var c))
                return c;
            return new Color(0.4f, 0.4f, 0.4f);
        }

        private static string GetDefaultValue(NodeType type) => type switch
        {
            NodeType.LiteralInt => "0",
            NodeType.LiteralFloat => "0",
            NodeType.LiteralBool => "false",
            NodeType.LiteralString => "",
            _ => ""
        };

        private static string GetDefaultValueType(NodeType type) => type switch
        {
            NodeType.LiteralInt => "int",
            NodeType.LiteralFloat => "float",
            NodeType.LiteralBool => "bool",
            NodeType.LiteralString => "string",
            _ => ""
        };

        private static bool IsLiteral(NodeType t) =>
            t is NodeType.LiteralBool or NodeType.LiteralInt or NodeType.LiteralFloat or NodeType.LiteralString;

        private struct PortDef
        {
            public string name;
            public string displayName;
            public string typeName;
        }

        private static List<PortDef> GetInputPorts(NodeType type) => type switch
        {
            NodeType.LiteralInt => new() { new() { name = "inputValue", displayName = "value", typeName = "int" } },
            NodeType.LiteralFloat => new() { new() { name = "inputValue", displayName = "value", typeName = "float" } },
            NodeType.LiteralBool => new() { new() { name = "inputValue", displayName = "value", typeName = "bool" } },
            NodeType.LiteralString => new() { new() { name = "inputValue", displayName = "value", typeName = "string" } },
            
            NodeType.MathAdd or NodeType.MathSubtract or NodeType.MathMultiply or NodeType.MathDivide or NodeType.MathModulo
                => new() { new() { name = "inputA", displayName = "A", typeName = "float" }, new() { name = "inputB", displayName = "B", typeName = "float" } },
            
            NodeType.CompareEqual or NodeType.CompareNotEqual or NodeType.CompareGreater or NodeType.CompareLess
                or NodeType.CompareGreaterOrEqual or NodeType.CompareLessOrEqual
                => new() { new() { name = "left", displayName = "left", typeName = "float" }, new() { name = "right", displayName = "right", typeName = "float" } },
            
            NodeType.LogicalAnd or NodeType.LogicalOr
                => new() { new() { name = "left", displayName = "left", typeName = "bool" }, new() { name = "right", displayName = "right", typeName = "bool" } },
            NodeType.LogicalNot
                => new() { new() { name = "input", displayName = "input", typeName = "bool" } },
            
            NodeType.ConsoleWriteLine
                => new() { new() { name = "execIn", displayName = "exec", typeName = "exec" }, new() { name = "message", displayName = "message", typeName = "string" } },
            
            NodeType.IntParse => new() { new() { name = "input", displayName = "input", typeName = "string" } },
            NodeType.FloatParse => new() { new() { name = "input", displayName = "input", typeName = "string" } },
            NodeType.ToStringConvert => new() { new() { name = "input", displayName = "input", typeName = "" } },
            
            NodeType.MathfAbs => new() { new() { name = "input", displayName = "input", typeName = "float" } },
            NodeType.MathfMax or NodeType.MathfMin
                => new() { new() { name = "inputA", displayName = "A", typeName = "float" }, new() { name = "inputB", displayName = "B", typeName = "float" } },
            
            _ => new()
        };

        private static List<PortDef> GetOutputPorts(NodeType type) => type switch
        {
            NodeType.LiteralInt => new() { new() { name = "output", displayName = "output", typeName = "int" } },
            NodeType.LiteralFloat => new() { new() { name = "output", displayName = "output", typeName = "float" } },
            NodeType.LiteralBool => new() { new() { name = "output", displayName = "output", typeName = "bool" } },
            NodeType.LiteralString => new() { new() { name = "output", displayName = "output", typeName = "string" } },
            
            NodeType.MathAdd or NodeType.MathSubtract or NodeType.MathMultiply or NodeType.MathDivide or NodeType.MathModulo
                => new() { new() { name = "output", displayName = "output", typeName = "float" } },
            
            NodeType.CompareEqual or NodeType.CompareNotEqual or NodeType.CompareGreater or NodeType.CompareLess
                or NodeType.CompareGreaterOrEqual or NodeType.CompareLessOrEqual
                => new() { new() { name = "result", displayName = "result", typeName = "bool" } },
            
            NodeType.LogicalAnd or NodeType.LogicalOr or NodeType.LogicalNot
                => new() { new() { name = "result", displayName = "result", typeName = "bool" } },
            
            NodeType.ConsoleWriteLine
                => new() { new() { name = "execOut", displayName = "exec", typeName = "exec" } },
            
            NodeType.IntParse => new() { new() { name = "output", displayName = "output", typeName = "int" } },
            NodeType.FloatParse => new() { new() { name = "output", displayName = "output", typeName = "float" } },
            NodeType.ToStringConvert => new() { new() { name = "output", displayName = "output", typeName = "string" } },
            
            NodeType.MathfAbs => new() { new() { name = "output", displayName = "output", typeName = "float" } },
            NodeType.MathfMax or NodeType.MathfMin
                => new() { new() { name = "output", displayName = "output", typeName = "float" } },
            
            _ => new()
        };
    }
}