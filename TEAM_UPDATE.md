# Обращение к команде: парсер, граф и плагин (MVP)

Привет, команда!

Ниже — **актуальное состояние** после перехода парсера на **Roslyn** и согласования контракта графа с тимлидом. **Sprint 2 (4–16 апреля 2026):** циклы, встроенные методы, порты потока, исправление сохранения JSON с `Vector2`.

---

## Обновление от 04.04.2026 — Sprint 2 (Backend 1, до предзащиты)

| Область | Изменение |
|--------|-----------|
| **Сохранение графа** | Исправлена ошибка Newtonsoft `Self referencing loop … Vector2.normalized`: [`Vector2JsonConverter`](UnityPackage_Extracted/Assets/Plugins/CustomVisualScripting/Integration/Vector2JsonConverter.cs) + `ReferenceLoopHandling.Ignore` в [`GraphSaver`](UnityPackage_Extracted/Assets/Plugins/CustomVisualScripting/Integration/GraphSaver.cs). |
| **`NodeType`** | Добавлены: `FlowFor`, `FlowWhile`, `ConsoleWriteLine`, `IntParse`, `FloatParse`, `ToStringConvert`, `MathfAbs`, `MathfMax`, `MathfMin` (Core + Unity `Core/Models` синхронизированы). |
| **Парсер** | `for` / `while` с портами `execIn`/`execOut`, `init`/`condition`/`increment`/`body` (for), `condition`/`body` (while). Составные присваивания `+=`…`%=`, пре/пост `++`/`--`. Вызовы: `Console.WriteLine`, `int.Parse` / `float.Parse`, `.ToString()`, `Mathf.Abs`/`Max`/`Min`. В обёртку парсера добавлена **заглушка `Mathf`**, чтобы разбор работал вне Unity. |
| **Генератор** | `SimpleCodeGenerator`: `for`/`while`, `Console.WriteLine`, встроенные методы; `System.Math` для Abs/Max/Min в генерируемом C#; корни потока — любая первая инструкция без входящего `execIn` (в т.ч. одиночный `Console.WriteLine`). Составные операции в коде дают эквивалент (`a += 2` → `a = a + 2`). |
| **Runtime (Unity)** | `NodeExecutor`: `ConsoleWriteLine` → `Debug.Log`, `IntParse`/`FloatParse`/`ToStringConvert`, `Mathf*`. `GraphRunner` пропускает `FlowFor`/`FlowWhile` (как `FlowIf`), без полноценного исполнения циклов в рантайме. |
| **Интеграция / редактор** | `GraphConverter`, `GraphSerializer` — цвета и подписи для новых типов. |
| **Тесты** | [`GeneratorTests.cs`](VisualScripting.Tests/GeneratorTests.cs) — **27** тестов (`dotnet test` зелёный). |

---

## Обновление от 28.03.2026 — что сделано (Backend 1 + интеграция)

| Область | Изменение |
|--------|-----------|
| **Парсер** | `RoslynCodeParser` переписан на **Microsoft.CodeAnalysis** (обход AST, без Regex и без Shunting Yard). |
| **Модель** | В `NodeData` добавлено поле **`VariableName`**. Парсер **не заполняет** `ExecutionFlow` / `InputConnections` — только **`Edges`** с именами портов по контракту. |
| **`NodeType`** | Добавлены: `MathModulo`, `CompareNotEqual`, `CompareGreaterOrEqual`, `CompareLessOrEqual`, `LogicalAnd`, `LogicalOr`, `LogicalNot`, `FlowElse`. Enum в Core и в `UnityPackage_Extracted` **синхронизирован**. |
| **Рёбра** | Единая схема портов: математика `inputA` / `inputB` / `output`; сравнения и логика (`&&`, ИЛИ, `!`) — `left` / `right` / `result` или `input` / `result` для `!`; `If` — `condition`, поток `true` / `false`; выполнение — `execIn` / `execOut`. |
| **Присваивание** | Результат — **один узел операции** с `VariableName` (без отдельного узла объявления для `z` в `int z = x + y`). Ссылки на переменные — **прямые рёбра** к узлу-источнику, **без VariableGet**. |
| **if / else if / else** | Цепочка **`FlowIf`** по ветке `false`, финальный **`FlowElse`**; тела — рёбра потока между инструкциями. |
| **Editor-ноды** | `CustomBaseNode` наследует **`BaseExecutionNode`** (есть `execIn` / `execOut`). Обновлены порты у математики и литералов. Добавлены **7 новых нод** + **`ElseNode`**. **`IfNode`** переведён на потоковую модель (`condition` + выходы `true` / `false`). |
| **Интеграция** | `GraphConverter` / `GraphSerializer` — цвета и имена для новых типов. `ParserBridge` логирует число нод и рёбер после парса. |
| **Генератор** | `SimpleCodeGenerator.GenerateCode` — **MVP-подмножество**: литералы с именем + бинарная математика по `Edges` (для простого roundtrip в тестах). |
| **Тесты** | Удалён устаревший `ParserCodegenTests` (ожидал другой парсер и Unity-скрипты). Добавлен **`RoslynParserMvpTests`** (5 тестов), `dotnet test` зелёный. |
| **Runtime** | `NodeExecutor` переведён на **`switch` по `NodeType`**, чтение входов из **`Edges`** (плюс legacy `InputConnections`). `GraphRunner` передаёт граф в executor. |

