using Pretender;

namespace Example.Tests;

public class SimpleTests
{
    [Fact]
    public void OneLiteral_OneAnyMatcher()
    {
        var pretendMyInterface = Pretend.That<IInterface>()
            .Setup(i => i.Greeting("Mike", It.IsAny<int>()))
            .Returns("Hi Mike!");

        var myInterface = pretendMyInterface.Create();

        Assert.Equal("Hi Mike!", myInterface.Greeting("Mike", 12));
    }

    [Fact]
    public async Task OneCapturedLocal()
    {
        var pretend = Pretend.That<IMyInterface>();

        var local = "Value";

        pretend
            .Setup(i => i.Greeting(local))
            .Returns("Thing");


        var myOtherInterface = pretend.Create();

        var value = await myOtherInterface.Greeting("Value");

        pretend.Verify(i => i.Greeting(local), 1);
        Assert.Equal("Thing", value);
    }

    [Fact]
    public void OneLiteral_OneFunctionMatcher()
    {
        var pretend = Pretend.That<IInterface>();

        var setup = pretend
            .Setup(i => i.Greeting("Hello", It.Is<int>(n => n > 10)));

        setup.Returns("2");

        var item = pretend.Create();

        var response = item.Greeting("Hello", 12);
        Assert.Equal("2", response);

        setup.Verify(1);
        setup.Verify(..2);
    }
}

public interface IMyInterface
{
    Task<string> Greeting(string? name);

    void Something();
}

public interface IInterface
{
    string? Greeting(string name, int num);
}