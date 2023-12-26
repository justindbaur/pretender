using Microsoft.CodeAnalysis;

namespace Pretender.SourceGenerator
{
    internal static class DiagnosticDescriptors
    {
        public static DiagnosticDescriptor UnableToPretendSealedType { get; } = new(
            "PRTND001",
            "Unable to Pretend Sealed Types",
            "Sealed types cannot be Pretended, did you mean to use an interface?",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidSetupArgument { get; } = new(
            "PRTND002",
            "Invalid Setup Argument",
            "We don't support operation type {0} as a setup argument.",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}