**Файлы «источник правды» в репозитории:** `VisualScripting.Core/` (парсер, модели, генератор, тесты); зеркало под Unity — `UnityPackage_Extracted/Assets/Plugins/CustomVisualScripting/`.

---

## Контракт портов (кратко, для Backend 2 и Fullstack)

| Тип узла | Входы | Выходы |
|----------|--------|--------|
| Литералы | — | `output` |
| Math (Add, Subtract, Multiply, Divide, Modulo) | `inputA`, `inputB` | `output` |
| Compare (Equal, Greater, Less, NotEqual, >=, <=) | `left`, `right` | `result` |
| And / Or | `left`, `right` | `result` |
| Not | `input` | `result` |
| If | `execIn`, `condition` | `true`, `false` (ветки потока); далее по цепочке `execOut` у листьевых инструкций |
| Else | `execIn` | `execOut` |
| **For** | `execIn`, данные: `init`, `condition`, `increment` | `body` (поток в тело), `execOut` (после цикла) |
| **While** | `execIn`, `condition` | `body`, `execOut` |
| **Console.WriteLine** | `execIn`, `message` | `execOut` |
| **int.Parse / float.Parse** | `input` | `output` |
| **ToString** | `input` | `output` |
| **Mathf.Abs** | `input` | `output` |
| **Mathf.Max / Min** | `inputA`, `inputB` | `output` |
| VariableSet | `execIn`, `value` | `execOut` |
| Прочие (Unity, переменные) | см. существующие ноды | — |

`VariableName`: непустой только у литерала с объявлением и у **корневого** узла результата присваивания; у промежуточных операций — пустая строка.

---

## Скоуп парсера (актуально после Sprint 2)

**Поддерживается:**

- Объявления `int x = …`, `float`, `string`, `bool` **с инициализатором** и **без инициализатора** (`int x;`).
- Простое присваивание `z = …` и **составные** `+=`, `-=`, `*=`, `/=`, `%=`.
- Префиксный/постфиксный `++` / `--` (как цепочка `Math` + `VariableSet`).
- Арифметика: `+ - * / %`, скобки, приоритеты из Roslyn.
- Сравнения: `== != > < >= <=`.
- Логика: `&&`, `||`, `!`.
- `if` / `else if` / `else`, вложенные `if`.
- Циклы **`for`**, **`while`** (в т.ч. вложенные в блоки).
- Вызовы: **`Console.WriteLine(...)`**, **`int.Parse`**, **`float.Parse`**, **`.ToString()`**, **`Mathf.Abs` / `Max` / `Min`** (через заглушку `Mathf` в обёртке парсера).

**Пока не поддерживается или ограничено:**

- Прочие вызовы методов (`Debug.Log`, `Math.Pow`, произвольные API), Unity-специфика (`transform`, `Vector3`, …).
- Массивы, дженерики, `switch`, `return`, async — по-прежнему вне скоупа.
- **Рантайм:** ветвление и циклы в `GraphRunner` не исполняются по полной семантике C# (узлы потока пропускаются или упрощённо); для демо опираться на **генератор кода** и тесты.

