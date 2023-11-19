using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pretender.SourceGenerator.SetupArguments
{
    internal class LocalReferenceArgumentSpec : SetupArgumentSpec
    {
        private readonly ILocalReferenceOperation _localReferenceOperation;

        public LocalReferenceArgumentSpec(
            ILocalReferenceOperation localReferenceOperation,
            IArgumentOperation originalArgument,
            int argumentPlacement) : base(originalArgument, argumentPlacement)
        {
            _localReferenceOperation = localReferenceOperation;
        }

        public override int NeededMatcherStatements => 3;

        public override ImmutableArray<StatementSyntax> CreateMatcherStatements(CancellationToken cancellationToken)
        {
            var variableName = $"{Parameter.Name}_local";
            var (identifier, accessor) = CreateArgumentAccessor();

            // This is for calling the UnsafeAccessor method that doesn't seem to work for my needs
            //statements[1] = LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
            //    .AddVariables(VariableDeclarator(variableName)
            //        .WithInitializer(EqualsValueClause(InvocationExpression(
            //            MemberAccessExpression(
            //                SyntaxKind.SimpleMemberAccessExpression,
            //                IdentifierName($"Setup{index}Accessor"),
            //                IdentifierName(((ILocalReferenceOperation)ArgumentOperation.Value).Local.Name)
            //                )
            //            )
            //        .AddArgumentListArguments(Argument(IdentifierName("target")))))));


            //statements[1] = LocalDeclarationStatement(VariableDeclaration(localOperation.Local.Type.AsUnknownTypeSyntax())
            //    .AddVariables(VariableDeclarator(variableName)
            //        .WithInitializer(EqualsValueClause(
            //            MemberAccessExpression(
            //                SyntaxKind.SimpleMemberAccessExpression,
            //                 ParenthesizedExpression(CastExpression(ParseTypeName("dynamic"), IdentifierName("target"))),
            //                 IdentifierName(localOperation.Local.Name))))
            //                )
            //           );

            // var arg_local = target.GetType().GetField("local").GetValue(target);

            // target.GetType()
            var getTypeInvocation = InvocationExpression(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("target"),
                    IdentifierName("GetType")));

            // target.GetType().GetField("local")!
            var getFieldInvocation = PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, InvocationExpression(MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                getTypeInvocation,
                IdentifierName("GetField")))
                    .AddArgumentListArguments(Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(_localReferenceOperation.Local.Name)))));

            var getValueInvocation = CastExpression(_localReferenceOperation.Local.Type.AsUnknownTypeSyntax(), InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    getFieldInvocation,
                    IdentifierName("GetValue")))
                    .AddArgumentListArguments(Argument(IdentifierName("target"))));

            // TODO: This really sucks, but neither other way works
            var localDeclaration = LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var")))
                .AddDeclarationVariables(VariableDeclarator(variableName)
                    .WithInitializer(EqualsValueClause(getValueInvocation)));
            //statements[1] = ExpressionStatement(
            //    ParseExpression($"var {variableName} = target.GetType().GetField(\"{localOperation.Local.Name}\")!.GetValue(target)")
            //);

            var ifCheck = CreateIfCheck(IdentifierName(identifier), IdentifierName(variableName));
            return ImmutableArray.Create<StatementSyntax>([accessor, localDeclaration, ifCheck]);
        }

        public override int GetHashCode()
        {
            return SymbolEqualityComparer.Default.GetHashCode(_localReferenceOperation.Local);
        }
    }
}
