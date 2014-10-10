﻿using Microsoft.Boogie;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Symbooglix;

namespace SymbooglixLibTests
{
    [TestFixture()]
    public class StateScheduling : SymbooglixTest
    {
        private void SimpleLoop(IStateScheduler scheduler)
        {
            p = loadProgram("programs/SimpleLoop.bpl");
            e = getExecutor(p, scheduler, GetSolver());
            e.UseConstantFolding = true;

            var main = p.TopLevelDeclarations.OfType<Implementation>().Where(i => i.Name == "main").First();

            var boundsVar = main.InParams[0];

            var entryBlock = main.Blocks[0];
            Assert.AreEqual("entry", entryBlock.Label);

            var loopHead = main.Blocks[1];
            Assert.AreEqual("loopHead", loopHead.Label);

            var loopBody = main.Blocks[2];
            Assert.AreEqual("loopBody", loopBody.Label);
            var loopBodyAssume = loopBody.Cmds[0] as AssumeCmd;
            Assert.IsNotNull(loopBodyAssume);

            var loopExit = main.Blocks[3];
            Assert.AreEqual("loopDone", loopExit.Label);
            var loopExitAssume = loopExit.Cmds[0] as AssumeCmd;
            Assert.IsNotNull(loopExitAssume);

            var exitBlock = main.Blocks[4];
            Assert.AreEqual("exit", exitBlock.Label);

            var tc = new TerminationCounter();
            tc.Connect(e);

            int change = 1;
            int contextChangeCount = 0;
            e.ContextChanged += delegate(object sender, Executor.ContextChangeEventArgs eventArgs)
            {
                ++contextChangeCount;

                var symbolicForBound = eventArgs.Previous.Symbolics.Where( s => s.Origin.IsVariable && s.Origin.AsVariable == boundsVar).First();

                if (change ==1)
                {
                    // FIXME: The Executor shouldn't pop the last stack frame so we can check where we terminated successfully
                    Assert.IsTrue(eventArgs.Previous.TerminationType.ExitLocation.IsTransferCmd);
                    Assert.IsTrue(eventArgs.Previous.TerminationType.ExitLocation.AsTransferCmd is ReturnCmd);
                    Assert.IsTrue(eventArgs.Previous.Finished());
                    Assert.AreEqual(3, eventArgs.Previous.Constraints.Count);
                    var exitConstraint = eventArgs.Previous.Constraints.Constraints.Where( c => c.Origin.IsCmd && c.Origin.AsCmd == loopExitAssume);
                    Assert.AreEqual(1, exitConstraint.Count());
                    Assert.AreEqual( symbolicForBound.Name + " <= 0", exitConstraint.First().Condition.ToString());

                    Assert.AreSame(loopBody, eventArgs.Next.GetCurrentBlock());
                    Assert.AreEqual(2, eventArgs.Next.Constraints.Count);
                    var bodyConstraint = eventArgs.Next.Constraints.Constraints.Where( c => c.Origin.IsCmd && c.Origin.AsCmd == loopBodyAssume);
                    Assert.AreEqual(1, bodyConstraint.Count());
                    Assert.AreEqual("0 < " + symbolicForBound.Name, bodyConstraint.First().Condition.ToString());
                }
                else if (change == 2)
                {
                    Assert.IsTrue(eventArgs.Previous.Finished());
                    Assert.AreSame(loopBody, eventArgs.Next.GetCurrentBlock());
                    Assert.AreEqual(4, eventArgs.Previous.Constraints.Count);

                    var exitConstraint = eventArgs.Previous.Constraints.Constraints.Where( c => c.Origin.IsCmd && c.Origin.AsCmd == loopExitAssume);
                    Assert.AreEqual(1, exitConstraint.Count());
                    Assert.AreEqual( symbolicForBound.Name + " <= 1", exitConstraint.First().Condition.ToString());

                    Assert.AreSame(loopBody, eventArgs.Next.GetCurrentBlock());
                    Assert.AreEqual(3, eventArgs.Next.Constraints.Count);
                    var bodyConstraints = eventArgs.Next.Constraints.Constraints.Where( c => c.Origin.IsCmd && c.Origin.AsCmd == loopBodyAssume).ToList();
                    Assert.AreEqual(2, bodyConstraints.Count());
                    Assert.AreEqual("0 < " + symbolicForBound.Name, bodyConstraints[0].Condition.ToString());
                    Assert.AreEqual("1 < " + symbolicForBound.Name, bodyConstraints[1].Condition.ToString());
                }


                ++change;
            };

            e.Run(main);
            Assert.AreEqual(3, tc.NumberOfTerminatedStates);
            Assert.AreEqual(3, tc.Sucesses);
            Assert.AreEqual(2, contextChangeCount);
        }

        [Test()]
        public void ExploreAllStatesDFS()
        {
            SimpleLoop(new DFSStateScheduler());
        }

        [Test()]
        public void ExploreAllStatesUntilTerminationBFS()
        {
            SimpleLoop(new UntilTerminationBFSStateScheduler());
        }

        [Test()]
        public void ExploreAllStatesBFS()
        {
            // TODO: SimpleLoop() assumes DFS can't use it
            //SimpleLoop(new BFSStateScheduler());
        }

