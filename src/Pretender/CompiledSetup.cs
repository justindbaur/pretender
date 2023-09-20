using System.ComponentModel;
using System.Linq.Expressions;

namespace Pretender
{
    public delegate bool Matcher(CallInfo callInfo);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CompiledSetup<T> : IPretendSetup<T>
    {
        private readonly Expression<Action<T>> _expression;
        private readonly Matcher _matcher;

        private Behavior? _behavior;

        public CompiledSetup(Pretend<T> pretend, Expression<Action<T>> expression, Matcher matcher)
        {
            Pretend = pretend;
            _expression = expression;
            _matcher = matcher;
        }

        public Pretend<T> Pretend { get; }
        public LambdaExpression Expression => _expression;

        public void SetBehavior(Behavior behavior)
        {
            if (_behavior != null)
            {
                throw new InvalidOperationException("You can't set multiple behaviors");
            }

            _behavior = behavior;
        }

        public void Execute(ref CallInfo callInfo)
        {
            // TODO: Mark as attempted?
            if (!_matcher(callInfo))
            {
                return;
            }

            // TODO: Mark as matched
            // TODO: Set times matched?

            // Run behavior
            if (_behavior is null)
            {
                return;
            }

            // For void returning we just run the behavior
            _behavior.Execute(ref callInfo);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CompiledSetup<T, TResult>(Pretend<T> pretend, Expression<Func<T, TResult>> expression, Matcher matcher) : IPretendSetup<T, TResult>
    {
        private readonly Matcher _matcher = matcher;
        private readonly Expression<Func<T, TResult>> _expression = expression;

        private Behavior? _behavior;

        public Pretend<T> Pretend { get; } = pretend;
        public Type ReturnType => typeof(TResult);

        public LambdaExpression Expression => _expression;

        public void Execute(ref CallInfo callInfo)
        {
            // TODO: Mark as attempted?
            if (!_matcher(callInfo))
            {
                return;
            }

            // TODO: Mark as matched
            // TODO: Set times matched?

            // Run behavior
            if (_behavior is null)
            {
                callInfo.ReturnValue = default(TResult);
                return;
            }

            _behavior.Execute(ref callInfo);

            if (ReturnType.IsValueType && callInfo.ReturnValue is null)
            {
                // TODO: Does this work with nullable?
                callInfo.ReturnValue = default(TResult);
            }
            else if (false) // Is awaitable
            {
                // return completed task
            }
        }

        public void SetBehavior(Behavior behavior)
        {
            if (_behavior is not null)
            {
                throw new InvalidOperationException("You can't set multiple behaviors");
            }

            _behavior = behavior;
        }
    }
}
