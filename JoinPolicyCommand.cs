using System;
using System.Collections.Generic;
using System.Management.Automation;
using Polly;

namespace pspolly
{
    [Cmdlet("Join", "PollyPolicy")]
    public class JoinPolicyCommand : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public Policy[] Policy { get; set; }

        protected override void ProcessRecord()
        {
            var result = Polly.Policy.Wrap(Policy);

            WriteObject(result, true);
        }
    }
}

