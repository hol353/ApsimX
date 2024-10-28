using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Functions
{

    /// <summary>
    /// A mathematical expression is evaluated using variables exposed within the Plant Modelling Framework.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ExpressionFunction : Model, IFunction
    {
        /// <summary>The expression.</summary>
        [Core.Description("Expression")]
        private string expression;

        /// <summary>The expression.</summary>
        [Core.Description("Expression")]
        public string Expression
        {
            get
            {
                return expression;
            }
            set
            {
                expression = value;
                parsed = false;
            }
        }

        /// <summary>The function</summary>
        private ExpressionEvaluator fn = new ExpressionEvaluator();
        /// <summary>The parsed</summary>
        private bool parsed = false;


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (!parsed)
            {
                Parse(fn, Expression);
                parsed = true;
            }
            FillVariableNames(fn, this, arrayIndex);
            Evaluate(fn);
            if (fn.Results != null && arrayIndex != -1)
                return fn.Results[arrayIndex];
            else
                return fn.Result;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ExpressionFunction()
        {
            Expression = string.Empty;
        }

        /// <summary>Parses the specified function.</summary>
        /// <param name="fn">The function.</param>
        /// <param name="ExpressionProperty">The expression property.</param>
        private static void Parse(ExpressionEvaluator fn, string ExpressionProperty)
        {
            fn.Parse(ExpressionProperty.Trim());
            fn.Infix2Postfix();
        }

        /// <summary>Fills the variable names.</summary>
        /// <param name="fn">The function.</param>
        /// <param name="RelativeTo">The relative to.</param>
        /// <param name="arrayIndex">The array index</param>
        /// <exception cref="System.Exception">Cannot find variable:  + sym.m_name +  in function:  + RelativeTo.Name</exception>
        private static void FillVariableNames(ExpressionEvaluator fn, Model RelativeTo, int arrayIndex)
        {
            List<Symbol> varUnfilled = fn.Variables;
            List<Symbol> varFilled = new List<Symbol>();
            Symbol symFilled;
            foreach (Symbol sym in varUnfilled)
            {
                symFilled.m_name = sym.m_name;
                symFilled.m_type = ExpressionType.Variable;
                symFilled.m_values = null;
                symFilled.m_value = 0;
                object sometypeofobject = RelativeTo.FindByPath(sym.m_name.Trim())?.Value;
                if (sometypeofobject == null)
                    throw new Exception("Cannot find variable: " + sym.m_name + " in function: " + RelativeTo.Name);
                if (sometypeofobject is Array)
                {
                    Array arr = sometypeofobject as Array;
                    symFilled.m_values = new double[arr.Length];
                    for (int i = 0; i < arr.Length; i++)
                    {
                        double val = Convert.ToDouble(arr.GetValue(i), System.Globalization.CultureInfo.InvariantCulture);
                        if (Double.IsNaN(val))
                            throw new Exception($"Variable[{i}]: {sym.m_name} in function: {RelativeTo.Name} is not defined or Not a Number");
                        else
                            symFilled.m_values[i] = val;
                    }
                }
                else if (sometypeofobject is IFunction)
                    symFilled.m_value = (sometypeofobject as IFunction).Value(arrayIndex);
                else
                    symFilled.m_value = Convert.ToDouble(sometypeofobject,
                                                         System.Globalization.CultureInfo.InvariantCulture);
                varFilled.Add(symFilled);
            }
            fn.Variables = varFilled;
        }

        /// <summary>Evaluates the specified function.</summary>
        /// <param name="fn">The function.</param>
        /// <exception cref="System.Exception"></exception>
        private static void Evaluate(ExpressionEvaluator fn)
        {
            fn.EvaluatePostfix();
            if (fn.Error)
            {
                throw new Exception(fn.ErrorDescription);
            }
        }

        /// <summary>
        /// Return the value of the specified property as an object. The PropertyName
        /// is relative to the RelativeTo argument (usually Plant).
        /// Format:
        /// [function(]VariableName[)]
        /// Where:
        /// function is optional and can be one of Sum, subtract, multiply, divide, max, min
        /// VariableName is the name of a Plant variable. It can also include an array. Array can
        /// have a filter inside of square brackets. Filter can be an array index (0 based)
        /// or be the name of a class or base class.
        /// e.g. Leaf.MinT
        /// sum(Leaf.Leaves[].Live.Wt)              - sums all live weights of all objects in leaves array.
        /// subtract(Leaf.Leaves[].Live.Wt)         - subtracts all live weights of all objects in leaves array.
        /// Leaf.Leaves[1].Live.Wt                  - returns the live weight of the 2nd element of the leaves array.
        /// sum(Leaf.Leaves[AboveGround].Live.Wt)   - returns the live weight of the 2nd element of the leaves array.
        /// </summary>
        /// <param name="Expression">The expression.</param>
        /// <param name="RelativeTo">The relative to.</param>
        /// <returns></returns>
        public static object Evaluate(string Expression, Model RelativeTo)
        {
            ExpressionEvaluator fn = new ExpressionEvaluator();
            Parse(fn, Expression);
            FillVariableNames(fn, RelativeTo, -1);
            Evaluate(fn);
            if (fn.Results != null)
                return fn.Results;
            else
                return fn.Result;
        }
    }
}
