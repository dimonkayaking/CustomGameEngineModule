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
            
            var root = rootVisualElement;
            
            _toolbar = new CustomToolbar();
            _toolbar.ParseButton.clicked += OnParse;
            _toolbar.GenerateButton.clicked += OnGenerate;
            _toolbar.SaveButton.clicked += OnSave;
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
            
            var placeholder = new Label("Здесь будет граф\n\nНажмите 'Парсить код' для отображения");
            placeholder.style.marginTop = 20;
            placeholder.style.marginLeft = 10;
            placeholder.style.color = Color.gray;
            placeholder.style.whiteSpace = WhiteSpace.Normal;
            _graphContainer.Add(placeholder);
            
            splitView.Add(_graphContainer);
            root.Add(splitView);
            
            _errorPanel = new ErrorPanel();
            root.Add(_errorPanel);
            
            _toolbar.SetStatusNormal("Готов к работе");
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
            UpdateGraphView();
            _toolbar.SetStatusSuccess($"Создано нод: {result.Graph.Nodes.Count}");
        }
        
        private void OnGenerate()
        {
            if (_currentGraph?.LogicGraph?.Nodes == null || _currentGraph.LogicGraph.Nodes.Count == 0)
            {
                _toolbar.SetStatusError("Сначала распарси код");
                return;
            }
            
            _toolbar.SetStatusWarning("Генерация...");
            
            string code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
            _codeEditor.Code = code;
            
            _toolbar.SetStatusSuccess("Код сгенерирован");
        }
        
        private void OnSave()
        {
            string path = EditorUtility.SaveFilePanel("Сохранить граф", Application.dataPath, "graph.json", "json");
            if (string.IsNullOrEmpty(path)) return;
            
            if (_graphView != null && _currentGraph.VisualNodes != null)
            {
                foreach (var nodeView in _graphView.nodeViews)
                {
                    if (nodeView.nodeTarget is CustomVisualScripting.Editor.Nodes.Base.CustomBaseNode customNode)
                    {
                        var visualNode = _currentGraph.VisualNodes.FirstOrDefault(v => v.NodeId == customNode.NodeId);
                        if (visualNode != null)
                        {
                            visualNode.Position = nodeView.GetPosition().position;
                        }
                    }
                }
            }
            
            if (GraphSaver.SaveToJson(_currentGraph, path))
            {
                _toolbar.SetStatusSuccess($"Сохранено: {Path.GetFileName(path)}");
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
            UpdateGraphView();
            _toolbar.SetStatusNormal("Очищено");
        }
        
        private void UpdateGraphView()
        {
            _graphContainer.Clear();
            
            if (_currentGraph?.LogicGraph?.Nodes == null || _currentGraph.LogicGraph.Nodes.Count == 0)
            {
                var placeholder = new Label("Нет нод для отображения\n\nНажмите 'Парсить код' для создания графа");
                placeholder.style.marginTop = 20;
                placeholder.style.marginLeft = 10;
                placeholder.style.color = Color.gray;
                placeholder.style.whiteSpace = WhiteSpace.Normal;
                _graphContainer.Add(placeholder);
                return;
            }
            
            try
            {
                CleanupGraph();
                
                _internalGraph = ScriptableObject.CreateInstance<BaseGraph>();
                
                foreach (var nodeData in _currentGraph.LogicGraph.Nodes)
                {
                    var node = CreateNodeFromData(nodeData);
                    if (node != null)
                    {
                        node.NodeId = nodeData.Id;
                        _internalGraph.AddNode(node);
                    }
                }
                
                if (_internalGraph.nodes.Count == 0)
                {
                    var placeholder = new Label("Не удалось создать узлы для отображения");
                    placeholder.style.marginTop = 20;
                    placeholder.style.marginLeft = 10;
                    placeholder.style.color = Color.yellow;
                    _graphContainer.Add(placeholder);
                    return;
                }
                
                _graphView = new BaseGraphView(this);
                _graphView.Initialize(_internalGraph);
                _graphView.style.flexGrow = 1;
                
                if (_currentGraph.VisualNodes != null && _currentGraph.VisualNodes.Count > 0)
                {
                    foreach (var nodeView in _graphView.nodeViews)
                    {
                        if (nodeView.nodeTarget is CustomVisualScripting.Editor.Nodes.Base.CustomBaseNode customNode)
                        {
                            var visualNode = _currentGraph.VisualNodes.FirstOrDefault(v => v.NodeId == customNode.NodeId);
                            if (visualNode != null)
                            {
                                nodeView.SetPosition(new Rect(visualNode.Position.x, visualNode.Position.y, 100, 50));
                            }
                        }
                    }
                }
                else
                {
                    int index = 0;
                    foreach (var nodeView in _graphView.nodeViews)
                    {
                        float x = 100 + (index % 5) * 200;
                        float y = 100 + (index / 5) * 150;
                        nodeView.SetPosition(new Rect(x, y, 100, 50));
                        index++;
                    }
                }
                
                _graphView.UpdateViewTransform(Vector3.zero, Vector3.one);
                _graphView.FrameAll();
                
                _graphContainer.Add(_graphView);
                
                _graphView.schedule.Execute(() => {
                    _graphView.FrameAll();
                    _graphView.UpdateViewTransform(Vector3.zero, Vector3.one);
                }).ExecuteLater(50);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VS] Ошибка создания графа: {e.Message}\n{e.StackTrace}");
                
                var errorLabel = new Label($"Ошибка отображения графа: {e.Message}");
                errorLabel.style.color = Color.red;
                errorLabel.style.marginTop = 20;
                errorLabel.style.marginLeft = 10;
                errorLabel.style.whiteSpace = WhiteSpace.Normal;
                _graphContainer.Add(errorLabel);
            }
        }
        
        private CustomVisualScripting.Editor.Nodes.Base.CustomBaseNode CreateNodeFromData(NodeData data)
        {
            if (data == null) return null;
            
            switch (data.Type)
            {
                case NodeType.LiteralInt:
                    return new CustomVisualScripting.Editor.Nodes.Literals.IntNode();
                case NodeType.LiteralFloat:
                    return new CustomVisualScripting.Editor.Nodes.Literals.FloatNode();
                case NodeType.LiteralBool:
                    return new CustomVisualScripting.Editor.Nodes.Literals.BoolNode();
                case NodeType.LiteralString:
                    return new CustomVisualScripting.Editor.Nodes.Literals.StringNode();
                case NodeType.MathAdd:
                    return new CustomVisualScripting.Editor.Nodes.Math.AddNode();
                case NodeType.MathSubtract:
                    return new CustomVisualScripting.Editor.Nodes.Math.SubtractNode();
                case NodeType.MathMultiply:
                    return new CustomVisualScripting.Editor.Nodes.Math.MultiplyNode();
                case NodeType.MathDivide:
                    return new CustomVisualScripting.Editor.Nodes.Math.DivideNode();
                case NodeType.CompareEqual:
                    return new CustomVisualScripting.Editor.Nodes.Comparison.EqualNode();
                case NodeType.CompareGreater:
                    return new CustomVisualScripting.Editor.Nodes.Comparison.GreaterNode();
                case NodeType.CompareLess:
                    return new CustomVisualScripting.Editor.Nodes.Comparison.LessNode();
                case NodeType.FlowIf:
                    return new CustomVisualScripting.Editor.Nodes.Flow.IfNode();
                case NodeType.DebugLog:
                    return new CustomVisualScripting.Editor.Nodes.Debug.DebugLogNode();
                case NodeType.UnityGetPosition:
                    return new CustomVisualScripting.Editor.Nodes.Unity.GetPositionNode();
                case NodeType.UnitySetPosition:
                    return new CustomVisualScripting.Editor.Nodes.Unity.SetPositionNode();
                case NodeType.UnityVector3:
                    return new CustomVisualScripting.Editor.Nodes.Unity.Vector3CreateNode();
                case NodeType.VariableGet:
                    return new CustomVisualScripting.Editor.Nodes.Variables.GetVariableNode();
                case NodeType.VariableSet:
                    return new CustomVisualScripting.Editor.Nodes.Variables.SetVariableNode();
                case NodeType.VariableDeclaration:
                    return new CustomVisualScripting.Editor.Nodes.Variables.VariableDeclarationNode();
                default:
                    return null;
            }
        }
        
        private void OnDestroy()
        {
            CleanupGraph();
        }
    }
}