using Http.Resilience.Policies;

namespace Http.Resilience
{
    public class EvaluateRetryPoliciesLoggingOptions
    {
        public EvaluateRetryPoliciesLoggingOptions()
        {
        }

        /// <summary>
        /// The check mark string to be used in log messages
        /// when <see cref="IRetryPolicy.ShouldRetry(object)"/> returned <c>true</c>.
        /// Default: "X" (check mark).
        /// </summary>
        public string ShouldRetry { get; set; } = "X";

        /// <summary>
        /// The check mark string to be used in log messages
        /// when <see cref="IRetryPolicy.ShouldRetry(object)"/> returned <c>false</c>.
        /// Default: "-" (dash).
        /// </summary>
        public string ShouldNotRetry { get; set; } = "-";

        /// <summary>
        /// The check mark string to be used in log messages
        /// when <see cref="IRetryPolicy.ShouldRetry(object)"/> was not evaluated
        /// since another retry policy has returned <c>true</c>.
        /// Default: " " (white space).
        /// </summary>
        public string NotEvaluated { get; set; } = " ";
    }
}