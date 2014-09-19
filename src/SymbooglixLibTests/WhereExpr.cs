﻿using NUnit.Framework;
using System;
using Symbooglix;

namespace SymbooglixLibTests
{
    [TestFixture()]
    public class WhereExpr : SymbooglixTest
    {
        [Test(),ExpectedException(typeof(NotImplementedException))]
        public void LocalVariable()
        {
            p = loadProgram("programs/WhereExpr.bpl");
            e = getExecutor(p, new DFSStateScheduler(), GetSolver());

            var counter = new TerminationCounter();
            counter.Connect(e);
            e.Run(getMain(p));

            Assert.AreEqual(0, counter.NumberOfFailures);
            Assert.AreEqual(1, counter.Sucesses);
        }

        [Test(),ExpectedException(typeof(NotImplementedException))]
        public void GlobalVariable()
        {
            p = loadProgram("programs/GlobalWhereExpr.bpl");
            e = getExecutor(p, new DFSStateScheduler(), GetSolver());

            var counter = new TerminationCounter();
            counter.Connect(e);
            e.Run(getMain(p));

            Assert.AreEqual(0, counter.NumberOfFailures);
            Assert.AreEqual(1, counter.Sucesses);
        }
    }
}
