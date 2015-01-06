﻿using NUnit.Framework;
using System;
using Symbooglix;

namespace SymbooglixLibTests
{
    [TestFixture()]
    public class FailureLimit : SymbooglixTest
    {
        [Test()]
        public void OneFailure()
        {
            p = LoadProgramFrom(@"
                procedure main()
                {
                    var x:int;

                    assert x > 0;
                    assert x < 0;
                }

            ", "test.bpl");

            e = GetExecutor(p, new DFSStateScheduler(), GetSolver());

            var failureLimiter = new FailureLimiter(1);
            var tc = new TerminationCounter();
            failureLimiter.Connect(e);
            tc.Connect(e);

            e.Run(GetMain(p));

            // FIXME: Need executor to give a statistic for number of states (this will include non-terminated states)
            Assert.AreEqual(1, tc.NumberOfTerminatedStates);
            Assert.AreEqual(1, tc.NumberOfFailures);
        }

        [Test()]
        public void TwoFailures()
        {
            p = LoadProgramFrom(@"
                procedure main()
                {
                    var x:int;

                    assert x > 0;
                    assert x == 5; // with error limit of 2, we won't follow the successful path
                }

            ", "test.bpl");

            e = GetExecutor(p, new DFSStateScheduler(), GetSolver());

            var failureLimiter = new FailureLimiter(2);
            var tc = new TerminationCounter();
            failureLimiter.Connect(e);
            tc.Connect(e);

            e.Run(GetMain(p));

            // FIXME: Need executor to give a statistic for number of states (this will include non-terminated states)
            Assert.AreEqual(2, tc.NumberOfTerminatedStates);
            Assert.AreEqual(2, tc.NumberOfFailures);
        }

        [Test(),ExpectedException(typeof(ArgumentException))]
        public void InvalidLimit()
        {
            var failureLimiter = new FailureLimiter(0);
            Console.WriteLine("{0}", failureLimiter.FailureLimit); // supress not used warning
        }
    }
}

