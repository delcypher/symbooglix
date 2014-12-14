﻿using Microsoft.Boogie;
using NUnit.Framework;
using System;
using Symbooglix;

namespace SymbooglixLibTests
{
    [TestFixture()]
    public class Assignment : SymbooglixTest
    {
        [Test()]
        public void SimpleConcreteAssignment()
        {
            p = LoadProgramFrom(@"
                procedure main()
                {
                    var x:int;
                    assert {:symbooglix_bp ""before""} true;
                    x := 5;
                    assert {:symbooglix_bp ""after""} true;
                }
            ", "file.bpl");
            e = GetExecutor(p, new DFSStateScheduler(), GetSolver());

            int count = 0;
            e.BreakPointReached += delegate(object sender, Executor.BreakPointEventArgs eventArgs)
            {
                switch (eventArgs.Name)
                {
                    case "before":
                        var vAndExpr = e.CurrentState.GetInScopeVariableAndExprByName("x");
                        Assert.IsInstanceOf<IdentifierExpr>(vAndExpr.Value);
                        Assert.IsInstanceOf<SymbolicVariable>((vAndExpr.Value as IdentifierExpr).Decl);
                        break;
                    case "after":
                        vAndExpr = e.CurrentState.GetInScopeVariableAndExprByName("x");
                        Assert.IsInstanceOf<LiteralExpr>(vAndExpr.Value);
                        var literal = vAndExpr.Value as LiteralExpr;
                        Assert.IsTrue(literal.isBigNum);
                        Assert.AreEqual(5, literal.asBigNum.ToInt);
                        break;
                    default:
                        Assert.Fail("unrecognised breakpoint");
                        break;
                }
                ++count;
            };
            e.Run(GetMain(p));
            Assert.AreEqual(2, count);
        }

        [Test()]
        public void SimpleSymbolicAssigment()
        {
            p = LoadProgramFrom(@"
                procedure main()
                {
                    var x:int;
                    assert {:symbooglix_bp ""before""} true;
                    x :=  x + x;
                    assert {:symbooglix_bp ""after""} true;
                }
            ", "file.bpl");
            e = GetExecutor(p, new DFSStateScheduler(), GetSolver());

            int count = 0;
            IdentifierExpr symbolic = null;
            e.BreakPointReached += delegate(object sender, Executor.BreakPointEventArgs eventArgs)
            {
                switch (eventArgs.Name)
                {
                    case "before":
                        var vAndExpr = e.CurrentState.GetInScopeVariableAndExprByName("x");
                        Assert.IsInstanceOf<IdentifierExpr>(vAndExpr.Value);
                        Assert.IsInstanceOf<SymbolicVariable>((vAndExpr.Value as IdentifierExpr).Decl);
                        symbolic = vAndExpr.Value as IdentifierExpr;
                        break;
                    case "after":
                        Assert.IsNotNull(symbolic);
                        vAndExpr = e.CurrentState.GetInScopeVariableAndExprByName("x");
                        Assert.IsInstanceOf<NAryExpr>(vAndExpr.Value);
                        Assert.IsInstanceOf<BinaryOperator>((vAndExpr.Value as NAryExpr).Fun);
                        Assert.AreEqual(BinaryOperator.Opcode.Add, (( vAndExpr.Value as NAryExpr).Fun as BinaryOperator).Op);
                        Assert.AreEqual(symbolic.Name + " + " + symbolic.Name, vAndExpr.Value.ToString());
                        break;
                    default:
                        Assert.Fail("unrecognised breakpoint");
                        break;
                }
                ++count;
            };
            e.Run(GetMain(p));
            Assert.AreEqual(2, count);
        }

        [Test()]
        public void SimpleMapAssigment()
        {
            p = LoadProgramFrom(@"
                procedure main()
                {
                    var x:[bool]int;
                    assert {:symbooglix_bp ""before""} true;
                    x[true] :=  8;
                    assert {:symbooglix_bp ""after""} true;
                    x[false] := 7;
                    assert {:symbooglix_bp ""after2""} true;
                }
            ", "file.bpl");
            e = GetExecutor(p, new DFSStateScheduler(), GetSolver());

            int count = 0;
            IdentifierExpr symbolic = null;
            e.BreakPointReached += delegate(object sender, Executor.BreakPointEventArgs eventArgs)
            {
                switch (eventArgs.Name)
                {
                    case "before":
                        var vAndExpr = e.CurrentState.GetInScopeVariableAndExprByName("x");
                        Assert.IsInstanceOf<IdentifierExpr>(vAndExpr.Value);
                        Assert.IsInstanceOf<SymbolicVariable>((vAndExpr.Value as IdentifierExpr).Decl);
                        symbolic = vAndExpr.Value as IdentifierExpr;
                        break;
                    case "after":
                        Assert.IsNotNull(symbolic);
                        vAndExpr = e.CurrentState.GetInScopeVariableAndExprByName("x");
                        Assert.IsInstanceOf<NAryExpr>(vAndExpr.Value);
                        Assert.IsInstanceOf<MapStore>((vAndExpr.Value as NAryExpr).Fun);
                        Assert.AreEqual(symbolic.Name + "[true := 8]", vAndExpr.Value.ToString());
                        break;
                    case "after2":
                        Assert.IsNotNull(symbolic);
                        vAndExpr = e.CurrentState.GetInScopeVariableAndExprByName("x");
                        Assert.IsInstanceOf<NAryExpr>(vAndExpr.Value);
                        Assert.IsInstanceOf<MapStore>((vAndExpr.Value as NAryExpr).Fun);
                        Assert.AreEqual(symbolic.Name + "[true := 8][false := 7]", vAndExpr.Value.ToString());

                        // Check order on the assign, false key should be outer most
                        var mapStore = vAndExpr.Value as NAryExpr;
                        Assert.IsInstanceOf<NAryExpr>(mapStore.Args[0]);
                        Assert.AreEqual(symbolic.Name + "[true := 8]", mapStore.Args[0].ToString());

                        Assert.IsInstanceOf<LiteralExpr>(mapStore.Args[1]);
                        Assert.IsTrue((mapStore.Args[1] as LiteralExpr).IsFalse);

                        Assert.IsInstanceOf<LiteralExpr>(mapStore.Args[2]);
                        Assert.IsTrue((mapStore.Args[2] as LiteralExpr).isBigNum);
                        Assert.AreEqual(7, (mapStore.Args[2] as LiteralExpr).asBigNum.ToInt);
                        break;
                    default:
                        Assert.Fail("unrecognised breakpoint");
                        break;
                }
                ++count;
            };
            e.Run(GetMain(p));
            Assert.AreEqual(3, count);
        }

        [Test()]
        public void ParallelConcreteAssignment()
        {
            p = LoadProgramFrom(@"
                procedure main()
                {
                    var x:int;
                    var y:int;
                    assert {:symbooglix_bp ""before""} true;
                    x, y := 5, 6;
                    assert {:symbooglix_bp ""after""} true;
                }
            ", "file.bpl");
            e = GetExecutor(p, new DFSStateScheduler(), GetSolver());

            int count = 0;
            e.BreakPointReached += delegate(object sender, Executor.BreakPointEventArgs eventArgs)
            {
                switch (eventArgs.Name)
                {
                    case "before":
                        var vAndExprForx = e.CurrentState.GetInScopeVariableAndExprByName("x");
                        Assert.IsInstanceOf<IdentifierExpr>(vAndExprForx.Value);
                        Assert.IsInstanceOf<SymbolicVariable>((vAndExprForx.Value as IdentifierExpr).Decl);

                        var vAndExprFory = e.CurrentState.GetInScopeVariableAndExprByName("y");
                        Assert.IsInstanceOf<IdentifierExpr>(vAndExprFory.Value);
                        Assert.IsInstanceOf<SymbolicVariable>((vAndExprFory.Value as IdentifierExpr).Decl);
                        break;
                    case "after":
                        vAndExprForx = e.CurrentState.GetInScopeVariableAndExprByName("x");
                        Assert.IsInstanceOf<LiteralExpr>(vAndExprForx.Value);
                        var literal = vAndExprForx.Value as LiteralExpr;
                        Assert.IsTrue(literal.isBigNum);
                        Assert.AreEqual(5, literal.asBigNum.ToInt);

                        vAndExprFory = e.CurrentState.GetInScopeVariableAndExprByName("y");
                        Assert.IsInstanceOf<LiteralExpr>(vAndExprFory.Value);
                        literal = vAndExprFory.Value as LiteralExpr;
                        Assert.IsTrue(literal.isBigNum);
                        Assert.AreEqual(6, literal.asBigNum.ToInt);
                        break;
                    default:
                        Assert.Fail("unrecognised breakpoint");
                        break;
                }
                ++count;
            };
            e.Run(GetMain(p));
            Assert.AreEqual(2, count);
        }

        [Test()]
        public void ParallelSymbolicAssignment()
        {
            p = LoadProgramFrom(@"
                procedure main()
                {
                    var x:int;
                    var y:int;
                    assert {:symbooglix_bp ""before""} true;
                    x, y := y, x;
                    assert {:symbooglix_bp ""after""} true;
                }
            ", "file.bpl");
            e = GetExecutor(p, new DFSStateScheduler(), GetSolver());

            int count = 0;
            IdentifierExpr symbolicForx = null;
            IdentifierExpr symbolicFory = null;
            e.BreakPointReached += delegate(object sender, Executor.BreakPointEventArgs eventArgs)
            {
                switch (eventArgs.Name)
                {
                    case "before":
                        {
                            var vAndExprForx = e.CurrentState.GetInScopeVariableAndExprByName("x");
                            Assert.IsInstanceOf<IdentifierExpr>(vAndExprForx.Value);
                            Assert.IsInstanceOf<SymbolicVariable>((vAndExprForx.Value as IdentifierExpr).Decl);
                            symbolicForx = vAndExprForx.Value as IdentifierExpr;

                            var vAndExprFory = e.CurrentState.GetInScopeVariableAndExprByName("y");
                            Assert.IsInstanceOf<IdentifierExpr>(vAndExprFory.Value);
                            Assert.IsInstanceOf<SymbolicVariable>((vAndExprFory.Value as IdentifierExpr).Decl);
                            symbolicFory = vAndExprFory.Value as IdentifierExpr;
                        }
                        break;
                    case "after":
                        {
                            Assert.IsNotNull(symbolicForx);
                            Assert.IsNotNull(symbolicFory);

                            // Check that swapped happened
                            var vAndExprForNewx = e.CurrentState.GetInScopeVariableAndExprByName("x");
                            Assert.AreSame(symbolicFory, vAndExprForNewx.Value);

                            var vAndExprForNewy = e.CurrentState.GetInScopeVariableAndExprByName("y");
                            Assert.AreSame(symbolicForx, vAndExprForNewy.Value);
                        }
                        break;
                    default:
                        Assert.Fail("unrecognised breakpoint");
                        break;
                }
                ++count;
            };
            e.Run(GetMain(p));
            Assert.AreEqual(2, count);
        }
    }
}
