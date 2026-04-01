## 🔧 ТЕХНОЛОГИЧЕСКИЙ СТЕК

| Компонент | Технология | Версия | Для чего |
|-----------|------------|--------|----------|
| **Core (Роль №1)** | .NET 10 | net10.0 | Парсер/генератор |
| **Парсер** | Roslyn | 5.0.0+ | Разбор C# кода |
| **Unity** | Unity Editor | 2026.1+ | Целевая платформа |
| **Визуал (Роль №2)** | Node Graph Processor | последняя | Визуальные ноды |
| **Сериализация** | Newtonsoft.JSON | 3.2.1 | Сохранение графов |
| **Тесты Core** | xUnit | последняя | Тестирование парсера |

---
## 📁 ИТОГОВАЯ СТРУКТУРА ПРОЕКТА
Visual Scripting Plugin для Unity на Node Graph Processor
```text
VisualScripting/ (КОРЕНЬ РЕПОЗИТОРИЯ)
│
├── Core/                                     ← РОЛЬ №1 (ОТДЕЛЬНЫЙ .NET ПРОЕКТ)
│   ├── VisualScripting.Core.csproj
│   ├── VisualScripting.Core.sln
│   ├── Directory.Build.props
│   ├── global.json
│   │
│   ├── Models/
│   │   ├── NodeType.cs
│   │   ├── NodeData.cs
│   │   └── GraphData.cs
│   │
│   ├── Parsers/
│   │   ├── ICodeParser.cs
│   │   ├── ParseResult.cs
│   │   ├── RoslynCodeParser.cs
│   │   └── GraphBuilderWalker.cs
│   │
│   ├── Generators/
│   │   ├── ICodeGenerator.cs
│   │   └── SimpleCodeGenerator.cs
│   │
│   └── Tests/
│       ├── ParserTests.cs
│       ├── GeneratorTests.cs
│       ├── GraphDataTests.cs
│       └── TestHelpers.cs
│
├── UnityProject/                              ← UNITY ПРОЕКТ (2026.1+)
│   ├── Assets/
│   │   └── Plugins/
│   │       └── VisualScripting/
│   │           │
│   │           ├── Core/                      ← DLL ОТ РОЛИ №1 (копируется сюда)
│   │           │   ├── VisualScripting.Core.dll
│   │           │   ├── VisualScripting.Core.xml
│   │           │   ├── Microsoft.CodeAnalysis.dll
│   │           │   ├── Microsoft.CodeAnalysis.CSharp.dll
│   │           │   ├── System.Collections.Immutable.dll
│   │           │   └── link.xml
│   │           │
│   │           ├── NodeGraph/                  ← ПОДКЛЮЧЕННАЯ БИБЛИОТЕКА
│   │           │   ├── Editor/
│   │           │   ├── Runtime/
│   │           │   └── package.json
│   │           │
│   │           ├── Editor/                     ← РОЛЬ №2 (Визуальные ноды)
│   │           │   ├── VisualScripting.Editor.asmdef
│   │           │   │
│   │           │   ├── Nodes/
│   │           │   │   ├── Base/
│   │           │   │   │   ├── BaseNode.cs
│   │           │   │   │   ├── BaseValueNode.cs
│   │           │   │   │   └── BaseExecutionNode.cs
│   │           │   │   │
│   │           │   │   ├── Literals/
│   │           │   │   │   ├── IntNode.cs
│   │           │   │   │   ├── FloatNode.cs
│   │           │   │   │   ├── StringNode.cs
│   │           │   │   │   └── BoolNode.cs
│   │           │   │   │
│   │           │   │   ├── Math/
│   │           │   │   │   ├── AddNode.cs
│   │           │   │   │   ├── SubtractNode.cs
│   │           │   │   │   ├── MultiplyNode.cs
│   │           │   │   │   └── DivideNode.cs
│   │           │   │   │
│   │           │   │   ├── Comparison/
│   │           │   │   │   ├── GreaterNode.cs
│   │           │   │   │   ├── LessNode.cs
│   │           │   │   │   └── EqualNode.cs
│   │           │   │   │
│   │           │   │   ├── Variables/
│   │           │   │   │   ├── VariableDeclarationNode.cs
│   │           │   │   │   ├── GetVariableNode.cs
│   │           │   │   │   └── SetVariableNode.cs
│   │           │   │   │
│   │           │   │   ├── Flow/
│   │           │   │   │   └── IfNode.cs
│   │           │   │   │
│   │           │   │   ├── Unity/
│   │           │   │   │   ├── Vector3CreateNode.cs
│   │           │   │   │   ├── GetPositionNode.cs
│   │           │   │   │   └── SetPositionNode.cs
│   │           │   │   │
│   │           │   │   ├── Debug/
│   │           │   │   │   └── DebugLogNode.cs
│   │           │   │   │
│   │           │   │   └── GraphSerializer.cs
│   │           │   │
│   │           │   └── (другие Editor папки)
│   │           │
│   │           ├── Runtime/                     ← РОЛЬ №2 (Runtime)
│   │           │   ├── VisualScripting.Runtime.asmdef
│   │           │   │
│   │           │   ├── Assets/
│   │           │   │   └── GraphAsset.cs
│   │           │   │
│   │           │   ├── Components/
│   │           │   │   └── VisualScriptBehaviour.cs
│   │           │   │
│   │           │   └── Execution/
│   │           │       ├── GraphRunner.cs
│   │           │       └── NodeExecutor.cs
│   │           │
│   │           ├── Integration/                  ← FULLSTACK (Мосты)
│   │           │   ├── ParserBridge.cs
│   │           │   ├── GeneratorBridge.cs
│   │           │   ├── GraphConverter.cs
│   │           │   ├── GraphSaver.cs
│   │           │   ├── GraphLoader.cs
│   │           │   └── Models/
│   │           │       ├── VisualNodeData.cs
│   │           │       └── CompleteGraphData.cs
│   │           │
│   │           ├── Windows/                      ← FULLSTACK (UI)
│   │           │   ├── VisualScriptingWindow.cs
│   │           │   ├── Views/
│   │           │   │   ├── CodeEditorView.cs
│   │           │   │   ├── ToolbarView.cs
│   │           │   │   └── ErrorPanel.cs
│   │           │   └── Styles/
│   │           │       └── WindowStyles.uss
│   │           │
│   │           ├── Textures/                     ← FULLSTACK
│   │           │   ├── grid_dark.png
│   │           │   └── grid_light.png
│   │           │
│   │           ├── Samples/                      ← FULLSTACK (Примеры)
│   │           │   ├── SimpleMath/
│   │           │   │   ├── SimpleMath.asset
│   │           │   │   └── SimpleMath.unity
│   │           │   └── PlayerController/
│   │           │       ├── PlayerController.asset
│   │           │       └── PlayerController.unity
│   │           │
│   │           └── Documentation/                ← FULLSTACK
│   │               ├── README.md
│   │               ├── INSTALL.md
│   │               ├── API.md
│   │               └── Tutorials/
│   │                   ├── 01_GettingStarted.md
│   │                   ├── 02_FirstGraph.md
│   │                   └── 03_Variables.md
│   │
│   ├── Packages/
│   │   └── manifest.json
│   │
│   └── ProjectSettings/
│       ├── ProjectVersion.txt
│       └── GraphicsSettings.asset
│
├── package.json                                   ← ДЛЯ UPM ПУБЛИКАЦИИ
├── README.md                                      ← ОБЩИЙ README
├── LICENSE                                        ← ЛИЦЕНЗИЯ
├── .gitignore                                     ← ИГНОРИРУЕМЫЕ ФАЙЛЫ
└── .gitattributes                                 ← АТРИБУТЫ GIT
```

