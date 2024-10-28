using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions.DemandFunctions
{
    /// <summary>Water demand is calculated using the Transpiration Efficiency (TE) approach (ie TE=Coefficient/VDP).</summary>
    [Serializable]
    public class TEWaterDemandFunction : Model, IFunction
    {
        /// <summary>Average Daily Vapour Pressure Deficit as a proportion of daily Maximum.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction SVPFrac = null;
        /// <summary>The met data</summary>
        [Link]
        public IWeather MetData = null;
        /// <summary>The photosynthesis</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Photosynthesis = null;
        /// <summary>Transpiration Efficiency Coefficient to relate TE to daily VPD</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction TranspirationEfficiencyCoefficient = null;

        /// <summary>Computes the vapour pressure deficit.</summary>
        /// <value>The vapour pressure deficit (hPa?)</value>
        public double VPD
        {
            get
            {
                double SVPmax = MetUtilities.svp(MetData.MaxT) * 0.1;
                double SVPmin = MetUtilities.svp(MetData.MinT) * 0.1;
                return Math.Max(SVPFrac.Value() * (SVPmax - SVPmin), 0.01);
            }
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return Photosynthesis.Value(arrayIndex) / (TranspirationEfficiencyCoefficient.Value(arrayIndex) / VPD / 0.001);
        }
    }
}


