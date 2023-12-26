using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator;

public class ScaffoldTypeOptions
{
    public ImmutableArray<FieldDeclarationSyntax> CustomFields { get; set; } = default;
    public Func<IMethodSymbol, BlockSyntax> AddMethodBody { get; set; } = (_) => Block();

    // TODO: Is there a better symbol for constructors, methods?
    public Func<(ParameterSyntax FirstParameter, StatementSyntax[] AdditionalBodyStatements)>? CustomizeConstructor { get; set; }
}