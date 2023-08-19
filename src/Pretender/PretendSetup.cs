using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using Pretender.Behaviors;
using Pretender.Matchers;

namespace Pretender
{
    internal class ReturningPretendSetup<T, TReturn> : IPretendSetup<T, TReturn>
    {
        private Behavior? _behavior;
        internal ReturningPretendSetup(Pretend<T> pretend, Expression<Func<T, TReturn>> setupExpression)
        {
            Pretend = pretend;
            Expression = setupExpression;
        }

        public Pretend<T> Pretend { get; }
        public Type ReturnType => typeof(TReturn);
        public LambdaExpression Expression { get; }

        public Pretend<T> Callback(Action<CallInfo> action)
        {
            _behavior = new CallbackBehavior(action);
            return Pretend;
        }

        public void Execute(CallInfo callInfo)
        {
            if (_behavior != null)
            {
                _behavior.Execute(callInfo);
                return;
            }

            // We will return a default value
            // TODO: Make this smarter
            callInfo.ReturnValue = default(TReturn);
        }

        public bool Matches(CallInfo callInfo)
        {
            // TODO: Support more expression types and for all others, make an anaylyzer to warn
            return Expression.Body switch
            {
                MethodCallExpression methodCall => callInfo.MethodInfo == methodCall.Method && MatchMethodParameters(callInfo.Arguments, methodCall.Arguments),
                _ => false,
            };
        }

        private bool MatchMethodParameters(object?[] arguments, ReadOnlyCollection<Expression> argumentExpressions)
        {
            Debug.Assert(arguments.Length == argumentExpressions.Count);

            var parametersValid = true;

            for (int i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                var argumentExpression = argumentExpressions[0];

                parametersValid = IsParameterValid(argument, argumentExpression);

                if (!parametersValid)
                {
                    break;
                }
            }

            return parametersValid;

            static bool IsParameterValid(object? parameter, Expression parameterExpression)
            {
                return parameterExpression switch
                {
                    ConstantExpression constant => constant.Value == parameter,
                    MethodCallExpression methodCall => TryMatchMethodParameter(parameter, methodCall),
                    _ => throw new NotImplementedException($"Expression of type ${parameterExpression.Type} is not currently supported."),
                };
            }
        }

        private static bool TryMatchMethodParameter(object? parameter, MethodCallExpression methodCall)
        {
            // TODO: Reduce the arguments on the method call expression and pass them into WellKnownMatchers

            if (!WellKnownMatchers.TryGet(methodCall.Method, methodCall.Arguments.Select(ReduceParameterExpression).ToArray(), out var matcher))
            {
                // TODO: Call the method if possible?
                return false;
            }

            return matcher.Matches(parameter);
        }

        private static object? ReduceParameterExpression(Expression expression)
        {
            return expression switch
            {
                ConstantExpression constant => constant.Value,
                LambdaExpression lambda => lambda.Compile(),
                _ => throw new NotImplementedException($"Unable to reduce {expression.NodeType} for parameter."),
            };
        }

        public Pretend<T> Returns(TReturn result)
        {
            _behavior = new ReturnValueBehavior(result);
            return Pretend;
        }

        public Pretend<T> Throws(Exception exception)
        {
            _behavior = new ThrowBehavior(exception);
            return Pretend;
        }
    }

    internal class NonReturningPretendSetup<T> : IPretendSetup<T>
    {
        private Behavior? _behavior;

        internal NonReturningPretendSetup(Pretend<T> pretend, Expression<Action<T>> setupExpression)
        {
            Pretend = pretend;
            // TODO: Look through the expression for the method info
            Expression = setupExpression;
        }

        public Pretend<T> Pretend { get; }
        public LambdaExpression Expression { get; }

        public Pretend<T> Callback(Action<CallInfo> action)
        {
            _behavior = new CallbackBehavior(action);
            return Pretend;
        }

        public Pretend<T> Throws(Exception exception)
        {
            _behavior = new ThrowBehavior(exception);
            return Pretend;
        }

        public void Execute(CallInfo callInfo)
        {
            if (_behavior != null)
            {
                _behavior.Execute(callInfo);
                return;
            }

            // This is the non returning version, no need to set anything
        }

        public bool Matches(CallInfo callInfo)
        {
            // Reflect over expression
            return true;
        }
    }
}
