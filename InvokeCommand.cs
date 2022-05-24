using System;
using System.Collections.Generic;
using System.Management.Automation;
using Polly;

namespace pspolly
{
    [Cmdlet("Invoke", "PollyCommand")]
    public class InvokeCommand : PSCmdlet
    {
        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        public Policy Policy { get; set; }
        [Parameter(Mandatory = true)]
        public ScriptBlock ScriptBlock { get; set; }
        [Parameter()]
        public string OperationKey { get; set; } = Guid.NewGuid().ToString();

        protected override void ProcessRecord()
        {
            var ctx = new Context(OperationKey);

            var result = Policy.Execute<IEnumerable<PSObject>>((Context context) =>
            {
                return ScriptBlock.Invoke(context);
            }, ctx);

            WriteObject(result, true);
        }
    }
}