# 👨‍💻 РОЛЬ №1: CORE DEVELOPER (Парсер/Генератор)

## 📌 ТЕКУЩЕЕ СОСТОЯНИЕ (УЖЕ ГОТОВО)

### ✅ Что уже реализовано:

#### 1. **Модели данных** (`VisualScripting.Core/Models/`)

**`NodeType.cs`** - enum с 19 типами нод
- Литералы: `VariableInt`, `VariableFloat`, `VariableString`, `VariableBool`
- Математика: `MathAdd`, `MathSubtract`, `MathMultiply`, `MathDivide`
- Сравнение: `CompareGreater`, `CompareLess`, `CompareEqual`
- Переменные: `VariableDeclaration`, `VariableRead`, `VariableAssignment`
- Управление: `IfStatement`
- Unity: `Vector3Create`, `TransformPositionRead`, `TransformPositionSet`
- Вывод: `DebugLog`

**`NodeData.cs`** - структура ноды
- `string Id` - уникальный идентификатор
- `NodeType Type` - тип ноды
- `string Value` - значение (для литералов) или имя переменной
- `string ValueType` - тип данных (int, float, string, bool)
- `Dictionary<string, string> InputConnections` - входные порты (цветные провода)
- `Dictionary<string, string> ExecutionFlow` - поток выполнения (белые провода)

