using System;
using System.IO;
using System.Linq;
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
        
        private string _currentFilePath;
        private bool _hasUnsavedChanges = false;
        
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
        }
        
        private void OnDisable()
        {
            CleanupGraph();
        }
        
        private void OnGUI()
        {
            if (EditorWindow.focusedWindow == this && !_hasUnsavedChanges)
            {
                _hasUnsavedChanges = true;
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
            _currentGraph = GraphConverter.LogicToComplete(result.Graph, _currentGraph);
            _hasUnsavedChanges = false;
            UpdateGraphView();
            _toolbar.SetStatusSuccess($"Создано нод: {result.Graph.Nodes.Count}");
        }
        
        private void OnGenerate()
        {
            _toolbar.SetStatusWarning("Генерация...");
            
            // Полная синхронизация графа перед генерацией
            SyncFullGraphFromView();
            
            string code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
            _codeEditor.Code = code;
            
            _toolbar.SetStatusSuccess("Код сгенерирован");
        }

        private void OnSave()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                OnSaveAs();
                return;
            }

            // Полная синхронизация графа перед сохранением
            SyncFullGraphFromView();
            SaveVisualNodePositions();

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
            SaveVisualNodePositions();
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
                UpdateGraphView();
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
            UpdateGraphView();
            _toolbar.SetStatusNormal("Очищено");
        }

        private void SaveVisualNodePositions()
        {
            if (_currentGraph?.VisualNodes == null || _graphView == null)
                return;

            foreach (var vnd in _currentGraph.VisualNodes)
            {
                var nodeView = _graphView.nodeViews?.FirstOrDefault(
                    nv => (nv.nodeTarget as CustomBaseNode)?.NodeId == vnd.NodeId);
                if (nodeView != null)
                {
                    vnd.Position = nodeView.GetPosition().position;
                }
            }
        }
        
        /// <summary>
        /// Полная синхронизация графа из визуального представления в данные
        /// </summary>
        private void SyncFullGraphFromView()
        {
            if (_graphView == null) return;
            
            // Очищаем текущие данные
            _currentGraph.LogicGraph.Nodes.Clear();
            _currentGraph.LogicGraph.Edges.Clear();
            
            // Синхронизируем ноды
            foreach (var nodeView in _graphView.nodeViews)
            {
                if (nodeView.nodeTarget is CustomBaseNode customNode)
                {
                    var nodeData = customNode.ToNodeData();
                    nodeData.Id = customNode.NodeId;
                    nodeData.VariableName = customNode.variableName;
                    
                    // Сохраняем значения в зависимости от типа ноды
                    if (customNode is IntNode intNode)
                        nodeData.Value = intNode.intValue.ToString();
                    else if (customNode is FloatNode floatNode)
                        nodeData.Value = floatNode.floatValue.ToString();
                    else if (customNode is BoolNode boolNode)
                        nodeData.Value = boolNode.boolValue.ToString();
                    else if (customNode is StringNode stringNode)
                        nodeData.Value = stringNode.stringValue;
                    
                    _currentGraph.LogicGraph.Nodes.Add(nodeData);
                }
            }
            
            // Синхронизируем связи (если есть API для получения портов)
            // TODO: Добавить синхронизацию связей из _graphView
            // Это потребует доступа к графу и его связям
            
            Debug.Log($"[VS] Синхронизировано нод: {_currentGraph.LogicGraph.Nodes.Count}");
        }
        
        private void UpdateGraphView()
        {
            _graphContainer.Clear();
            
            try
            {
                CleanupGraph();
                
                _internalGraph = ScriptableObject.CreateInstance<BaseGraph>();
                
                if (_currentGraph?.LogicGraph?.Nodes != null && _currentGraph.LogicGraph.Nodes.Count > 0)
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
                            else if (node is FloatNode floatNode && float.TryParse(nodeData.Value, out float floatVal))
                                floatNode.floatValue = floatVal;
                            else if (node is BoolNode boolNode && bool.TryParse(nodeData.Value, out bool boolVal))
                                boolNode.boolValue = boolVal;
                            else if (node is StringNode stringNode)
                                stringNode.stringValue = nodeData.Value;
                            
                            _internalGraph.AddNode(node);
                        }
                    }
                }
                
                _graphView = new BaseGraphView(this);
                _graphView.Initialize(_internalGraph);
                _graphView.style.flexGrow = 1;
                
                foreach (var nodeView in _graphView.nodeViews)
                {
                    if (nodeView.nodeTarget is CustomBaseNode customNode)
                    {
                        nodeView.title = customNode.name;
                    }
                }
                
                if (_currentGraph?.VisualNodes != null && _currentGraph.VisualNodes.Count > 0)
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
                
                if (_currentGraph?.LogicGraph?.Edges != null && _currentGraph.LogicGraph.Edges.Count > 0)
                {
                    Debug.Log($"[VS] Начинаем создание {_currentGraph.LogicGraph.Edges.Count} связей...");
                    
                    foreach (var edge in _currentGraph.LogicGraph.Edges)
                    {
                        if (edge.FromPort == "execOut" || edge.FromPort == "execIn" || 
                            edge.ToPort == "execOut" || edge.ToPort == "execIn")
                        {
                            Debug.Log($"[VS] Пропускаем порт потока: {edge.FromPort} → {edge.ToPort}");
                            continue;
                        }
                        
                        var fromNodeView = _graphView.nodeViews.FirstOrDefault(nv => 
                            (nv.nodeTarget as CustomBaseNode)?.NodeId == edge.FromNodeId);
                        var toNodeView = _graphView.nodeViews.FirstOrDefault(nv => 
                            (nv.nodeTarget as CustomBaseNode)?.NodeId == edge.ToNodeId);
                        
                        if (fromNodeView != null && toNodeView != null)
                        {
                            var fromPort = fromNodeView.GetFirstPortViewFromFieldName(edge.FromPort);
                            var toPort = toNodeView.GetFirstPortViewFromFieldName(edge.ToPort);
                            
                            if (fromPort != null && toPort != null)
                            {
                                _graphView.Connect(fromPort, toPort);
                                Debug.Log($"[VS] Связь создана: {edge.FromNodeId}.{edge.FromPort} → {edge.ToNodeId}.{edge.ToPort}");
                            }
                            else
                            {
                                Debug.LogWarning($"[VS] Не найден порт: FromPort='{edge.FromPort}', ToPort='{edge.ToPort}'");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[VS] Не найдена нода: FromNodeId='{edge.FromNodeId}', ToNodeId='{edge.ToNodeId}'");
                        }
                    }
                }
                
                _graphView.UpdateViewTransform(Vector3.zero, Vector3.one);
                _graphView.FrameAll();
                
                _graphContainer.Add(_graphView);
                
                if (_internalGraph.nodes.Count == 0)
                {
                    var overlay = new Label("Нет нод\n\nНажмите 'Парсить код' для создания графа\nили перетащите ноду из меню (если доступно)");
                    overlay.style.position = Position.Absolute;
                    overlay.style.top = 20;
                    overlay.style.left = 20;
                    overlay.style.color = Color.gray;
                    overlay.style.fontSize = 14;
                    overlay.style.whiteSpace = WhiteSpace.Normal;
                    _graphContainer.Add(overlay);
                }
                
                _toolbar.SetStatusSuccess($"Граф готов{(_internalGraph.nodes.Count > 0 ? $" — {_internalGraph.nodes.Count} нод" : "")}");
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
            
            int edgeCount = _currentGraph.LogicGraph.Edges?.Count ?? 0;
            var label = new Label($"Граф: {_currentGraph.LogicGraph.Nodes.Count} нод, {edgeCount} связей");
            label.style.color = Color.white;
            label.style.fontSize = 14;
            label.style.marginBottom = 10;
            info.Add(label);
            
            foreach (var node in _currentGraph.LogicGraph.Nodes)
            {
                var color = GraphConverter.GetNodeColor(node.Type);
                var displayName = GraphConverter.GetNodeDisplayName(node.Type);
                var varPart = string.IsNullOrEmpty(node.VariableName) ? "" : $" [{node.VariableName}]";
                var valPart = string.IsNullOrEmpty(node.Value) ? "" : $" = {node.Value}";
                
                var text = $"  • {displayName}{varPart}{valPart} (ID: {node.Id})";
                var nodeLabel = new Label(text);
                nodeLabel.style.color = color;
                nodeLabel.style.marginLeft = 20;
                nodeLabel.style.marginBottom = 2;
                info.Add(nodeLabel);
            }
            
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
                case NodeType.CompareEqual: return new EqualNode();
                case NodeType.CompareGreater: return new GreaterNode();
                case NodeType.CompareLess: return new LessNode();
                case NodeType.FlowIf: return new IfNode();
                case NodeType.DebugLog: return new DebugLogNode();
                default: return null;
            }
        }
        
        private void OnDestroy()
        {
            if (_hasUnsavedChanges)
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