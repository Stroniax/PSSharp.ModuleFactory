namespace PSSharp.ModuleFactory.Commands
{
    [Cmdlet(VerbsOther.Use, "DisposableObject",
        RemotingCapability = RemotingCapability.None)]
    public sealed class UseDisposableObjectCommand : PSCmdlet, IDisposable
    {
        [Parameter(Mandatory = true, Position = 0)]
        public IDisposable? InputObject { get; set; }
        [Parameter(Mandatory = true, Position = 1)]
        public ScriptBlock ScriptBlock { get; set; } = null!;
        protected override void EndProcessing()
        {
            try
            {
                SessionState.InvokeCommand.InvokeScript(false, ScriptBlock, null);
            }
            finally
            {
                InputObject?.Dispose();
                InputObject = null;
            }
        }
        public void Dispose()
        {
            InputObject?.Dispose();
            InputObject = null;

            GC.SuppressFinalize(this);
        }

        ~UseDisposableObjectCommand()
        {
            Dispose();
        }
    }
}
