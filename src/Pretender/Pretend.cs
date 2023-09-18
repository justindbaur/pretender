using System.Diagnostics;
using System.Linq.Expressions;

namespace Pretender;

[DebuggerDisplay("DebuggerToString(),nq")]
public class Pretend<T>
{
    private readonly List<IPretendSetup<T>> _setups;

    public Pretend()
    {
        _setups = new List<IPretendSetup<T>>();
    }

    // TODO: Create interceptor for returning the configured type
    public T Create()
    {
        throw new InvalidOperationException("This method should have been intercepted via a source generator.");
    }

    public IPretendSetup<T, TReturn> Setup<TReturn>(Expression<Func<T, TReturn>> setupExpression)
    {
        var setup = new ReturningPretendSetup<T, TReturn>(this, setupExpression);
        _setups.Add(setup);
        return setup;
    }

    //public IPretendSetup<T> Setup(Expression<Action<T>> setupExpression)
    //{
    //    var setup = new NonReturningPretendSetup<T>(this, setupExpression);
    //    _setups.Add(setup);
    //    return setup;
    //}
    public IPretendSetup<T, TReturn> SetupCore<TReturn>()
    {
        return null!;
    }

    public IPretendSetup<T> SetupCore()
    {
        return null!;
    }

    public void Handle(CallInfo callInfo)
    {
        foreach (var setup in _setups)
        {
            if (!setup.Matches(callInfo))
            {
                continue;
            }

            setup.Execute(callInfo);
            break;
        }
    }

    private string DebuggerToString()
    {
        return $"Type = {typeof(T).FullName}";
    }
}

public static class Pretend
{
    public static Pretend<T> For<T>()
    {
        return new Pretend<T>();
    }

    public static T Of<T>()
    {
        var pretend = new Pretend<T>();
        return pretend.Create();
    }
}
