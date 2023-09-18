using Example;

using Pretender;

namespace MyTest;

public class TestClass
{
    public TestClass()
    {
        var pretend = Pretend.For<IInterface>();
        pretend.Setup(i => i.Greeting("John", 12));
    }
}

/// <summary>
/// My Information
/// </summary>
//public interface IInterface
//{
//    string Greeting(string name, int hello);
//}