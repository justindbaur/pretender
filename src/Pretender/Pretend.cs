using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Pretender;

[DebuggerDisplay("{DebuggerToString(),nq}")]
public class Pretend<T>
{
    private readonly List<IPretendSetup<T>> _setups;

    public Pretend()
    {
        _setups = [];
    }

    // TODO: Create interceptor for returning the configured type
    public T Create()
    {
        throw new InvalidProgramException("This method should have been intercepted via a source generator.");
    }

    public IPretendSetup<T, TReturn> Setup<TReturn>(Expression<Func<T, TReturn>> setupExpression)
    {
        throw new InvalidProgramException("This method should have been intercepted via a source generator.");
    }

    public IPretendSetup<T> Setup(Expression<Action<T>> setupExpression)
    {
        throw new InvalidProgramException("This method should have been intercepted via a source generator.");
    }

    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    // TODO: Make this obsolete
    public void Handle(ref CallInfo callInfo)
    {
        foreach (var setup in _setups)
        {
            setup.Execute(ref callInfo);
        }
    }

    private string DebuggerToString()
    {
        return $"Type = {typeof(T).FullName}";
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    // TODO: Make obsolete?
    public void AddSetup(IPretendSetup<T> setup)
    {
        _setups.Add(setup);
    }
}

public static class Pretend
{
    public static Pretend<T> For<T>()
    {
        return new Pretend<T>();
    }

    //public static T Of<T>()
    //{
    //    var pretend = new Pretend<T>();
    //    return pretend.Create();
    //}
}