        private void ExploreOrderInit(IStateScheduler scheduler, out Implementation main, out Block entryBlock, out List<Block> l)
        {
            p = loadProgram("programs/StateScheduleTest.bpl");
            e = getExecutor(p, scheduler, GetSolver());

            main = p.TopLevelDeclarations.OfType<Implementation>().Where(i => i.Name == "main").First();
            entryBlock = main.Blocks[0];
            Assert.AreEqual("entry", entryBlock.Label);

            // Collect "l<N>" blocks
            l = new List<Block>();
            for (int index = 0; index <= 5; ++index)
            {
                l.Add(main.Blocks[index + 1]);
                Assert.AreEqual("l" + index, l[index].Label);
            }
        }

        [Test()]
        public void ExploreOrderDFS()
        {
            List<Block> l;
            Block entryBlock;
            Implementation main;
            ExploreOrderInit(new DFSStateScheduler(), out main, out entryBlock, out l);

            int changed = 0;
            e.ContextChanged += delegate(object sender, Executor.ContextChangeEventArgs eventArgs)
            {
                switch(changed)
                {
                    case 0:
                        Assert.IsInstanceOfType(typeof(TerminatedWithoutError),eventArgs.Previous.TerminationType);
                        Assert.AreSame(l[2],eventArgs.Previous.Mem.Stack[0].CurrentBlock);

                        Assert.IsFalse(eventArgs.Next.Finished());
                        Assert.AreSame(l[3],eventArgs.Next.GetCurrentBlock());
                        break;
                    case 1:
                        Assert.IsInstanceOfType(typeof(TerminatedWithoutError),eventArgs.Previous.TerminationType);
                        Assert.AreSame(l[3],eventArgs.Previous.Mem.Stack[0].CurrentBlock);

                        /* FIXME: At a three way branch we schedule l0, l2, l1 rather than
                         * l0, l1, l2. i.e. we aren't going left to right over the GotoCmd targets.
                         * This is because the DFSScheduler executes the last added state first so it executes the ExecutionState
                         * going to l2 before the state going to l1.
                         *
                         * I'm not sure this is desirable.
                         */
                        Assert.IsFalse(eventArgs.Next.Finished());
                        Assert.AreSame(l[2],eventArgs.Next.GetCurrentBlock());
                        break;
                    case 2:
                        Assert.IsInstanceOfType(typeof(TerminatedWithoutError),eventArgs.Previous.TerminationType);
                        Assert.AreSame(l[2],eventArgs.Previous.GetCurrentBlock());

                        Assert.IsFalse(eventArgs.Next.Finished());
                        Assert.AreSame(l[1],eventArgs.Next.GetCurrentBlock());
                        break;

                    case 3:
                        Assert.IsTrue(eventArgs.Previous.TerminationType is TerminatedWithoutError);
                        Assert.AreSame(l[4],eventArgs.Previous.Mem.Stack[0].CurrentBlock);

                        Assert.IsFalse(eventArgs.Next.Finished());
                        Assert.AreSame(l[5],eventArgs.Next.GetCurrentBlock());
                        break;
                    default:
                        Assert.Fail("Too many context changes");
                        break;
                }
                ++changed;
            };

            e.Run(main);

            Assert.AreEqual(4, changed);
        }

        [Test()]
        public void ExploreOrderUntilEndBFS()
        {
            List<Block> l;
            Block entryBlock;
            Implementation main;
            ExploreOrderInit(new UntilTerminationBFSStateScheduler(), out main, out entryBlock, out l);

            int changed = 0;
            e.ContextChanged += delegate(object sender, Executor.ContextChangeEventArgs eventArgs)
            {
                switch(changed)
                {
                    case 0:
                        Assert.IsInstanceOfType(typeof(TerminatedWithoutError),eventArgs.Previous.TerminationType);
                        Assert.AreSame(l[2],eventArgs.Previous.Mem.Stack[0].CurrentBlock);

                        Assert.IsFalse(eventArgs.Next.Finished());
                        Assert.AreSame(l[1],eventArgs.Next.GetCurrentBlock());
                        break;
                    case 1:
                        Assert.IsInstanceOfType(typeof(TerminatedWithoutError),eventArgs.Previous.TerminationType);
                        Assert.AreSame(l[4],eventArgs.Previous.Mem.Stack[0].CurrentBlock);

                        Assert.IsFalse(eventArgs.Next.Finished());
                        Assert.AreSame(l[2],eventArgs.Next.GetCurrentBlock());
                        break;
                    case 2:
                        Assert.IsInstanceOfType(typeof(TerminatedWithoutError),eventArgs.Previous.TerminationType);
                        Assert.AreSame(l[2],eventArgs.Previous.GetCurrentBlock());

                        Assert.IsFalse(eventArgs.Next.Finished());
                        Assert.AreSame(l[3],eventArgs.Next.GetCurrentBlock());
                        break;

                    case 3:
                        Assert.IsTrue(eventArgs.Previous.TerminationType is TerminatedWithoutError);
                        Assert.AreSame(l[3],eventArgs.Previous.Mem.Stack[0].CurrentBlock);

                        Assert.IsFalse(eventArgs.Next.Finished());
                        Assert.AreSame(l[5],eventArgs.Next.GetCurrentBlock());
                        break;
                    default:
                        Assert.Fail("Too many context changes");
                        break;
                }
                ++changed;
            };

            e.Run(main);

            Assert.AreEqual(4, changed);
        }
    }
}
