using System.Linq.Expressions;

namespace Pretender
{
    public interface IPretendSetup<T>
    {
        Pretend<T> Pretend { get; }
        LambdaExpression Expression { get; }
        bool Matches(CallInfo callInfo);
        void Execute(CallInfo callInfo);
    }

    public interface IPretendSetup<T, TResult> : IPretendSetup<T>
    {
        Type ReturnType { get; }
        Pretend<T> Returns(TResult result);
    }
}
