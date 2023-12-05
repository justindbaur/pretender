using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator.Emitter
{
    internal class GrandEmitter
    {
        private readonly ImmutableArray<PretendEmitter> _pretendEmitters;
        private readonly ImmutableArray<SetupEmitter> _setupEmitters;
        private readonly ImmutableArray<VerifyEmitter> _verifyEmitters;
        private readonly ImmutableArray<CreateEmitter> _createEmitters;

        public GrandEmitter(
            ImmutableArray<PretendEmitter> pretendEmitters,
            ImmutableArray<SetupEmitter> setupEmitters,
            ImmutableArray<VerifyEmitter> verifyEmitters,
            ImmutableArray<CreateEmitter> createEmitters)
        {
            _pretendEmitters = pretendEmitters;
            _setupEmitters = setupEmitters;
            _verifyEmitters = verifyEmitters;
            _createEmitters = createEmitters;
        }

        public CompilationUnitSyntax Emit(CancellationToken cancellationToken)
        {
            var namespaceDeclaration = KnownBlocks.OurNamespace
                .AddUsings(
                    UsingDirective(ParseName("System")),
                    KnownBlocks.CompilerServicesUsing,
                    UsingDirective(ParseName("System.Threading.Tasks")),
                    KnownBlocks.PretenderUsing,
                    KnownBlocks.PretenderInternalsUsing
                );

            foreach (var pretendEmitter in _pretendEmitters)
            {
                cancellationToken.ThrowIfCancellationRequested();
                namespaceDeclaration = namespaceDeclaration
                    .AddMembers(pretendEmitter.Emit(cancellationToken));
            }

            var setupInterceptorsClass = ClassDeclaration("SetupInterceptors")
                .WithModifiers(TokenList(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.StaticKeyword)));

            cancellationToken.ThrowIfCancellationRequested();

            int setupIndex = 0;
            foreach (var setupEmitter in _setupEmitters)
            {
                cancellationToken.ThrowIfCancellationRequested();
                setupInterceptorsClass = setupInterceptorsClass
                    .AddMembers(setupEmitter.Emit(setupIndex, cancellationToken));
                setupIndex++;
            }

            var verifyInterceptorsClass = ClassDeclaration("VerifyInterceptors")
                .AddModifiers(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.StaticKeyword));

            cancellationToken.ThrowIfCancellationRequested();

            int verifyIndex = 0;
            foreach (var verifyEmitter in _verifyEmitters)
            {
                cancellationToken.ThrowIfCancellationRequested();

                verifyInterceptorsClass = verifyInterceptorsClass
                    .AddMembers(verifyEmitter.Emit(verifyIndex, cancellationToken));
                verifyIndex++;
            }

            var createInterceptorsClass = ClassDeclaration("CreateInterceptors")
                .AddModifiers(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.StaticKeyword));

            cancellationToken.ThrowIfCancellationRequested();

            int createIndex = 0;
            foreach (var createEmitter in _createEmitters)
            {
                cancellationToken.ThrowIfCancellationRequested();

                createInterceptorsClass = createInterceptorsClass
                    .AddMembers(createEmitter.Emit(cancellationToken));
                createIndex++;
            }

            namespaceDeclaration = namespaceDeclaration
                .AddMembers(setupInterceptorsClass, verifyInterceptorsClass, createInterceptorsClass);

            cancellationToken.ThrowIfCancellationRequested();

            return CompilationUnit()
                .AddMembers(
                KnownBlocks.InterceptsLocationAttribute,
                namespaceDeclaration)
                .NormalizeWhitespace();
        }
    }
}
