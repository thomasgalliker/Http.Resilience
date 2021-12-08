using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Http.Resilience.Internals
{
    //
    // Summary:
    //     Defines the options used for configuring the retry policy.
    public class VssHttpRetryOptions
    {
        //
        // Summary:
        //     Returns false if we should continue retrying based on the response, and true
        //     if we should not, even though this is technically a retryable status code.
        //
        // Parameters:
        //   response:
        //     The response to check if we should retry the request.
        //
        // Returns:
        //     False if we should retry, true if we should not based on the response.
        public delegate bool VssHttpRetryableStatusCodeFilter(HttpResponseMessage response);

        private int isReadOnly;

        private int maxRetries;

        private TimeSpan minBackoff;

        private TimeSpan maxBackoff;

        private TimeSpan backoffCoefficient;

        private ICollection<HttpStatusCode> retryableStatusCodes;

        private ICollection<VssHttpRetryableStatusCodeFilter> retryFilters;

        private static readonly TimeSpan DefaultMinBackoff = TimeSpan.FromSeconds(10.0);

        private static readonly TimeSpan DefaultMaxBackoff = TimeSpan.FromMinutes(10.0);

        private static readonly TimeSpan DefaultBackoffCoefficient = TimeSpan.FromSeconds(1.0);

        private static readonly Lazy<VssHttpRetryOptions> DefaultOptions = new Lazy<VssHttpRetryOptions>(() => new VssHttpRetryOptions().MakeReadonly());

        private static readonly VssHttpRetryableStatusCodeFilter s_hostShutdownFilter = (response) => response.Headers.Contains("X-VSS-HostOfflineError");

        //
        // Summary:
        //     Gets a singleton read-only instance of the default settings.
        public static VssHttpRetryOptions Default => DefaultOptions.Value;

        //
        // Summary:
        //     Gets or sets the coefficient which exponentially increases the backoff starting
        //     at Microsoft.VisualStudio.Services.Common.VssHttpRetryOptions.MinBackoff.
        public TimeSpan BackoffCoefficient
        {
            get
            {
                return this.backoffCoefficient;
            }
            set
            {
                this.ThrowIfReadonly();
                this.backoffCoefficient = value;
            }
        }

        //
        // Summary:
        //     Gets or sets the minimum backoff interval to be used.
        public TimeSpan MinBackoff
        {
            get
            {
                return this.minBackoff;
            }
            set
            {
                this.ThrowIfReadonly();
                this.minBackoff = value;
            }
        }

        //
        // Summary:
        //     Gets or sets the maximum backoff interval to be used.
        public TimeSpan MaxBackoff
        {
            get
            {
                return this.maxBackoff;
            }
            set
            {
                this.ThrowIfReadonly();
                this.maxBackoff = value;
            }
        }

        //
        // Summary:
        //     Gets or sets the maximum number of retries allowed.
        public int MaxRetries
        {
            get
            {
                return this.maxRetries;
            }
            set
            {
                this.ThrowIfReadonly();
                this.maxRetries = value;
            }
        }

        //
        // Summary:
        //     Gets a set of HTTP status codes which should be retried.
        public ICollection<HttpStatusCode> RetryableStatusCodes
        {
            get
            {
                return this.retryableStatusCodes;
            }
            private set
            {
                this.ThrowIfReadonly();
                this.retryableStatusCodes = value;
            }
        }

        public VssHttpRetryOptions()
            : this(new VssHttpRetryableStatusCodeFilter[1]
            {
                s_hostShutdownFilter
            })
        {
        }

        public VssHttpRetryOptions(IEnumerable<VssHttpRetryableStatusCodeFilter> filters)
        {
            this.BackoffCoefficient = DefaultBackoffCoefficient;
            this.MinBackoff = DefaultMinBackoff;
            this.MaxBackoff = DefaultMaxBackoff;
            this.MaxRetries = 5;
            this.RetryableStatusCodes = new HashSet<HttpStatusCode>
            {
                HttpStatusCode.BadGateway,
                HttpStatusCode.GatewayTimeout,
                HttpStatusCode.ServiceUnavailable
            };
            this.retryFilters = new HashSet<VssHttpRetryableStatusCodeFilter>(filters);
        }

        //
        // Summary:
        //     How to verify that the response can be retried.
        //
        // Parameters:
        //   response:
        //     Response message from a request
        //
        // Returns:
        //     True if the request can be retried, false otherwise.
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

        //
        // Summary:
        //     Ensures that no further modifications may be made to the retry options.
        //
        // Returns:
        //     A read-only instance of the retry options
        public VssHttpRetryOptions MakeReadonly()
        {
            if (Interlocked.CompareExchange(ref this.isReadOnly, 1, 0) == 0)
            {
                this.retryableStatusCodes = new ReadOnlyCollection<HttpStatusCode>(this.retryableStatusCodes.ToList());
                this.retryFilters = new ReadOnlyCollection<VssHttpRetryableStatusCodeFilter>(this.retryFilters.ToList());
            }

            return this;
        }

        //
        // Summary:
        //     Throws an InvalidOperationException if this is marked as ReadOnly.
        private void ThrowIfReadonly()
        {
            if (this.isReadOnly > 0)
            {
                throw new InvalidOperationException();
            }
        }
    }
}