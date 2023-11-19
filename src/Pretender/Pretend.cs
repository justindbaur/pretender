using System.ComponentModel;
using System.Diagnostics;

namespace Pretender;

[DebuggerDisplay("{DebuggerToString(),nq}")]
public sealed partial class Pretend<T>
{
    // TODO: Should we minimize allocations for rarely called mocks?
    private List<CallInfo>? _calls;

    public Pretend()
    {
    }

    public IPretendSetup<T, TReturn> Setup<TReturn>(Func<T, TReturn> setupExpression)
    {
        throw new InvalidProgramException("This method should have been intercepted via a source generator.");
    }

    public IPretendSetup<T> SetupSet<TReturn>(Func<T, TReturn> setupExpression)
    {
        throw new InvalidProgramException("This method should have been intercepted via a source generator.");
    }

    public IPretendSetup<T> Setup(Action<T> setupExpression)
    {
        throw new InvalidProgramException("This method should have been intercepted via a source generator.");
    }

    public void Verify(Action<T> verifyExpression, Called called)
    {
        throw new InvalidProgramException("This method should have been intercepted via a source generator.");
    }

    public void Verify<TReturn>(Func<T, TReturn> verifyExpression, Called called)
    {
        throw new InvalidProgramException("This method should have been intercepted via a source generator.");
    }

    // TODO: VerifySet?

    [EditorBrowsable(EditorBrowsableState.Never)]
    // TODO: Make this obsolete
    [StackTraceHidden]
    public void Verify(IPretendSetup<T> pretendSetup, Called called)
    {
        // Right now we can't trust that this setup was created before, loop over all the calls and check it
        int timesCalled = 0;
        if (_calls != null)
        {
            for (var i = 0; i < _calls.Count; i++)
            {
                var call = _calls[i];
                if (pretendSetup.Matches(call))
                {
                    timesCalled++;
                }
            }
        }

        called.Validate(timesCalled);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    // TODO: Make this obsolete
    public void Handle(CallInfo callInfo)
    {
        _calls = [];
        _calls.Add(callInfo);

        if (_setups != null)
        {
            foreach (var setup in _setups)
            {
                setup.Execute(callInfo);
            }
        }
    }

    private string DebuggerToString()
    {
        return $"Type = {typeof(T).FullName}";
    }

    // private Dictionary<int, IPretendSetup<T>>? _setupDictionary;
    private List<IPretendSetup<T>>? _setups;

    [EditorBrowsable(EditorBrowsableState.Never)]
    // TODO: Make Obsolete
    public IPretendSetup<T> GetOrCreateSetup(int hashCode, Func<Pretend<T>, Action<T>, IPretendSetup<T>> setupCreator, Action<T> setupExpression)
    {
        _setups ??= [];
        var newSetup = setupCreator(this, setupExpression);
        _setups.Add(newSetup);
        return newSetup;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    // TODO: Make Obsolete
    public IPretendSetup<T, TResult> GetOrCreateSetup<TResult>(int hashCode, Func<Pretend<T>, Func<T, TResult>, IPretendSetup<T, TResult>> setupCreator, Func<T, TResult> setupExpression)
    {
        _setups ??= [];
        var newSetup = setupCreator(this, setupExpression);
        _setups.Add(newSetup);
        return newSetup;
    }
}

public static class Pretend
{
    public static Pretend<T> That<T>()
    {
        return new Pretend<T>();
    }

    //public static T Of<T>()
    //{
    //    var pretend = new Pretend<T>();
    //    return pretend.Create();
    //}
}
