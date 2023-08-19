using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Pretender.SourceGenerator
{
    internal static class DiagnosticDescriptors
    {
        public static DiagnosticDescriptor UnableToPretendSealedType { get; } = new(
            "PRTD001",
            "Unabled to Pretend Sealed Types",
            "Sealed types cannot be Pretended, did you mean to use an interface?",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
