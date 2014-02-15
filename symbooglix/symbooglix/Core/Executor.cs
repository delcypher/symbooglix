using System;
using Microsoft.Boogie;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace symbooglix
{
    public abstract class AExecutor
    {
        public AExecutor(Program prog) { this.prog = prog;} //FIXME: make copy so it possible to have multiple executors running in parallel

        public abstract bool prepare(); // Modify program to make it suitable for execution
        public abstract bool run(Implementation entryPoint);
        public abstract bool terminate();

        protected Program prog;
    }

    public class PrintingExecutor : AExecutor
    {
        public PrintingExecutor(Program prog, IStateScheduler scheduler) : base(prog) 
        { 
            stateScheduler = scheduler;
            symbolicPool = new SymbolicPool();
        }

        private IStateScheduler stateScheduler;
        private ExecutionState currentState;
        private ExecutionState initialState; // Represents a state that has not entered any procedures
        private SymbolicPool symbolicPool;
        private bool hasBeenPrepared = false;

        public override bool prepare()
        {
            // Create initial execution state
            initialState = new ExecutionState();

            // Load Globals
            var GVs = prog.TopLevelDeclarations.OfType<GlobalVariable>();
            foreach (GlobalVariable gv in GVs)
            {
                // Make symbolic initially
                // FIXME: We should probably check if globals are set to constant value first
                var s = symbolicPool.getFreshSymbolic(gv.TypedIdent);
                initialState.mem.globals.Add(gv, s.expr);
                initialState.symbolics.Add(s);
            }

            // FIXME: Load axioms

            // FIXME: Initialise constants from axioms

            hasBeenPrepared = true;
            return true;
        }

        public override bool run(Implementation entryPoint)
        {
            if (!hasBeenPrepared)
                prepare();

            // FIXME: Clone initialState so we can deal with execution at a different entry point later on
            currentState = initialState;

            stateScheduler.addState(currentState);
            
            // FIXME: Check entry point is in prog?


            // Push entry point onto stack frame
            enterProcedure(entryPoint);

            while (stateScheduler.getNumberOfStates() != 0)
            {
                currentState = stateScheduler.getNextState();
                currentState.getCurrentStackFrame().currentInstruction.MoveNext();
                executeInstruction();
            }
            System.Diagnostics.Debug.WriteLine("Finished executing all states");

            return true;
        }

        public override bool terminate()
        {
            //TODO
            return true;
        }

        private void executeInstruction()
        {
            Absy currentInstruction = currentState.getCurrentStackFrame().currentInstruction.Current;
            if (currentInstruction == null)
                throw new NullReferenceException("Instruction was null");

            if (currentInstruction is Cmd)
            {
                handleSimpleInstruction(currentInstruction as Cmd);
            } else if (currentInstruction is TransferCmd)
            {
                handleTransferCmd(currentInstruction as TransferCmd);
            } else
            {
                throw new NotSupportedException("Unsupported instruction");
            }
        }

        public void enterProcedure(Implementation p)
        {
            Debug.WriteLine("Entering procedure " + p.Name);

            // FIXME: The boundary between Executor and ExecutionState is
            // unclear, who should do the heavy lifting?
            currentState.enterProcedure(p);

            // Load procedure in parameters on to stack
            foreach(Variable v in p.InParams)
            {
                // FIXME: How do we know where these input parameters have come from
                // we do not need fresh symbolics if we've come from a call

                // Just make symbolic for now
                var s = symbolicPool.getFreshSymbolic(v.TypedIdent);
                currentState.getCurrentStackFrame().locals.Add(v, s.expr);
                currentState.symbolics.Add(s);
            }

            // Load procedure out parameters on to stack
            foreach(Variable v in p.OutParams)
            {
                // Make symbolic
                var s = symbolicPool.getFreshSymbolic(v.TypedIdent);
                currentState.getCurrentStackFrame().locals.Add(v, s.expr);
                currentState.symbolics.Add(s);
            }

            // Load procedure's declared locals on to stack
            foreach(Variable v in p.LocVars)
            {
                // Make symbolic
                var s = symbolicPool.getFreshSymbolic(v.TypedIdent);
                currentState.getCurrentStackFrame().locals.Add(v, s.expr);
                currentState.symbolics.Add(s);
            }
        }

        public void leaveProcedure()
        {
            Debug.WriteLine("Leaving Procedure " + currentState.getCurrentStackFrame().procedure.Name);
            currentState.leaveProcedure();

            if (currentState.finished())
                stateScheduler.removeState(currentState);
     
        }

        public void handleSimpleInstruction(Cmd si)
        {
            Debug.WriteLine("Exec: " + si.ToString().TrimEnd('\n'));
        }

        public void handleTransferCmd(TransferCmd ti)
        {
            Console.WriteLine("Exec: " + ti.ToString());

            // FIXME: Do the exit correctly
            leaveProcedure();

        }

    }
}

