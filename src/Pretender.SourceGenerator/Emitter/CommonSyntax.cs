using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator.Emitter
{
    internal static class CommonSyntax
    {
        // General
        public static PredefinedTypeSyntax VoidType { get; } = PredefinedType(Token(SyntaxKind.VoidKeyword));
        public static TypeSyntax VarType { get; } = ParseTypeName("var");
        public static GenericNameSyntax GenericPretendType { get; } = GenericName("Pretend");

        // Verify
        public static SyntaxToken SetupIdentifier { get; } = Identifier("setup");
        public static SyntaxToken CalledIdentifier { get; } = Identifier("called");
        public static ParameterSyntax CalledParameter { get; } = Parameter(CalledIdentifier)
            .WithType(ParseTypeName("Called"));
    }
}