using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Organs;

namespace Models.Functions
{
    /// <summary>
    /// Light Senescence controlled by:
    /// </summary>
    [Serializable]
    [Description("Calculate LightSenescence")]
    public class LightSenescenceFunction : Model, IFunction
    {
        [Link(Type = LinkType.Ancestor)]
        private SorghumLeaf Leaf = null;

        /// <summary>The met data</summary>
        [Link]
        private IWeather metData = null;

        /// <summary>Radiation level for onset of light senescence.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("Mj/m^2")]
        private IFunction senRadnCrit = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction senLightTimeConst = null;

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        virtual protected void OnPlantSowing(object sender, SowingParameters data)
        {
            totalLaiEqlbLight = 0;
            avgLaiEquilibLight = 0;
            laiEqlbLightTodayQ = new Queue<double>();
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            totalLaiEqlbLight = 0;
            avgLaiEquilibLight = 0;
            laiEqlbLightTodayQ?.Clear();
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            var senRadiationCrit = senRadnCrit.Value();
            double critTransmission = MathUtilities.Divide(senRadiationCrit, metData.Radn, 1);
            /* TODO : Direct translation - needs cleanup */
            //            ! needs rework for row spacing
            double laiEqlbLightToday;
            if (critTransmission > 0.0)
            {
                laiEqlbLightToday = -Math.Log(critTransmission) / Leaf.extinctionCoefficientFunction.Value();
            }
            else
            {
                laiEqlbLightToday = Leaf.LAI;
            }
            // average of the last 10 days of laiEquilibLight
            avgLaiEquilibLight = UpdateAvLaiEquilibLight(laiEqlbLightToday, 10);//senLightTimeConst?

            // dh - In old apsim, we had another variable frIntcRadn which is always set to 0.
            // Set Plant::radnInt(void) in Plant.cpp.
            double radnInt = metData.Radn * Leaf.CoverGreen;
            double radnTransmitted = metData.Radn - radnInt;
            double dltSlaiLight = 0.0;
            if (radnTransmitted < senRadiationCrit)
                dltSlaiLight = Math.Max(0.0, MathUtilities.Divide(Leaf.LAI - avgLaiEquilibLight, senLightTimeConst.Value(), 0.0));
            dltSlaiLight = Math.Min(dltSlaiLight, Leaf.LAI);
            return dltSlaiLight;
        }

        private double totalLaiEqlbLight;
        private double avgLaiEquilibLight;
        private Queue<double> laiEqlbLightTodayQ;
        private double UpdateAvLaiEquilibLight(double laiEqlbLightToday, int days)
        {
            totalLaiEqlbLight += laiEqlbLightToday;
            laiEqlbLightTodayQ.Enqueue(laiEqlbLightToday);
            if (laiEqlbLightTodayQ.Count > days)
            {
                totalLaiEqlbLight -= laiEqlbLightTodayQ.Dequeue();
            }
            return MathUtilities.Divide(totalLaiEqlbLight, laiEqlbLightTodayQ.Count, 0);
        }
    }
}

