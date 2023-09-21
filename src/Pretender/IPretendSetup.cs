using System.ComponentModel;
using System.Linq.Expressions;

namespace Pretender
{
    public interface IPretendSetup<T>
    {
        Pretend<T> Pretend { get; }
        LambdaExpression Expression { get; }
        void Execute(ref CallInfo callInfo);

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        void SetBehavior(Behavior behavior);
    }

    public interface IPretendSetup<T, TResult> : IPretendSetup<T>
    {
        Type ReturnType => typeof(TResult);
    }
}
