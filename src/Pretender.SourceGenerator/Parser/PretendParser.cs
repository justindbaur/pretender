using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Pretender.Settings;
using Pretender.SourceGenerator.Emitter;
using Pretender.SourceGenerator.Fakes;
using Pretender.SourceGenerator.Invocation;

namespace Pretender.SourceGenerator.Parser
{
    internal class PretendParser
    {
        private static readonly List<IKnownFake> s_knownFakes =
        [
            new ILoggerFake(),
        ];

        private readonly KnownTypeSymbols _knownTypeSymbols;
        private readonly PretenderSettings _settings;

        public PretendParser(PretendInvocation pretendInvocation, KnownTypeSymbols knownTypeSymbols, PretenderSettings settings)
        {
            PretendInvocation = pretendInvocation;
            _knownTypeSymbols = knownTypeSymbols;
            _settings = settings;
        }

        public PretendInvocation PretendInvocation { get; }

        public (PretendEmitter? Emitter, ImmutableArray<Diagnostic>? Diagnostics) Parse(CancellationToken cancellationToken)
        {
            var pretendType = PretendInvocation.PretendType;
            if (pretendType.IsSealed)
            {
                var sealedError = Diagnostic.Create(
                    DiagnosticDescriptors.UnableToPretendSealedType,
                    PretendInvocation.Location);
                return (null, ImmutableArray.Create(sealedError));
            }

            // TODO: If we are filling an existing time, check that it is partial

            // TODO: Do more error diagnostics

            if (_settings.Behavior == PretendBehavior.PreferFakes)
            {
                foreach (var fake in s_knownFakes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (fake.TryConstruct(pretendType, _knownTypeSymbols, cancellationToken, out var fakeType))
                    {
                        // TODO: Do something
                    }
                }
            }

            var methodStrategies = _knownTypeSymbols.GetTypesStrategies(pretendType);

            cancellationToken.ThrowIfCancellationRequested();

            // TODO: Do a larger amount of parsing

            cancellationToken.ThrowIfCancellationRequested();

            return (new PretendEmitter(PretendInvocation.PretendType, _knownTypeSymbols, PretendInvocation.FillExisting), null);
        }
    }
}