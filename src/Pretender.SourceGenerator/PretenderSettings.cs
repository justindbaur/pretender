using Microsoft.CodeAnalysis;
using Pretender.Settings;

namespace Pretender.SourceGenerator
{
    internal class PretenderSettings
    {
        public static PretenderSettings Default { get; } = new PretenderSettings(
            PretendBehavior.PreferFakes
            );

        public static PretenderSettings FromAttribute(AttributeData attributeData)
        {
            PretendBehavior? behavior = null;

            foreach (var namedArg in attributeData.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case nameof(Behavior):
                        behavior = (PretendBehavior)namedArg.Value.Value!;
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            return new PretenderSettings(
                behavior.GetValueOrDefault(PretendBehavior.PreferFakes)
                );
        }

        public PretenderSettings(PretendBehavior behavior)
        {
            Behavior = behavior;
        }

        public PretendBehavior Behavior {  get; }
    }
}
