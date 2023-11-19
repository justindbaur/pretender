using System.Diagnostics.CodeAnalysis;

namespace Pretender.Internals
{
    public readonly struct SetupWrapper<T> : IEquatable<SetupWrapper<T>>
    {
        private readonly IPretendSetup<T> _setup;
        private readonly int _hashCode;

        public SetupWrapper(IPretendSetup<T> setup, int hashCode)
        {
            _setup = setup;
            _hashCode = hashCode;
        }

        public readonly IPretendSetup<T> Setup => _setup;

        public bool Equals(SetupWrapper<T> other)
        {
            return _hashCode == other._hashCode;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is SetupWrapper<T> otherWrapper && Equals(otherWrapper);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}
