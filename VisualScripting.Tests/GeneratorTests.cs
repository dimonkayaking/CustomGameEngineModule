using VisualScripting.Core.Generators;
using VisualScripting.Core.Models;
using VisualScripting.Core.Parsers;

namespace VisualScripting.Tests;

public class GeneratorTests
{
    private readonly RoslynCodeParser _parser = new();
    private readonly SimpleCodeGenerator _generator = new();

    private string Roundtrip(string code)
    {
        var result = _parser.Parse(code);
        Assert.False(result.HasErrors, string.Join("\n", result.Errors));
        return _generator.Generate(result.Graph);
    }

    [Fact]
    public void SimpleArithmetic()
    {
        var code = "int x = 10;\nint y = 20;\nint z = x + y;";
        var output = Roundtrip(code);
        Assert.Contains("int x = 10;", output);
        Assert.Contains("int y = 20;", output);
        Assert.Contains("int z = x + y;", output);
    }

    [Fact]
    public void ArithmeticWithIntermediateNode()
    {
        var code = "int x = 10;\nint y = 20;\nint z = x + y * 2;";
        var output = Roundtrip(code);
        Assert.Contains("int x = 10;", output);
        Assert.Contains("int y = 20;", output);
        Assert.Contains("int z = x + (y * 2);", output);
    }

    [Fact]
    public void ModuloOperator()
    {
        var code = "int x = 10;\nint y = 3;\nint z = x % y;";
        var output = Roundtrip(code);
        Assert.Contains("int z = x % y;", output);
    }

    [Fact]
    public void ComparisonOperators()
    {
        var code = "int a = 5;\nint b = 10;\nbool r1 = a >= b;\nbool r2 = a <= b;\nbool r3 = a != b;";
        var output = Roundtrip(code);
        Assert.Contains("bool r1 = a >= b;", output);
        Assert.Contains("bool r2 = a <= b;", output);
        Assert.Contains("bool r3 = a != b;", output);
    }

    [Fact]
    public void LogicalOperators()
    {
        var code = "bool a = true;\nbool b = false;\nbool r1 = a && b;\nbool r2 = a || b;\nbool r3 = !a;";
        var output = Roundtrip(code);
        Assert.Contains("bool r1 = a && b;", output);
        Assert.Contains("bool r2 = a || b;", output);
        Assert.Contains("bool r3 = !a;", output);
    }

    [Fact]
    public void SimpleIfElse()
    {
        var code = @"
int x = 10;
int y = 20;
int z = 0;
if (x > y)
{
    z = x + y;
}
else
{
    z = x - y;
}";
        var output = Roundtrip(code);
        Assert.Contains("int x = 10;", output);
        Assert.Contains("int y = 20;", output);
        Assert.Contains("int z = 0;", output);
        Assert.Contains("if (x > y)", output);
        Assert.Contains("z = x + y;", output);
        Assert.Contains("else", output);
        Assert.Contains("z = x - y;", output);
    }

    [Fact]
    public void IfElseIfElse()
    {
        var code = @"
int x = 10;
int y = 20;
int z;
if (x > y)
{
    z = x;
}
else if (x == y)
{
    z = 0;
}
else
{
    z = y;
}";
        var output = Roundtrip(code);
        Assert.Contains("int x = 10;", output);
        Assert.Contains("int y = 20;", output);
        Assert.Contains("int z;", output);
        Assert.Contains("if (x > y)", output);
        Assert.Contains("z = x;", output);
        Assert.Contains("else if (x == y)", output);
        Assert.Contains("z = 0;", output);
        Assert.Contains("else", output);
        Assert.Contains("z = y;", output);
    }

    [Fact]
    public void ConditionWithLogic()
    {
        var code = @"
int x = 10;
int y = 20;
int z = 0;
bool flag = true;
if (x >= y && z != 0 || !flag)
{
    z = x + y;
}
else
{
    z = x - y;
}";
        var output = Roundtrip(code);
        Assert.Contains("int x = 10;", output);
        Assert.Contains("bool flag = true;", output);
        Assert.Contains("if (", output);
        Assert.Contains(">=", output);
        Assert.Contains("&&", output);
        Assert.Contains("||", output);
        Assert.Contains("!flag", output);
        Assert.Contains("z = x + y;", output);
        Assert.Contains("z = x - y;", output);
    }

