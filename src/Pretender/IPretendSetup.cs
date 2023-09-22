using System.ComponentModel;

namespace Pretender
{
    public interface IPretendSetup<T>
    {
        Pretend<T> Pretend { get; }
        void Execute(ref CallInfo callInfo);

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        void SetBehavior(Behavior behavior);
    }

    public interface IPretendSetup<T, TResult> : IPretendSetup<T>
    {
        Type ReturnType => typeof(TResult);
    }
}
