using Http.Resilience.Policies;

namespace Http.Resilience
{
    public class EvaluateRetryPoliciesLoggingOptions
    {
        public EvaluateRetryPoliciesLoggingOptions(
            string shouldRetryTrue,
            string shouldRetryFalse,
            string shouldRetryUnknown)
        {
            this.ShouldRetry = shouldRetryTrue;
            this.ShouldNotRetry = shouldRetryFalse;
            this.NotEvaluated = shouldRetryUnknown;
        }

        /// <summary>
        /// The check mark string to be used in log messages
        /// when <see cref="IRetryPolicy.ShouldRetry(object)"/> returned <c>true</c>.
        /// </summary>
        public string ShouldRetry { get; set; }

        /// <summary>
        /// The check mark string to be used in log messages
        /// when <see cref="IRetryPolicy.ShouldRetry(object)"/> returned <c>false</c>.
        /// </summary>
        public string ShouldNotRetry { get; set; }

        /// <summary>
        /// The check mark string to be used in log messages
        /// when <see cref="IRetryPolicy.ShouldRetry(object)"/> was not evaluated
        /// since another retry policy has returned <c>true</c>.
        /// </summary>
        public string NotEvaluated { get; set; }
    }
}