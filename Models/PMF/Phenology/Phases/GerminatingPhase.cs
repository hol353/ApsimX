using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.Soils;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from a start stage to an end stage and assumes
    /// germination will be reached on the day after sowing or the first day
    /// thereafter when the extractable soil water at sowing depth is greater than zero."
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class GerminatingPhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private IPhysical soilPhysical = null;

        /// <summary>Link to the soil water balance.</summary>
        [Link]
        private ISoilWater waterBalance = null;

        [Link]
        private Plant plant = null;

        [Link]
        private Phenology phenology = null;

        [Link]
        private IClock clock = null;

        [Link]
        private ISoilTemperature soilTemperature = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction minSoilTemperature = null;

        // 2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The soil layer in which the seed is sown.</summary>
        private int SowLayer = 0;


        // 3. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler SeedImbibed;

        /// <summary>The phenological stage at the start of this phase.</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The phenological stage at the end of this phase.</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Is the phase emerged from the ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = false;

        /// <summary>Fraction of phase that is complete (0-1).</summary>
        [JsonIgnore]
        public double FractionComplete { get { return 0; } }

        /// <summary>
        /// Date for germination to occur.  null by default so model is used
        /// </summary>
        [JsonIgnore]
        public string GerminationDate { get; set; }

        // 4. Public method
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Compute the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
            double sowLayerTemperature = soilTemperature.Value[SowLayer];

            if (GerminationDate != null)
            {
                if (DateUtilities.DayMonthIsEqual(GerminationDate, clock.Today))
                {
                    doGermination(ref proceedToNextPhase, ref propOfDayToUse);
                }
            }

            else if (!phenology.OnStartDayOf("Sowing") && waterBalance.SWmm[SowLayer] > soilPhysical.LL15mm[SowLayer] && sowLayerTemperature >= minSoilTemperature.Value())
            {
                doGermination(ref proceedToNextPhase, ref propOfDayToUse);
            }

            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase() { GerminationDate = null; }

        // 5. Private methods
        //-----------------------------------------------------------------------------------------------------------------

        private void doGermination(ref bool proceedToNextPhase, ref double propOfDayToUse)
        {
            if (SeedImbibed != null)
                SeedImbibed.Invoke(this, new EventArgs());
            proceedToNextPhase = true;
            propOfDayToUse = 1;
        }

        /// <summary>Called when crop is ending.</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            SowLayer = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, plant.SowingData.Depth);
        }

    }
}