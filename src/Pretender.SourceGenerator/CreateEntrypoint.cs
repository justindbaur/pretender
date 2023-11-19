using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace Pretender.SourceGenerator
{
    internal class CreateEntrypoint
    {
        public CreateEntrypoint(IInvocationOperation operation, ImmutableArray<ITypeSymbol>? typeArguments)
        {
            Operation = operation;
            Location = new InterceptsLocationInfo(operation);
            TypeArguments = typeArguments;

            // TODO: Do any Diagnostics?
        }

        public InterceptsLocationInfo Location { get; }
        public IInvocationOperation Operation { get; }
        public ImmutableArray<ITypeSymbol>? TypeArguments { get; }

        public MethodDeclarationSyntax GetMethodDeclaration(int index)
        {
            var returnType = Operation.TargetMethod.ReturnType;
            var returnTypeSyntax = returnType.AsUnknownTypeSyntax();

            TypeParameterSyntax[] typeParameters;
            ParameterSyntax[] methodParameters;
            ArgumentSyntax[] constructorArguments;

            if (TypeArguments.HasValue)
            {
                typeParameters = new TypeParameterSyntax[TypeArguments.Value.Length];

                // We always take the Pretend<T> argument first as a this parameter
                methodParameters = new ParameterSyntax[TypeArguments.Value.Length + 1];
                constructorArguments = new ArgumentSyntax[TypeArguments.Value.Length + 1];

                for (var i = 0; i < TypeArguments.Value.Length; i++)
                {
                    var typeName = $"T{i}";
                    var argName = $"arg{i}";

                    typeParameters[i] = TypeParameter(typeName);
                    methodParameters[i + 1] = Parameter(Identifier(argName))
                        .WithType(ParseTypeName(typeName));
                    constructorArguments[i + 1] = Argument(IdentifierName(argName));
                }
            }
            else
            {
                typeParameters = [];
                methodParameters = new ParameterSyntax[1];
                constructorArguments = new ArgumentSyntax[1];
            }

            methodParameters[0] = Parameter(Identifier("pretend"))
                .WithType(GenericName("Pretend")
                    .AddTypeArgumentListArguments(returnTypeSyntax))
                .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword))
            );

            constructorArguments[0] = Argument(IdentifierName("pretend"));

            var objectCreation = ObjectCreationExpression(ParseTypeName(returnType.ToPretendName()))
                .WithArgumentList(ArgumentList(SeparatedList(constructorArguments)));

            var method = MethodDeclaration(returnTypeSyntax, $"Create{index}")
                .WithBody(Block(ReturnStatement(objectCreation)))
                .WithParameterList(ParameterList(SeparatedList(methodParameters)))
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword)));

            if (typeParameters.Length > 0)
            {
                return method
                    .WithTypeParameterList(TypeParameterList(SeparatedList(typeParameters)));
            }

            return method;
        }
    }

    public class CreateEntryPointComparer : IEqualityComparer<CreateEntrypoint>
    {
        public static CreateEntryPointComparer Instance = new();

        bool IEqualityComparer<CreateEntrypoint>.Equals(CreateEntrypoint x, CreateEntrypoint y)
        {
            return SymbolEqualityComparer.Default.Equals(x.Operation.TargetMethod.ReturnType, y.Operation.TargetMethod.ReturnType)
                && CompareTypeArguments(x.TypeArguments, y.TypeArguments);
        }

        static bool CompareTypeArguments(ImmutableArray<ITypeSymbol>? x,  ImmutableArray<ITypeSymbol>? y)
        {
            if (!x.HasValue)
            {
                return !y.HasValue;
            }

            var xArray = x.Value;
            var yArray = y!.Value;

            if (xArray.Length != yArray.Length)
            {
                return false;
            }

            for (int i = 0; i < xArray.Length; i++)
            {
                if (!SymbolEqualityComparer.IncludeNullability.Equals(xArray[i], yArray[i]))
                {
                    return false;
                }
            }

            return true;
        }

        int IEqualityComparer<CreateEntrypoint>.GetHashCode(CreateEntrypoint obj)
        {
            return SymbolEqualityComparer.Default.GetHashCode(obj.Operation.TargetMethod.ReturnType);
        }
    }

}
