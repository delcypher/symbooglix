﻿using NUnit.Framework;
using System;
using Symbooglix;
using Microsoft.Boogie;

namespace ConstantFoldingTests
{
    [TestFixture()]
    public class FoldBvsle : TestBase
    {
        [Test()]
        public void PositivePositiveTrue()
        {
            helper(5, 6, true);
        }

        [Test()]
        public void PositivePositiveTrueEqual()
        {
            helper(6, 6, true);
        }

        [Test()]
        public void PositivePositiveFalse()
        {
            helper(6, 5, false);
        }

        [Test()]
        public void PositiveNegativeFalse()
        {
            helper(6, -5, false);
        }

        [Test()]
        public void NegativePositiveTrue()
        {
            helper(-6, 5, true);
        }
            
        [Test()]
        public void NegativeNegativeTrue()
        {
            helper(-6, -5, true);
        }

        [Test()]
        public void NegativeNegativeTrueEqual()
        {
            helper(-6, -6, true);
        }

        [Test()]
        public void NegativeNegativeFalse()
        {
            helper(-5, -6, false);
        }



        private void helper(int value0, int value1, bool truth)
        {
            var x = builder.ConstantBV(value0, 4);
            var y = builder.ConstantBV(value1, 4);
            var bvslt = builder.BVSLE(x, y);

            var CFT = new ConstantFoldingTraverser();
            var result = CFT.Traverse(bvslt);

            Assert.IsInstanceOfType(typeof(LiteralExpr), result);
            Assert.IsTrue(( result as LiteralExpr ).isBool);
            Assert.IsTrue(( result as LiteralExpr ).asBool == truth);
        }
    }
}
