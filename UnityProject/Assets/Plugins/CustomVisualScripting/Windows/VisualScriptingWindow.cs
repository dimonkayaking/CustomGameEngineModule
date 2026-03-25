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
using CustomVisualScripting.Editor.Nodes.Unity;
using CustomVisualScripting.Editor.Nodes.Variables;
using CustomToolbar = CustomVisualScripting.Windows.Views.ToolbarView;

namespace CustomVisualScripting.Windows
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
            _internalGraph = ScriptableObject.CreateInstance<BaseGraph>();
            
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
            
            var placeholder = new Label("Здесь будет граф");
            placeholder.style.marginTop = 20;
            placeholder.style.marginLeft = 10;
            placeholder.style.color = Color.gray;
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
            
            // Сохраняем позиции узлов из визуального графа
            if (_graphView != null)
            {
                foreach (var nodeView in _graphView.nodeViews)
                {
                    if (nodeView.nodeTarget is CustomBaseNode customNode)
                    {
                        var nodeData = _currentGraph.LogicGraph.Nodes.FirstOrDefault(n => n.Id == customNode.NodeId);
                        if (nodeData != null)
                        {
                            nodeData.Value = customNode.ToNodeData().Value;
                            nodeData.ValueType = customNode.ToNodeData().ValueType;
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
                var placeholder = new Label("Нет нод для отображения");
                placeholder.style.marginTop = 20;
                placeholder.style.marginLeft = 10;
                placeholder.style.color = Color.gray;
                _graphContainer.Add(placeholder);
                return;
            }
            
            try
            {
                // Очищаем старый граф
                if (_internalGraph != null)
                {
                    DestroyImmediate(_internalGraph);
                }
                
                // Создаем новый граф
                _internalGraph = ScriptableObject.CreateInstance<BaseGraph>();
                
                // Добавляем узлы из данных
                foreach (var nodeData in _currentGraph.LogicGraph.Nodes)
                {
                    var node = CreateNodeFromData(nodeData);
                    if (node != null)
                    {
                        node.NodeId = nodeData.Id;
                        node.InitializeFromData(nodeData);
                        _internalGraph.AddNode(node);
                    }
                }
                
                // Создаем визуальное представление
                if (_graphView != null)
                {
                    _graphView.Dispose();
                }
                
                _graphView = new BaseGraphView();
                _graphView.Initialize(_internalGraph);
                _graphView.style.flexGrow = 1;
                
                // Устанавливаем позиции узлов из сохраненных данных
                if (_currentGraph.VisualNodes != null)
                {
                    foreach (var visualNode in _currentGraph.VisualNodes)
                    {
                        var nodeView = _graphView.nodeViews.FirstOrDefault(v => 
                            (v.nodeTarget as CustomBaseNode)?.NodeId == visualNode.NodeId);
                        if (nodeView != null)
                        {
                            nodeView.SetPosition(new Rect(visualNode.Position, Vector2.zero));
                            nodeView.SetCollapsed(visualNode.IsCollapsed);
                        }
                    }
                }
                
                _graphView.OnNodeSelected = (nodeView) => { /* TODO: выделение узла */ };
                
                _graphContainer.Add(_graphView);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VS] Ошибка создания графа: {e.Message}");
                
                var errorLabel = new Label($"Ошибка отображения графа: {e.Message}");
                errorLabel.style.color = Color.red;
                errorLabel.style.marginTop = 20;
                errorLabel.style.marginLeft = 10;
                _graphContainer.Add(errorLabel);
            }
        }
        
        private CustomBaseNode CreateNodeFromData(NodeData data)
        {
            if (data == null) return null;
            
            CustomBaseNode node = data.Type switch
            {
                NodeType.LiteralInt => new IntNode(),
                NodeType.LiteralFloat => new FloatNode(),
                NodeType.LiteralBool => new BoolNode(),
                NodeType.LiteralString => new StringNode(),
                NodeType.MathAdd => new AddNode(),
                NodeType.MathSubtract => new SubtractNode(),
                NodeType.MathMultiply => new MultiplyNode(),
                NodeType.MathDivide => new DivideNode(),
                NodeType.CompareEqual => new EqualNode(),
                NodeType.CompareGreater => new GreaterNode(),
                NodeType.CompareLess => new LessNode(),
                NodeType.FlowIf => new IfNode(),
                NodeType.DebugLog => new DebugLogNode(),
                NodeType.UnityGetPosition => new GetPositionNode(),
                NodeType.UnitySetPosition => new SetPositionNode(),
                NodeType.UnityVector3 => new Vector3CreateNode(),
                NodeType.VariableGet => new GetVariableNode(),
                NodeType.VariableSet => new SetVariableNode(),
                NodeType.VariableDeclaration => new VariableDeclarationNode(),
                _ => null
            };
            
            if (node != null)
            {
                node.InitializeFromData(data);
            }
            
            return node;
        }
        
        private void OnDestroy()
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
    }
}