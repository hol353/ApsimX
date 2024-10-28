using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Organs;
using Models.Soils;

namespace Models.Functions
{
    /// <summary>
    /// A simple scale to convert soil water content into a value between 0 and 2 where 0 = LL15, 1 = DUL and 2 = SAT
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("A simple scale to convert soil water content into a value between 0 and 2 where 0 = LL/LL15, 1 = DUL and 2 = SAT")]
    public class SoilWaterScale : Model, IFunction
    {
        /// <summary>Options for lower limit</summary>
        public enum LowerLimit
        {
            /// <summary>SoilCrop.LL</summary>
            LL,

            /// <summary>Physical.LL15</summary>
            LL15
        }

        /// <summary> </summary>
        [Description("Lower Limit: ")]
        [Display(Type = DisplayType.None)]
        public LowerLimit LLModel { get; set; } = LowerLimit.LL15;

        [Link]
        private ISoilWater soilwater = null;

        [Link]
        private Physical physical = null;

        private Root root = null;

        private double[] lls = null;

        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e) {
            root = FindAncestor<Root>();
            if (LLModel == LowerLimit.LL)
            {
                if (root == null)
                    throw new Exception($"SoilWaterScale ({Name}) cannot use LL for lower limit as it does not have a root as an ancestor.");
                else
                    lls = root.SoilCrop.LL;
            }
            else
            {
                lls = physical.LL15;
            }
        }

        /// <summary>Gets the value of the function.</summary>
        public double Value(int arrayIndex = -1)
        {
            //if (arrayIndex == -1) //Red and White Clovers both don't pass a number to this.
            //    throw new Exception($"Soil Water Scale ({Parent.Name}.{Name}) requires an array index for soil layer");

            if (arrayIndex == -1)
                return 1;

            if (arrayIndex >= soilwater.SW.Length)
                throw new Exception($"Soil Water Scale ({Parent.Name}.{Name}): ArrayIndex {arrayIndex} is more than SoilWater SW length {soilwater.SW.Length}.");

            if (arrayIndex >= physical.SAT.Length)
                throw new Exception($"Soil Water Scale ({Parent.Name}.{Name}): ArrayIndex {arrayIndex} is invalid index for SAT array length {physical.SAT.Length}.");

            if (arrayIndex >= physical.DUL.Length)
                throw new Exception($"Soil Water Scale ({Parent.Name}.{Name}): ArrayIndex {arrayIndex} is invalid index for DUL array length {physical.DUL.Length}.");

            if (arrayIndex >= lls.Length)
                throw new Exception($"Soil Water Scale ({Parent.Name}.{Name}): ArrayIndex {arrayIndex} is invalid index for LL array length {lls.Length}.");

            double sw = soilwater.SW[arrayIndex];
            double sat = physical.SAT[arrayIndex];
            double dul = physical.DUL[arrayIndex];
            double ll = lls[arrayIndex];

            double sws;
            if (sw >= sat)               // saturated - 2
                sws = 2;
            else if (sw >= dul)          // draining - 1 to 2
                sws = 1.0 + MathUtilities.Divide(sw - dul, sat - dul, 0.0);
            else if (sw > ll)            // unsaturated - 0 to 1
                sws = MathUtilities.Divide(sw - ll, dul - ll, 0.0);
            else                         // dry  - 0
                sws = 0.0;
            return sws;
        }
    }
}