using Pretender.SourceGenerator.SetupArguments;
using Pretender.SourceGenerator.Writing;

namespace Pretender.SourceGenerator.Emitter
{
    internal class NoopArgumentEmitter : SetupArgumentEmitter
    {
        public NoopArgumentEmitter(SetupArgumentSpec argumentSpec)
            : base(argumentSpec)
        { }

        public override bool EmitsMatcher => false;

        public override void EmitArgumentMatcher(IndentedTextWriter writer, CancellationToken cancellationToken)
        {
            // Intentional no-op
        }
    }
}