namespace Http.Resilience
{
    public class LoggingOptions
    {
        public EvaluateRetryPoliciesLoggingOptions EvaluateRetryPolicies { get; set; } = new EvaluateRetryPoliciesLoggingOptions("X", "-", " ");
    }
}