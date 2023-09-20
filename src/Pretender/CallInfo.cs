using System.Reflection;

namespace Pretender
{
    public ref struct CallInfo
    {
        public CallInfo(MethodInfo methodInfo, Span<object?> arguments)
        {
            MethodInfo = methodInfo;
            Arguments = arguments;
        }

        public MethodInfo MethodInfo { get; }
        public Span<object?> Arguments { get; }
        public object? ReturnValue { get; set; }
    }
}
