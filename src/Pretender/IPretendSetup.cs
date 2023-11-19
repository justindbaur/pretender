using System.ComponentModel;

namespace Pretender
{
    public interface IPretendSetup<T>
    {
        Pretend<T> Pretend { get; }
        internal void Execute(CallInfo callInfo);
        internal bool Matches(CallInfo callInfo);
        int TimesCalled { get; }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        void SetBehavior(Behavior behavior);
        void Verify(Called called) => Pretend.Verify(pretendSetup: this, called);
    }

    public interface IPretendSetup<T, TResult> : IPretendSetup<T>
    {
        Type ReturnType => typeof(TResult);
    }
}
