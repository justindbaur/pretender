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
    public void Test2()
    {
        var pretend = Pretend.For<IMyOtherInterface>();

        pretend
            .Setup(i => i.Greeting())
            .Callback((ref CallInfo callInfo) =>
            {
                callInfo.Arguments[0] = 1;
            });


        var myOtherInterface = pretend.Create();
        myOtherInterface.Greeting();
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
    string Greeting(string? name);

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
    string Greeting(string name, int hello);
}

public sealed class TestClass
{

}