namespace Pretender
{
    public readonly struct Called
    {
        private readonly int _from;
        private readonly int _to;
        private readonly CalledKind _calledKind;

        private Called(int from, int to, CalledKind calledKind)
        {
            _from = from;
            _to = to;
            _calledKind = calledKind;
        }

        enum CalledKind
        {
            Exact
        }

        public static Called Exactly(int expectedCalls)
            => new(expectedCalls, expectedCalls, CalledKind.Exact);

        public static implicit operator Called(int expectedCalls)
            => new(expectedCalls, expectedCalls, CalledKind.Exact);

        public void Validate(int callCount)
        {
            switch (_calledKind)
            { 
                case CalledKind.Exact:
                    if (callCount != _from)
                    {
                        // TODO: Better exception
                        throw new Exception("It was not called exactly that many times.");
                    }
                    break;
                default:
                    throw new Exception("Invalid call kind.");
            }

        }
    }
}
