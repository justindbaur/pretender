using Pretender;

namespace Example;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var pretendMyInterface = Pretend.For<IMyInterface>();
        //pretendMyInterface
        //    .Setup(i => i.Greeting(It.Is<string>(s => s == "Bob" || s == "John")))
        //    .Returns("Hello");

        pretendMyInterface
            .Setup(i => i.Greeting("Mikey"))
            .Returns("Mike!");

        var myInterface = new MyInterfacePretendImplementation(pretendMyInterface);

        Assert.Equal("Hello", myInterface.Greeting("John"));
        Assert.Equal("Mike!", myInterface.Greeting("Mike"));
    }

    [Fact]
    public void Test2()
    {
        var pretend = Pretend.For<IMyOtherInterface>();

        var thing = new MyOtherInterfacePretendImplementation(pretend);
    }

    [Fact]
    public void Test3()
    {
        var pretend = Pretend.For<IInterface>();
        pretend
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