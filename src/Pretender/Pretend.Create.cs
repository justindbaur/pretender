namespace Pretender
{
    public partial class Pretend<T>
    {
        public T Create()
        {
            throw new InvalidProgramException("This method should have been intercepted via a source generator.");
        }

        public T Create<T0>(T0 arg0)
        {
            throw new InvalidProgramException("This method should have been intercepted via a source generator.");
        }

        public T Create<T0, T1>(T0 arg0, T1 arg1)
        {
            throw new InvalidProgramException("This method should have been intercepted via a source generator.");
        }

        public T Create<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2)
        {
            throw new InvalidProgramException("This method should have been intercepted via a source generator.");
        }

        public T Create<T0, T1, T2, T3>(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            throw new InvalidProgramException("This method should have been intercepted via a source generator.");
        }

        public T Create<T0, T1, T2, T3, T4>(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            throw new InvalidProgramException("This method should have been intercepted via a source generator.");
        }

        public T Create<T0, T1, T2, T3, T4, T5>(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            throw new InvalidProgramException("This method should have been intercepted via a source generator.");
        }

        // TODO: Support overloads up to 16
        // TODO: Support params object[] args after that, maybe when params Span<object?> comes around?
    }
}
