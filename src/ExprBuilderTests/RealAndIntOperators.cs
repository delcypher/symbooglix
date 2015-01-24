﻿using System;
using NUnit.Framework;
using Microsoft.Boogie;
using Symbooglix;

namespace ExprBuilderTests
{
    [TestFixture()]
    public class RealAndIntOperators : IErrorSink
    {
        public RealAndIntOperators()
        {
            // This is a hack
            SymbooglixLibTests.SymbooglixTest.SetupCmdLineParser();
            SymbooglixLibTests.SymbooglixTest.SetupDebug();
        }

        private IExprBuilder GetBuilder()
        {
            return new SimpleExprBuilder();
        }

        public void Error(IToken tok, string msg)
        {
            Assert.Fail(msg);
        }

        private void CheckIsReal(Expr result)
        {
            Assert.IsTrue(result.ShallowType.IsReal);
            Assert.IsNotNull(result.Type);
            Assert.IsTrue(result.Type.IsReal);
            var TC = new TypecheckingContext(this);
            result.Typecheck(TC);
        }

        private void CheckIsInt(Expr result)
        {
            Assert.IsTrue(result.ShallowType.IsInt);
            Assert.IsNotNull(result.Type);
            Assert.IsTrue(result.Type.IsInt);
            var TC = new TypecheckingContext(this);
            result.Typecheck(TC);
        }

        // Add tests
        [Test()]
        public void AddReal()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantReal("1.0");
            var constant1 = builder.ConstantReal("2.0");
            var result = builder.Add(constant0, constant1);
            Assert.AreEqual("1e0 + 2e0", result.ToString());
            CheckIsReal(result);
        }

        [Test()]
        public void AddInt()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantInt(1);
            var constant1 = builder.ConstantInt(2);
            var result = builder.Add(constant0, constant1);
            Assert.AreEqual("1 + 2", result.ToString());
            CheckIsInt(result);
        }

        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void AddLhsWrongType()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantInt(1);
            var constant1 = builder.ConstantReal("2.0");
            builder.Add(constant0, constant1);
        }

        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void AddRhsWrongType()
        {
            var builder = GetBuilder();
            var constant1 = builder.ConstantInt(1);
            var constant0 = builder.ConstantReal("2.0");
            builder.Add(constant0, constant1);
        }

        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void AddLhsRhsWrongType()
        {
            var builder = GetBuilder();
            var constant1 = builder.True;
            var constant0 = builder.False;
            builder.Add(constant0, constant1);
        }

        // Sub tests
        [Test()]
        public void SubReal()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantReal("1.0");
            var constant1 = builder.ConstantReal("2.0");
            var result = builder.Sub(constant0, constant1);
            Assert.AreEqual("1e0 - 2e0", result.ToString());
            CheckIsReal(result);
        }

        [Test()]
        public void SubInt()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantInt(1);
            var constant1 = builder.ConstantInt(2);
            var result = builder.Sub(constant0, constant1);
            Assert.AreEqual("1 - 2", result.ToString());
            CheckIsInt(result);
        }

        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void SubLhsWrongType()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantInt(1);
            var constant1 = builder.ConstantReal("2.0");
            builder.Sub(constant0, constant1);
        }

        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void SubRhsWrongType()
        {
            var builder = GetBuilder();
            var constant1 = builder.ConstantInt(1);
            var constant0 = builder.ConstantReal("2.0");
            builder.Sub(constant0, constant1);
        }

        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void SubLhsRhsWrongType()
        {
            var builder = GetBuilder();
            var constant1 = builder.True;
            var constant0 = builder.False;
            builder.Sub(constant0, constant1);
        }

        // Mul tests
        [Test()]
        public void MulReal()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantReal("1.0");
            var constant1 = builder.ConstantReal("2.0");
            var result = builder.Mul(constant0, constant1);
            Assert.AreEqual("1e0 * 2e0", result.ToString());
            CheckIsReal(result);
        }

        [Test()]
        public void MulInt()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantInt(1);
            var constant1 = builder.ConstantInt(2);
            var result = builder.Mul(constant0, constant1);
            Assert.AreEqual("1 * 2", result.ToString());
            CheckIsInt(result);
        }

        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void MulLhsWrongType()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantInt(1);
            var constant1 = builder.ConstantReal("2.0");
            builder.Mul(constant0, constant1);
        }

        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void MulRhsWrongType()
        {
            var builder = GetBuilder();
            var constant1 = builder.ConstantInt(1);
            var constant0 = builder.ConstantReal("2.0");
            builder.Mul(constant0, constant1);
        }

        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void MulLhsRhsWrongType()
        {
            var builder = GetBuilder();
            var constant1 = builder.True;
            var constant0 = builder.False;
            builder.Mul(constant0, constant1);
        }

        // Div tests
        [Test()]
        public void DivInt()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantInt(1);
            var constant1 = builder.ConstantInt(2);
            var result = builder.Div(constant0, constant1);
            Assert.AreEqual("1 div 2", result.ToString());
            CheckIsInt(result);
        }

        // Note Div can't take real operands
        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void DivReal()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantReal("1.0");
            var constant1 = builder.ConstantReal("2.0");
            builder.Div(constant0, constant1);
        }

        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void DivMismatchArgTypes()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantReal("1.0");
            var constant1 = builder.False;
            builder.Div(constant0, constant1);
        }

        // RealDiv tests
        [Test()]
        public void RealDivIntArgs()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantInt(1);
            var constant1 = builder.ConstantInt(2);
            var result = builder.RealDiv(constant0, constant1);
            Assert.AreEqual("1 / 2", result.ToString());
            CheckIsReal(result);
        }

        [Test()]
        public void RealDivRealArgs()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantReal("1.0");
            var constant1 = builder.ConstantReal("2.0");
            var result = builder.RealDiv(constant0, constant1);
            Assert.AreEqual("1e0 / 2e0", result.ToString());
            CheckIsReal(result);
        }

        [Test()]
        public void RealDivMixedIntRealArgs()
        {
            var builder = GetBuilder();
            var constant1 = builder.ConstantReal("1.0");
            var constant0 = builder.ConstantInt(2);
            var result = builder.RealDiv(constant0, constant1);
            Assert.AreEqual("2 / 1e0", result.ToString());
            CheckIsReal(result);
        }

        [Test()]
        public void RealDivMixedRealIntArgs()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantReal("1.0");
            var constant1 = builder.ConstantInt(2);
            var result = builder.RealDiv(constant0, constant1);
            Assert.AreEqual("1e0 / 2", result.ToString());
            CheckIsReal(result);
        }

        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void RealDivWrongArgTypes()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantReal("1.0");
            var constant1 = builder.False;
            var result = builder.RealDiv(constant0, constant1);
            Assert.AreEqual("1e0 / 2e0", result.ToString());
            CheckIsReal(result);
        }

        // Mod tests
        [Test()]
        public void ModInt()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantInt(1);
            var constant1 = builder.ConstantInt(2);
            var result = builder.Mod(constant0, constant1);
            Assert.AreEqual("1 mod 2", result.ToString());
            CheckIsInt(result);
        }

        // Note Div can't take real operands
        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void ModReal()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantReal("1.0");
            var constant1 = builder.ConstantReal("2.0");
            builder.Mod(constant0, constant1);
        }

        [Test(), ExpectedException(typeof(ExprTypeCheckException))]
        public void ModMismatchArgTypes()
        {
            var builder = GetBuilder();
            var constant0 = builder.ConstantReal("1.0");
            var constant1 = builder.False;
            builder.Mod(constant0, constant1);
        }
    }
}

