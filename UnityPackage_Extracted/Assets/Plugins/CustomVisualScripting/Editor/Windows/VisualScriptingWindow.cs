using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Integration;
using CustomVisualScripting.Integration.Models;
using CustomVisualScripting.Windows.Views;
using CustomVisualScripting.Editor.Nodes.Base;
using CustomVisualScripting.Editor.Nodes.Literals;
using CustomVisualScripting.Editor.Nodes.Math;
using CustomVisualScripting.Editor.Nodes.Comparison;
using CustomVisualScripting.Editor.Nodes.Flow;
using CustomVisualScripting.Editor.Nodes.Debug;
using CustomVisualScripting.Editor.Nodes.Logic;
using CustomVisualScripting.Editor.Nodes.Conversion;
using CustomVisualScripting.Editor.Nodes.Unity;
using CustomVisualScripting.Runtime.Execution;
using VisualScripting.Core.Models;
using CustomToolbar = CustomVisualScripting.Windows.Views.ToolbarView;

namespace CustomVisualScripting.Editor.Windows
{
    public class VisualScriptingWindow : EditorWindow
    {
        private CompleteGraphData _currentGraph;
        private BaseGraph _internalGraph;
        private BaseGraphView _graphView;
        private VisualElement _graphContainer;
        
        private CodeEditorView _codeEditor;
        private CustomToolbar _toolbar;
        private ErrorPanel _errorPanel;
        private ConsoleView _consoleView;
        
        private string _currentFilePath;
        private bool _hasUnsavedChanges = false;
        
        private GraphRunner _runner;
        
        [MenuItem("Tools/Visual Scripting")]
        public static void OpenWindow()
        {
            var window = GetWindow<VisualScriptingWindow>();
            window.titleContent = new GUIContent("Visual Scripting");
            window.minSize = new Vector2(900, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            ParserBridge.Initialize();
            GeneratorBridge.Initialize();
            Application.logMessageReceived += OnLogMessageReceived;
            
            _currentGraph = new CompleteGraphData();
            _hasUnsavedChanges = false;
        }
        
        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            CleanupGraph();
        }
        
        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (_consoleView != null)
            {
                _consoleView.AddMessage(condition, type);
            }
        }
        
        private void CleanupGraph()
        {
            if (_graphView != null)
            {
                _graphView.Dispose();
                _graphView = null;
            }
            
            if (_internalGraph != null)
            {
                DestroyImmediate(_internalGraph);
                _internalGraph = null;
            }
        }
        
        private void CreateGUI()
        {
            _currentGraph = new CompleteGraphData();
            _hasUnsavedChanges = false;
            
            var root = rootVisualElement;
            
            _toolbar = new CustomToolbar();
            _toolbar.ParseButton.clicked += OnParse;
            _toolbar.GenerateButton.clicked += OnGenerate;
            _toolbar.RunButton.clicked += OnRun;
            _toolbar.StopButton.clicked += OnStop;
            _toolbar.SaveButton.clicked += OnSave;
            _toolbar.SaveAsButton.clicked += OnSaveAs;
            _toolbar.LoadButton.clicked += OnLoad;
            _toolbar.ClearButton.clicked += OnClear;
            root.Add(_toolbar);
            
            var splitView = new TwoPaneSplitView(0, 350, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1;
            
            _codeEditor = new CodeEditorView();
            splitView.Add(_codeEditor);
            
            _graphContainer = new VisualElement();
            _graphContainer.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));
            _graphContainer.style.flexGrow = 1;
            
            splitView.Add(_graphContainer);
            root.Add(splitView);
            
            _errorPanel = new ErrorPanel();
            root.Add(_errorPanel);
            
            _consoleView = new ConsoleView();
            _consoleView.style.marginTop = 5;
            root.Add(_consoleView);
            
            _toolbar.SetStatusNormal("Готов к работе");
            
