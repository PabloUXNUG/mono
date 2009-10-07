//
// CSharpUnaryOperationBinder.cs
//
// Authors:
//	Marek Safar  <marek.safar@gmail.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Compiler = Mono.CSharp;

namespace Microsoft.CSharp.RuntimeBinder
{
	public class CSharpUnaryOperationBinder : UnaryOperationBinder
	{
		IList<CSharpArgumentInfo> argumentInfo;
		bool is_checked;
		
		public CSharpUnaryOperationBinder (ExpressionType operation, bool isChecked, IEnumerable<CSharpArgumentInfo> argumentInfo)
			: base (operation)
		{
			this.argumentInfo = argumentInfo.ToReadOnly ();
			if (this.argumentInfo.Count != 1)
				throw new ArgumentException ("Unary operation requires 1 argument");

			this.is_checked = isChecked;
		}
		
		public IList<CSharpArgumentInfo> ArgumentInfo {
			get {
				return argumentInfo;
			}
		}

		public bool IsChecked {
			get {
				return is_checked;
			}
		}

		public override bool Equals (object obj)
		{
			var other = obj as CSharpUnaryOperationBinder;
			return other != null && base.Equals (obj) && other.is_checked == is_checked &&
				other.argumentInfo.SequenceEqual (argumentInfo);
		}
		
		public override int GetHashCode ()
		{
			return Extensions.HashCode (
				Operation.GetHashCode (),
				is_checked.GetHashCode (),
				argumentInfo[0].GetHashCode ());
		}

		Compiler.Unary.Operator GetOperator ()
		{
			switch (Operation) {
			case ExpressionType.Negate:
				return Compiler.Unary.Operator.UnaryNegation;
			case ExpressionType.Not:
				return Compiler.Unary.Operator.LogicalNot;
			case ExpressionType.OnesComplement:
				return Compiler.Unary.Operator.OnesComplement;
			case ExpressionType.UnaryPlus:
				return Compiler.Unary.Operator.UnaryPlus;
			default:
				throw new NotImplementedException (Operation.ToString ());
			}
		}
		
		public override DynamicMetaObject FallbackUnaryOperation (DynamicMetaObject target, DynamicMetaObject errorSuggestion)
		{
			Compiler.Expression expr = CSharpBinder.CreateCompilerExpression (argumentInfo [0], target, true);

			if (Operation == ExpressionType.IsTrue) {
				expr = new Compiler.BooleanExpression (expr);
			} else {
				if (Operation == ExpressionType.Increment)
					expr = new Compiler.UnaryMutator (Compiler.UnaryMutator.Mode.PreIncrement, expr);
				else if (Operation == ExpressionType.Decrement)
					expr = new Compiler.UnaryMutator (Compiler.UnaryMutator.Mode.PreDecrement, expr);
				else
					expr = new Compiler.Unary (GetOperator (), expr);

				expr = new Compiler.Cast (new Compiler.TypeExpression (typeof (object), Compiler.Location.Null), expr);

				if (is_checked)
					expr = new Compiler.CheckedExpr (expr, Compiler.Location.Null);
			}

			var restrictions = CSharpBinder.CreateRestrictionsOnTarget (target);
			return CSharpBinder.Bind (target, expr, restrictions, errorSuggestion);
		}
	}
}