**`GraphData.cs`** - контейнер для графа
- `List<NodeData> Nodes` - список всех нод

#### 2. **Парсер** (`VisualScripting.Core/Parsers/`)

**`ICodeParser.cs`** - интерфейс
- `ParseResult Parse(string code)`

**`ParseResult.cs`** - результат парсинга
- `GraphData Graph` - полученный граф
- `List<string> Errors` - список ошибок
- `bool HasErrors` - флаг наличия ошибок

**`RoslynCodeParser.cs`** - основной парсер
- Создает синтаксическое дерево из кода
- Собирает диагностические ошибки
- Запускает `GraphBuilderWalker`

**`GraphBuilderWalker.cs`** - обходчик AST
- Обходит синтаксическое дерево
- Создает ноды для всех 19 типов
- Поддерживает переменные, if, transform, Vector3
- Формирует `InputConnections` и `ExecutionFlow`

#### 3. **Генератор** (`VisualScripting.Core/Generators/`)

**`ICodeGenerator.cs`** - интерфейс
- `string GenerateCode(GraphData graph)`

**`SimpleCodeGenerator.cs`** - основной генератор
- Находит стартовые ноды (без входящего ExecutionFlow)
- Рекурсивно обходит граф по `ExecutionFlow`
- Генерирует валидный C# код
- Поддерживает все 19 типов нод
- Защита от циклических ссылок

#### 4. **Тесты** (`VisualScripting.Core/Tests/`)

**`ParserTests.cs`** - 12+ юнит-тестов
- Тест базового выражения: `Debug.Log(1 + 2);`
- Тест переменных и Execution Flow
- Тест объявления без инициализации
- Тест transform и Vector3
- Тест if/else
- Тест if с несколькими инструкциями
- Тест литерала `1.5` (double)
- Тест защиты от циклических графов
- Тест синтаксических ошибок
- Тест операторов сравнения (`>`, `<`, `==`)
- **Тест демо-скрипта** (интеграционный):
```csharp
float speed = 5;
float offset = speed * 0.1f;
if (speed > 3)
{
    transform.position = new Vector3(offset, 0, 0);
    UnityEngine.Debug.Log(speed);
}
else
{
    UnityEngine.Debug.Log(0);
}
```

---

## 📌 ЧТО НУЖНО СДЕЛАТЬ (Роль №1)

### 1. **Подготовка DLL для Unity**
   - Собрать проект под `net10.0`
   - Получить `VisualScripting.Core.dll`
   - Собрать все зависимые DLL:
     - `Microsoft.CodeAnalysis.dll`
     - `Microsoft.CodeAnalysis.CSharp.dll`
     - `System.Collections.Immutable.dll`
   - Проверить совместимость с Unity 2026.1

### 2. **XML-документация**
   - Добавить XML-комментарии ко всем public методам
   - Сгенерировать файл `VisualScripting.Core.xml`

### 3. **Оптимизация (опционально)**
   - Кэширование результатов парсинга
   - Оптимизация обхода больших графов

### 4. **Поддержка**
   - Консультации по моделям данных
   - Исправление багов по мере нахождения

---

## 📦 ЧТО СДАЕШЬ (Роль №1)

```
VisualScripting.Core.zip
├── VisualScripting.Core.dll
├── Microsoft.CodeAnalysis.dll
├── Microsoft.CodeAnalysis.CSharp.dll
├── System.Collections.Immutable.dll
├── VisualScripting.Core.xml
└── Tests/ (исходники тестов, опционально)
```