            UpdateGraphView();
        }
        
        private void OnParse()
        {
            _toolbar.SetStatusWarning("Парсинг...");
            
            var result = ParserBridge.Parse(_codeEditor.Code);
            
            if (result.HasErrors)
            {
                _errorPanel.ShowErrors(result.Errors);
                _toolbar.SetStatusError($"Ошибок: {result.Errors.Count}");
                return;
            }
            
            _errorPanel.Clear();
            
            _currentGraph = new CompleteGraphData();
            _currentGraph = GraphConverter.LogicToComplete(result.Graph, _currentGraph);
            _hasUnsavedChanges = true;
            
            RecreateGraphView();
            
            _toolbar.SetStatusSuccess($"Создано нод: {result.Graph.Nodes.Count}, связей: {result.Graph.Edges.Count}");
        }
        
        private void RecreateGraphView()
        {
            CleanupGraph();
            _graphContainer.Clear();
            UpdateGraphView();
        }
        
        private void OnGenerate()
        {
            _toolbar.SetStatusWarning("Генерация...");
            
            SyncFullGraphFromView();
            
            string code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
            _codeEditor.Code = code;
            
            _toolbar.SetStatusSuccess("Код сгенерирован");
        }
        
        private void OnRun()
        {
            if (_currentGraph?.LogicGraph == null || _currentGraph.LogicGraph.Nodes.Count == 0)
            {
                _toolbar.SetStatusError("Нет графа для выполнения");
                return;
            }
            
            _toolbar.SetRunMode(true);
            _toolbar.SetStatusWarning("Выполнение...");
            
            _runner = new GraphRunner();
            
            _runner.OnLogMessage += (message, type) => {
                if (_consoleView != null)
                {
                    _consoleView.AddMessage(message, type);
                }
            };
            
            try
            {
                _runner.Run(_currentGraph.LogicGraph);
                _toolbar.SetStatusSuccess("Выполнение завершено");
            }
            catch (Exception e)
            {
                _toolbar.SetStatusError($"Ошибка: {e.Message}");
                Debug.LogError($"[VS] Ошибка выполнения: {e.Message}");
            }
            finally
            {
                _toolbar.SetRunMode(false);
                _runner = null;
            }
        }
        
        private void OnStop()
        {
            if (_runner != null)
            {
                _runner.Clear();
                _runner = null;
            }
            _toolbar.SetRunMode(false);
            _toolbar.SetStatusNormal("Выполнение остановлено");
        }
        
        private void OnSave()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                OnSaveAs();
                return;
            }
            
            SyncFullGraphFromView();
            
            if (GraphSaver.SaveToJson(_currentGraph, _currentFilePath))
            {
                _toolbar.SetStatusSuccess($"Сохранено: {Path.GetFileName(_currentFilePath)}");
                _hasUnsavedChanges = false;
            }
            else
            {
                _toolbar.SetStatusError("Ошибка сохранения");
            }
        }
        
        private void OnSaveAs()
        {
            string defaultName = !string.IsNullOrEmpty(_currentFilePath)
                ? Path.GetFileName(_currentFilePath)
                : "graph.json";
            
            string path = EditorUtility.SaveFilePanel("Сохранить граф как", Application.dataPath, defaultName, "json");
            if (string.IsNullOrEmpty(path)) return;
            
            SyncFullGraphFromView();
            _currentFilePath = path;
            
            if (GraphSaver.SaveToJson(_currentGraph, path))
            {
                _toolbar.SetStatusSuccess($"Сохранено: {Path.GetFileName(path)}");
                _hasUnsavedChanges = false;
            }
            else
            {
                _toolbar.SetStatusError("Ошибка сохранения");
            }
        }
        
        private void OnLoad()
        {
            string path = EditorUtility.OpenFilePanel("Загрузить граф", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            
            var loaded = GraphSaver.LoadFromJson(path);
            if (loaded != null)
            {
                _currentGraph = loaded;
                _currentFilePath = path;
                _hasUnsavedChanges = false;
                _codeEditor.Code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
                RecreateGraphView();
                _toolbar.SetStatusSuccess($"Загружено: {Path.GetFileName(path)}");
            }
            else
            {
                _toolbar.SetStatusError("Ошибка загрузки");
            }
        }
        
        private void OnClear()
        {
            _codeEditor.Clear();
            _currentGraph = new CompleteGraphData();
            _errorPanel.Clear();
            _currentFilePath = null;
            _hasUnsavedChanges = false;
            RecreateGraphView();
            _toolbar.SetStatusNormal("Очищено");
        }
        
        private void SyncFullGraphFromView()
        {
            if (_graphView == null || _internalGraph == null) return;
            
            _currentGraph.LogicGraph.Nodes.Clear();
            _currentGraph.LogicGraph.Edges.Clear();

            var graphNodes = _internalGraph.nodes.OfType<CustomBaseNode>().ToList();
            var validNodeIds = new HashSet<string>();

            foreach (var customNode in graphNodes)
            {
                var nodeData = customNode.ToNodeData();
                nodeData.Id = customNode.NodeId;
                nodeData.VariableName = customNode.variableName;

                if (customNode is IntNode intNode)
                    nodeData.Value = intNode.intValue.ToString();
                else if (customNode is FloatNode floatNode)
                    nodeData.Value = floatNode.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                else if (customNode is BoolNode boolNode)
                    nodeData.Value = boolNode.boolValue.ToString();
                else if (customNode is StringNode stringNode)
                    nodeData.Value = stringNode.stringValue;

                if (customNode is IfNode ifNode)
                {
                    nodeData.ConditionSubGraph = ifNode.conditionSubGraph;
                    nodeData.BodySubGraph = ifNode.bodySubGraph;
                }
                else if (customNode is ElseNode elseNode)
                {
                    nodeData.BodySubGraph = elseNode.bodySubGraph;
                }
                else if (customNode is ForNode forNode)
                {
                    nodeData.ConditionSubGraph = forNode.conditionSubGraph;
                    nodeData.BodySubGraph = forNode.bodySubGraph;
                }
                else if (customNode is WhileNode whileNode)
                {
                    nodeData.ConditionSubGraph = whileNode.conditionSubGraph;
                    nodeData.BodySubGraph = whileNode.bodySubGraph;
                }

                _currentGraph.LogicGraph.Nodes.Add(nodeData);
                validNodeIds.Add(customNode.NodeId);
            }
            
            foreach (var edgeView in _graphView.edgeViews)
            {
                if (edgeView == null) continue;
                
                var fromPort = edgeView.output as PortView;
                var toPort = edgeView.input as PortView;
                
                if (fromPort == null || toPort == null) continue;
                
                var fromNode = fromPort.owner.nodeTarget as CustomBaseNode;
                var toNode = toPort.owner.nodeTarget as CustomBaseNode;
                
                if (fromNode == null || toNode == null) continue;
                if (!validNodeIds.Contains(fromNode.NodeId) || !validNodeIds.Contains(toNode.NodeId)) continue;
                
                Debug.Log($"[VS] Сохраняем связь: {fromNode.NodeId}.{fromPort.fieldName} → {toNode.NodeId}.{toPort.fieldName}");
                
                _currentGraph.LogicGraph.Edges.Add(new EdgeData
                {
                    FromNodeId = fromNode.NodeId,
                    FromPort = fromPort.fieldName,
                    ToNodeId = toNode.NodeId,
                    ToPort = toPort.fieldName
                });
            }
            
            SaveVisualNodePositions();
            _hasUnsavedChanges = true;
        }
        
        private void SaveVisualNodePositions()
        {
            if (_currentGraph?.VisualNodes == null || _graphView == null || _internalGraph == null)
                return;
            
            _currentGraph.VisualNodes.Clear();

            foreach (var customNode in _internalGraph.nodes.OfType<CustomBaseNode>())
            {
                if (!_graphView.nodeViewsPerNode.TryGetValue(customNode, out var nodeView))
                    continue;

                _currentGraph.VisualNodes.Add(new VisualNodeData
                {
                    NodeId = customNode.NodeId,
                    Position = nodeView.GetPosition().position,
                    IsCollapsed = false
                });
            }
        }
        
        private void UpdateGraphView()
        {
            _graphContainer.Clear();
            
            try
            {
                _internalGraph = ScriptableObject.CreateInstance<BaseGraph>();
                var nodeMap = new Dictionary<string, BaseNode>();
                
                if (_currentGraph?.LogicGraph?.Nodes != null)
                {
                    foreach (var nodeData in _currentGraph.LogicGraph.Nodes)
                    {
                        var node = CreateNodeFromData(nodeData);
                        if (node != null)
                        {
                            node.NodeId = nodeData.Id;
                            node.InitializeFromData(nodeData);
                            
                            if (node is IntNode intNode && int.TryParse(nodeData.Value, out int intVal))
                                intNode.intValue = intVal;
                            else if (node is FloatNode floatNode && float.TryParse(nodeData.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
                                floatNode.floatValue = floatVal;
                            else if (node is BoolNode boolNode && bool.TryParse(nodeData.Value, out bool boolVal))
                                boolNode.boolValue = boolVal;
                            else if (node is StringNode stringNode)
                                stringNode.stringValue = nodeData.Value;

                            if (node is IfNode ifNode)
                            {
                                ifNode.conditionSubGraph = nodeData.ConditionSubGraph ?? new VisualScripting.Core.Models.GraphData();
                                ifNode.bodySubGraph = nodeData.BodySubGraph ?? new VisualScripting.Core.Models.GraphData();
                            }
                            else if (node is ElseNode elseNode)
                            {
                                elseNode.bodySubGraph = nodeData.BodySubGraph ?? new VisualScripting.Core.Models.GraphData();
                            }
                            else if (node is ForNode forNode)
                            {
                                forNode.conditionSubGraph = nodeData.ConditionSubGraph ?? new VisualScripting.Core.Models.GraphData();
                                forNode.bodySubGraph = nodeData.BodySubGraph ?? new VisualScripting.Core.Models.GraphData();
                            }
                            else if (node is WhileNode whileNode)
                            {
                                whileNode.conditionSubGraph = nodeData.ConditionSubGraph ?? new VisualScripting.Core.Models.GraphData();
                                whileNode.bodySubGraph = nodeData.BodySubGraph ?? new VisualScripting.Core.Models.GraphData();
                            }
                            
                            _internalGraph.AddNode(node);
                            nodeMap[nodeData.Id] = node;
                        }
                    }
                }
                
                _graphView = new BaseGraphView(this);
                _graphView.Initialize(_internalGraph);
                _graphView.style.flexGrow = 1;
                
                if (_currentGraph?.LogicGraph?.Edges != null && nodeMap.Count > 0)
                {
                    foreach (var edgeData in _currentGraph.LogicGraph.Edges)
                    {
                        // Removing execution port filter
                        
                        if (!nodeMap.TryGetValue(edgeData.FromNodeId, out var fromNode)) continue;
                        if (!nodeMap.TryGetValue(edgeData.ToNodeId, out var toNode)) continue;
                        
                        if (!_graphView.nodeViewsPerNode.TryGetValue(fromNode, out var fromNodeView)) continue;
                        if (!_graphView.nodeViewsPerNode.TryGetValue(toNode, out var toNodeView)) continue;
                        
                        Debug.Log($"[VS] Восстанавливаем связь: {edgeData.FromNodeId}.{edgeData.FromPort} → {edgeData.ToNodeId}.{edgeData.ToPort}");
                        
                        var fromPort = fromNodeView.outputPortViews.FirstOrDefault(p => p.fieldName == edgeData.FromPort || p.portName == edgeData.FromPort);
                        var toPort = toNodeView.inputPortViews.FirstOrDefault(p => p.fieldName == edgeData.ToPort || p.portName == edgeData.ToPort);
                        
                        if (fromPort == null)
                        {
                            Debug.LogWarning($"[VS] Не найден выходной порт: '{edgeData.FromPort}' в ноде {fromNode.GetType().Name}. Доступные: {string.Join(", ", fromNodeView.outputPortViews.Select(p => p.fieldName))}");
                            continue;
                        }
                        if (toPort == null)
                        {
                            Debug.LogWarning($"[VS] Не найден входной порт: '{edgeData.ToPort}' в ноде {toNode.GetType().Name}. Доступные: {string.Join(", ", toNodeView.inputPortViews.Select(p => p.fieldName))}");
                            continue;
                        }
                        
                        bool alreadyConnected = false;
                        foreach (var existingEdge in _graphView.edgeViews)
                        {
                            if (existingEdge.output == fromPort && existingEdge.input == toPort)
                            {
                                alreadyConnected = true;
                                break;
                            }
                        }
                        
                        if (!alreadyConnected)
                        {
                            _graphView.Connect(fromPort, toPort);
                            Debug.Log($"[VS] Связь создана: {edgeData.FromNodeId}.{edgeData.FromPort} → {edgeData.ToNodeId}.{edgeData.ToPort}");
                        }
                    }
                }
                
                if (_currentGraph?.VisualNodes != null)
                {
                    foreach (var nodeView in _graphView.nodeViews)
                    {
                        if (nodeView.nodeTarget is CustomBaseNode customNode)
                        {
                            var visualNode = _currentGraph.VisualNodes.FirstOrDefault(v => v.NodeId == customNode.NodeId);
                            if (visualNode != null)
                            {
                                nodeView.SetPosition(new Rect(visualNode.Position, Vector2.zero));
                            }
                        }
                    }
                }
                
                _graphView.UpdateViewTransform(Vector3.zero, Vector3.one);
                _graphView.FrameAll();
                
                _graphContainer.Add(_graphView);
                
                int nodeCount = _internalGraph.nodes.Count;
                _toolbar.SetStatusSuccess($"Граф готов — {nodeCount} нод");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VS] Ошибка создания графа: {e.Message}\n{e.StackTrace}");
                ShowTextualGraph();
            }
        }
        
        private void ShowTextualGraph()
        {
            var info = new VisualElement();
            info.style.marginTop = 10;
            info.style.marginLeft = 10;
            info.style.flexGrow = 1;
            
            var label = new Label($"Граф: {_currentGraph.LogicGraph.Nodes.Count} нод, {_currentGraph.LogicGraph.Edges.Count} связей");
            label.style.color = Color.white;
            label.style.fontSize = 14;
            info.Add(label);
            
            _graphContainer.Add(info);
        }
        
        private CustomBaseNode CreateNodeFromData(NodeData data)
        {
            if (data == null) return null;
            
            switch (data.Type)
            {
                case NodeType.LiteralInt: return new IntNode();
                case NodeType.LiteralFloat: return new FloatNode();
                case NodeType.LiteralBool: return new BoolNode();
                case NodeType.LiteralString: return new StringNode();
                case NodeType.MathAdd: return new AddNode();
                case NodeType.MathSubtract: return new SubtractNode();
                case NodeType.MathMultiply: return new MultiplyNode();
                case NodeType.MathDivide: return new DivideNode();
                case NodeType.MathModulo: return new ModuloNode();
                case NodeType.CompareEqual: return new EqualNode();
                case NodeType.CompareNotEqual: return new NotEqualNode();
                case NodeType.CompareGreater: return new GreaterNode();
                case NodeType.CompareGreaterOrEqual: return new GreaterOrEqualNode();
                case NodeType.CompareLess: return new LessNode();
                case NodeType.CompareLessOrEqual: return new LessOrEqualNode();
                case NodeType.LogicalAnd: return new AndNode();
                case NodeType.LogicalOr: return new OrNode();
                case NodeType.LogicalNot: return new NotNode();
                case NodeType.FlowIf: return new IfNode();
                case NodeType.FlowElse: return new ElseNode();
                case NodeType.FlowFor: return new ForNode();
                case NodeType.FlowWhile: return new WhileNode();
                case NodeType.ConsoleWriteLine: return new ConsoleWriteLineNode();
                case NodeType.DebugLog: return new DebugLogNode();
                case NodeType.IntParse: return new IntParseNode();
                case NodeType.FloatParse: return new FloatParseNode();
                case NodeType.ToStringConvert: return new ToStringNode();
                case NodeType.MathfAbs: return new MathfAbsNode();
                case NodeType.MathfMax: return new MathfMaxNode();
                case NodeType.MathfMin: return new MathfMinNode();
                case NodeType.UnityVector3: return new Vector3CreateNode();
                case NodeType.UnityGetPosition: return new GetPositionNode();
                case NodeType.UnitySetPosition: return new SetPositionNode();
                default: return null;
            }
        }
        
        private void OnDestroy()
        {
            if (_hasUnsavedChanges && _currentGraph != null && _currentGraph.LogicGraph.Nodes.Count > 0)
            {
                bool save = EditorUtility.DisplayDialog(
                    "Несохранённые изменения",
                    "Хотите сохранить граф перед закрытием?",
                    "Сохранить",
                    "Не сохранять"
                );
                
                if (save)
                {
                    OnSave();
                }
            }
            
            CleanupGraph();
        }
    }
}