using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using Pretender;

namespace Comparison
{
    [MemoryDiagnoser]
    public class Simple
    {
        [Benchmark]
        public ISimpleInterface MoqTest()
        {
            var mock = new Moq.Mock<ISimpleInterface>();

             mock.Setup(i => i.Foo("1"))
                .Returns("2");

            return mock.Object;
        }

        [Benchmark]
        public ISimpleInterface NSubstituteTest()
        {
            var substitute = NSubstitute.Substitute.For<ISimpleInterface>();

            NSubstitute.SubstituteExtensions.Returns(substitute.Foo("1"), "2");

            return substitute;
        }

        [Benchmark]
        public ISimpleInterface PretenderTest()
        {
            var pretend = Pretend.For<ISimpleInterface>();

            pretend.Setup(i => i.Foo("1"))
                .Returns("2");

            return pretend.Create();
        }


        public interface ISimpleInterface
        {
            string Foo(string bar);
        }
    }
}
