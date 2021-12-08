using System;
using System.Collections;

namespace Http.Resilience.Tests
{
    //var httpClientSimulator = new HttpClientSimulator<HttpResponseMessage>();
    //httpClientSimulator.AddAttempt(new WebException("Test exception", WebExceptionStatus.ReceiveFailure));
    //httpClientSimulator.AddAttempt(new HttpResponseMessage(HttpStatusCode.OK));

    public class HttpClientSimulator<TResult>
    {
        private readonly Queue attempts = new Queue(new ArrayList());

        public void AddAttempt(TResult factory)
        {
            this.attempts.Enqueue(factory);
        }

        public void AddAttempt(Exception exception)
        {
            this.attempts.Enqueue(exception);
        }

        public TResult Invoke()
        {
            var peek = this.attempts.Dequeue();
            if (peek is Exception ex)
            {
                throw ex;
            }

            return (TResult)peek;
        }
    }
}
