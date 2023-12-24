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

#pragma warning disable CS0618
        void Verify(Called called) => Pretend.Verify(pretendSetup: this, called);
#pragma warning restore CS0618
    }

    public interface IPretendSetup<T, TResult> : IPretendSetup<T>
    {
        Type ReturnType => typeof(TResult);
    }
}
