using System.ComponentModel;
using System.Linq.Expressions;

namespace Pretender
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class CompiledSetup<T> : IPretendSetup<T>
    {
        private readonly Func<CallInfo, bool> _matches;

        public CompiledSetup(Pretend<T> pretend, LambdaExpression expression, Func<CallInfo, bool> matches)
        {
            Pretend = pretend;
            Expression = expression;
            _matches = matches;
        }

        public Pretend<T> Pretend { get; }
        public LambdaExpression Expression { get; }

        public void Execute(CallInfo callInfo)
        {
            throw new NotImplementedException();
        }

        public bool Matches(CallInfo callInfo)
        {
            return _matches(callInfo);
        }
    }
}
