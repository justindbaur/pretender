using Microsoft.CodeAnalysis;
using Pretender.SourceGenerator.Parser;
using Pretender.SourceGenerator.Writing;

namespace Pretender.SourceGenerator.Emitter
{
    internal class PretendEmitter
    {
        private readonly INamedTypeSymbol _pretendType;
        private readonly KnownTypeSymbols _knownTypeSymbols;
        private readonly IReadOnlyDictionary<IMethodSymbol, MethodStrategy> _methodStrategies;
        private readonly bool _fillExisting;

        public PretendEmitter(INamedTypeSymbol pretendType, KnownTypeSymbols knownTypeSymbols, bool fillExisting)
        {
            _methodStrategies = knownTypeSymbols.GetTypesStrategies(pretendType);
            _pretendType = pretendType;
            _knownTypeSymbols = knownTypeSymbols;
            _fillExisting = fillExisting;
        }

        public INamedTypeSymbol PretendType => _pretendType;

        public void Emit(IndentedTextWriter writer, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // TODO: Generate debugger display
            if (_fillExisting)
            {
                writer.WriteLine($"public partial class {_pretendType.Name}");
            }
            else
            {
                writer.WriteLine($"file class {_knownTypeSymbols.GetPretendName(PretendType)} : {_pretendType.ToFullDisplayString()}");
            }
            using (writer.WriteBlock())
            {
                // static fields
                foreach (var strategyEntry in _methodStrategies)
                {
                    var strategy = strategyEntry.Value;
                    writer.Write($"public static readonly MethodInfo {strategy.UniqueName}_MethodInfo = typeof({_knownTypeSymbols.GetPretendName(PretendType)})");
                    strategyEntry.Value.EmitMethodGetter(writer, token);
                    writer.WriteLine(skipIfPresent: true);
                }
                writer.WriteLine();

                // instance fields
                writer.WriteLine($"private readonly Pretend<{_pretendType.ToFullDisplayString()}> _pretend;");
                writer.WriteLine();

                // main constructor
                writer.WriteLine($"public {_knownTypeSymbols.GetPretendName(PretendType)}(Pretend<{_pretendType.ToFullDisplayString()}> pretend)");
                using (writer.WriteBlock())
                {
                    writer.WriteLine("_pretend = pretend;");
                }

                token.ThrowIfCancellationRequested();

                // TODO: Stub other base type constructors

                // methods/properties
                var allMembers = _pretendType.GetMembers();

                foreach (var member in allMembers)
                {
                    token.ThrowIfCancellationRequested();

                    if (member.IsStatic)
                    {
                        // TODO: I should probably stub out static abstracts
                        continue;
                    }

                    if (member is IMethodSymbol constructorSymbol && constructorSymbol.MethodKind == MethodKind.Constructor)
                    {
                        if (constructorSymbol.Parameters.Length != 0)
                        {
                            throw new NotImplementedException("We have not implemented constructors with parameters yet.");
                        }
                    }
                    else if (member is IMethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.Ordinary)
                    {
                        // Emit Method body
                        writer.WriteLine();
                        writer.Write($"public {methodSymbol.ReturnType.ToUnknownTypeString()} {methodSymbol.Name}");

                        var hasTypeParameters = methodSymbol.TypeParameters.Length > 0;

                        if (hasTypeParameters)
                        {
                            writer.Write($"<{string.Join(", ", methodSymbol.TypeParameters.Select(t => t.Name))}>");
                        }

                        var parameters = methodSymbol.Parameters.Select(p =>
                        {
                            string output = "";
                            if (p.RefKind == RefKind.Out)
                            {
                                output += "out ";
                            }
                            else if (p.RefKind == RefKind.Ref)
                            {
                                output += "ref ";
                            }
                            else if (p.RefKind == RefKind.RefReadOnly)
                            {
                                output += "ref readonly ";
                            }

                            output += $"{p.Type.ToUnknownTypeString()} {p.Name}";
                            return output;
                        });
                        writer.WriteLine($"({string.Join(", ", parameters)})");
                        EmitMethodBody(writer, methodSymbol);
                    }
                    else if (member is IPropertySymbol propertySymbol)
                    {
                        // Emit property
                        writer.WriteLine();
                        writer.WriteLine($"public {propertySymbol.Type.ToUnknownTypeString()} {propertySymbol.Name}");
                        using (writer.WriteBlock())
                        {
                            if (propertySymbol.GetMethod is not null)
                            {
                                writer.WriteLine("get");
                                EmitMethodBody(writer, propertySymbol.GetMethod);
                            }

                            if (propertySymbol.SetMethod is not null)
                            {
                                writer.WriteLine("set");
                                EmitMethodBody(writer, propertySymbol.SetMethod);
                            }
                        }
                    }
                }
            }
        }

        private void EmitMethodBody(IndentedTextWriter writer, IMethodSymbol methodSymbol)
        {
            using (writer.WriteBlock())
            {
                writer.WriteLine($"object?[] __arguments__ = [{string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))}];");
                // TODO: Probably create an Argument object
                writer.WriteLine($"var __callInfo__ = new CallInfo({_methodStrategies[methodSymbol].UniqueName}_MethodInfo, __arguments__);");
                writer.WriteLine("_pretend.Handle(__callInfo__);");

                foreach (var parameter in methodSymbol.Parameters)
                {
                    if (parameter.RefKind != RefKind.Ref && parameter.RefKind != RefKind.Out)
                    {
                        continue;
                    }

                    writer.WriteLine($"{parameter.Name} = __arguments__[{parameter.Ordinal}];");
                }

                if (methodSymbol.ReturnType.SpecialType != SpecialType.System_Void)
                {
                    // TODO: What do I do about the nullability issues?
                    writer.WriteLine($"return ({methodSymbol.ReturnType.ToUnknownTypeString()})__callInfo__.ReturnValue;");
                }
            }
        }

        public class Comparer : IEqualityComparer<PretendEmitter>
        {
            public static Comparer Default = new();

            bool IEqualityComparer<PretendEmitter>.Equals(PretendEmitter x, PretendEmitter y)
            {
                return SymbolEqualityComparer.Default.Equals(x._pretendType, y._pretendType);
            }

            int IEqualityComparer<PretendEmitter>.GetHashCode(PretendEmitter obj)
            {
                return SymbolEqualityComparer.Default.GetHashCode(obj._pretendType);
            }
        }
    }
}