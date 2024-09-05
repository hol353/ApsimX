using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;

namespace Models.Toy
{
    /// <summary>
    /// Calculates vapour pressure deficit
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    public class Toy1 : Model
    {
        [Link]
        private IWeather weather = null;

        /// <summary>
        /// Vapour pressure deficit
        /// </summary>
        [Description("Vapour pressure deficit")]
        [Units("hPa")]
        public double VPD { get; private set; }

        /// <summary>
        /// Initialise the model.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [EventSubscribe("StartOfSimulation")]
        public void OnStartOfSimulation(object sender, EventArgs args)
        {
            // no initialisation
        }

        /// <summary>
        /// Perform daily calculations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [EventSubscribe("StartOfDay")]
        public void OnStartOfDay(object sender, EventArgs args)
        {
            const double SVPfrac = 0.66;
            double VPDmint = MetUtilities.svp(weather.MinT) - weather.VP;
            VPDmint = Math.Max(VPDmint, 0.0);

            double VPDmaxt = MetUtilities.svp(weather.MaxT) - weather.VP;
            VPDmaxt = Math.Max(VPDmaxt, 0.0);

            VPD = SVPfrac * VPDmaxt + (1 - SVPfrac) * VPDmint;
        }
    }
}