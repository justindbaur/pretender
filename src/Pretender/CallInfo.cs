using System.Reflection;

namespace Pretender
{
    public class CallInfo
    {
        public CallInfo(MethodInfo methodInfo, object?[] arguments)
        {
            MethodInfo = methodInfo;
            Arguments = arguments;
        }

        public MethodInfo MethodInfo { get; }
        public object?[] Arguments { get; }
        public object? ReturnValue { get; set; }
    }
}