---

# 👨‍🎨 РОЛЬ №2: GRAPH DEVELOPER (Визуальные ноды + Runtime)

## 📌 ТЕКУЩЕЕ СОСТОЯНИЕ

**НИЧЕГО НЕ СДЕЛАНО** - начинаешь с нуля.

---

## 📌 ЧТО НУЖНО СДЕЛАТЬ (Роль №2)

### 1. **Подключить Node Graph Processor** (1 день)

**Действия:**
- В Unity: `Window → Package Manager → + → Add package from git URL`
- Вставить: `https://github.com/alelievr/NodeGraphProcessor.git`
- Проверить, что библиотека появилась в `Packages/`
- Создать тестовый граф для ознакомления

---

---

### 2. **Создать базовые классы** 

#### `BaseNode.cs`
**Назначение:** Базовый класс для всех визуальных нод  
**Где лежит:** `Editor/Nodes/Base/BaseNode.cs`  
**Что должно быть:**
- Наследование от `Node` (из Node Graph Processor)
- Поле `public string NodeId` - для связи с Core
- Метод `public virtual void InitializeFromData(NodeData data)` - заполняет ноду из данных Core
- Метод `public virtual NodeData ToNodeData()` - создает NodeData из визуальной ноды
- Виртуальные методы для переопределения в наследниках

#### `BaseValueNode.cs`
**Назначение:** Для нод, которые возвращают значение  
**Где лежит:** `Editor/Nodes/Base/BaseValueNode.cs`  
**Наследуется от:** `BaseNode`  
**Что должно быть:**
- Выходной порт `value` (тип object или generic)
- Абстрактный метод `object GetValue()`
- Поддержка разных типов данных (int, float, string, bool, Vector3)

#### `BaseExecutionNode.cs`
**Назначение:** Для нод с потоком выполнения  
**Где лежит:** `Editor/Nodes/Base/BaseExecutionNode.cs`  
**Наследуется от:** `BaseNode`  
**Что должно быть:**
- Входной Execution порт: `execIn`
- Выходной Execution порт: `execOut` (для последовательного выполнения)
- Дополнительные Execution порты для if (true/false)
- Методы для управления потоком

---

### 4. **Реализовать ВСЕ 19 типов нод** (3-4 недели)

#### 📁 **Категория: Literals (4 ноды) - простые**

**Файл:** `Editor/Nodes/Literals/IntNode.cs`
- **Назначение:** Целочисленная константа
- **Порты:** 1 выходной (`value`)
- **UI:** Поле ввода для целого числа
- **Данные:** Хранит значение в `NodeData.Value`

**Файл:** `Editor/Nodes/Literals/FloatNode.cs`
- **Назначение:** Число с плавающей точкой
- **Порты:** 1 выходной (`value`)
- **UI:** Поле ввода для числа (с поддержкой суффикса f)
- **Данные:** Хранит значение в `NodeData.Value`

**Файл:** `Editor/Nodes/Literals/StringNode.cs`
- **Назначение:** Строковая константа
- **Порты:** 1 выходной (`value`)
- **UI:** Текстовое поле
- **Данные:** Хранит значение в `NodeData.Value`

**Файл:** `Editor/Nodes/Literals/BoolNode.cs`
- **Назначение:** Булево значение
- **Порты:** 1 выходной (`value`)
- **UI:** Чекбокс или выпадающий список (true/false)
- **Данные:** Хранит значение в `NodeData.Value`

---

#### 📁 **Категория: Math (4 ноды) - средние**

**Файл:** `Editor/Nodes/Math/AddNode.cs`
- **Назначение:** Сложение (a + b)
- **Порты:** 2 входных (`left`, `right`), 1 выходной (`result`)
- **Логика:** Возвращает `left + right`
- **Поддержка:** Работает с int, float

**Файл:** `Editor/Nodes/Math/SubtractNode.cs`
- **Назначение:** Вычитание (a - b)
- **Порты:** 2 входных (`left`, `right`), 1 выходной (`result`)
- **Логика:** Возвращает `left - right`

