using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Pretender.SourceGenerator.Writing;

namespace Pretender.SourceGenerator.Emitter
{
    internal class VerifyEmitter
    {
        private readonly ITypeSymbol _pretendType;
        private readonly ITypeSymbol? _returnType;
        private readonly SetupActionEmitter _setupActionEmitter;
        private readonly IInvocationOperation _invocationOperation;

        public VerifyEmitter(ITypeSymbol pretendType, ITypeSymbol? returnType, SetupActionEmitter setupActionEmitter, IInvocationOperation invocationOperation)
        {
            _pretendType = pretendType;
            _returnType = returnType;
            _setupActionEmitter = setupActionEmitter;
            _invocationOperation = invocationOperation;
        }

        public void Emit(IndentedTextWriter writer, int index, CancellationToken cancellationToken)
        {
            // TODO: Create method trivia
            var location = new InterceptsLocationInfo(_invocationOperation);
            writer.WriteLine(@$"[InterceptsLocation(@""{location.FilePath}"", {location.LineNumber}, {location.CharacterNumber})]");

            // TODO: Get property setup expression type
            var setupExpressionType = _returnType != null
                ? $"Func<{_pretendType.ToFullDisplayString()}, {_returnType.ToUnknownTypeString()}>"
                : $"Action<{_pretendType.ToFullDisplayString()}>";


            writer.WriteLine($"internal static void Verify{index}(this Pretend<{_pretendType.ToUnknownTypeString()}> pretend, {setupExpressionType} setupExpression, Called called)");
            using (writer.WriteBlock())
            {
                writer.Write("var setup = ");
                _setupActionEmitter.Emit(writer, cancellationToken);
                writer.WriteLine("setup.Verify(called);");
            }
        }
    }
}
