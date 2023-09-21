namespace SourceGeneratorTests;

public class MainTests
{
    const string sourceToTest = $$"""
        var pretend = Pretend.For<ISimpleInterface>();

        pretend
            .Setup(i => i.AsyncMethod("John"));

        pretend
            .Setup(i => i.AsyncReturningMethod(It.Is<string>(i => i == "Test")));

        var service = pretend.Create();
        var anotherService = pretend.Create();
        """;

    [Fact]
    public async Task Test1()
    {
        var (result, compilation) = await TestHelper.RunGeneratorAsync(sourceToTest);

        Assert.Equal(3, result.GeneratedSources.Length);

        var source1 = result.GeneratedSources[0];
        var text1 = source1.SourceText.ToString();
        var source2 = result.GeneratedSources[1];
        var text2 = source2.SourceText.ToString();
        var source3 = result.GeneratedSources[2];
        var text3 = source3.SourceText.ToString();
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
