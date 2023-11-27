using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Pretender.SourceGenerator.Emitter
{
    internal class CreateEmitter
    {
        private readonly IInvocationOperation _originalOperation;
        private readonly ImmutableArray<ITypeSymbol>? _typeArguments;
        private readonly ImmutableArray<InterceptsLocationInfo> _locations;
        private readonly int _index;

        public CreateEmitter(IInvocationOperation originalOperation, ImmutableArray<ITypeSymbol>? typeArguments, ImmutableArray<InterceptsLocationInfo> locations, int index)
        {
            _originalOperation = originalOperation;
            _typeArguments = typeArguments;
            _locations = locations;
            _index = index;
        }

        public IInvocationOperation Operation => _originalOperation;

        public MethodDeclarationSyntax Emit(CancellationToken cancellationToken)
        {
            var returnType = _originalOperation.TargetMethod.ReturnType;

            var returnTypeSyntax = returnType.AsUnknownTypeSyntax();

            TypeParameterSyntax[] typeParameters;
            ParameterSyntax[] methodParameters;
            ArgumentSyntax[] constructorArguments;

            if (_typeArguments.HasValue)
            {
                typeParameters = new TypeParameterSyntax[_typeArguments.Value.Length];

                // We always take the Pretend<T> argument first as a this parameter
                methodParameters = new ParameterSyntax[_typeArguments.Value.Length + 1];
                constructorArguments = new ArgumentSyntax[_typeArguments.Value.Length + 1];

                for (var i = 0; i < _typeArguments.Value.Length; i++)
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

            var method = MethodDeclaration(returnTypeSyntax, $"Create{_index}")
                .WithBody(Block(ReturnStatement(objectCreation)))
                .WithParameterList(ParameterList(SeparatedList(methodParameters)))
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword)));

            method = method.WithAttributeLists(List(CreateInterceptsAttributes()));

            if (typeParameters.Length > 0)
            {
                return method
                    .WithTypeParameterList(TypeParameterList(SeparatedList(typeParameters)));
            }

            return method;
        }

        private ImmutableArray<AttributeListSyntax> CreateInterceptsAttributes()
        {
            var builder = ImmutableArray.CreateBuilder<AttributeListSyntax>(_locations.Length);

            foreach (var location in _locations)
            {
                var attribute = Create(location.ToAttributeSyntax());
                builder.Add(attribute);
            }

            return builder.MoveToImmutable();

            static AttributeListSyntax Create(AttributeSyntax attribute)
            {
                return AttributeList(SingletonSeparatedList(attribute));
            }
        }
    }
}