Оператор степени в стиле Python **не входит**.

---

## Что НЕ входит в текущий скоуп (напоминание)

- Произвольные вызовы методов и Unity API, кроме перечисленных выше
- Массивы, коллекции, обобщённые типы
- `switch`, `return`, корутины, async
- Полное пошаговое исполнение циклов/ветвлений в `GraphRunner` без доработки Backend 2

---

## Пример кода для ручной проверки парсера в редакторе

```csharp
int x = 10;
int y = 20;
int z = x + y * 2 % 3;
bool flag = true;
if (x >= y && z != 0 || !flag)
{
    z = x + y;
}
else
{
    z = x - y;
}
```

Ожидается: ноды литералов, `Multiply` → `Modulo` → `Add`, цепочка логики к `FlowIf`, ветка `FlowElse`, рёбра с портами из таблицы выше.

---

## Задачи по ролям (обновлено)

### Backend 1 (парсер / мост) — статус

Sprint 2 (ТЗ 4–16 апреля): циклы, встроенные методы, порты потока в рёбрах, исправление JSON/`Vector2`, тесты расширены. Дальше — согласование с Backend 2 / Fullstack по **визуальным** нодам `For`/`While`/`Console.WriteLine` и по исполнению циклов в рантайме.

### Backend 2 (генератор / рантайм)

- Ориентироваться на **`Edges`** и **`VariableName`**, а не на `ExecutionFlow` от парсера.
- Расширять `SimpleCodeGenerator` под `if`, сравнения и логику по мере необходимости; согласовать порядок эмиссии инструкций с топологией графа.

### Fullstack / визуальный граф (GraphProcessor)

- Подключить отображение новых типов нод и проводов с **именами портов из контракта**.
- Доработать **`GraphSerializer`**: сериализация/десериализация **`Edges`** (сейчас в коде помечено TODO).

### Тимлид / сценарий демо

- Для записи демо использовать **фрагменты из раздела «Пример кода»** или согласовать новый целевой скрипт после расширения парсера под Unity.

---

## Вызов API (без изменений по смыслу)

```csharp
// Код → граф
var result = ParserBridge.Parse(codeText);
if (result.HasErrors) {
    foreach (var err in result.Errors)
        Debug.LogError(err);
} else {
    // загрузка result.Graph в UI / GraphView
}

// Граф → код (MVP генератора — ограниченный подмножество)
var generator = new SimpleCodeGenerator();
string code = generator.GenerateCode(graph);
```

---

## Тесты (актуально)

| Файл / класс | Количество | Назначение |
|----------------|------------|------------|
| `GeneratorTests.cs` | 27 | Арифметика, `%`, сравнения, логика, `if`/`else if`/`else`, объявление без инициализатора, `for` + составное присваивание в теле, `while` + декремент, `Console.WriteLine`, `int`/`float`.Parse, `ToString`, `+=`, `Mathf` Abs/Max/Min, roundtrip через `SimpleCodeGenerator`. |

Запуск: из корня репозитория `dotnet test VisualScripting.Tests/VisualScripting.Tests.csproj`.

---

## Структура в репозитории

```
VisualScripting.Core/           ← парсер, модели, SimpleCodeGenerator, тесты (ссылка из .Tests)
UnityPackage_Extracted/Assets/Plugins/CustomVisualScripting/
├── Core/                      ← зеркало моделей и RoslynCodeParser (исходники; в Unity нужны ещё DLL Roslyn)
├── Editor/Nodes/              ← визуальные ноды (GraphProcessor)
├── Integration/               ← ParserBridge, GraphConverter, GraphSaver (+ Vector2JsonConverter)
└── Runtime/Execution/         ← NodeExecutor, GraphRunner
```

**Точка внимания при merge:** `NodeType.cs`, `NodeData.cs`, `GraphData.cs`, `RoslynCodeParser.cs` — правки только по согласованному контракту.

---

Если нужна короткая выжимка для чата команды — переслать раздел **«Обновление от 04.04.2026 — Sprint 2»** и таблицу **«Контракт портов»**.
