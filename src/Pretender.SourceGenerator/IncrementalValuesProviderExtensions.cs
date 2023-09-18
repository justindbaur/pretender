using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Pretender.SourceGenerator
{
    internal static class IncrementalValuesProviderExtensions
    {
        public static IncrementalValuesProvider<(TSource Source, int Index, ImmutableArray<TElement> Elements)> GroupWith<TSource, TElement>(
            this IncrementalValuesProvider<TSource> source,
            Func<TSource, TElement> sourceToElementTransform,
            IEqualityComparer<TSource> comparer)
        {
            return source.Collect().SelectMany((values, _) =>
            {
                Dictionary<TSource, ImmutableArray<TElement>.Builder> map = new(comparer);
                foreach (var value in values)
                {
                    if (!map.TryGetValue(value, out ImmutableArray<TElement>.Builder builder))
                    {
                        builder = ImmutableArray.CreateBuilder<TElement>();
                        map.Add(value, builder);
                    }
                    builder.Add(sourceToElementTransform(value));
                }
                ImmutableArray<(TSource Key, int Index, ImmutableArray<TElement> Elements)>.Builder result =
                    ImmutableArray.CreateBuilder<(TSource, int, ImmutableArray<TElement>)>();
                var index = 0;
                foreach (var entry in map)
                {
                    result.Add((entry.Key, index, entry.Value.ToImmutable()));
                    index++;
                }
                return result;
            });
        }
    }
}
