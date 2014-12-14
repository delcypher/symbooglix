﻿using Microsoft.Boogie;
using NUnit.Framework;
using System;
using System.Linq;

namespace TransformTests
{
    [TestFixture()]
    public class GlobalDDE
    {
        [Test()]
        public void FuncUsedInImpl()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                // Foo is live so it should not be removed
                function foo(x:int) returns(bool);

                procedure main()
                {
                    assert foo(5);
                }
                ", "test.bpl");

            Assert.AreEqual(1, FunctionCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(1, FunctionCount(prog));

        }

        [Test()]
        public void FuncUsedInRequires()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                // Foo is live so it should not be removed
                function foo(x:int) returns(bool);

                procedure main()
                requires foo(5) == true;
                {
                    return;
                }
                ", "test.bpl");

            Assert.AreEqual(1, FunctionCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(1, FunctionCount(prog));

        }

        [Test()]
        public void FuncUsedInEnsures()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                // Foo is live so it should not be removed
                function foo(x:int) returns(bool);

                procedure main()
                ensures foo(5) == true; // This doesn't make sense but we only care about uses
                {
                    return;
                }
                ", "test.bpl");

            Assert.AreEqual(1, FunctionCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(1, FunctionCount(prog));

        }

        [Test()]
        public void FuncNotUsed()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                // Foo is dead so it should be removed
                function foo(x:int) returns(bool);

                procedure main()
                {
                    assert true;
                }
                ", "test.bpl");

            Assert.AreEqual(1, FunctionCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(0, FunctionCount(prog));

        }

        [Test()]
        public void InDirectFuncUseWithImplicitAxiom()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                // Foo is not dead because it's used by bar
                function foo(x:int) returns(bool);

                // This gets converted to an axiom during parsing so it is considered to be used
                function bar() returns (bool)
                {
                    foo(5)
                }

                // Baz is dead
                function baz() returns (bool)
                {
                    true
                }   

                procedure main()
                {
                    assert bar();
                }
                ", "test.bpl");

            Assert.AreEqual(3, FunctionCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(2, FunctionCount(prog));

        }

        [Test()]
        public void InDirectFuncUseWithoutImplicitAxiom()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                // Foo is not dead because it's used by bar
                function foo(x:int) returns(bool);

                // No axiom is used here
                function {:inline } bar() returns (bool)
                {
                    foo(5)
                }

                // Baz is dead
                function baz() returns (bool)
                {
                    true
                }   

                procedure main()
                {
                    assert bar();
                }
                ", "test.bpl");

            Assert.AreEqual(3, FunctionCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(2, FunctionCount(prog));

        }

        [Test()]
        public void FuncNotInCodeButUsedInDeadAxiom()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                // Foo is dead so it should be removed
                function foo(x:int) returns(bool);

                // Axiom is dead so it should be removed
                axiom foo(5) == true;


                procedure main()
                {
                    assert true;
                }
                ", "test.bpl");

            Assert.AreEqual(1, FunctionCount(prog));
            Assert.AreEqual(1, AxiomCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(0, FunctionCount(prog));
            Assert.AreEqual(0, AxiomCount(prog));

        }

        [Test()]
        public void DeadAxiom()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                // Axiom's that don't reference any globals variables
                // or functions are considered to be dead.
                // FIXME: This means ""axiom false;"" is considered to be dead
                // and will be removed. This might not be desirable
                axiom true;


                procedure main()
                {
                    assert true;
                }
                ", "test.bpl");

            Assert.AreEqual(1, AxiomCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(0, AxiomCount(prog));

        }

        [Test()]
        public void FuncNotInCodeButUsedInLiveAxiom()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                // Foo looks dead but we can't remove it
                // due to axiom
                function foo(x:int) returns(bool);
                
                const g:bool;

                // Axiom is not dead due to global variable use
                axiom (foo(5) == true) && g;

                procedure main()
                {
                    assert g;
                }
                ", "test.bpl");

            Assert.AreEqual(1, FunctionCount(prog));
            Assert.AreEqual(1, AxiomCount(prog));
            Assert.AreEqual(1, GlobalVariableCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(1, FunctionCount(prog));
            Assert.AreEqual(1, AxiomCount(prog));
            Assert.AreEqual(1, GlobalVariableCount(prog));
        }

        [Test()]
        public void GlobalVariableNotUsed()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                var g:bool;

                procedure main()
                {
                    return;
                }
                ", "test.bpl");

            Assert.AreEqual(1, GlobalVariableCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(0, GlobalVariableCount(prog));
        }

        [Test()]
        public void GlobalVariableUsedInImpl()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                var g:bool;

                procedure main()
                {
                    assert g;
                }
                ", "test.bpl");

            Assert.AreEqual(1, GlobalVariableCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(1, GlobalVariableCount(prog));
        }

        [Test()]
        public void GlobalVariableSomeAliveSomeDead()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                var g:bool;
                var d1:bool; // dead
                var d2:bool; // dead

                procedure main()
                {
                    assert g;
                }
                ", "test.bpl");

            Assert.AreEqual(3, GlobalVariableCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(1, GlobalVariableCount(prog));
        }

        [Test()]
        public void GlobalVariableUsedInProcModSet()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                var g:bool;

                procedure main();
                modifies g;

                ", "test.bpl");

            Assert.AreEqual(1, GlobalVariableCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(1, GlobalVariableCount(prog));
        }

        [Test()]
        public void GlobalVariableUsedInRequires()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                var g:bool;

                procedure main();
                requires g;

                ", "test.bpl");

            Assert.AreEqual(1, GlobalVariableCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(1, GlobalVariableCount(prog));
        }

        [Test()]
        public void GlobalVariableUsedInEnsures()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                var g:bool;

                procedure main();
                ensures g;

                ", "test.bpl");

            Assert.AreEqual(1, GlobalVariableCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(1, GlobalVariableCount(prog));
        }

        [Test()]
        public void DeadGlobalVariableUsedInAxiom()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                const g:bool;
                axiom g == true;

                procedure main();

                ", "test.bpl");

            Assert.AreEqual(1, GlobalVariableCount(prog));
            Assert.AreEqual(1, AxiomCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(0, GlobalVariableCount(prog));
            Assert.AreEqual(0, AxiomCount(prog));
        }

        [Test()]
        public void LiveGlobalVariableUsedInAxiom()
        {
            var prog = SymbooglixLibTests.SymbooglixTest.LoadProgramFrom(@"
                const g:bool;
                axiom g == true;

                procedure main()
                {
                    assert g;
                }

                ", "test.bpl");

            Assert.AreEqual(1, GlobalVariableCount(prog));
            Assert.AreEqual(1, AxiomCount(prog));
            RunGDDE(prog);
            Assert.AreEqual(1, GlobalVariableCount(prog));
            Assert.AreEqual(1, AxiomCount(prog));
        }

        public void RunGDDE(Program prog)
        {
            var GDDE = new Symbooglix.Transform.GlobalDeadDeclEliminationPass();
            var PM = new Symbooglix.Transform.PassManager();
            PM.Add(GDDE);
            PM.Run(prog);
        }

        public int FunctionCount(Program prog)
        {
            return prog.TopLevelDeclarations.OfType<Function>().Count();
        }

        public int AxiomCount(Program prog)
        {
            return prog.TopLevelDeclarations.OfType<Axiom>().Count();
        }

        public int GlobalVariableCount(Program prog)
        {
            return prog.TopLevelDeclarations.OfType<Variable>().Count();
        }
    }
}
