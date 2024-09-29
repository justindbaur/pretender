using Pretender;

namespace Example.Tests
{
    public static class FieldConstants
    {
        public const string MyConstant = "something";
    }

    public interface IFieldTest
    {
        int MyMethod(string myArg);
    }


    public class FieldTests
    {
        [Theory]
        [InlineData("something", true)]
        [InlineData("something_else", false)]
        public void ReferenceFieldConstant(string actualArg, bool shouldMatch)
        {
            var pretend = Pretend.That<IFieldTest>();

            pretend
                .Setup(i => i.MyMethod(FieldConstants.MyConstant))
                .Returns(1);

            var test = pretend.Create();

            if (false)
            {
                throw new Exception("something");
            }

            var result = test.MyMethod(actualArg);

            Assert.Equal(shouldMatch, result == 1);
        }
    }
}
