using System.Security.Cryptography.X509Certificates;

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
        await RunAndCompareAsync($$"""
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
    }

    [Fact]
    public async Task FieldReference()
    {
        await RunAndCompareAsync($$"""
            using System;
            using Pretender;
            
            namespace FieldReference;

            public static class Constants
            {
                public const string MyConstant = "my_string";
            }

            public interface ITest
            {
                void Method(string arg);
            }

            public class TestClass
            {
                public TestClass()
                {
                    var pretend = Pretend.That<ITest>();

                    pretend.Setup(i => i.Method(Constants.MyConstant));
                }
            }
            """);
    }


    //[Fact]
    //public async Task AbstractClass()
    //{
    //    await RunAndCompareAsync($$"""
    //        #nullable enable
    //        using System;
    //        using System.Threading.Tasks;
    //        using Pretender;

    //        namespace AbstractClass;

    //        public abstract class MyAbstractClass
    //        {
    //            abstract Task<string> MethodAsync(string str);
    //            abstract string Name { get; set; }
    //        }

    //        public class TestClass
    //        {
    //            public TestClass()
    //            {
    //                var pretend = Pretend.That<MyAbstractClass>();

    //                pretend.Setup(c => c.MethodAsync("Hi"));
    //            }
    //        }
    //        """);
    //}

    [Fact]
    public async Task Test3()
    {
        await RunAndComparePartialAsync($$"""
            var pretendSimpleInterface = Pretend.That<ISimpleInterface>();
            
            pretendSimpleInterface
                .Setup(i => i.Foo("1", 1))
                .Returns("Hi");
            
            var pretend = pretendSimpleInterface.Create();

            pretendSimpleInterface.Verify(i => i.Foo("1", 1), 2);
            """);
    }
}