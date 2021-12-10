using System;
using FluentAssertions;
using Http.Resilience.Internals;
using Xunit;
using Xunit.Abstractions;

namespace Http.Resilience.Tests
{
    public class BackoffTimerHelperTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public BackoffTimerHelperTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Theory]
        [ClassData(typeof(GetExponentialBackoffTestdata))]
        public void ShouldReturnOK((int Attempt, TimeSpan MinBackoff, TimeSpan MaxBackoff, TimeSpan DeltaBackoff) input, TimeSpan expectedBackoff)
        {
            // Act
            var backoff = BackoffTimerHelper.GetExponentialBackoff(input.Attempt, input.MinBackoff, input.MaxBackoff, input.DeltaBackoff);

            // Assert
            this.testOutputHelper.WriteLine($"backoff={backoff}");
            backoff.Should().BeCloseTo(expectedBackoff, precision: input.DeltaBackoff);
        }

        public class GetExponentialBackoffTestdata : TheoryData<(int Attempt, TimeSpan MinBackoff, TimeSpan MaxBackoff, TimeSpan DeltaBackoff), TimeSpan>
        {
            public GetExponentialBackoffTestdata()
            {
                // Without DeltaBackoff
                this.Add((Attempt: 1, MinBackoff: TimeSpan.FromSeconds(1), MaxBackoff: TimeSpan.FromSeconds(10), DeltaBackoff: TimeSpan.Zero), TimeSpan.FromSeconds(1));
                this.Add((Attempt: 2, MinBackoff: TimeSpan.FromSeconds(1), MaxBackoff: TimeSpan.FromSeconds(10), DeltaBackoff: TimeSpan.Zero), TimeSpan.FromSeconds(1));
                this.Add((Attempt: 3, MinBackoff: TimeSpan.FromSeconds(1), MaxBackoff: TimeSpan.FromSeconds(10), DeltaBackoff: TimeSpan.Zero), TimeSpan.FromSeconds(1));

                // With DeltaBackoff
                this.Add((Attempt: 1, MinBackoff: TimeSpan.FromSeconds(1), MaxBackoff: TimeSpan.FromSeconds(10), DeltaBackoff: TimeSpan.FromSeconds(1)), TimeSpan.FromTicks(20559743L));
            }
        }
    }
}
