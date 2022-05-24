using System;
using System.Management.Automation;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;

namespace pspolly
{
    [Cmdlet("New", "PollyPolicy")]
    public class NewPolicyCommand : PSCmdlet
    {
        [Parameter(ParameterSetName = "Retry", Mandatory = true)]
        public SwitchParameter Retry { get; set; }
        [Parameter(ParameterSetName = "Retry")]
        public int RetryCount { get; set; } = 1;
        [Parameter(ParameterSetName = "Retry")]
        public TimeSpan[] RetryWait { get; set; }

        [Parameter(ParameterSetName = "RetryForever", Mandatory = true)]
        public SwitchParameter RetryForever { get; set; }
        [Parameter(ParameterSetName = "RetryForever")]
        [Parameter(ParameterSetName = "Retry")]
        public ScriptBlock SleepDuration { get; set; }

        [Parameter(ParameterSetName = "CircuitBreaker", Mandatory = true)]
        public SwitchParameter CircuitBreaker { get; set; }
        [Parameter(ParameterSetName = "CircuitBreaker")]
        public int ExceptionsAllowedBeforeBreaking { get; set; } = 1;
        [Parameter(ParameterSetName = "CircuitBreaker")]
        public ScriptBlock OnBreak { get; set; }
        [Parameter(ParameterSetName = "CircuitBreaker")]
        public TimeSpan DurationOfBreak { get; set; }

        [Parameter(ParameterSetName = "Timeout")]
        public TimeSpan Timeout { get; set; }
        [Parameter(ParameterSetName = "Timeout")]
        public ScriptBlock OnTimeout { get; set; }
        [Parameter(ParameterSetName = "Cache")]
        public DateTimeOffset AbsoluteExpiration { get; set; }
        [Parameter(ParameterSetName = "Cache")]
        public TimeSpan SlidingExpiration { get; set; }
        [Parameter(ParameterSetName = "Cache")]
        public ScriptBlock OnCacheError { get; set; }
        [Parameter(ParameterSetName = "RateLimit", Mandatory = true)]
        public int Executions { get; set; }
        [Parameter(ParameterSetName = "RateLimit", Mandatory = true)]
        public TimeSpan PerTimeSpan { get; set; }

        private static MemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
        private static MemoryCacheProvider _memoryCacheProvider = new MemoryCacheProvider(_memoryCache);

        protected override void ProcessRecord()
        {
            var policyBuilder = Policy.Handle<Exception>();

            Policy policy = null;
            if (ParameterSetName == "Retry")
            {
                if (SleepDuration != null)
                {
                    policy = policyBuilder.WaitAndRetry(RetryCount, (retryCount) =>
                                       {
                                           var psObject = SleepDuration.Invoke(retryCount);
                                           return (TimeSpan)psObject[0].BaseObject;
                                       });
                }
                if (RetryWait != null)
                {
                    policy = policyBuilder.WaitAndRetry(RetryWait);
                }
                else
                {
                    policy = policyBuilder.Retry(RetryCount);
                }
            }

            if (ParameterSetName == "RetryForever")
            {
                if (SleepDuration == null)
                {
                    policy = policyBuilder.RetryForever();
                }
                else
                {
                    policy = policyBuilder.WaitAndRetryForever((retryCount) =>
                    {
                        var psObject = SleepDuration.Invoke(retryCount);
                        return (TimeSpan)psObject[0].BaseObject;
                    });
                }
            }

            if (ParameterSetName == "CircuitBreaker")
            {
                policy = policyBuilder.CircuitBreaker(ExceptionsAllowedBeforeBreaking, DurationOfBreak, (ex, ts) =>
                {
                    OnBreak?.Invoke(ex, ts);
                }, () => { });
            }

            if (ParameterSetName == "Timeout")
            {
                policy = Policy.Timeout(Timeout, onTimeout: (context, timespan, task) =>
                {
                    OnTimeout?.Invoke(context, timespan, task);
                });
            }

            if (ParameterSetName == "Cache")
            {
                if (MyInvocation.BoundParameters.ContainsKey("AbsoluteExpiration"))
                {
                    policy = Policy.Cache(_memoryCacheProvider, new AbsoluteTtl(AbsoluteExpiration), (context, key, ex) =>
                    {
                        OnCacheError?.Invoke(context, key, ex);
                    });
                }

                if (MyInvocation.BoundParameters.ContainsKey("SlidingExpiration"))
                {
                    policy = Policy.Cache(_memoryCacheProvider, new SlidingTtl(SlidingExpiration), (context, key, ex) =>
                    {
                        OnCacheError?.Invoke(context, key, ex);
                    });
                }
            }

            if (ParameterSetName == "RateLimit")
            {
                policy = Policy.RateLimit(Executions, PerTimeSpan);
            }

            WriteObject(policy);
        }
    }
}

