namespace Pretender.Tests
{
    public class CalledTests
    {
        public static IEnumerable<object[]> Validate_DoesNotThrowData()
        {
            // Exactly
            yield return Data(1, 1);
            yield return Data(20, 20);

            // AtLeast
            yield return Data(Called.AtLeastOnce(), 1);
            yield return Data(Called.AtLeastOnce(), 2);

            yield return Data(Called.AtLeast(3), 3);
            yield return Data(Called.AtLeast(3), 10);

            // Range
            yield return Data(1..4, 1);
            yield return Data(1..4, 2);
            yield return Data(1..4, 3);

            static object[] Data(Called called, int calls)
            {
                return [called, calls];
            }
        }

        [Theory]
        [MemberData(nameof(Validate_DoesNotThrowData))]
        public void Validate_DoesNotThrow(Called called, int calls)
        {
            called.Validate(calls);
        }

        public static IEnumerable<object[]> Validate_ThrowsData()
        {
            // Exactly
            yield return Data(1, 2);
            yield return Data(1, 0);

            // AtLeast
            yield return Data(Called.AtLeastOnce(), 0);
            yield return Data(Called.AtLeast(5), 0);
            yield return Data(Called.AtLeast(5), 4);

            // Range
            yield return Data(2..5, 0);
            yield return Data(2..5, 1);
            yield return Data(2..5, 5);
            yield return Data(2..5, 6);

            static object[] Data(Called called, int calls)
            {
                return [called, calls];
            }
        }

        [Theory]
        [MemberData(nameof(Validate_ThrowsData))]
        public void Validate_Throws(Called called, int calls)
        {
            Assert.Throws<Exception>(() => called.Validate(calls));
        }
    }
}