using System;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>
    /// This Function calculates a mean daily temperature from Max and Min weighted toward Max according to the specified MaximumTemperatureWeighting factor. 
    /// This is then passed into the XY matrix as the x property and the function returns the y value.
    /// A value equal to 1.0 means it will use max temperature, a value of 0.5 means average temperature.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class WeightedTemperatureFunction : Model, IFunction
    {
        #region Class Data Members
        /// <summary>Gets the xy pairs.</summary>
        /// <value>The xy pairs.</value>
        [Link(Type = LinkType.Child, ByName = true)]
        private XYPairs XYPairs = null;   // Temperature effect on Growth Interpolation Set

        /// <summary>The maximum temperature weighting</summary>
        [Description("MaximumTemperatureWeighting")]
        public double MaximumTemperatureWeighting { get; set; }

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        #endregion

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double Tav = MaximumTemperatureWeighting * MetData.MaxT + (1 - MaximumTemperatureWeighting) * MetData.MinT;
            return XYPairs.ValueIndexed(Tav);
        }
    }
}
