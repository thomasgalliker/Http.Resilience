using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Http.Resilience.Internals
{
    /// <summary>
    /// Defines the options used for configuring the retry policy.
    /// </summary>
    public class HttpRetryOptions
    {
        /// <summary>
        /// Returns false if we should continue retrying based on the response, and true
        /// if we should not, even though this is technically a retryable status code.
        /// </summary>
        /// <param name="response">The response to check if we should retry the request.</param>
        /// <returns>False if we should retry, true if we should not based on the response.</returns>
        public delegate bool HttpRetryableStatusCodeFilter(HttpResponseMessage response);

        private int isReadOnly;
        private int maxRetries;
        private TimeSpan minBackoff;
        private TimeSpan maxBackoff;
        private TimeSpan backoffCoefficient;
        private ICollection<HttpStatusCode> retryableStatusCodes;
        private ICollection<HttpRetryableStatusCodeFilter> retryFilters;
        private bool ensureSuccessStatusCode = true;

        private static readonly TimeSpan DefaultMinBackoff = TimeSpan.FromSeconds(1d);
        private static readonly TimeSpan DefaultMaxBackoff = TimeSpan.FromSeconds(10d);
        private static readonly TimeSpan DefaultBackoffCoefficient = TimeSpan.FromSeconds(1d);
        private static readonly int DefaultMaxRetries = 5;

        private static readonly Lazy<HttpRetryOptions> DefaultOptions = new Lazy<HttpRetryOptions>(() =>
        {
            return new HttpRetryOptions().MakeReadonly();
        });

        private static readonly HttpRetryableStatusCodeFilter hostShutdownFilter = (response) =>
        {
            return response.Headers.Contains("X-VSS-HostOfflineError");
        };

        public HttpRetryOptions()
            : this(new HttpRetryableStatusCodeFilter[1]
            {
                hostShutdownFilter
            })
        {
        }

        public HttpRetryOptions(IEnumerable<HttpRetryableStatusCodeFilter> filters)
        {
            this.BackoffCoefficient = DefaultBackoffCoefficient;
            this.MinBackoff = DefaultMinBackoff;
            this.MaxBackoff = DefaultMaxBackoff;
            this.MaxRetries = DefaultMaxRetries;
            this.RetryableStatusCodes = new HashSet<HttpStatusCode>
            {
                HttpStatusCode.BadGateway,
                HttpStatusCode.GatewayTimeout,
                HttpStatusCode.ServiceUnavailable
            };
            this.retryFilters = new HashSet<HttpRetryableStatusCodeFilter>(filters);
        }

        /// <summary>
        /// Gets a singleton read-only instance of the default settings.
        /// </summary>
        public static HttpRetryOptions Default => DefaultOptions.Value;

        /// <summary>
        /// Calls <seealso cref="HttpResponseMessage.EnsureSuccessStatusCode"/> on <seealso cref="HttpResponseMessage"/>
        /// which converts an unsuccessful HTTP status code into a <seealso cref="HttpRequestException"/> (Default=true).
        /// </summary>
        public bool EnsureSuccessStatusCode
        {
            get => this.ensureSuccessStatusCode;
            set
            {
                this.ThrowIfReadonly();
                this.ensureSuccessStatusCode = value;
            }
        }

        /// <summary>
        /// Gets or sets the coefficient which exponentially increases the backoff starting at MinBackoff.
        /// </summary>
        public TimeSpan BackoffCoefficient
        {
            get => this.backoffCoefficient;
            set
            {
                this.ThrowIfReadonly();
                this.backoffCoefficient = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum backoff interval to be used.
        /// </summary>
        public TimeSpan MinBackoff
        {
            get => this.minBackoff;
            set
            {
                this.ThrowIfReadonly();
                this.minBackoff = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum backoff interval to be used.
        /// </summary>
        public TimeSpan MaxBackoff
        {
            get => this.maxBackoff;
            set
            {
                this.ThrowIfReadonly();
                this.maxBackoff = value;
            }
        }

        // TODO:  Note that this was renamed from
        //  "maxRetries" to match the behavior of the parameter (i.e. maxRetries was previously
        //  behaving like maxAttempts).


        /// <summary>
        /// Gets or sets the maximum number of retries allowed.
        /// </summary>
        /// <remarks>
        /// This is the total number of attempts to invoke the submitted action with. A value of
        //  1 indicates that no retries will be attempted.
        /// </remarks>
        public int MaxRetries
        {
            get => this.maxRetries;
            set
            {
                this.ThrowIfReadonly();
                this.maxRetries = value;
            }
        }

        /// <summary>
        /// Gets a set of HTTP status codes which should be retried.
        /// </summary>
        public ICollection<HttpStatusCode> RetryableStatusCodes
        {
            get => this.retryableStatusCodes;
            private set
            {
                this.ThrowIfReadonly();
                this.retryableStatusCodes = value;
            }
        }

        /// <summary>
        /// How to verify that the response can be retried.
        /// </summary>
        /// <param name="response">Response message from a request.</param>
        /// <returns>True if the request can be retried, false otherwise.</returns>
        public bool IsRetryableResponse(HttpResponseMessage response)
        {
            if (this.retryableStatusCodes.Contains(response.StatusCode))
            {
                foreach (var retryFilter in this.retryFilters)
                {
                    if (retryFilter(response))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Ensures that no further modifications may be made to the retry options.
        /// </summary>
        /// <returns>A read-only instance of the retry options</returns>
        public HttpRetryOptions MakeReadonly()
        {
            if (Interlocked.CompareExchange(ref this.isReadOnly, 1, 0) == 0)
            {
                this.retryableStatusCodes = new ReadOnlyCollection<HttpStatusCode>(this.retryableStatusCodes.ToList());
                this.retryFilters = new ReadOnlyCollection<HttpRetryableStatusCodeFilter>(this.retryFilters.ToList());
            }

            return this;
        }

        /// <summary>
        /// Throws an InvalidOperationException if this is marked as ReadOnly.
        /// </summary>
        private void ThrowIfReadonly([CallerMemberName] string propertyName = "")
        {
            if (this.isReadOnly > 0)
            {
                throw new InvalidOperationException($"{nameof(HttpRetryOptions)} is marked as readonly; '{propertyName}' cannot be changed.");
            }
        }
    }
}