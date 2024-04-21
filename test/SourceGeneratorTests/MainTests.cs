namespace SourceGeneratorTests;

public partial class MainTests : TestBase
{
    [Fact]
    public async Task ReturningMethod()
    {
        await RunAndComparePartialAsync($$"""
            var pretendSimpleInterface = Pretend.That<ISimpleInterface>();
            
            pretendSimpleInterface
                .Setup(i => i.Bar)
                .Returns("Hi");
            
            var pretend = pretendSimpleInterface.Create();
            """);
    }

    [Fact]
    public async Task TaskOfTMethod()
    {
        var (result, c) = await RunGeneratorAsync($$"""
            using System;
            using System.Threading.Tasks;
            using Pretender;
            using Pretender.Settings;

            [assembly: PretenderSettings(Behavior = PretendBehavior.AlwaysPretend)]

            namespace TaskOfTMethodNamespace;

            public interface IMyInterface
            {
                Task<string> MethodAsync(string str);
            }

            public class TestClass
            {
                public TestClass()
                {
                    var pretend = Pretend.That<IMyInterface>();

                    pretend.Setup(i => i.MethodAsync(It.Is<string>(v => v == "Hi!")));
                }
            }
            """);

        var source = Assert.Single(result.GeneratedSources);

        var text1 = source.SourceText.ToString();
        //var text2 = result.GeneratedSources[1].SourceText.ToString();
    }


    [Fact]
    public async Task AbstractClass()
    {
        var (result, c) = await RunPartialGeneratorAsync($$"""
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using Pretender;

            namespace AbstractClass;

            public abstract class MyAbstractClass
            {
                abstract Task<string> MethodAsync(string str);
                abstract string Name { get; set; }
            }

            public class TestClass
            {
                public TestClass()
                {
                    var pretend = Pretend.That<MyAbstractClass>();

                    pretend.Setup(c => c.MethodAsync("Hi"));
                }
            }
            """);

        var source = Assert.Single(result.GeneratedSources);

        var sourceText = source.SourceText.ToString();
    }

    [Fact]
    public async Task Test3()
    {
        var (result, compilation) = await RunPartialGeneratorAsync($$"""
            var pretendSimpleInterface = Pretend.That<ISimpleInterface>();
            
            pretendSimpleInterface
                .Setup(i => i.Foo("1", 1))
                .Returns("Hi");
            
            var pretend = pretendSimpleInterface.Create();

            pretendSimpleInterface.Verify(i => i.Foo("1", 1), 2);
            """);

        var source = Assert.Single(result.GeneratedSources);
        var text = source.SourceText.ToString();
    }
}