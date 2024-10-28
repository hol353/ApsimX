using System;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Function to return the value for a function with a trigger and slope from it
    ///                /
    ///               /
    /// -------------/
    ///             x
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.LinearAfterThresholdView")]
    [PresenterName("UserInterface.Presenters.LinearAfterThresholdPresenter")]
    [Description("Use a linear function with a gradient after a trigger value is exceeded.")]
    public class LinearAfterThresholdFunction : Model, IFunction
    {
        /// <summary>The x property</summary>
        [Description("XProperty")]
        public string XProperty { get; set; }

        /// <summary>
        /// The trigger value on the X axis
        /// </summary>
        [Description("X value trigger")]
        public double XTrigger { get; set; }

        /// <summary>
        /// The slope or gradient of the linear series
        /// </summary>
        [Description("Gradient")]
        public double Slope { get; set; }

        /// <summary>Constructor</summary>
        public LinearAfterThresholdFunction() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xproperty"></param>
        public LinearAfterThresholdFunction(string xproperty)
        {
            XProperty = xproperty;
        }

        /// <summary>
        /// Get the value of the function
        /// </summary>
        /// <param name="arrayIndex"></param>
        /// <returns></returns>
        public double Value(int arrayIndex = -1)
        {
            object v = this.FindByPath(XProperty)?.Value;
            if (v == null)
                throw new Exception($"Cannot find value for {FullPath} XProperty: {XProperty}");
            double x;
            if (v is Array)
                x = (double)(v as Array).GetValue(arrayIndex);
            else if (v is IFunction)
                x = (v as IFunction).Value(arrayIndex);
            else
                x = (double)v;

            return ValueForX(x);
        }

        /// <summary>
        /// Gets the value of the function for a given value of the x property.
        /// </summary>
        /// <param name="x">An x-value.</param>
        public double ValueForX(double x)
        {
            if (x <= XTrigger)
                return 0;
            else
                return Math.Max(0.0, (x - XTrigger)) * Slope;
        }
    }
}
