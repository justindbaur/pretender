using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.SetupArguments;
using Pretender.SourceGenerator.Writing;

namespace Pretender.SourceGenerator.Emitter
{
    internal class MatcherArgumentEmitter : SetupArgumentEmitter
    {
        private readonly INamedTypeSymbol _matcherType;

        // TODO: Also take args
        public MatcherArgumentEmitter(INamedTypeSymbol matcherType, SetupArgumentSpec argumentSpec)
            : base(argumentSpec)
        {
            _matcherType = matcherType;
        }

        public override void EmitArgumentMatcher(IndentedTextWriter writer, CancellationToken cancellationToken)
        {
            //var arguments = new ArgumentSyntax[_invocationOperation.Arguments.Length];
            //bool allArgumentsSafe = true;

            //for (int i = 0; i < arguments.Length; i++)
            //{
            //    var arg = _invocationOperation.Arguments[i];
            //    if (arg.Value is ILiteralOperation literalOperation)
            //    {
            //        arguments[i] = Argument(literalOperation.ToLiteralExpression());
            //    }
            //    else if (arg.Value is IDelegateCreationOperation delegateCreation)
            //    {

            //        if (delegateCreation.Target is IAnonymousFunctionOperation anonymousFunctionOperation)
            //        {
            //            if (anonymousFunctionOperation.Symbol.IsStatic) // This isn't enough either though, they could call a static method that only exists in their context
            //            {
            //                // If it's guaranteed to be static, we can just rewrite it in our code
            //                arguments[i] = Argument(ParseExpression(delegateCreation.Syntax.GetText().ToString()));
            //            }
            //            else if (false) // Is non-scope capturing
            //            {
            //                // This is a lot more work but also very powerful in terms of speed
            //                // We need to rewrite the delegate and replace all local references with our getter
            //                allArgumentsSafe = false;
            //            }
            //            else
            //            {
            //                // We need a static matcher
            //                allArgumentsSafe = false;
            //            }
            //        }
            //        else
            //        {
            //            allArgumentsSafe = false;
            //        }
            //    }
            //    else
            //    {
            //        allArgumentsSafe = false;
            //    }
            //}

            //if (!allArgumentsSafe)
            //{
            //    createdMatchStatements = false;
            //    return;
            //}

            EmitArgumentAccessor(writer);

            var matcherLocalName = $"{ArgumentSpec.Parameter.Name}_matcher";

            // TODO: Get arguments
            writer.WriteLine($"var {matcherLocalName} = new {_matcherType.ToFullDisplayString()}();");
            writer.WriteLine($"if (!{matcherLocalName}.Matches({ArgumentSpec.Parameter.Name}_arg))");
            using (writer.WriteBlock())
            {
                writer.WriteLine("return false;");
            }
        }
    }
}
