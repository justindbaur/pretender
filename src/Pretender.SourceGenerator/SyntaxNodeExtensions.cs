using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using Microsoft.CodeAnalysis;

namespace Pretender.SourceGenerator
{
    internal static class SyntaxNodeExtensions
    {
        public static TSyntax WithInheritDoc<TSyntax>(this TSyntax node)
            where TSyntax : SyntaxNode
        {
            return node.WithLeadingTrivia(TriviaList(Comment("/// <inheritdoc/>")));
        }
    }
}
