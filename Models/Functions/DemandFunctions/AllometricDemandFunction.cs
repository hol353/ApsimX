using System;
using Models.Core;

namespace Models.Functions.DemandFunctions
{
    /// <summary>This function calculated dry matter demand using plant allometry which is described using a simple power function (y=kX^p).</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AllometricDemandFunction : Model, IFunction
    {
        /// <summary>The constant</summary>
        [Description("Constant")]
        public double Const { get; set; }
        /// <summary>The power</summary>
        [Description("Power")]
        public double Power { get; set; }

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction XValue = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction YValue = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">
        /// Cannot find variable:  + XProperty +  in function:  + this.Name
        /// or
        /// Cannot find variable:  + YProperty +  in function:  + this.Name
        /// </exception>
        public double Value(int arrayIndex = -1)
        {
            double returnValue = 0.0;
            if (XValue.Value(arrayIndex) < 0)
                throw new Exception(this.Name + "'s XValue returned a negative which will cause the power function to return a Nan");
            double Target = Const * Math.Pow(XValue.Value(arrayIndex), Power);
            returnValue = Math.Max(0.0, Target - YValue.Value(arrayIndex));
            return returnValue;
        }
    }
}
