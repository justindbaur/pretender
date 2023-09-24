using System.Reflection;

namespace Pretender
{
    public ref struct CallInfo(MethodInfo methodInfo, Span<object?> arguments)
    {
        public MethodInfo MethodInfo { get; } = methodInfo;
        public Span<object?> Arguments { get; } = arguments;
        public object? ReturnValue { get; set; }
    }
}
