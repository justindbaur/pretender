namespace Pretender.Matchers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MatcherAttribute<T> : MatcherAttribute
        where T : IMatcher
    {
        public MatcherAttribute()
            : base(typeof(T))
        {

        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MatcherAttribute : Attribute
    {
        public MatcherAttribute(Type matcherType)
        {
            MatcherType = matcherType;
        }

        public Type MatcherType { get; }
    }
}