    [Fact]
    public void DeclarationWithoutInitializer()
    {
        var code = "int x;\nint y = 10;";
        var result = _parser.Parse(code);
        Assert.False(result.HasErrors, string.Join("\n", result.Errors));

        var declNode = result.Graph.Nodes.FirstOrDefault(n => n.VariableName == "x");
        Assert.NotNull(declNode);
        Assert.Equal(NodeType.VariableDeclaration, declNode.Type);
        Assert.Equal("int", declNode.ValueType);

        var output = _generator.Generate(result.Graph);
        Assert.Contains("int x;", output);
        Assert.Contains("int y = 10;", output);
    }

    [Fact]
    public void AssignmentCreatesVariableSetNode()
    {
        var code = "int x = 10;\nx = 20;";
        var result = _parser.Parse(code);
        Assert.False(result.HasErrors, string.Join("\n", result.Errors));

        var setNode = result.Graph.Nodes.FirstOrDefault(n => n.Type == NodeType.VariableSet);
        Assert.NotNull(setNode);
        Assert.Equal("x", setNode.VariableName);
    }

    [Fact]
    public void FloatLiteral()
    {
        var code = "float x = 1.5f;";
        var output = Roundtrip(code);
        Assert.Contains("float x = 1.5f;", output);
    }

    [Fact]
    public void BoolLiteral()
    {
        var code = "bool flag = true;";
        var output = Roundtrip(code);
        Assert.Contains("bool flag = true;", output);
    }

    [Fact]
    public void StringLiteral()
    {
        var code = "string name = \"hello\";";
        var output = Roundtrip(code);
        Assert.Contains("string name = \"hello\";", output);
    }

    [Fact]
    public void EmptyGraphReturnsComment()
    {
        var output = _generator.Generate(new GraphData());
        Assert.Contains("//", output);
    }

    [Fact]
    public void NullGraphReturnsComment()
    {
        var output = _generator.Generate(null!);
        Assert.Contains("//", output);
    }

    [Fact]
    public void DivisionOperator()
    {
        var code = "int x = 10;\nint y = 2;\nint z = x / y;";
        var output = Roundtrip(code);
        Assert.Contains("int z = x / y;", output);
    }

    [Fact]
    public void SubtractionOperator()
    {
        var code = "int x = 10;\nint y = 2;\nint z = x - y;";
        var output = Roundtrip(code);
        Assert.Contains("int z = x - y;", output);
    }

    [Fact]
    public void NestedInlineExpressions()
    {
        var code = "int a = 1;\nint b = 2;\nint c = 3;\nint d = a + b * c;";
        var output = Roundtrip(code);
        Assert.Contains("int d = a + (b * c);", output);
    }

    [Fact]
    public void IfWithOnlyTrueBranch()
    {
        var code = @"
int x = 10;
if (x > 5)
{
    x = 0;
}";
        var output = Roundtrip(code);
        Assert.Contains("if (x > 5)", output);
        Assert.Contains("x = 0;", output);
        Assert.DoesNotContain("else", output);
    }

    [Fact]
    public void ForLoopWithCompoundAssignmentInBody()
    {
        var code = @"
int sum = 0;
for (int i = 0; i < 10; i++)
{
    sum += i;
}";
        var output = Roundtrip(code);
        Assert.Contains("for (int i = 0; i < 10; i++)", output);
        Assert.Contains("sum = sum + i;", output);
    }

    [Fact]
    public void WhileLoop()
    {
        var code = @"
int n = 3;
while (n > 0)
{
    n--;
}";
        var output = Roundtrip(code);
        Assert.Contains("while (n > 0)", output);
        Assert.Contains("n = n - 1;", output);
    }

    [Fact]
    public void ConsoleWriteLineStatement()
    {
        var code = @"Console.WriteLine(""Hello"");";
        var output = Roundtrip(code);
        Assert.Contains("Console.WriteLine(\"Hello\");", output);
    }

