using Pretender;

namespace Example.Tests;

public class MatcherTests
{
    [Fact]
    public void OneAnyMatcher_OneAnonymousMatcher()
    {
        var testInterfacePretend = Pretend.That<ITestInterface>();

        testInterfacePretend
            .Setup(i => i.Test(It.IsAny<string>(), It.Is<string>(s => s == "Hi")))
            .Returns(1);

        var testInterface = testInterfacePretend.Create();

        var returnValue = testInterface.Test("Yo", "Hi");
        Assert.Equal(1, returnValue);
    }

    [Fact]
    public void TwoAnonymousMatchers()
    {
        var testInterfacePretend = Pretend.That<ITestInterface>();

        testInterfacePretend
            .Setup(i => i.Test(It.Is<string>(s => s == "Yo"), It.Is<string>(s => s == "Hi")))
            .Returns(1);

        var testInterface = testInterfacePretend.Create();

        var returnValue = testInterface.Test("Yo", "Hi");
        Assert.Equal(1, returnValue);
    }

    [Fact]
    public void AnonymousMatcherThatCapturesLocal()
    {
        var testInterfacePretend = Pretend.That<ITestInterface>();

        string local = "Yo";

        testInterfacePretend
            .Setup(i => i.Test(It.Is<string>(s => s == local), It.Is<string>(s => s == "Hi")))
            .Returns(1);

        var testInterface = testInterfacePretend.Create();

        var returnValue = testInterface.Test("Yo", "Hi");
        Assert.Equal(1, returnValue);
    }

    public string TestProperty { get; set; } = "Yo";

    [Fact]
    public void AnonymousMatcherThatCapturesProperty()
    {
        var testInterfacePretend = Pretend.That<ITestInterface>();

        testInterfacePretend
            .Setup(i => i.Test(It.Is<string>(s => s == TestProperty), It.Is<string>(s => s == "Hi")))
            .Returns(1);

        var testInterface = testInterfacePretend.Create();

        var returnValue = testInterface.Test("Yo", "Hi");
        Assert.Equal(1, returnValue);
    }
}

public interface ITestInterface
{
    int Test(string arg1, string arg2);
}
