using Pretender.SourceGenerator.SetupArguments;
using Pretender.SourceGenerator.Writing;

namespace Pretender.SourceGenerator.Emitter
{
    internal class CaptureInvocationArgumentEmitter : SetupArgumentEmitter
    {
        public CaptureInvocationArgumentEmitter(SetupArgumentSpec argumentSpec) : base(argumentSpec)
        {
        }

        public override bool NeedsCapturer => true;

        public override void EmitArgumentMatcher(IndentedTextWriter writer, CancellationToken cancellationToken)
        {
            EmitArgumentAccessor(writer);
            //writer.WriteLine($"var {ArgumentSpec.Parameter.Name}_capture = ({ArgumentSpec.Parameter.Type.ToUnknownTypeString()})captured[{ArgumentSpec.Parameter.Ordinal}];");
            //EmitIfReturnFalseCheck(writer, $"{ArgumentSpec.Parameter.Name}_arg", $"{ArgumentSpec.Parameter.Name}_capture");

            writer.WriteLine($"if (!{Parameter.Name}_capturedMatcher.Matches({Parameter.Name}_arg))");
            using (writer.WriteBlock())
            {
                writer.WriteLine("return false;");
            }
        }
    }
}
