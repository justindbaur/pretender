using Pretender.SourceGenerator.SetupArguments;
using Pretender.SourceGenerator.Writing;

namespace Pretender.SourceGenerator.Emitter
{
    internal class CapturedArgumentEmitter : SetupArgumentEmitter
    {
        public CapturedArgumentEmitter(SetupArgumentSpec argumentSpec) : base(argumentSpec)
        {
            
        }

        public override bool NeedsCapturer => true;

        public override void EmitArgumentMatcher(IndentedTextWriter writer, CancellationToken cancellationToken)
        {
            EmitArgumentAccessor(writer);
            writer.WriteLine($"var {Parameter.Name}_capturedArg = ({Parameter.Type.ToUnknownTypeString()})capturedArguments[{Parameter.Ordinal}];");
            writer.WriteLine($"if ({Parameter.Name}_arg != {Parameter.Name}_capturedArg)");
            using (writer.WriteBlock())
            {
                writer.WriteLine("return false;");
            }
        }
    }
}
