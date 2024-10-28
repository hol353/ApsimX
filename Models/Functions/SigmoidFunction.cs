using System;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// This function is calcualted using a sigmoid function of the form y = Xmax * 1 / 1 + e^-(Xvalue - Xo) / b^.
    /// </summary>
    [Serializable]
    [Description("Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Ymax * 1/1+exp(-(XValue-Xo)/b)")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SigmoidFunction : Model, IFunction, IIndexedFunction
    {
        /// <summary>The ymax</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Ymax = null;
        /// <summary>The x value</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        IFunction XValue = null;
        /// <summary>The Xo</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Xo = null;
        /// <summary>The b</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction b = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Error with values to Sigmoid function</exception>
        public double Value(int arrayIndex = -1)
        {
            return ValueIndexed(XValue.Value(arrayIndex));
        }

        /// <summary>Values the indexed.</summary>
        /// <param name="dX">The d x.</param>
        /// <returns></returns>
        public double ValueIndexed(double dX)
        {
            try
            {
                return Ymax.Value(-1) * Function(dX, Xo.Value(-1), b.Value(-1));
            }
            catch (Exception)
            {
                throw new Exception("Error with values to Sigmoid function");
            }
        }


        /// <summary>
        /// General sigmoid function
        /// </summary>
        /// <param name="dX"></param>
        /// <param name="Xo"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public double Function(double dX, double Xo, double b)
        {
            return 1 / (1 + Math.Exp(-(dX - Xo) / b));
        }
    }
}
