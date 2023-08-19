using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Pretender.Matchers
{
    // TODO: I hate how most of this works but shouldn't be needed like this if we do source gen for setups
    internal static class WellKnownMatchers
    {
        delegate IMatcher MatcherCreator(Type[] typeArguments, object?[] arguments);
        delegate IMatcher? OptionalMatcherCreator(object?[] arguments);

        private static readonly FrozenDictionary<MethodInfo, MatcherCreator> _knownMatchers;
        private static readonly ConcurrentDictionary<MethodInfo, OptionalMatcherCreator> _customMatchers = new ConcurrentDictionary<MethodInfo, OptionalMatcherCreator>();

        static WellKnownMatchers()
        {
            var items = new Dictionary<MethodInfo, MatcherCreator>
            {
                // TODO: Add more built in matchers
                { typeof(It).GetMethod(nameof(It.IsAny))!.GetGenericMethodDefinition(), (_, _) => AnyMatcher.Instance },
                { 
                    typeof(It).GetMethod(nameof(It.Is))!.GetGenericMethodDefinition(),
                    (types, arguments) => (IMatcher)Activator.CreateInstance(typeof(AnonymousMatcher<>).MakeGenericType(types), arguments)!
                },
            };

            _knownMatchers = items.ToFrozenDictionary();
        }

        public static bool TryGet(MethodInfo methodInfo, object?[] arguments, [NotNullWhen(true)] out IMatcher? matcher)
        {
            matcher = null;
            if (_knownMatchers.TryGetValue(methodInfo.GetGenericMethodDefinition(), out var matcherCreator))
            {
                matcher = matcherCreator(methodInfo.GetGenericArguments(), arguments);
                return true;
            }

            var customCreator = _customMatchers.GetOrAdd(methodInfo, (method) =>
            {
                var customMatcherAttribute = methodInfo.GetCustomAttribute<MatcherAttribute>();

                if (customMatcherAttribute == null) 
                {
                    return (_) => null;
                }

                return customMatcherAttribute == null
                    ? ((_) => null)
                    : ((arguments) => (IMatcher?)Activator.CreateInstance(customMatcherAttribute.MatcherType.MakeGenericType(methodInfo.GetGenericArguments()), arguments));
            });

            matcher = customCreator(arguments);
            return matcher != null;
        }
    }
}
