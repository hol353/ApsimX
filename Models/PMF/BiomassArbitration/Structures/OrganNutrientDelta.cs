using System;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;
using Newtonsoft.Json;

namespace Models.PMF
{

    /// <summary>
    /// This is the basic organ class that contains biomass structures and transfers
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class OrganNutrientDelta : Model
    {

        ///1. Links
        ///------------------------------------------------------------------------------------------------

        /// <summary>The parent plant</summary>
        [Link(Type = LinkType.Ancestor)]
        private Organ organ = null;

        /// <summary>The demand of nutrients by the organ from the arbitrator</summary>
        [Link(Type = LinkType.Child)]
        [Units("g/m2/d")]
        private NutrientDemandFunctions demandFunctions = null;

        /// <summary>The supply of nutrients from the organ to the arbitrator</summary>
        [Units("g/m2")]
        [Link(Type = LinkType.Child)]
        private NutrientSupplyFunctions supplyFunctions = null;

        /// <summary>The concentrations of nutrients at cardinal thresholds</summary>
        [Units("g Nutrient/g dWt")]
        [Link(Type = LinkType.Child)]
        private IConcentratinOrFraction concentrationOrFractionFunction = null;

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Tolerance for biomass comparisons</summary>
        protected double BiomassToleranceValue = 0.0000000001;


        ///3. The Constructor
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Constructor</summary>
        public OrganNutrientDelta()
        {
            //demandFunctions = new NutrientDemandFunctions();
            //supplyFunctions = new NutrientSupplyFunctions();
            //thresholds = new NutrientConcentrationFunctions();
            Supplies = new OrganNutrientSupplies();
            SuppliesAllocated = new OrganNutrientSupplies();
            Demands = new NutrientPoolsState(0, 0, 0);
            PriorityScaledDemand = new NutrientPoolsState(0, 0, 0);
            DemandsAllocated = new NutrientPoolsState(0, 0, 0);
        }

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------

        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------
        /// <summary>The dry matter potentially being allocated</summary>

        /// <summary>The max, crit and min nutirent concentrations</summary>
        [JsonIgnore]
        public string OrganAndNutrientNames
        { get { return organ.Name + this.Name; } }

        /// <summary>The max, crit and min nutirent concentrations</summary>
        [JsonIgnore]
        public NutrientPoolsState ConcentrationOrFraction { get; set; }

        /// <summary> Resource supplied to arbitration by the organ</summary>
        /// [JsonIgnore]
        public OrganNutrientSupplies Supplies { get; set; }

        /// <summary> Resource supplied to arbitration by the organ that was allocated</summary>
        [JsonIgnore]
        public OrganNutrientSupplies SuppliesAllocated { get; set; }

        /// <summary> Resource demanded by the organ through arbitration</summary>
        [JsonIgnore]
        public NutrientPoolsState Demands { get; set; }

        /// <summary> demands scaled for priority</summary>
        [JsonIgnore]
        public NutrientPoolsState PriorityScaledDemand { get; set; }

        /// <summary> Resource demands met as a result of arbitration</summary>
        [JsonIgnore]
        public NutrientPoolsState DemandsAllocated { get; set; }

        /// <summary> demands as yet un met by arbitration</summary>
        [JsonIgnore]
        public NutrientPoolsState OutstandingDemands
        {
            get
            {
                NutrientPoolsState outstanding = new NutrientPoolsState(Demands - DemandsAllocated);
                return outstanding;
            }
        }

        /// <summary> The maximum possible biomass with Nutrient Allocation</summary>

        public double MaxCDelta { get; set; }


        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        private double ThrowIfNegative(IFunction function)
        {
            double retVal = function.Value();
            if (retVal < -0.0000000000001)
                throw new Exception((function as IModel).FullPath + " is returning a negative supply (" + retVal + ").  It must be >= 0");
            else
                return retVal;
        }

        private NutrientPoolsState ThrowIfNegative(NutrientPoolFunctions functions)
        {
            NutrientPoolsState returns = new NutrientPoolsState(
            ThrowIfNegative(functions.Structural),
            ThrowIfNegative(functions.Metabolic),
            ThrowIfNegative(functions.Storage));
            return returns;
        }

        /// <summary> set concentrationOrFraction property</summary>
        public void setConcentrationsOrProportions()
        {
            ConcentrationOrFraction = concentrationOrFractionFunction.ConcentrationsOrFractionss;
            if (this.Name == "Carbon")
                if ((ConcentrationOrFraction.Total > 1.01) || (ConcentrationOrFraction.Total < 0.99))
                    throw new Exception("Concentrations of Carbon in "+organ.Name+" must add to 1 to keep demands entire");
        }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        public void SetSuppliesAndDemands()
        {
            Clear();
            setConcentrationsOrProportions();
            Supplies.ReAllocation = ThrowIfNegative(supplyFunctions.ReAllocation);
            Supplies.ReTranslocation = ThrowIfNegative(supplyFunctions.ReTranslocation) * (1 - organ.SenescenceRate);
            Supplies.Fixation = ThrowIfNegative(supplyFunctions.Fixation);
            Supplies.Uptake = ThrowIfNegative(supplyFunctions.Uptake);

            Demands = new NutrientPoolsState(
            ThrowIfNegative(demandFunctions.Structural),
            ThrowIfNegative(demandFunctions.Metabolic),
            ThrowIfNegative(demandFunctions.Storage));
            PriorityScaledDemand = new NutrientPoolsState(
            demandFunctions.Structural.Value() * demandFunctions.QStructuralPriority.Value(),
            demandFunctions.Metabolic.Value() * demandFunctions.QMetabolicPriority.Value(),
            demandFunctions.Storage.Value() * demandFunctions.QStoragePriority.Value());
        }


        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            Supplies.Clear();
            SuppliesAllocated.Clear();
            Demands = new NutrientPoolsState(0, 0, 0);
            PriorityScaledDemand = new NutrientPoolsState(0, 0, 0);
            DemandsAllocated = new NutrientPoolsState(0, 0, 0);
        }

        /// <summary>Called when [Sowing] is broadcast</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Sowing")]
        protected void OnSowing(object sender, EventArgs e)
        {
            setConcentrationsOrProportions();
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            // Deltas = new OrganResourceStates();
            // Live = new ResourcePools();
            //  Dead = new ResourcePools();
            ConcentrationOrFraction = new NutrientPoolsState(0, 0, 0);
        }
    }

}