**Файл:** `Editor/Nodes/Math/MultiplyNode.cs`
- **Назначение:** Умножение (a * b)
- **Порты:** 2 входных (`left`, `right`), 1 выходной (`result`)
- **Логика:** Возвращает `left * right`

**Файл:** `Editor/Nodes/Math/DivideNode.cs`
- **Назначение:** Деление (a / b)
- **Порты:** 2 входных (`left`, `right`), 1 выходной (`result`)
- **Логика:** Возвращает `left / right` (проверка на 0)

---

#### 📁 **Категория: Comparison (3 ноды) - средние**

**Файл:** `Editor/Nodes/Comparison/GreaterNode.cs`
- **Назначение:** Больше (a > b)
- **Порты:** 2 входных (`left`, `right`), 1 выходной (`result` - bool)
- **Логика:** Возвращает `left > right`

**Файл:** `Editor/Nodes/Comparison/LessNode.cs`
- **Назначение:** Меньше (a < b)
- **Порты:** 2 входных (`left`, `right`), 1 выходной (`result` - bool)
- **Логика:** Возвращает `left < right`

**Файл:** `Editor/Nodes/Comparison/EqualNode.cs`
- **Назначение:** Равно (a == b)
- **Порты:** 2 входных (`left`, `right`), 1 выходной (`result` - bool)
- **Логика:** Возвращает `left == right`

---

#### 📁 **Категория: Variables (3 ноды) - сложные**

**Файл:** `Editor/Nodes/Variables/VariableDeclarationNode.cs`
- **Назначение:** Объявление переменной (int a = 5;)
- **Порты:** 
  - Входной данных: `value` (начальное значение)
  - Входной Execution: `execIn`
  - Выходной Execution: `execOut`
- **UI:** 
  - Поле для имени переменной
  - Выпадающий список для типа (int, float, string, bool)
- **Логика:** Создает переменную в области видимости

**Файл:** `Editor/Nodes/Variables/GetVariableNode.cs`
- **Назначение:** Чтение переменной (...a...)
- **Порты:** 1 выходной (`value`)
- **UI:** Выпадающий список со всеми объявленными переменными
- **Логика:** Возвращает текущее значение переменной

**Файл:** `Editor/Nodes/Variables/SetVariableNode.cs`
- **Назначение:** Присваивание переменной (a = 10;)
- **Порты:**
  - Входной данных: `value` (новое значение)
  - Входной Execution: `execIn`
  - Выходной Execution: `execOut`
- **UI:** Выпадающий список со всеми объявленными переменными
- **Логика:** Устанавливает новое значение переменной

---

#### 📁 **Категория: Flow (1 нода) - очень сложная**

**Файл:** `Editor/Nodes/Flow/IfNode.cs`
- **Назначение:** Условный оператор (if/else)
- **Порты:**
  - Входной данных: `condition` (bool)
  - Входной Execution: `execIn`
  - Выходной Execution: `execTrue` (если condition == true)
  - Выходной Execution: `execFalse` (если condition == false)
  - Выходной Execution: `execOut` (после if)
- **Логика:**
  - Проверяет условие
  - Передает поток в соответствующую ветку
  - После выполнения ветки возвращается к `execOut`

---

#### 📁 **Категория: Unity (3 ноды) - средние**

**Файл:** `Editor/Nodes/Unity/Vector3CreateNode.cs`
- **Назначение:** Создание Vector3 (new Vector3(x, y, z))
- **Порты:** 3 входных (`x`, `y`, `z`), 1 выходной (`result` - Vector3)
- **Логика:** Возвращает `new Vector3(x, y, z)`

**Файл:** `Editor/Nodes/Unity/GetPositionNode.cs`
- **Назначение:** Чтение transform.position
- **Порты:** 1 выходной (`position` - Vector3)
- **Логика:** Возвращает `transform.position` (в рантайме)

**Файл:** `Editor/Nodes/Unity/SetPositionNode.cs`
- **Назначение:** Установка transform.position
- **Порты:**
  - Входной данных: `value` (Vector3)
  - Входной Execution: `execIn`
  - Выходной Execution: `execOut`
- **Логика:** Устанавливает `transform.position = value`

