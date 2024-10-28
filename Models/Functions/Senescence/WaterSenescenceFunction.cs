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
    ///Water Senescense
    /// </summary>
    [Serializable]
    [Description("Water Senescence")]
    public class WaterSenescenceFunction : Model, IFunction
    {
        [Link(Type = LinkType.Ancestor)]
        private SorghumLeaf leaf = null;

        /// <summary>The met data</summary>
        [Link]
        private IWeather metData = null;

        /// <summary>Delay factor for water senescence.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction senWaterTimeConst = null; //waterSenescence

        /// <summary>supply:demand ratio for onset of water senescence.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction senThreshold = null;  //waterSenescence

        /// <summary>SupplyDemand Ratio</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction SDRatio = null; //waterSenescence

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        virtual protected void OnPlantSowing(object sender, SowingParameters data)
        {
            totalLaiEquilibWater = 0.0;
            avLaiEquilibWater = 0.0;
            laiEquilibWaterQ = new Queue<double>();

            totalSDRatio = 0.0;
            avSDRatio = 0.0;
            sdRatioQ = new Queue<double>();

        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            totalLaiEquilibWater = 0.0;
            avLaiEquilibWater = 0.0;
            laiEquilibWaterQ?.Clear();
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            //watSupply is calculated in SorghumArbitrator:StoreWaterVariablesForNitrogenUptake
            //Arbitrator.WatSupply = Plant.Root.PlantAvailableWaterSupply();
            double dlt_dm_transp = leaf.potentialBiomassTEFunction.Value();

            //double radnCanopy = divide(plant->getRadnInt(), coverGreen, plant->today.radn);
            double effectiveRue = MathUtilities.Divide(leaf.photosynthesis.Value(), leaf.RadiationIntercepted, 0);

            double radnCanopy = MathUtilities.Divide(leaf.RadiationIntercepted, leaf.CoverGreen, metData.Radn);
            if (MathUtilities.FloatsAreEqual(leaf.CoverGreen, 0))
                radnCanopy = 0;

            double sen_radn_crit = MathUtilities.Divide(dlt_dm_transp, effectiveRue, radnCanopy);
            double intc_crit = MathUtilities.Divide(sen_radn_crit, radnCanopy, 1.0);
            if (MathUtilities.FloatsAreEqual(sen_radn_crit, 0))
                intc_crit = 0;

            //            ! needs rework for row spacing
            double laiEquilibWaterToday;
            if (intc_crit < 1.0)
                laiEquilibWaterToday = -Math.Log(1.0 - intc_crit) / leaf.extinctionCoefficientFunction.Value();
            else
                laiEquilibWaterToday = leaf.LAI;

            // calculate average of the last 10 days of laiEquilibWater`
            avLaiEquilibWater = UpdateAvLaiEquilibWater(laiEquilibWaterToday, 10);

            //// calculate a 5 day moving average of the supply demand ratio
            avSDRatio = UpdateAvSDRatio(SDRatio.Value(), 5);

            double dltSlaiWater = 0.0;

            if (avSDRatio < senThreshold.Value())
                dltSlaiWater = Math.Max(0.0, MathUtilities.Divide((leaf.LAI - avLaiEquilibWater), senWaterTimeConst.Value(), 0.0));
            dltSlaiWater = Math.Min(leaf.LAI, dltSlaiWater);
            return dltSlaiWater;
        }

        private double totalLaiEquilibWater;
        private double avLaiEquilibWater;
        private Queue<double> laiEquilibWaterQ;
        private double UpdateAvLaiEquilibWater(double valToday, int days)
        {
            totalLaiEquilibWater += valToday;
            laiEquilibWaterQ.Enqueue(valToday);
            if (laiEquilibWaterQ.Count > days)
            {
                totalLaiEquilibWater -= laiEquilibWaterQ.Dequeue();
            }
            return MathUtilities.Divide(totalLaiEquilibWater, laiEquilibWaterQ.Count, 0);
        }

        private double totalSDRatio;
        private double avSDRatio;
        private Queue<double> sdRatioQ;
        private double UpdateAvSDRatio(double valToday, int days)
        {
            totalSDRatio += valToday;
            sdRatioQ.Enqueue(valToday);
            if (sdRatioQ.Count > days)
            {
                totalSDRatio -= sdRatioQ.Dequeue();
            }
            return MathUtilities.Divide(totalSDRatio, sdRatioQ.Count, 0);
        }
    }
}

