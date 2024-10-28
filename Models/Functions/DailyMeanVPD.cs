using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>
    /// This Function calculates a mean daily VPD from Max and Min weighted toward Max according to the specified MaximumVPDWeight factor.  
    /// This is then passed into the XY matrix as the x property and the function returns the y value
    /// A value equal to 1.0 means it will use VPD at max temperature, a value of 0.5 means average VPD.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class DailyMeanVPD : Model, IFunction
    {
        #region Class Data Members

        /// <summary>The maximum temperature weighting</summary>
        [Description("The weight of 'VPD at daily maximum temperature' in daily mean VPD")]
        public double MaximumVPDWeight { get; set; } = 0.66;

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        #endregion

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double VPDmint = MetUtilities.svp((float)MetData.MinT) - MetData.VP;
            VPDmint = Math.Max(VPDmint, 0.0);

            double VPDmaxt = MetUtilities.svp((float)MetData.MaxT) - MetData.VP;
            VPDmaxt = Math.Max(VPDmaxt, 0.0);

            return MaximumVPDWeight * VPDmaxt + (1 - MaximumVPDWeight) * VPDmint;
        }
    }
}