---

#### 📁 **Категория: Debug (1 нода) - средняя**

**Файл:** `Editor/Nodes/Debug/DebugLogNode.cs`
- **Назначение:** Вывод в консоль (Debug.Log)
- **Порты:**
  - Входной данных: `message` (object)
  - Входной Execution: `execIn`
  - Выходной Execution: `execOut`
- **Логика:** Вызывает `UnityEngine.Debug.Log(message)`

---

### 4. **Создать GraphSerializer** (1 неделя)

**Файл:** `Editor/Nodes/GraphSerializer.cs`

**Назначение:** Конвертация между визуальным графом (Node Graph Processor) и логическим графом (Core)

**Метод 1:** `ToGraphData(VisualScriptingGraph graph)`
- Вход: визуальный граф (с нодами и соединениями)
- Выход: `GraphData` (для Core)
- **Что делает:**
  - Проходит по всем нодам в графе
  - Для каждой ноды вызывает `ToNodeData()`
  - Собирает все соединения данных (`InputConnections`)
  - Собирает все соединения потока (`ExecutionFlow`)
  - Формирует `GraphData`

**Метод 2:** `FromGraphData(VisualScriptingGraph graph, GraphData data)`
- Вход: пустой визуальный граф + `GraphData` от Core
- Выход: заполненный визуальный граф
- **Что делает:**
  - Очищает граф
  - Для каждой ноды из `data.Nodes` создает соответствующий визуальный класс
  - Вызывает `InitializeFromData()` для заполнения
  - Создает соединения данных из `InputConnections`
  - Создает соединения потока из `ExecutionFlow`

---

### 5. **Создать Runtime систему** (1-2 недели)

#### `GraphAsset.cs`
**Где лежит:** `Runtime/Assets/GraphAsset.cs`  
**Тип:** `ScriptableObject`  
**Назначение:** Хранение графа как ассета в проекте  
**Содержимое:**
- `public CompleteGraphData graphData` - полные данные графа (логика + визуал)
- Метод создания через меню `Assets → Create → Visual Scripting → Graph`

#### `VisualScriptBehaviour.cs`
**Где лежит:** `Runtime/Components/VisualScriptBehaviour.cs`  
**Тип:** `MonoBehaviour`  
**Назначение:** Компонент для запуска графа на GameObject  
**Поля:**
- `public GraphAsset graphAsset` - ссылка на граф
- `private GraphRunner runner` - раннер

**Методы:**
- `void Start()` - создает `GraphRunner` из `graphAsset`
- `void Update()` - вызывает `runner.Update()` каждый кадр
- `void Execute()` - ручной запуск выполнения

#### `GraphRunner.cs`
**Где лежит:** `Runtime/Execution/GraphRunner.cs`  
**Назначение:** Выполнение графа в рантайме  
**Поля:**
- `private GraphData graph` - логический граф
- `private Dictionary<string, object> variables` - значения переменных
- `private string currentNodeId` - текущая выполняемая нода

**Методы:**
- `void Start()` - находит стартовые ноды и начинает выполнение
- `void Update()` - продолжает выполнение (для многошаговых операций)
- `void ExecuteNode(NodeData node)` - выполняет одну ноду
- `object GetValueFromPort(string nodeId, string portName)` - получает значение из порта

#### `NodeExecutor.cs`
**Где лежит:** `Runtime/Execution/NodeExecutor.cs`  
**Назначение:** Статические методы выполнения конкретных типов нод  
**Методы:**
- `object ExecuteMathNode(NodeData node, Dictionary<string, object> inputs)` - математика
- `object ExecuteComparisonNode(NodeData node, Dictionary<string, object> inputs)` - сравнение
- `void ExecuteDebugNode(NodeData node, Dictionary<string, object> inputs)` - Debug.Log
- `Vector3 ExecuteVector3Create(Dictionary<string, object> inputs)` - создание Vector3
- И т.д. для всех типов

---

## 📦 ЧТО СДАЕШЬ (Роль №2)

