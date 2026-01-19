using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Pretender.SourceGenerator.Utilities;

internal record struct Result<TValue>(TValue Value, EquatableArray<DiagnosticInfo> Errors)
    where TValue : IEquatable<TValue>?;