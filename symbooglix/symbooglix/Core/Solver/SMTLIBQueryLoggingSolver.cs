using System;
using System.Collections.Generic;
using Microsoft.Boogie;
using System.IO;

namespace symbooglix
{
    namespace Solver
    {
        // FIXME: We need to refactor this so it can reused to builder an actual solver
        public class SMTLIBQueryLoggingSolver : ISolver
        {
            private ISolver UnderlyingSolver;
            private SMTLIBQueryPrinter Printer;
            private int useCounter=0;
            private ConstraintManager currentConstraints = null;
            public SMTLIBQueryLoggingSolver(ISolver UnderlyingSolver, TextWriter TW, bool humanReadable)
            {
                this.UnderlyingSolver = UnderlyingSolver;
                Printer = new SMTLIBQueryPrinter(TW, humanReadable);
            }

            public void SetConstraints(ConstraintManager cm)
            {
                if (useCounter > 0)
                {
                    Printer.printExit();
                    Printer.clearDeclarations();
                    Printer.printCommentLine("End of Query " + (useCounter -1));
                }
                Printer.printCommentLine("Query " + useCounter + " Begin");

                // Let the printer find the declarations
                currentConstraints = cm;
                foreach (var constraint in cm.constraints)
                {
                    Printer.addDeclarations(constraint);
                }
                UnderlyingSolver.SetConstraints(cm);
            }

            private void printDeclarationsAndConstraints()
            {
                Printer.printCommentLine("Variable declarations");
                Printer.printVariableDeclarations();
                Printer.printCommentLine("Function declarations");
                Printer.printFunctionDeclarations();
                Printer.printCommentLine("Constraints");
                foreach (var constraint in currentConstraints.constraints)
                {
                    Printer.printAssert(constraint);
                }
            }

            public Result IsQuerySat(Expr Query, out IAssignment assignment)
            {
                ++useCounter;
                throw new NotImplementedException();
            }

            public Result IsQuerySat(Expr Query)
            {
                ++useCounter;
                Printer.addDeclarations(Query);
                printDeclarationsAndConstraints();
                Printer.printCommentLine("IsQuerySat()");
                Printer.printAssert(Query);
                Printer.printCheckSat();
                return UnderlyingSolver.IsQuerySat(Query);
            }

            public Result IsNotQuerySat(Expr Query, out IAssignment assignment)
            {
                ++useCounter;
                throw new NotImplementedException();
            }

            public Result IsNotQuerySat(Expr Query)
            {
                ++useCounter;
                Printer.addDeclarations(Query);
                printDeclarationsAndConstraints();
                Printer.printCommentLine("IsNotQuerySat");
                Expr notExpr = Expr.Not(Query);
                Printer.printAssert(notExpr);
                Printer.printCheckSat();
                return UnderlyingSolver.IsQuerySat(notExpr);
            }
        }
    }
}

