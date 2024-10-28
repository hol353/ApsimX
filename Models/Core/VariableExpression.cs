using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Functions;

namespace Models.Core
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class VariableExpression : IVariable
    {
        /// <summary>
        /// The expression string.
        /// </summary>
        private string expression;

        /// <summary>
        /// An instance of the expression evaluator once the expression has been parsed.
        /// </summary>
        private ExpressionEvaluator fn = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableExpression" /> class.
        /// </summary>
        /// <param name="expression">The string expression</param>
        /// <param name="model">The model</param>
        public VariableExpression(string expression, Model model)
        {
            this.expression = expression;
            this.Object = model;

            // Perform initial parsing.
            this.fn = new ExpressionEvaluator();
            this.fn.Parse(this.expression.Trim());
            this.fn.Infix2Postfix();
        }

        /// <summary>
        /// A reference to the variables class so that getting of variable values is possible.
        /// </summary>
        public override Object Object { get; set; }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public override string Name
        {
            get
            {
                return expression;
            }
        }

        /// <summary>
        /// Gets or sets the value of the property.
        /// </summary>
        public override object Value
        {
            get
            {
                this.FillVariableNames();
                fn.EvaluatePostfix();
                if (fn.Error)
                {
                    throw new Exception(fn.ErrorDescription);
                }
                if (fn.Results != null)
                {
                    return fn.Results;
                }
                else
                {
                    return fn.Result;
                }
            }

            set
            {
                throw new Exception("Cannot set the value of an expression: " + expression);
            }
        }

        /// <summary>
        /// Gets a description of the property or null if not found.
        /// </summary>
        public override string Description
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the text to use as a label for the property.
        /// </summary>
        public override string Caption
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the units of the property (in brackets) or null if not found.
        /// </summary>
        public override string Units
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        /// <summary>
        /// Gets the units of the property as formmatted for display (in parentheses) or null if not found.
        /// </summary>
        public override string UnitsLabel
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Fill the function variables with names.
        /// </summary>
        private void FillVariableNames()
        {
            List<Symbol> variablesToFill = fn.Variables;
            for (int i = 0; i < variablesToFill.Count; i++)
            {
                Symbol sym = (Symbol)variablesToFill[i];
                sym.m_values = null;
                sym.m_value = 0;
                IVariable sometypeofobject = (Object as Model).Locator.GetObjectProperties(sym.m_name.Trim(), LocatorFlags.IncludeReportVars | LocatorFlags.ThrowOnError);
                if (sometypeofobject == null)
                    throw new Exception("Cannot find variable: " + sym.m_name + " while evaluating expression: " + expression);

                object objectValue = sometypeofobject.Value;
                if (objectValue == null)
                    throw new Exception("Variable " + sym.m_name + " evaluated to NULL in expression: " + expression);
                    
                if (objectValue is double)
                    sym.m_value = (double)objectValue;
                else if (objectValue is int)
                    sym.m_value = Convert.ToDouble(objectValue, System.Globalization.CultureInfo.InvariantCulture);
                else if (objectValue is double[])
                {
                    sym.m_values = (double[])objectValue;
                }
                else if (objectValue is double[][])
                {
                    double[][] allvalues = objectValue as double[][];
                    List<double> singleArrayOfValues = new List<double>();
                    foreach (double[] dimension in allvalues)
                        foreach (double value in dimension)
                            singleArrayOfValues.Add(value);
                    sym.m_values = (double[])singleArrayOfValues.ToArray();
                }
                else if (objectValue is IFunction fun)
                    sym.m_value = fun.Value();
                else
                {
                    try
                    {
                        sym.m_value = Convert.ToDouble(objectValue, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch (InvalidCastException)
                    {
                        throw new Exception($"Don't know how to use type {sometypeofobject.GetType()} of variable {sym.m_name} in an expression!");
                    }
                }
                variablesToFill[i] = sym;
            }
            fn.Variables = variablesToFill;
        }

        /// <summary>
        /// Gets the associated display type for the related property.
        /// </summary>
        public override DisplayAttribute Display { get { return null; } }

        /// <summary>
        /// Gets the data type of the property
        /// </summary>
        public override Type DataType
        {
            get
            {
                return Object.GetType();
            }
        }

        /// <summary>
        /// Gets or sets the value of the specified property with arrays converted to comma separated strings.
        /// </summary>
        public override object ValueWithArrayHandling
        {
            get
            {
                return Object;
            }
        }


        /// <summary>
        /// Returns true if the variable is writable
        /// </summary>
        public override bool Writable { get { return false; } }

        /// <summary>
        /// Return an attribute
        /// </summary>
        /// <param name="attributeType">Type of attribute to find</param>
        /// <returns>The attribute or null if not found</returns>
        public override Attribute GetAttribute(Type attributeType) { return null; }

        /// <summary>Return the summary comments from the source code.</summary>
        public override string Summary { get { return null; } }

        /// <summary>Return the remarks comments from the source code.</summary>
        public override string Remarks { get { return null; } }
    }
}