    [Fact]
    public void IntParseRoundtrip()
    {
        var code = @"int x = int.Parse(""42"");";
        var output = Roundtrip(code);
        Assert.Contains("int x = int.Parse(\"42\");", output);
    }

    [Fact]
    public void FloatParseRoundtrip()
    {
        var code = @"float f = float.Parse(""3.14"");";
        var output = Roundtrip(code);
        Assert.Contains("float f = float.Parse(\"3.14\");", output);
    }

    [Fact]
    public void ToStringRoundtrip()
    {
        var code = @"
int n = 5;
string s = n.ToString();";
        var output = Roundtrip(code);
        Assert.Contains("string s = n.ToString();", output);
    }

    [Fact]
    public void CompoundAssignmentPlus()
    {
        var code = @"
int a = 1;
a += 2;";
        var output = Roundtrip(code);
        Assert.Contains("a = a + 2;", output);
    }

    [Fact]
    public void MathfAbsMaxMinRoundtrip()
    {
        var code = @"
float x = 1f;
float y = Mathf.Abs(x);
float z = Mathf.Max(x, y);
float w = Mathf.Min(x, y);";
        var result = _parser.Parse(code);
        Assert.False(result.HasErrors, string.Join("\n", result.Errors));
        var output = _generator.Generate(result.Graph);
        Assert.Contains("Math.Abs", output);
        Assert.Contains("Math.Max", output);
        Assert.Contains("Math.Min", output);
    }

    [Fact]
    public void VariableReassignment()
    {
        var code = "int x = 10;\nx = 20;";
        var output = Roundtrip(code);
        Assert.Contains("int x = 10;", output);
        Assert.Contains("x = 20;", output);
    }

    [Fact]
    public void VariableReassignmentWithExpression()
    {
        var code = "int x = 10;\nint y = 5;\nx = x + y;";
        var output = Roundtrip(code);
        Assert.Contains("int x = 10;", output);
        Assert.Contains("int y = 5;", output);
        Assert.Contains("x = x + y;", output);
    }

    [Fact]
    public void FloatVariableRoundtrip()
    {
        var code = "float speed = 5.5f;";
        var output = Roundtrip(code);
        Assert.Contains("float speed = 5.5f;", output);
    }

    [Fact]
    public void FloatArithmeticRoundtrip()
    {
        var code = "float a = 1.5f;\nfloat b = 2.5f;\nfloat c = a + b;";
        var output = Roundtrip(code);
        Assert.Contains("float a = 1.5f;", output);
        Assert.Contains("float b = 2.5f;", output);
        Assert.Contains("float c = a + b;", output);
    }

    [Fact]
    public void IfWithVariableAssignmentInBranch()
    {
        var code = @"
int score = 75;
string grade = """";
if (score >= 90)
{
    grade = ""A"";
}";
        var output = Roundtrip(code);
        Assert.Contains("int score = 75;", output);
        Assert.Contains("if (score >= 90)", output);
        Assert.Contains("grade = \"A\";", output);
    }

    [Fact]
    public void IfElseIfElseChain()
    {
        var code = @"
int score = 75;
string grade = """";
if (score >= 90)
{
    grade = ""A"";
}
else if (score >= 80)
{
    grade = ""B"";
}
else
{
    grade = ""F"";
}";
        var output = Roundtrip(code);
        Assert.Contains("if (score >= 90)", output);
        Assert.Contains("grade = \"A\";", output);
        Assert.Contains("else if (score >= 80)", output);
        Assert.Contains("grade = \"B\";", output);
        Assert.Contains("else", output);
        Assert.Contains("grade = \"F\";", output);
    }

    [Fact]
    public void ConsoleWriteLineWithVariable()
    {
        var code = @"
string message = ""Hello World"";
Console.WriteLine(message);";
        var output = Roundtrip(code);
        Assert.Contains("string message = \"Hello World\";", output);
        Assert.Contains("Console.WriteLine(message);", output);
    }

