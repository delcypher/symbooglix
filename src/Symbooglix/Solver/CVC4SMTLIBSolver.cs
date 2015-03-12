using System;
using Symbooglix.Solver;

namespace Symbooglix
{
    namespace Solver
    {
        public class CVC4SMTLIBSolver : SimpleSMTLIBSolver
        {
            SMTLIBQueryPrinter.Logic LogicToUse = SMTLIBQueryPrinter.Logic.ALL_SUPPORTED;  // Non standard
            public CVC4SMTLIBSolver(bool useNamedAttributes, string pathToSolver, bool persistentProcess) :
                base(useNamedAttributes, pathToSolver, "--lang smt2 --incremental", persistentProcess) // CVC4 specific command line flags
            {
            }

            // HACK:
            public CVC4SMTLIBSolver(bool useNamedAttributes, string pathToSolver, bool persistentProcess, SMTLIBQueryPrinter.Logic logic) :
                this(useNamedAttributes, pathToSolver, persistentProcess)
            {
                // We should not use DO_NOT_SET because CVC4 complains if no
                // logic is set which causes a SolverErrorException to be raised.
                if (logic != SMTLIBQueryPrinter.Logic.DO_NOT_SET)
                    LogicToUse = logic;
            }

            protected override void SetSolverOptions()
            {
                Printer.PrintSetLogic(LogicToUse);
            }
        }
    }
}

