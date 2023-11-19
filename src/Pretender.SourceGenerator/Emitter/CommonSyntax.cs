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
        public static UsingDirectiveSyntax UsingSystem { get; } = UsingDirective(ParseName("System"));
        public static UsingDirectiveSyntax UsingSystemThreadingTasks { get; } = UsingDirective(ParseName("System.Threading.Tasks"));

        // Verify
        public static SyntaxToken SetupIdentifier { get; } = Identifier("setup");
        public static SyntaxToken CalledIdentifier { get; } = Identifier("called");
        public static ParameterSyntax CalledParameter { get; } = Parameter(CalledIdentifier)
            .WithType(ParseTypeName("Called"));

        public static CompilationUnitSyntax CreateVerifyCompilationUnit(MethodDeclarationSyntax[] verifyMethods)
        {
            var classDeclaration = ClassDeclaration("VerifyInterceptors")
                .AddModifiers(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.StaticKeyword))
                .AddMembers(verifyMethods);

            var namespaceDeclaration = NamespaceDeclaration(ParseName("Pretender.SourceGeneration"))
                .AddMembers(classDeclaration)
                .AddUsings(UsingSystem, KnownBlocks.CompilerServicesUsing, UsingSystemThreadingTasks, KnownBlocks.PretenderUsing, KnownBlocks.PretenderInternalsUsing);

            return CompilationUnit()
                .AddMembers(KnownBlocks.InterceptsLocationAttribute, namespaceDeclaration)
                .NormalizeWhitespace();
        }
    }
}
