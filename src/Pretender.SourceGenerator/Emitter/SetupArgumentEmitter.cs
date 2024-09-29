using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.SetupArguments;
using Pretender.SourceGenerator.Writing;

namespace Pretender.SourceGenerator.Emitter
{
    internal abstract class SetupArgumentEmitter
    {
        protected SetupArgumentEmitter(SetupArgumentSpec argumentSpec)
        {
            ArgumentSpec = argumentSpec;
        }

        protected SetupArgumentSpec ArgumentSpec { get; }

        public IParameterSymbol Parameter => ArgumentSpec.Parameter;

        public virtual bool EmitsMatcher => true;
        public ImmutableArray<ILocalSymbol> NeededLocals { get; }
        public virtual bool NeedsCapturer { get; }
        public virtual bool NeedsMatcher { get; }

        public abstract void EmitArgumentMatcher(IndentedTextWriter writer, CancellationToken cancellationToken);

        protected void EmitArgumentAccessor(IndentedTextWriter writer)
        {
            // var name_arg = (string?)callInfo[0];
            writer.WriteLine($"var {ArgumentSpec.Parameter.Name}_arg = ({ArgumentSpec.Parameter.Type.ToUnknownTypeString()})callInfo.Arguments[{ArgumentSpec.Parameter.Ordinal}];");
        }

        protected void EmitIfReturnFalseCheck(IndentedTextWriter writer, string left, string right)
        {
            writer.WriteLine($"if ({left} != {right})");
            using (writer.WriteBlock())
            {
                writer.WriteLine("return false;");
            }
        }
    }
}