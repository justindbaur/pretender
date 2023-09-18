using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceGeneratorTests;

public class MainTests
{
    const string sourceToTest = $$"""
        var pretend = Pretend.For<ISimpleInterface>();
        pretend.Setup(i => i.Greeting("John", 12));
        """;

    [Fact]
    public async Task Test1()
    {
        var (result, compilation) = await TestHelper.RunGeneratorAsync(sourceToTest);

        Assert.Equal(2, result.GeneratedSources.Length);

        var source1 = result.GeneratedSources[0];
        var text1 = source1.SourceText.ToString();
        var source2 = result.GeneratedSources[1];
        var text2 = source2.SourceText.ToString();
    }

    [Fact]
    public void Test2()
    {
        var expression = TestHelper.GetSyntax($$"""
            _pretend.Handle(callInfo);
            """);

        Console.WriteLine(expression);
    }

    [Fact]
    public void Test3()
    {
        var compilation = TestHelper.GetCompilation(sourceToTest);
    }
}
