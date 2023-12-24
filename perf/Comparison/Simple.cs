using BenchmarkDotNet.Attributes;
using Pretender;

namespace Comparison
{
    [MemoryDiagnoser]
    public class Simple
    {
        [Benchmark]
        public string MoqTest()
        {
            var mock = new Moq.Mock<ISimpleInterface>();

             mock.Setup(i => i.Foo(Moq.It.IsAny<string>()))
                .Returns("2");

            var simpleInterface = mock.Object;
            return simpleInterface.Foo("1");
        }

        [Benchmark]
        public string NSubstituteTest()
        {
            var substitute = NSubstitute.Substitute.For<ISimpleInterface>();

            NSubstitute.SubstituteExtensions.Returns(substitute.Foo(NSubstitute.Arg.Any<string>()), "2");

            return substitute.Foo("1");
        }

        [Benchmark(Baseline = true)]
        public string PretenderTest()
        {
            var pretend = Pretend.That<ISimpleInterface>();

            pretend.Setup(i => i.Foo(It.IsAny<string>()))
                .Returns("2");

            var simpleInterface = pretend.Create();
            return simpleInterface.Foo("1");
        }


        public interface ISimpleInterface
        {
            string Foo(string bar);
        }
    }
}
