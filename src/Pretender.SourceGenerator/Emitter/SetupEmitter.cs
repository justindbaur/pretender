using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.Writing;

namespace Pretender.SourceGenerator.Emitter
{
    internal class SetupEmitter
    {
        private readonly SetupActionEmitter _setupActionEmitter;
        private readonly IInvocationOperation _setupInvocation;

        public SetupEmitter(SetupActionEmitter setupActionEmitter, IInvocationOperation setupInvocation)
        {
            _setupActionEmitter = setupActionEmitter;
            _setupInvocation = setupInvocation;
        }

        // TODO: Run cancellationToken a lot more
        public void Emit(IndentedTextWriter writer, int index, CancellationToken cancellationToken)
        {
            var setupMethod = _setupActionEmitter.SetupMethod;
            var pretendType = _setupActionEmitter.PretendType;

#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var location = _setupInvocation.SemanticModel!.GetInterceptableLocation((InvocationExpressionSyntax)_setupInvocation.Syntax, cancellationToken);
#pragma warning restore RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            string typeArgs;
            string actionType;
            if (setupMethod.ReturnsVoid)
            {
                typeArgs = $"<{pretendType.ToFullDisplayString()}>";
                actionType = "Action";
            }
            else
            {
                typeArgs = $"<{pretendType.ToFullDisplayString()}, {setupMethod.ReturnType.ToUnknownTypeString()}>";
                actionType = "Func";
            }

#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            writer.WriteLine(location!.GetInterceptsLocationAttributeSyntax());
#pragma warning restore RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            writer.Write($"internal static IPretendSetup{typeArgs} Setup{index}");
            writer.WriteLine($"(this Pretend<{pretendType.ToUnknownTypeString()}> pretend, {actionType}{typeArgs} setupExpression)");
            using (writer.WriteBlock())
            {
                writer.Write("return ");
                _setupActionEmitter.Emit(writer, cancellationToken);
            }
        }
    }
}