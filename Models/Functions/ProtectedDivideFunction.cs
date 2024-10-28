using System;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Returns special values if
    /// the numerator is 0 or if the denominator is 0.
    /// </summary>
    /// <remarks>
    /// Currently used in sorghum/maize code to mimic divide functions
    /// in old apsim which return 10 if the denominator is 0 or 0 if
    /// the numerator is 0.
    /// </remarks>
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Serializable]
    public class ProtectedDivideFunction : Model, IFunction
    {
        /// <summary>Value to return if numerator is 0.</summary>
        [Description("Value to return if numerator is 0:")]
        public double NumeratorErrVal { get; set; }

        /// <summary>Value to return if denominator is 0.</summary>
        [Description("Value to return if denominator is 0:")]
        public double DenominatorErrVal { get; set; }

        /// <summary>
        /// Returns the value of the function.
        /// </summary>
        public double Value(int arrayIndex = -1)
        {
            IFunction[] children = this.FindAllChildren<IFunction>().ToArray();
            int n = children?.Length ?? 0;
            if (n < 2)
                throw new Exception($"Error in ProtectedDivideFunction {Name}: 2 child functions required, only found {n}");

            double numerator = children[0].Value(arrayIndex);
            double denominator = children[1].Value(arrayIndex);

            if (MathUtilities.FloatsAreEqual(numerator, 0))
                return NumeratorErrVal;

            if (MathUtilities.FloatsAreEqual(denominator, 0))
                return DenominatorErrVal;

            return numerator / denominator;
        }
    }
}
