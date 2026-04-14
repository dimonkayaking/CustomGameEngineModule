using System;
using Xunit;
using VisualScripting.Core.Models;
using VisualScripting.Core.Parsers;
using VisualScripting.Core.Generators;

public class PrintOutput
{
    public static void Main()
    {
        var code = @"
int a = 15;
int b = 7;
int result = 0;

if (a > b)
{
    result = a - b;
}
else if (a < b)
{
    result = b - a;
}
else
{
    result = a + b;
}

int max = (a > b) ? a : b;
int absolute = (result < 0) ? -result : result;";

        var parser = new RoslynCodeParser();
        var result = parser.Parse(code);
        var gen = new SimpleCodeGenerator(result.Graph);
        var generated = gen.GenerateCode();
        Console.WriteLine("START GENERATED:");
        Console.WriteLine(generated);
        Console.WriteLine("END GENERATED:");
    }
}
