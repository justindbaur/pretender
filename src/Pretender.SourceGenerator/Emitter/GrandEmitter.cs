using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Pretender.SourceGenerator.Writing;
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

        public string Emit(CancellationToken cancellationToken)
        {
            var writer = new IndentedTextWriter();

            // InceptsLocationAttribute
            writer.Write(KnownBlocks.InterceptsLocationAttribute, isMultiline: true);
            writer.WriteLine();
            writer.WriteLine();

            writer.WriteLine("namespace Pretender.SourceGeneration");
            using (writer.WriteBlock())
            {
                writer.WriteLine("using System;");
                writer.WriteLine("using System.Reflection;");
                writer.WriteLine("using System.Runtime.CompilerServices;");
                writer.WriteLine("using System.Threading.Tasks;");
                writer.WriteLine("using Pretender;");
                writer.WriteLine("using Pretender.Internals;");
                writer.WriteLine();

                foreach (var pretendEmitter in _pretendEmitters)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    pretendEmitter.Emit(writer, cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();

                writer.WriteLine();
                writer.WriteLine("file static class SetupInterceptors");
                using (writer.WriteBlock())
                {
                    int setupIndex = 0;
                    foreach (var setupEmitter in _setupEmitters)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        setupEmitter.Emit(writer, setupIndex, cancellationToken);
                        setupIndex++;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                writer.WriteLine();
                writer.WriteLine("file static class VerifyInterceptors");
                using (writer.WriteBlock())
                {
                    int verifyIndex = 0;
                    foreach (var verifyEmitter in _verifyEmitters)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        verifyEmitter.Emit(writer, verifyIndex, cancellationToken);
                        verifyIndex++;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                writer.WriteLine();
                writer.WriteLine("file static class CreateInterceptors");
                using (writer.WriteBlock())
                {
                    int createIndex = 0;
                    foreach (var createEmitter in _createEmitters)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        createEmitter.Emit(writer, cancellationToken);
                        createIndex++;
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            return writer.ToString();
        }
    }
}
