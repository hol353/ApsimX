using System;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Return the value of a nominated internal numerical variable
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Returns the value of a nominated internal Plant numerical variable")]
    public class VariableReference : Model, IFunction
    {
        private string trimmedVariableName = "";
        private IVariable variable = null;

        /// <summary>The variable name</summary>
        [Description("Specify an internal variable")]
        public string VariableName
        {
            get
            {
                return trimmedVariableName;
            }
            set
            {
                trimmedVariableName = value.Trim();
                variable = null;
            }
        }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (variable == null)
            {
                try
                {
                    variable = Locator.GetObject(trimmedVariableName, LocatorFlags.ThrowOnError);
                }
                catch (Exception err)
                {
                    throw new Exception($"Error while locating variable '{VariableName}' in variable reference '{this.FullPath}'", err);
                }
            }

            object o = variable.Value;
            if (o is IFunction)
                return (o as IFunction).Value(arrayIndex);
            else if (o is Array)
                return Convert.ToDouble((o as Array).GetValue(arrayIndex),
                                        System.Globalization.CultureInfo.InvariantCulture);
            else
            {
                double doubleValue = Convert.ToDouble(o, System.Globalization.CultureInfo.InvariantCulture);
                if (double.IsNaN(doubleValue))
                    throw new Exception("NaN (not a number) found when getting variable: " + VariableName);
                return doubleValue;
            }
        }
    }
}