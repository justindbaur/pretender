using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Pretender.SourceGenerator.Parser;
using Pretender.SourceGenerator.Writing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator
{
    internal static class SymbolExtensions
    {
        // TODO: Replace many of these with KnownTypeSymbols
        public static bool EqualsByName(this ITypeSymbol type, string[] name)
        {
            var length = name.Length;
            // Check that the type name matches what we expect
            if (type.Name != name[length - 1])
            {
                return false;
            }
            // Enumerate the containing namespaces to ensure they match
            var targetNamespace = type.ContainingNamespace;
            for (var i = length - 2; i >= 0; i--)
            {
                if (targetNamespace.Name != name[i])
                {
                    return false;
                }
                targetNamespace = targetNamespace.ContainingNamespace;
            }

            // Once all namespace parts have been enumerated
            // we should be in the global namespace
            if (!targetNamespace.IsGlobalNamespace)
            {
                return false;
            }

            return true;
        }

        public static TypeSyntax AsUnknownTypeSyntax(this ITypeSymbol type)
        {
            var typeSyntax = ParseTypeName(type.ToFullDisplayString());
            if (type.NullableAnnotation == NullableAnnotation.Annotated)
            {
                return NullableType(typeSyntax, Token(SyntaxKind.QuestionToken));
            }

            return typeSyntax;
        }

        public static string ToUnknownTypeString(this ITypeSymbol type)
        {
            return type.NullableAnnotation == NullableAnnotation.Annotated
                ? $"{type.ToFullDisplayString()}?"
                : type.ToFullDisplayString();
        }

        public static ExpressionSyntax ToDefaultValueSyntax(this INamedTypeSymbol type, KnownTypeSymbols knownTypeSymbols)
        {
            // They have explicitly annotated this type as nullable, so return null
            if (type.NullableAnnotation == NullableAnnotation.Annotated)
            {
                return LiteralExpression(SyntaxKind.DefaultLiteralExpression);
            }

            var nullableEnabled = type.NullableAnnotation != NullableAnnotation.None;

            var comparer = SymbolEqualityComparer.Default;

            if (type.IsUnboundGenericType)
            {
                throw new NotImplementedException("We believe this should have been impossible, please report this issue with a minimally reproducible sample.");
            }

            if (type.IsGenericType)
            {
                var unboundType = type.ConstructUnboundGenericType();

                if (comparer.Equals(unboundType, knownTypeSymbols.TaskOfT_Unbound))
                {
                    // Create Task.FromResult();
                    // TODO: Is this ever an unsafe cast? How could you have Task<anonymous_type>?
                    var resultType = (INamedTypeSymbol)type.TypeArguments[0];

                    // Recursion? Issue?
                    return KnownBlocks.TaskFromResult(resultType.AsUnknownTypeSyntax(), resultType.ToDefaultValueSyntax(knownTypeSymbols));
                }

                if (comparer.Equals(unboundType, knownTypeSymbols.ValueTaskOfT_Unbound))
                {
                    var resultType = (INamedTypeSymbol)type.TypeArguments[0];

                    // Recursion!
                    return KnownBlocks.ValueTaskFromResult(resultType.AsUnknownTypeSyntax(), resultType.ToDefaultValueSyntax(knownTypeSymbols));
                }

                // TODO: Support IEnumerable, Lists, Arrays, and others
            }

            if (comparer.Equals(type, knownTypeSymbols.Task))
            {
                return KnownBlocks.TaskCompletedTask;
            }

            if (comparer.Equals(type, knownTypeSymbols.ValueTask))
            {
                return KnownBlocks.ValueTaskCompletedTask;
            }

            if (comparer.Equals(type, knownTypeSymbols.String))
            {
                // They have requested not-null so special case non-null string to be string.Empty
                // people may not like this.
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    PredefinedType(Token(SyntaxKind.StringKeyword)),
                    IdentifierName("Empty"));
            }

            // No better default found, just use 'default' even though it might not be in line with nullability annotations
            return LiteralExpression(SyntaxKind.DefaultLiteralExpression);
        }

        public static string ToPretendName(this ITypeSymbol symbol)
        {
            return $"Pretend{symbol.Name}{SymbolEqualityComparer.Default.GetHashCode(symbol):X}";
        }

        public static string ToMethodInfoCacheName(this IMethodSymbol method)
        {
            return $"MethodInfo_{method.Name}_{SymbolEqualityComparer.Default.GetHashCode(method).ToString("X")}";
        }

        public static string ToFullDisplayString(this ITypeSymbol type)
        {
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        public static void ScaffoldImplementation(this ITypeSymbol type, IndentedTextWriter writer, ScaffoldTypeOptions options)
        {
            Debug.Assert(!type.IsSealed, "I can't scaffold an implementation of a sealed class");

            // Add the base type PretendIMyType : IMyType
            writer.WriteLine($"file class {type.ToPretendName()} : {type.ToFullDisplayString()}");
            using (writer.WriteBlock())
            {

            }

            // Add fields first
            //classDeclaration = classDeclaration.AddMembers([.. options.CustomFields]);

            // TODO: Only public and non-sealed?
            var typeMembers = type.GetMembers();


            // The options indicate wanting to customize a constructor
            if (options.CustomizeConstructor != null)
            {

            }

            var members = new List<MemberDeclarationSyntax>();

            (ParameterSyntax FirstParameter, StatementSyntax[] AdditionalBodyStatements) constructorCustomization = default;
            foreach (var member in typeMembers)
            {
                if (member is IMethodSymbol constructor && constructor.MethodKind == MethodKind.Constructor)
                {
                    ParameterSyntax[] constructorParameters;
                    int startingIndex = 0;

                    if (constructorCustomization == default && options.CustomizeConstructor != null)
                    {
                        constructorCustomization = options.CustomizeConstructor();
                        startingIndex++;
                        constructorParameters = new ParameterSyntax[constructor.Parameters.Length + 1];
                        constructorParameters[0] = constructorCustomization.FirstParameter;
                    }
                    else
                    {
                        constructorParameters = new ParameterSyntax[constructor.Parameters.Length];
                    }

                    for (var i = startingIndex; i < constructorParameters.Length; i++)
                    {
                        constructorParameters[i] = constructor.Parameters[i - startingIndex].ToParameterSyntax();
                    }

                    var baseInitializer = ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(
                            SeparatedList(constructor.Parameters.Select(p => Argument(IdentifierName(p.Name))))));

                    var constructorDeclaration = ConstructorDeclaration(type.ToPretendName())
                        .AddParameterListParameters(constructorParameters)
                        .WithInitializer(baseInitializer);

                    if (constructorCustomization != default)
                    {
                        constructorDeclaration = constructorDeclaration
                            .AddBodyStatements(constructorCustomization.AdditionalBodyStatements!);
                    }

                    members.Add(constructorDeclaration);
                }

                if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
                {
                    var methodDeclaration = method.ToMethodSyntax();
                    var body = options.AddMethodBody(method);
                    methodDeclaration = methodDeclaration.WithBody(body);
                    members.Add(methodDeclaration);
                }

                if (member is IPropertySymbol property)
                {
                    var propertyDeclaration = property.ToPropertySyntax();

                    // TODO: Customize each body
                    if (property.GetMethod != null)
                    {
                        propertyDeclaration = propertyDeclaration
                            .AddAccessorListAccessors(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithBody(options.AddMethodBody(property.GetMethod)));
                    }

                    if (property.SetMethod != null)
                    {
                        // TODO: Is this right?
                        var accessorKind = property.SetMethod.IsInitOnly
                            ? SyntaxKind.InitAccessorDeclaration
                            : SyntaxKind.SetAccessorDeclaration;

                        propertyDeclaration = propertyDeclaration
                            .AddAccessorListAccessors(AccessorDeclaration(accessorKind)
                                .WithBody(options.AddMethodBody(property.SetMethod)));
                    }

                    members.Add(propertyDeclaration);
                }

                // TODO: Do constructors, and use CustomizeConstructor
                // and set addedConstructor = true;
            }

            if (constructorCustomization == default && options.CustomizeConstructor != null)
            {
                constructorCustomization = options.CustomizeConstructor();

                var defaultConstructor = ConstructorDeclaration(type.ToPretendName())
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(ParameterList(SingletonSeparatedList(constructorCustomization.FirstParameter)))
                    .AddBodyStatements(constructorCustomization.AdditionalBodyStatements)
                    .WithInheritDoc();

                members.Insert(0, defaultConstructor);
            }

            // TODO: Add GeneratedCodeAttribute
            // TODO: Add ExcludeFromCodeCoverageAttribute

            //return classDeclaration.AddMembers([.. members]);
        }

        public static ImmutableArray<IMethodSymbol> GetApplicableMethods(this INamedTypeSymbol type)
        {
            return type.GetMembers()
                .Where(m => !m.IsStatic)
                .OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Ordinary
                    || m.MethodKind == MethodKind.PropertyGet
                    || m.MethodKind == MethodKind.PropertySet)
                .ToImmutableArray();
        }

        public static MethodDeclarationSyntax ToMethodSyntax(this IMethodSymbol method)
        {
            var methodDeclarationSyntax = MethodDeclaration(
                method.ReturnType.AsUnknownTypeSyntax(),
                method.Name
            );

            foreach (var typeParameter in method.TypeParameters)
            {
                methodDeclarationSyntax = methodDeclarationSyntax
                    .AddTypeParameterListParameters(typeParameter.ToTypeParameterSyntax());

                // TODO: This isn't implemented
                if (typeParameter.TryToTypeParameterConstraintClauseSyntax(out var typeParameterConstraints))
                {
                    methodDeclarationSyntax = methodDeclarationSyntax
                        .AddConstraintClauses(TypeParameterConstraintClause(typeParameter.Name)
                            .AddConstraints(typeParameterConstraints));
                }
            }

            foreach (var parameter in method.Parameters)
            {
                methodDeclarationSyntax = methodDeclarationSyntax
                    .AddParameterListParameters(parameter.ToParameterSyntax());
            }

            methodDeclarationSyntax = methodDeclarationSyntax
                .AddModifiers(Token(SyntaxKind.PublicKeyword));

            return methodDeclarationSyntax;
        }

        public static PropertyDeclarationSyntax ToPropertySyntax(this IPropertySymbol property)
        {
            var propertyDeclarationSyntax = PropertyDeclaration(
                property.Type.AsUnknownTypeSyntax(),
                property.Name)
                .AddModifiers(Token(SyntaxKind.PublicKeyword));

            return propertyDeclarationSyntax;
        }

        public static ParameterSyntax ToParameterSyntax(this IParameterSymbol parameter)
        {
            var parameterSyntax = Parameter(Identifier(parameter.Name))
                .WithType(parameter.Type.AsUnknownTypeSyntax());

            if (parameter.HasExplicitDefaultValue)
            {
                parameterSyntax = parameterSyntax
                    .WithDefault(EqualsValueClause(parameter.ToLiteralExpression()));
            }

            var modifiers = new List<SyntaxToken>();
            if (parameter.RefKind == RefKind.Ref)
            {
                modifiers.Add(Token(SyntaxKind.RefKeyword));
            }
            else if (parameter.RefKind == RefKind.Out)
            {
                modifiers.Add(Token(SyntaxKind.OutKeyword));
            }
            else if (parameter.RefKind == RefKind.RefReadOnly)
            {
                modifiers.Add(Token(SyntaxKind.RefKeyword));
                modifiers.Add(Token(SyntaxKind.ReadOnlyKeyword));
            }

            parameterSyntax = parameterSyntax
                .AddModifiers([.. modifiers]);

            // TODO: Anything else I need to do?
            // scoped?
            // this

            return parameterSyntax;
        }

        public static LiteralExpressionSyntax ToLiteralExpression(this IParameterSymbol parameterSymbol)
        {
            Debug.Assert(parameterSymbol.HasExplicitDefaultValue);
            return ToLiteralExpression(parameterSymbol.ExplicitDefaultValue);
        }

        private static LiteralExpressionSyntax ToLiteralExpression(object? value)
        {
            if (value == null)
            {
                return LiteralExpression(SyntaxKind.NullLiteralExpression);
            }

            throw new NotImplementedException($"We don't support literals of {value.GetType()} yet.");
        }

        public static TypeParameterSyntax ToTypeParameterSyntax(this ITypeParameterSymbol typeParameter)
        {
            var varianceKeyword = Token(typeParameter.Variance == VarianceKind.None
                    ? SyntaxKind.None
                    : typeParameter.Variance == VarianceKind.In
                        ? SyntaxKind.InKeyword
                        : SyntaxKind.OutKeyword);

            // TODO: Add attributes
            return TypeParameter(typeParameter.Name)
                .WithVarianceKeyword(varianceKeyword);
        }

        public static bool TryToTypeParameterConstraintClauseSyntax(this ITypeParameterSymbol typeParameter, out TypeParameterConstraintSyntax[] typeParameterConstrains)
        {
            // TODO: Support this
            typeParameterConstrains = [];
            return false;
        }
    }
}
