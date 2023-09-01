using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using Http.Resilience.Extensions;

namespace Http.Resilience
{
    /// <summary>
    ///     Defines the options used for configuring the retry policy.
    /// </summary>
    public class HttpRetryOptions
    {
        /// <summary>
        ///     Returns false if we should continue retrying based on the response, and true
        ///     if we should not, even though this is technically a retryable status code.
        /// </summary>
        /// <param name="statusCode">The response status code to check if we should retry the request.</param>
        /// <returns>False if we should retry, true if we should not based on the response.</returns>
        public delegate bool HttpResponseMessageFilter(int statusCode, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers);

        private const int DefaultMaxRetries = 5;

        private static readonly TimeSpan DefaultMinBackoff = TimeSpan.FromSeconds(1d);
        private static readonly TimeSpan DefaultMaxBackoff = TimeSpan.FromSeconds(10d);
        private static readonly TimeSpan DefaultBackoffCoefficient = TimeSpan.FromSeconds(1d);

        private static readonly Lazy<HttpRetryOptions> DefaultOptions = new(() =>
        {
            return new HttpRetryOptions().MakeReadonly();
        });

        private static readonly HttpResponseMessageFilter HostShutdownFilter = (statusCode, headers) =>
        {
            return headers.Any(h => h.Key == "X-VSS-HostOfflineError");
        };

        private TimeSpan backoffCoefficient;
        private bool ensureSuccessStatusCode = true;
        private ICollection<HttpResponseMessageFilter> httpResponseMessageFilters;

        private int isReadOnly;
        private TimeSpan maxBackoff;
        private int maxRetries;
        private TimeSpan minBackoff;
        private ICollection<HttpStatusCode> retryableStatusCodes;
        private LoggingOptions logging = new LoggingOptions();

        public HttpRetryOptions()
            : this(new[] { HostShutdownFilter })
        {
        }

        public HttpRetryOptions(IEnumerable<HttpResponseMessageFilter> filters)
        {
            this.BackoffCoefficient = DefaultBackoffCoefficient;
            this.MinBackoff = DefaultMinBackoff;
            this.MaxBackoff = DefaultMaxBackoff;
            this.MaxRetries = DefaultMaxRetries;
            this.RetryableStatusCodes = new HashSet<HttpStatusCode>
            {
                HttpStatusCode.BadGateway,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.GatewayTimeout
            };
            this.httpResponseMessageFilters = new HashSet<HttpResponseMessageFilter>(filters);
        }

        /// <summary>
        ///     Gets a singleton read-only instance of the default settings.
        /// </summary>
        public static HttpRetryOptions Default => DefaultOptions.Value;

        /// <summary>
        /// Calls <seealso cref="HttpResponseMessage.EnsureSuccessStatusCode" /> on <seealso cref="HttpResponseMessage" />
        /// which converts an unsuccessful HTTP status code into a <seealso cref="HttpRequestException" />
        /// (Default: true).
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
        ///     Gets or sets the coefficient which exponentially increases the backoff starting at MinBackoff.
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
        ///     Gets or sets the minimum backoff interval to be used.
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
        ///     Gets or sets the maximum backoff interval to be used.
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

        public LoggingOptions Logging
        {
            get => this.logging;
            set
            {
                this.ThrowIfReadonly();
                this.logging = value ?? new LoggingOptions();
            }
        }
        /// <summary>
        ///     Gets or sets the maximum number of retries for one particular request.
        /// </summary>
        /// <remarks>
        ///     A value of 0 indicates that no retries will be attempted.
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
        /// (Default: BadGateway/502, ServiceUnavailable/503, GatewayTimeout/504"/>)
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
        ///     Verifies if the request with the given <paramref name="httpResponseMessage" /> can be retried.
        /// </summary>
        /// <param name="httpResponseMessage">Response message from a request.</param>
        /// <returns>True if the request can be retried, false otherwise.</returns>
        public bool IsRetryableResponse(HttpResponseMessage httpResponseMessage)
        {
            return this.IsRetryableResponse(httpResponseMessage.StatusCode, httpResponseMessage.Headers);
        }

        public bool IsRetryableResponse(HttpWebResponse httpWebResponse)
        {
            return this.IsRetryableResponse(httpWebResponse.StatusCode, httpWebResponse.Headers.GetHeaders());
        }

        public bool IsRetryableResponse(HttpStatusCode statusCode, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            var isRetryFiltered = false;
            var hasRetryableStatusCode = this.RetryableStatusCodes.Contains(statusCode);
            if (hasRetryableStatusCode)
            {
                isRetryFiltered = this.IsRetryFiltered((int)statusCode, headers);
            }

            return hasRetryableStatusCode && !isRetryFiltered;
        }

        private bool IsRetryFiltered(int statusCode, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            foreach (var filter in this.httpResponseMessageFilters)
            {
                try
                {
                    if (filter(statusCode, headers))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignored
                }
            }

            return false;
        }

        /// <summary>
        ///     Ensures that no further modifications may be made to the retry options.
        /// </summary>
        /// <returns>A read-only instance of the retry options</returns>
        public HttpRetryOptions MakeReadonly()
        {
            if (Interlocked.CompareExchange(ref this.isReadOnly, 1, 0) == 0)
            {
                this.retryableStatusCodes = new ReadOnlyCollection<HttpStatusCode>(this.retryableStatusCodes.ToList());
                this.httpResponseMessageFilters = new ReadOnlyCollection<HttpResponseMessageFilter>(this.httpResponseMessageFilters.ToList());
            }

            return this;
        }

        /// <summary>
        ///     Throws an InvalidOperationException if this is marked as ReadOnly.
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