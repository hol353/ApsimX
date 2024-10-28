using System;
using System.Collections.Generic;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// A linear interpolation model, where an 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("A Y value is returned for the current vaule of the XValue child via linear interpolation of the XY pairs specified")]
    public class LinearInterpolationFunction : Model, IFunction
    {
        /// <summary>The ys are all the same</summary>
        private bool YsAreAllTheSame = false;

        /// <summary>Gets the xy pairs.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private XYPairs XYPairs = null;

        private Dictionary<double, double> cache = new Dictionary<double, double>();

        /// <summary>The x value to use for interpolation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction XValue = null;

        /// <summary>Constructor</summary>
        public LinearInterpolationFunction() { }

        /// <summary>Constructor</summary>
        public LinearInterpolationFunction(double[] x, double[] y)
        {
            XYPairs = new XYPairs() { X = x, Y = y };
        }

        /// <summary>Return the name of the x variable. Used as graph x axis title.</summary>
        public string XVariableName
        {
            get
            {
                if (XValue == null)
                    return string.Empty;
                else if (XValue is VariableReference)
                    return (XValue as VariableReference).VariableName;
                else
                    return "XValue";
            }
        }

        /// <summary>Constructor</summary>
        /// <param name="xProperty">name of the x property</param>
        /// <param name="x">x values.</param>
        /// <param name="y">y values.</param>
        public LinearInterpolationFunction(string xProperty, double[] x, double[] y)
        {
            VariableReference XValue = new VariableReference();
            XValue.VariableName = xProperty;
            XYPairs = new XYPairs();
            XYPairs.X = x;
            XYPairs.Y = y;
        }

        /// <summary>Called when model has been created.</summary>
        public override void OnCreated()
        {
            base.OnCreated();
            if (XYPairs != null)
            {
                for (int i = 1; i < XYPairs.Y.Length; i++)
                    if (XYPairs.Y[i] != XYPairs.Y[i - 1])
                    {
                        YsAreAllTheSame = false;
                        return;
                    }

                // If we get this far then the Y values must all be the same.
                YsAreAllTheSame = XYPairs.Y.Length > 0;
            }
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Cannot find value for  + Name +  XProperty:  + XProperty</exception>
        public double Value(int arrayIndex = -1)
        {
            // Shortcut exit when the Y values are all the same. Runs quicker.
            try
            {
                if (YsAreAllTheSame)
                    return XYPairs.Y[0];
                else
                    return XYPairs.ValueIndexed(XValue.Value(arrayIndex));
            }
            catch (Exception err)
            {
                throw new Exception($"Unable to evaluate linear interpolation function {FullPath}", err);
            }
        }

        /// <summary>Values for x.</summary>
        /// <param name="XValue">The x value.</param>
        /// <returns></returns>
        public double ValueForX(double XValue)
        {
            return XYPairs.ValueIndexed(XValue);
        }
    }
}