```
VisualScripting/ (корень репозитория)
│
├── Core/                                     ← РОЛЬ №1
│
└── UnityProject/                              ← UNITY ПРОЕКТ
    └── Assets/
        └── Plugins/
            └── VisualScripting/
                │
                ├── Core/                      ← DLL от Роли №1 (уже есть)
                │   └── VisualScripting.Core.dll
                │
                ├── Editor/                     ← ТВОЯ ПАПКА (РОЛЬ №2)
                │   ├── VisualScripting.Editor.asmdef
                │   │
                │   ├── Nodes/
                │   │   ├── Base/
                │   │   │   ├── BaseNode.cs
                │   │   │   ├── BaseValueNode.cs
                │   │   │   └── BaseExecutionNode.cs
                │   │   │
                │   │   ├── Literals/
                │   │   │   ├── IntNode.cs
                │   │   │   ├── FloatNode.cs
                │   │   │   ├── StringNode.cs
                │   │   │   └── BoolNode.cs
                │   │   │
                │   │   ├── Math/
                │   │   │   ├── AddNode.cs
                │   │   │   ├── SubtractNode.cs
                │   │   │   ├── MultiplyNode.cs
                │   │   │   └── DivideNode.cs
                │   │   │
                │   │   ├── Comparison/
                │   │   │   ├── GreaterNode.cs
                │   │   │   ├── LessNode.cs
                │   │   │   └── EqualNode.cs
                │   │   │
                │   │   ├── Variables/
                │   │   │   ├── VariableDeclarationNode.cs
                │   │   │   ├── GetVariableNode.cs
                │   │   │   └── SetVariableNode.cs
                │   │   │
                │   │   ├── Flow/
                │   │   │   └── IfNode.cs
                │   │   │
                │   │   ├── Unity/
                │   │   │   ├── Vector3CreateNode.cs
                │   │   │   ├── GetPositionNode.cs
                │   │   │   └── SetPositionNode.cs
                │   │   │
                │   │   ├── Debug/
                │   │   │   └── DebugLogNode.cs
                │   │   │
                │   │   └── GraphSerializer.cs
                │   │
                │   └── (другие Editor папки будут добавлять другие)
                │
                ├── Runtime/                     ← ТВОЯ ПАПКА (РОЛЬ №2)
                │   ├── VisualScripting.Runtime.asmdef
                │   │
                │   ├── Assets/
                │   │   └── GraphAsset.cs
                │   │
                │   ├── Components/
                │   │   └── VisualScriptBehaviour.cs
                │   │
                │   └── Execution/
                │       ├── GraphRunner.cs
                │       └── NodeExecutor.cs
                │
                ├── Integration/                 ← FULLSTACK
                ├── Windows/                     ← FULLSTACK
                ├── Documentation/                ← FULLSTACK
                └── (остальные папки)
```

---

# 🤝 ВЗАИМОДЕЙСТВИЕ МЕЖДУ РОЛЯМИ

## Роль №1 → Роль №2
- Передает `VisualScripting.Core.dll`
- Передает спецификацию `GraphData` и `NodeData`
- Консультирует по форматам данных

## Роль №2 → Роль №1
- Запрашивает уточнения по моделям данных
- Сообщает о несоответствиях

## Роль №2 → FULLSTACK
- Передает готовые визуальные компоненты
- Передает `GraphSerializer` для интеграции

## FULLSTACK → Все
- Координирует сроки
- Собирает всё в работающий продукт

---

## 📊 ГРАФИК РАБОТЫ (Роль №2)

| Задачи |
|--------|
| Подключение Node Graph, структура папок, базовые классы |
| Литералы (4 ноды) + Математика (4 ноды) |
| Сравнение (3 ноды) + начало Переменных |
| Переменные (3 ноды) + If (1 нода) |
| Unity-ноды (3 ноды) + Debug (1 нода) |
| GraphSerializer |
| Runtime система |
| Финальные правки, тестирование |

---

## ✅ КРИТЕРИИ ГОТОВНОСТИ (Роль №2)

- [ ] Все 19 классов нод созданы и работают
- [ ] Ноды можно создавать на канвасе
- [ ] Порты соединяются проводами
- [ ] `GraphSerializer` правильно конвертирует в/из `GraphData`
- [ ] Runtime выполняет простые графы
- [ ] Нет ошибок в консоли Unity
