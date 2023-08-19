using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceGeneratorTests;

public class MainTests
{
    [Fact]
    public void Test1()
    {
        var (source, diagnostics) = TestHelper.RunGenerator($$"""
            using Pretender;

            namespace MyTest;

            public class TestClass
            {
                public TestClass()
                {
                    var pretend = Pretend.For<IMyOtherInterface>();
                    var pretend = Pretend.For<IMyOtherInterface>();
                }
            }

            /// <summary>
            /// My Information
            /// </summary>
            public interface IInterface
            {
                string Greeting(string name, int hello);
            }

            public interface IMyOtherInterface
            {
                void Greeting();
            }
            """);

        Assert.NotNull(source);
    }

    [Fact]
    public void Test2()
    {
        var expression = TestHelper.GetSyntax($$"""
            _pretend.Handle(callInfo);
            """);

        Console.WriteLine(expression);
    }
}
