using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Pretender.SourceGenerator
{
    internal class PretendTypeDiagnostics
    {
        public static PretendTypeDiagnostics FromMethodGeneric(IInvocationOperation invocationOperation, SemanticModel semanticModel)
        {
            Debug.Assert(invocationOperation.TargetMethod.TypeArguments.Length == 1, "This should have been asserted already");
            var typeArgument = invocationOperation.TargetMethod.TypeArguments[0];
            return new PretendTypeDiagnostics(typeArgument,
                invocationOperation.Syntax.GetLocation());
        }

        public static PretendTypeDiagnostics FromConstructorGeneric()
        {
            throw new NotImplementedException();
        }

        public PretendTypeDiagnostics(ITypeSymbol typeSymbol, Location location)
        {
            TypeToPretend = typeSymbol;

            // TODO: Do more diagnostics
            if (TypeToPretend.IsSealed)
            {
                Diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.UnableToPretendSealedType,
                    location,
                    TypeToPretend));
            }

            PretendName = GetPretendName(typeSymbol);
        }

        public ITypeSymbol TypeToPretend { get; }
        public string PretendName { get; }

        public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

        private static string GetPretendName(ITypeSymbol typeToPretend)
        {
            if (typeToPretend.TypeKind == TypeKind.Interface)
            {
                // Interfaces generally have an I prefix, we will try to strip it off
                if (typeToPretend.Name.StartsWith("I")
                    && typeToPretend.Name.Length > 1
                    && typeToPretend.Name[1] == char.ToUpper(typeToPretend.Name[1]))
                {
                    return typeToPretend.Name.Substring(1) + "PretendImplementation";
                }
            }

            return typeToPretend.Name + "PretendImplementation";
        }
    }
}
