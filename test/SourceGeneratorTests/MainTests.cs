namespace SourceGeneratorTests;

public partial class MainTests : TestBase
{
    [Fact]
    public async Task ReturningMethod()
    {
        await RunAndCompareAsync($$"""
            var pretendSimpleInterface = Pretend.That<ISimpleInterface>();
            
            pretendSimpleInterface
                .Setup(i => i.Bar)
                .Returns("Hi");
            
            var pretend = pretendSimpleInterface.Create();
            """);
    }


    [Fact]
    public async Task Test2()
    {
        var (result, compilation) = await RunGeneratorAsync($$"""
            var pretendSimpleInterface = Pretend.That<SimpleAbstractClass>();

            var simpleInterface = pretendSimpleInterface.Create();
            """);

        Assert.Equal(3, result.GeneratedSources.Length);

        var source1 = result.GeneratedSources[0];
        var text1 = source1.SourceText.ToString();
        var source2 = result.GeneratedSources[1];
        var text2 = source2.SourceText.ToString();
        var source3 = result.GeneratedSources[2];
        var text3 = source3.SourceText.ToString();
    }

    [Fact]
    public async Task Test3()
    {
        var (result, compilation) = await RunGeneratorAsync($$"""
            var pretendSimpleInterface = Pretend.That<ISimpleInterface>();
            
            pretendSimpleInterface
                .Setup(i => i.Foo("1", 1))
                .Returns("Hi");
            
            var pretend = pretendSimpleInterface.Create();
            """);

        Assert.Equal(3, result.GeneratedSources.Length);

        var source1 = result.GeneratedSources[0];
        var text1 = source1.SourceText.ToString();
        var source2 = result.GeneratedSources[1];
        var text2 = source2.SourceText.ToString();
        var source3 = result.GeneratedSources[2];
        var text3 = source3.SourceText.ToString();
    }
}
