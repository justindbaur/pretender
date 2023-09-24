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

             mock.Setup(i => i.Foo(Moq.It.Is<string>(static i => i == "1")))
                .Returns("2");

            var simpleInterface = mock.Object;
            return simpleInterface.Foo("1");
        }

        [Benchmark]
        public string NSubstituteTest()
        {
            var substitute = NSubstitute.Substitute.For<ISimpleInterface>();

            NSubstitute.SubstituteExtensions.Returns(substitute.Foo(NSubstitute.Arg.Is<string>(static i => i == "1")), "2");

            return substitute.Foo("1");
        }

        [Benchmark(Baseline = true)]
        public string PretenderTest()
        {
            var pretend = Pretend.For<ISimpleInterface>();

            pretend.Setup(i => i.Foo(It.Is<string>(static i => i == "1")))
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
