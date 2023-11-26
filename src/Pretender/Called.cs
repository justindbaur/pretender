using System.Diagnostics;

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
            Exact,
            AtLeast,
            Range,
        }

        public static Called Exactly(int expectedCalls)
            => new(expectedCalls, expectedCalls, CalledKind.Exact);

        public static Called AtLeastOnce() => AtLeast(1);

        public static Called AtLeast(int minimumCalls)
            => new(minimumCalls, int.MaxValue, CalledKind.AtLeast);

        public static Called Range(Range range)
        {
            if (range.Start.IsFromEnd || range.End.IsFromEnd)
            {
                throw new ArgumentException();
            }

            return new(range.Start.Value, range.End.Value, CalledKind.Range);
        }

        public static implicit operator Called(Range range) => Range(range);

        public static implicit operator Called(int expectedCalls) => Exactly(expectedCalls);

        [StackTraceHidden]
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
                case CalledKind.AtLeast:
                    if (callCount < _from)
                    {
                        throw new Exception($"It was not called at least {_from} time(s)");
                    }
                    break;
                case CalledKind.Range:
                    if (callCount < _from || callCount >= _to)
                    {
                        throw new Exception($"It was not between the range {_from}..{_to}");
                    }
                    break;
                default:
                    throw new Exception("Invalid call kind.");
            }
        }

        public override string ToString()
        {
            return $"From = {_from}, To = {_to}, Kind = {_calledKind}";
        }
    }
}
