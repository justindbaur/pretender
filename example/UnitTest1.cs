using Pretender;

namespace Example;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var pretendMyInterface = Pretend.That<IInterface>()
            .Setup(i => i.Greeting("Mike", It.IsAny<int>()))
            .Returns("Hi Mike!");

        var myInterface = pretendMyInterface.Create();

        Assert.Equal("Hi Mike!", myInterface.Greeting("Mike", 12));
    }

    [Fact]
    public async Task Test2()
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
    public void Test3()
    {
        var pretend = Pretend.That<IInterface>()
            .Setup(i => i.Greeting("Hello", It.IsAny<int>()));

        var item = pretend.Pretend.Create();

        var response = item.Greeting("Hello", 12);
        Assert.Null(response);

        pretend.Verify(1);
    }
}

public interface IMyInterface
{
    /// <summary>
    /// Hello
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Task<string> Greeting(string? name);

    void Something();
}

public interface IMyOtherInterface
{
    void Greeting();
}

/// <summary>
/// My Information
/// </summary>
public interface IInterface
{
    string? Greeting(string name, int num);
}

public sealed class TestClass
{

}