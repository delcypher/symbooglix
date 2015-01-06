﻿using System;

namespace Symbooglix
{
    public class FailureLimiter : TerminationCounter
    {
        public int FailureLimit
        {
            get;
            private set;
        }
        public FailureLimiter(int failureLimit)
        {
            if (failureLimit < 1)
                throw new ArgumentException("failureLimit must be >= 1");

            FailureLimit = failureLimit;
        }

        public override void Connect(Executor e)
        {
            e.StateTerminated += HandleStateTerminated;
        }

        void HandleStateTerminated(object sender, Executor.ExecutionStateEventArgs e)
        {
            // Make sure the counting happens first
            base.handle(sender, e);

            if (NumberOfFailures >= FailureLimit)
            {
                var executor = sender as Executor;
                Console.WriteLine("Failure limit of {0} reached. Terminating Executor", FailureLimit);
                executor.Terminate();
            }
        }

        public override void Disconnect(Executor e)
        {
            e.StateTerminated -= HandleStateTerminated;
        }
    }
}

