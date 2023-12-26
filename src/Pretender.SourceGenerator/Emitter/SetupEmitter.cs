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

            var location = new InterceptsLocationInfo(_setupInvocation);

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

            writer.WriteLine(@$"[InterceptsLocation(@""{location.FilePath}"", {location.LineNumber}, {location.CharacterNumber})]");
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