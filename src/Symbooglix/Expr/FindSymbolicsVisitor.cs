using System;
using System.Collections.Generic;
using Microsoft.Boogie;
using System.Linq;
using System.Diagnostics;

namespace Symbooglix
{
    public class FindSymbolicsVisitor : StandardVisitor
    {
        public ICollection<SymbolicVariable> symbolics;

        // Internal container
        public FindSymbolicsVisitor()
        {
            symbolics = new List<SymbolicVariable>();
        }

        // Use external container
        public FindSymbolicsVisitor(ICollection<SymbolicVariable> externalContainer)
        {
            symbolics = externalContainer;
        }

        public override Expr VisitIdentifierExpr(IdentifierExpr node)
        {
            Debug.Assert(node != null);

            if (node.Decl is SymbolicVariable)
            {
                Debug.Assert(node == ( node.Decl as SymbolicVariable ).Expr, "Different instances for IdentiferExpr");
                symbolics.Add(node.Decl as SymbolicVariable);
            }
            return node;
        }
    }
}
