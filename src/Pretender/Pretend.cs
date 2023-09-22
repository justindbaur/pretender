using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Pretender;

[DebuggerDisplay("{DebuggerToString(),nq}")]
public class Pretend<T>
{
    private List<IPretendSetup<T>>? _setups;
    private IPretendSetup<T>? _singleSetup;

    public Pretend()
    {
    }

    // TODO: Create interceptor for returning the configured type
    public T Create()
    {
        throw new InvalidProgramException("This method should have been intercepted via a source generator.");
    }

    public IPretendSetup<T, TReturn> Setup<TReturn>(Func<T, TReturn> setupExpression)
    {
        throw new InvalidProgramException("This method should have been intercepted via a source generator.");
    }

    public IPretendSetup<T> Setup(Action<T> setupExpression)
    {
        throw new InvalidProgramException("This method should have been intercepted via a source generator.");
    }

    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    // TODO: Make this obsolete
    public void Handle(ref CallInfo callInfo)
    {
        if (_singleSetup != null)
        {
            _singleSetup.Execute(ref callInfo);
        }
        else if (_setups != null)
        {
            foreach (var setup in _setups)
            {
                setup.Execute(ref callInfo);
            }
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
        if (_setups == null && _singleSetup == null)
        {
            _singleSetup = setup;
        }
        else if (_setups == null)
        {
            _setups ??= new List<IPretendSetup<T>>();
            _setups.Add(_singleSetup!);
            _setups.Add(setup);
            _singleSetup = null;
        }
        else
        {
            _setups.Add(setup);
        }
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