    [Fact]
    public void MultipleConsoleWriteLines()
    {
        var code = @"
Console.WriteLine(""First"");
Console.WriteLine(""Second"");";
        var output = Roundtrip(code);
        Assert.Contains("Console.WriteLine(\"First\");", output);
        Assert.Contains("Console.WriteLine(\"Second\");", output);
    }

    [Fact]
    public void ForLoopSimple()
    {
        var code = @"
int sum = 0;
for (int i = 0; i < 5; i++)
{
    sum += i;
}";
        var output = Roundtrip(code);
        Assert.Contains("for (int i = 0; i < 5; i++)", output);
        Assert.Contains("sum = sum + i;", output);
    }

    [Fact]
    public void WhileLoopWithDecrement()
    {
        var code = @"
int count = 10;
while (count > 0)
{
    count--;
}";
        var output = Roundtrip(code);
        Assert.Contains("while (count > 0)", output);
        Assert.Contains("count = count - 1;", output);
    }

    [Fact]
    public void ParserCreatesExecFlowEdges()
    {
        var code = @"
int x = 10;
int y = 20;";
        var result = _parser.Parse(code);
        Assert.False(result.HasErrors, string.Join("\n", result.Errors));

        var execEdges = result.Graph.Edges.Where(e => e.ToPort == "execIn" || e.FromPort == "execOut").ToList();
        Assert.True(execEdges.Count > 0, "Parser should create execution flow edges between statements");
    }

    [Fact]
    public void ParserIfCreatesConditionEdge()
    {
        var code = @"
int x = 10;
if (x > 5)
{
    x = 0;
}";
        var result = _parser.Parse(code);
        Assert.False(result.HasErrors, string.Join("\n", result.Errors));

        var ifNode = result.Graph.Nodes.FirstOrDefault(n => n.Type == NodeType.FlowIf);
        Assert.NotNull(ifNode);

        var conditionEdge = result.Graph.Edges.FirstOrDefault(e => e.ToNodeId == ifNode.Id && e.ToPort == "condition");
        Assert.NotNull(conditionEdge);

        var trueEdge = result.Graph.Edges.FirstOrDefault(e => e.FromNodeId == ifNode.Id && e.FromPort == "true");
        Assert.NotNull(trueEdge);
    }

    [Fact]
    public void ParserIfElseCreatesLadder()
    {
        var code = @"
int x = 10;
if (x > 20)
{
    x = 1;
}
else
{
    x = 2;
}";
        var result = _parser.Parse(code);
        Assert.False(result.HasErrors, string.Join("\n", result.Errors));

        var ifNode = result.Graph.Nodes.FirstOrDefault(n => n.Type == NodeType.FlowIf);
        Assert.NotNull(ifNode);
        var elseNode = result.Graph.Nodes.FirstOrDefault(n => n.Type == NodeType.FlowElse);
        Assert.NotNull(elseNode);

        var falseEdge = result.Graph.Edges.FirstOrDefault(
            e => e.FromNodeId == ifNode.Id && e.FromPort == "false" && e.ToNodeId == elseNode.Id);
        Assert.NotNull(falseEdge);
    }

    [Fact]
    public void VariableSetNodeCreated()
    {
        var code = "int x = 10;\nx = 20;";
        var result = _parser.Parse(code);
        Assert.False(result.HasErrors, string.Join("\n", result.Errors));

        var setNodes = result.Graph.Nodes.Where(n => n.Type == NodeType.VariableSet).ToList();
        Assert.True(setNodes.Count > 0);
        Assert.Equal("x", setNodes[0].VariableName);
    }

    [Fact]
    public void ConsoleWriteLineNodeCreated()
    {
        var code = @"Console.WriteLine(""Test"");";
        var result = _parser.Parse(code);
        Assert.False(result.HasErrors, string.Join("\n", result.Errors));

        var cwlNode = result.Graph.Nodes.FirstOrDefault(n => n.Type == NodeType.ConsoleWriteLine);
        Assert.NotNull(cwlNode);

        var msgEdge = result.Graph.Edges.FirstOrDefault(e => e.ToNodeId == cwlNode.Id && e.ToPort == "message");
        Assert.NotNull(msgEdge);
    }
}
