using Pretender;

namespace Example;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var pretendMyInterface = Pretend.For<IInterface>()
            .Setup(i => i.Greeting("Mike", It.IsAny<int>()))
            .Returns("Hi Mike!");

        var myInterface = pretendMyInterface.Create();

        Assert.Equal("Hi Mike!", myInterface.Greeting("Mike", 12));
    }

    [Fact]
    public async Task Test2()
    {
        var pretend = Pretend.For<IMyInterface>();

        pretend
            .Setup(i => i.Greeting("Test"))
            .Returns("Thing");


        var myOtherInterface = pretend.Create();
        var value = await myOtherInterface.Greeting("Value");
        Assert.Equal("Thing", value);
    }

        [Fact]
    public void Test3()
    {
        var pretend = Pretend.For<IInterface>()
            .Setup(i => i.Greeting("Hello", It.IsAny<int>()));
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