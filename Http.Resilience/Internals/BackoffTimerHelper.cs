using System;
using System.ComponentModel;
using System.Threading;

namespace Http.Resilience.Internals
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class BackoffTimerHelper
    {
        private static readonly ThreadLocal<Random> Rnd = new ThreadLocal<Random>(() => new Random());

        public static TimeSpan GetRandomBackoff(TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan? previousBackoff = null)
        {
            var random = !previousBackoff.HasValue ? new Random() : new Random((int)previousBackoff.Value.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(random.Next((int)minBackoff.TotalMilliseconds, (int)maxBackoff.TotalMilliseconds));
        }

        public static TimeSpan GetExponentialBackoff(int attempt, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff, double radix)
        {
            //ArgumentUtility.CheckForOutOfRange(radix, "radix", 1.0);
            var num = Rnd.Value.NextDouble();
            var num2 = deltaBackoff == TimeSpan.Zero ? 0.0 : deltaBackoff.TotalMilliseconds * (0.8 + num * 0.4);
            var num3 = attempt < 0 ? Math.Pow(radix, attempt) * num2 : (Math.Pow(radix, attempt) - 1.0) * num2;
            return TimeSpan.FromMilliseconds(Math.Min(minBackoff.TotalMilliseconds + num3, maxBackoff.TotalMilliseconds));
        }

        public static TimeSpan GetExponentialBackoff(int attempt, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff)
        {
            return GetExponentialBackoff(attempt, minBackoff, maxBackoff, deltaBackoff, 2.0);
        }
    }
}