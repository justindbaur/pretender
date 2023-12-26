﻿using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Pretender.SourceGenerator
{
    internal static class CSharpSyntaxUtilities
    {
        // Standard format for double and single on non-inbox frameworks to ensure value is round-trippable.
        public const string DoubleFormatString = "G17";
        public const string SingleFormatString = "G9";

        // Format a literal in C# format -- works around https://github.com/dotnet/roslyn/issues/58705

        public static string FormatLiteral(object? value, ITypeSymbol type)
        {
            if (value == null)
            {
                return $"default({type.ToFullDisplayString()})";
            }

            switch (value)
            {
                case string @string:
                    return SymbolDisplay.FormatLiteral(@string, quote: true);
                case char @char:
                    return SymbolDisplay.FormatLiteral(@char, quote: true);
                case double.NegativeInfinity:
                    return "double.NegativeInfinity";
                case double.PositiveInfinity:
                    return "double.PositiveInfinity";
                case double.NaN:
                    return "double.NaN";
                case double @double:
                    return $"{@double.ToString(DoubleFormatString, CultureInfo.InvariantCulture)}D";
                case float.NegativeInfinity:
                    return "float.NegativeInfinity";
                case float.PositiveInfinity:
                    return "float.PositiveInfinity";
                case float.NaN:
                    return "float.NaN";
                case float @float:
                    return $"{@float.ToString(SingleFormatString, CultureInfo.InvariantCulture)}F";
                case decimal @decimal:
                    // we do not need to specify a format string for decimal as it's default is round-trippable on all frameworks.
                    return $"{@decimal.ToString(CultureInfo.InvariantCulture)}M";
                case bool @bool:
                    return @bool ? "true" : "false";
                default:
                    // Assume this is a number.
                    return FormatNumber();
            }
            string FormatNumber() => $"({type.ToFullDisplayString()})({Convert.ToString(value, CultureInfo.InvariantCulture)})";
        }
    }
}