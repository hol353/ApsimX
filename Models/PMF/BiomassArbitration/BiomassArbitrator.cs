using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Newtonsoft.Json;

namespace Models.PMF
{
    ///<summary>
    /// The Arbitrator class determines the allocation of dry matter (DM) and Nitrogen between each of the organs in the crop model. Each organ can have up to three different pools of biomass:
    /// 
    /// * **Structural biomass** which is essential for growth and remains within the organ once it is allocated there.
    /// * **Metabolic biomass** which generally remains within an organ but is able to be re-allocated when the organ senesces and may be retranslocated when demand is high relative to supply.
    /// * **Storage biomass** which is partitioned to organs when supply is high relative to demand and is available for retranslocation to other organs whenever supply from uptake, fixation, or re-allocation is lower than demand.
    /// 
    /// The process followed for biomass arbitration is shown in Figure [FigureNumber]. Arbitration calculations are triggered by a series of events (shown below) that are raised every day.  For these calculations, at each step the Arbitrator exchange information with each organ, so the basic computations of demand and supply are done at the organ level, using their specific parameters. 
    /// 
    /// 1. **doPotentialPlantGrowth**.  When this event occurs, each organ class executes code to determine their potential growth, biomass supplies and demands.  In addition to demands for structural, non-structural and metabolic biomass (DM and N) each organ may have the following biomass supplies: 
    /// 	* **Fixation supply**.  From photosynthesis (DM) or symbiotic fixation (N)
    /// 	* **Uptake supply**.  Typically uptake of N from the soil by the roots but could also be uptake by other organs (eg foliage application of N).
    /// 	* **Retranslocation supply**.  Storage biomass that may be moved from organs to meet demands of other organs.
    /// 	* **Reallocation supply**. Biomass that can be moved from senescing organs to meet the demands of other organs.
    /// 2. **doPotentialPlantPartitioning.** On this event the Arbitrator first executes the DoDMSetup() method to gather the DM supplies and demands from each organ, these values are computed at the organ level.  It then executes the DoPotentialDMAllocation() method which works out how much biomass each organ would be allocated assuming N supply is not limiting and sends these allocations to the organs.  Each organ then uses their potential DM allocation to determine their N demand (how much N is needed to produce that much DM) and the arbitrator calls DoNSetup() to gather the N supplies and demands from each organ and begin N arbitration.  Firstly DoNReallocation() is called to redistribute N that the plant has available from senescing organs.  After this step any unmet N demand is considered as plant demand for N uptake from the soil (N Uptake Demand).
    /// 3. **doNutrientArbitration.** When this event occurs, the soil arbitrator gets the N uptake demands from each plant (where multiple plants are growing in competition) and their potential uptake from the soil and determines how much of their demand that the soil is able to provide.  This value is then passed back to each plant instance as their Nuptake and doNUptakeAllocation() is called to distribute this N between organs.  
    /// 4. **doActualPlantPartitioning.**  On this event the arbitrator call DoNRetranslocation() and DoNFixation() to satisfy any unmet N demands from these sources.  Finally, DoActualDMAllocation is called where DM allocations to each organ are reduced if the N allocation is insufficient to achieve the organs minimum N concentration and final allocations are sent to organs. 
    /// 
    /// ![Alt Text](ArbitratorSequenceDiagram.png)
    /// 
    /// **Figure [FigureNumber]:**  Schematic showing the procedure for arbitration of biomass partitioning.  Pink boxes represent events that occur every day and their numbering shows the order of calculations. Blue boxes represent the methods that are called when these events occur.  Orange boxes contain properties that make up the organ/arbitrator interface.  Green boxes are organ specific properties.
    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IPlant))]
    public class BiomassArbitrator : Model, ITotalCFixationSupply
    {
        ///1. Links
        ///------------------------------------------------------------------------------------------------

        /// <summary>The top level plant object in the Plant Modelling Framework</summary>
        [Link]
        private Plant plant = null;

        /// <summary>The zone.</summary>
        [Link(Type = LinkType.Ancestor)]
        protected IZone zone = null;

        [Link] 
        private Clock clock = null;

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------
        private double tolerence = 1e-12;
        private double StartN { get; set; }
        private double StartC { get; set; }

        ///3. The Constructor
        /// -------------------------------------------------------------------------------------------------
        BiomassArbitrator()
        {

        }

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------


        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------

        /// <summary>The list of organs</summary>
        [JsonIgnore]
        public List<Organ> PlantOrgans = new List<Organ>();

        /// <summary>The variables for DM</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public PlantNutrientsDelta Carbon { get; private set; }

        /// <summary>The variables for N</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public PlantNutrientsDelta Nitrogen { get; private set; }

        /// <summary> The amount of carbon fixed by photosynthesis</summary>
        public double TotalCFixationSupply => Carbon.TotalFixationSupply;

        /// <summary> The amount of carbon fixed by photosynthesis</summary>
        public double TotalCReTranslocationSupply => Carbon.TotalReTranslocationSupply;

        /// <summary> The amount of carbon fixed by photosynthesis</summary>
        public double TotalCReAllocationSupply => Carbon.TotalReAllocationSupply;

        /// <summary>Gets the n supply relative to N demand.</summary>
        /// <value>The n supply.</value>
        [JsonIgnore]
        public double FN
        {
            get
            {
                double fn = 1.0;
                if (Nitrogen != null)
                    fn = Math.Min(1, MathUtilities.Divide(Nitrogen.TotalPlantSupply, Nitrogen.TotalPlantDemand, 1));
                return fn;
            }
        }

        /// <summary>Gets the Amount of C not allocated</summary>
        /// <value>The n supply.</value>
        [JsonIgnore]
        [Units("gC/m2")]
        public double UnallocatedC { get; private set; }

        /// <summary>Gets the Amount of N not allocated</summary>
        /// <value>The n supply.</value>
        [JsonIgnore]
        [Units("gN/m2")]
        public double UnallocatedN { get; private set; }

        /// <summary>The amount of C not allocated because supply was insufficient to maintain minimum N conc</summary>
        /// <value>The n supply.</value>
        [JsonIgnore]
        [Units("gN/m2")]
        public double CUnallocatedDueToNLimitation { get; private set; }

        /// <summary>The amount of C not allocated because supply was insufficient to maintain minimum N conc</summary>
        /// <value>The n supply.</value>
        [JsonIgnore]
        [Units("gN/m2")]
        public double CFixationDownRegulationToNLimitation { get; private set; }

        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            PlantOrgans = plant.FindAllChildren<Organ>().ToList();
        }


        /// First get all demands and supplies, send potential DM allocations and do N reallocation so N uptake demand can be calculated

        /// <summary>Does the water limited dm allocations.  Water constaints to growth are accounted for in the calculation of DM supply
        /// and does initial N calculations to work out how much N uptake is required to pass to SoilArbitrator</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantPartioning")]
        virtual protected void OnDoPotentialPlantPartioning(object sender, EventArgs e)
        {
            if (plant.IsEmerged)
            {
                // Calculate potential DM allocaiton without nutrient limitation

                double CTotalReAllocationAllocated = DoAllocation(Carbon.TotalReAllocationSupply, Carbon);
                if (Math.Abs(Carbon.TotalReAllocationSupply- CTotalReAllocationAllocated) < -tolerence)
                    throw new Exception("Error in reallocatoin of carbon");
                foreach (OrganNutrientDelta o in Carbon.ArbitratingOrgans)
                    if (o.Supplies.ReAllocation.Total > 0)
                    {
                        o.SuppliesAllocated.ReAllocation = new NutrientPoolsState(
                            0.0,
                            calcAllocated(CTotalReAllocationAllocated, o.Supplies.ReAllocation.Metabolic, Carbon.TotalReAllocationSupply),
                            calcAllocated(CTotalReAllocationAllocated, o.Supplies.ReAllocation.Storage, Carbon.TotalReAllocationSupply));
                    }

                double CTotalFixationAllocated = DoAllocation(Carbon.TotalFixationSupply, Carbon);
                if (Math.Abs(CTotalFixationAllocated - Carbon.TotalFixationSupply)<-tolerence)
                    throw new Exception("Error in allocatoin of fixed carbon");
                foreach (OrganNutrientDelta o in Carbon.ArbitratingOrgans)
                    if (o.Supplies.Fixation > 0)
                    {
                        o.SuppliesAllocated.Fixation = calcAllocated(CTotalFixationAllocated, o.Supplies.Fixation, Carbon.TotalFixationSupply);
                    }

                double CTotalReTranslocationAllocated = DoAllocation(Carbon.TotalReTranslocationSupply, Carbon);
                if (Math.Abs(CTotalReTranslocationAllocated - Carbon.TotalReTranslocationSupply)<-tolerence)
                    throw new Exception("Error in reTranslocatoin of carbon");
                foreach (OrganNutrientDelta o in Carbon.ArbitratingOrgans)
                    if (o.Supplies.ReTranslocation.Total > 0)
                    {
                        o.SuppliesAllocated.ReTranslocation = new NutrientPoolsState(
                            0,
                            calcAllocated(CTotalReTranslocationAllocated, o.Supplies.ReTranslocation.Metabolic, Carbon.TotalReTranslocationSupply),
                            calcAllocated(CTotalReTranslocationAllocated, o.Supplies.ReTranslocation.Storage, Carbon.TotalReTranslocationSupply));
                    }

                foreach (Organ o in PlantOrgans)
                    o.Nitrogen.SetSuppliesAndDemands();

                // Calculate N Reallocation
                NTotalReAlocationAllocated = DoAllocation(Nitrogen.TotalReAllocationSupply, Nitrogen);
                foreach (OrganNutrientDelta o in Nitrogen.ArbitratingOrgans)
                    if (o.Supplies.ReAllocation.Total > 0)
                    {
                        o.SuppliesAllocated.ReAllocation = new NutrientPoolsState(
                            0,
                            calcAllocated(NTotalReAlocationAllocated, o.Supplies.ReAllocation.Metabolic, Nitrogen.TotalReAllocationSupply),
                            calcAllocated(NTotalReAlocationAllocated, o.Supplies.ReAllocation.Storage, Nitrogen.TotalReAllocationSupply));

                    }
            }
        }

        double NTotalReAlocationAllocated = 0;
        /// <summary>Takes N Allocation from Soil arbitration and partitions it within the plant</summary>
        public void AllocateNUptake(double TotalPlantUptake)
        {
            double NTotalUptakeAllocated = DoAllocation(TotalPlantUptake, Nitrogen);

            double check = NTotalUptakeAllocated - (Nitrogen.TotalPlantDemand - NTotalReAlocationAllocated);
            if (check > tolerence)
                throw new Exception("NUptake exceeds demand");
            //Let uptake organs know what theire uptake
            int count = 0;
            foreach (Organ o in PlantOrgans)
            {
                if (o.WaterNitrogenUptakeObject != null)
                {
                    if (count > 0)
                        throw new Exception("Two organs have IWaterNitrogenUptake");
                    o.Nitrogen.SuppliesAllocated.Uptake = TotalPlantUptake / zone.Area;
                    count += 1;
                }
            }
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantPartioning")]
        virtual protected void OnDoActualPlantPartioning(object sender, EventArgs e)
        {
            if (plant.IsEmerged)
            {
                double NTotalFixationAllocated = DoAllocation(Nitrogen.TotalFixationSupply, Nitrogen);
                foreach (OrganNutrientDelta o in Nitrogen.ArbitratingOrgans)
                    if (o.Supplies.Fixation > 0)
                    {
                        o.SuppliesAllocated.Fixation = calcAllocated(NTotalFixationAllocated, o.Supplies.Fixation, Nitrogen.TotalFixationSupply);
                    }

                double NTotalReTranslocationAllocated = DoAllocation(Nitrogen.TotalReTranslocationSupply, Nitrogen);
                foreach (OrganNutrientDelta o in Nitrogen.ArbitratingOrgans)
                    if (o.Supplies.ReTranslocation.Total > 0)
                    {
                        o.SuppliesAllocated.ReTranslocation = new NutrientPoolsState(
                            0,
                            calcAllocated(NTotalReTranslocationAllocated, o.Supplies.ReTranslocation.Metabolic, Nitrogen.TotalReTranslocationSupply),
                            calcAllocated(NTotalReTranslocationAllocated, o.Supplies.ReTranslocation.Storage, Nitrogen.TotalReTranslocationSupply));

                    }

                NutrientConstrainedDMAllocation();

                UnallocatedC = Math.Max(0, Carbon.TotalFixationSupply + Carbon.TotalReAllocationSupply - Carbon.TotalPlantDemandsAllocated);
                UnallocatedN = Math.Max(0, Nitrogen.TotalUptakeSupply + Nitrogen.TotalReAllocationSupply + Nitrogen.TotalFixationSupply - Nitrogen.TotalPlantDemandsAllocated);
            }
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            StartC = 0;
            StartN = 0;
            foreach (Organ o in PlantOrgans)
            {
                StartC += o.C;
                StartN += o.N;
            }
        }

        /// <summary>
        /// Event from clock when biomass partitioning is complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("PartitioningComplete")]
        private void OnPartitioningComplete(object sender, EventArgs e)
        {
            if (plant.IsEmerged)
            {
                double endC = 0;
                double endN = 0;
                double checkC = StartC;
                double checkN = StartN;
                foreach (Organ o in PlantOrgans)
                {
                    checkC += (o.Carbon.SuppliesAllocated.Fixation + o.Carbon.SuppliesAllocated.Uptake - o.Detached.C - o.LiveRemoved.C - o.DeadRemoved.C);
                    checkN += (o.Nitrogen.SuppliesAllocated.Fixation + o.Nitrogen.SuppliesAllocated.Uptake - o.Detached.N - o.LiveRemoved.N - o.DeadRemoved.N);
                    endC += o.C;
                    endN += o.N;
                }
                
                if (!MathUtilities.FloatsAreEqual(checkC, endC, 1e-11))
                    throw new Exception(clock.Today.ToString() + " Mass balance violation in Carbon");
                if (!MathUtilities.FloatsAreEqual(checkN, endN, 1e-12))
                    throw new Exception(clock.Today.ToString() + "Mass balance violation in Nitrogen");
            }
        }

        /// <summary>Determines Nutrient limitations to DM allocations</summary>
        public void NutrientConstrainedDMAllocation()
        {
            double PreNStressDMAllocation = Carbon.TotalPlantDemandsAllocated;
            CFixationDownRegulationToNLimitation = 0;

            //To introduce functionality for other nutrients we need to repeat this for loop for each new nutrient type
            // Calculate posible growth based on Minimum N requirement of organs
            foreach (Organ o in PlantOrgans)
            {
                var N = o.Nitrogen;
                if (N.DemandsAllocated.Total > N.Demands.Total || MathUtilities.FloatsAreEqual(N.DemandsAllocated.Total, N.Demands.Total))
                    N.MaxCDelta = 100000000; //given high value so where there is no N deficit in organ and N limitation to growth  
                else
                    if (N.DemandsAllocated.Total == 0 || N.ConcentrationOrFraction.Structural == 0)
                    N.MaxCDelta = 0;
                else
                    N.MaxCDelta = N.DemandsAllocated.Total / N.ConcentrationOrFraction.Structural;

                var C = o.Carbon;
                if ((C.DemandsAllocated.Metabolic + C.DemandsAllocated.Structural) != 0)
                {
                    double StructuralProportion = C.DemandsAllocated.Structural / C.DemandsAllocated.Total;
                    double MetabolicProportion = C.DemandsAllocated.Metabolic / C.DemandsAllocated.Total;
                    double StorageProportion = C.DemandsAllocated.Storage / C.DemandsAllocated.Total; ;
                    // Reset C demand allocations based on what is possible with given N supply
                    C.DemandsAllocated = new NutrientPoolsState(
                        Math.Min(C.DemandsAllocated.Structural, N.MaxCDelta * StructuralProportion),  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function
                        Math.Min(C.DemandsAllocated.Metabolic, N.MaxCDelta * MetabolicProportion),
                        Math.Min(C.DemandsAllocated.Storage, N.MaxCDelta * StorageProportion));
                }
            }

            CUnallocatedDueToNLimitation = PreNStressDMAllocation - Carbon.TotalPlantDemandsAllocated;

            //Finally we need to wind back allocations of C supplies if nutrient limitation constrained growth
           if (CUnallocatedDueToNLimitation > 0)
            {
                //First wind back retranslocation
                double PotentialAllocationYetToWindBack = CUnallocatedDueToNLimitation;
                if (Carbon.TotalReTranslocationSupply > 0)
                {
                    double AllocationToWindBack = Math.Min(Carbon.TotalReTranslocationSupplyAllocated, PotentialAllocationYetToWindBack);
                    double constrainedAllocation = Carbon.TotalReTranslocationSupplyAllocated - AllocationToWindBack;

                    foreach (Organ o in PlantOrgans) //Redo ReTrans with constrained amount
                    {
                        o.Carbon.SuppliesAllocated.ReTranslocation = new NutrientPoolsState(
                                0.0,
                                calcAllocated(constrainedAllocation, o.Carbon.Supplies.ReTranslocation.Metabolic, Carbon.TotalReTranslocationSupply),
                                calcAllocated(constrainedAllocation, o.Carbon.Supplies.ReTranslocation.Storage, Carbon.TotalReTranslocationSupply));
                    }
                    PotentialAllocationYetToWindBack -= AllocationToWindBack;

                }

                //Then wind back fixation
                if ((Carbon.TotalFixationSupplyAllocated > 0)&&(PotentialAllocationYetToWindBack > 0))
                {
                    CFixationDownRegulationToNLimitation = Math.Min(PotentialAllocationYetToWindBack, Carbon.TotalFixationSupply);
                    double AllocationToWindBack = Math.Min(Carbon.TotalFixationSupplyAllocated, PotentialAllocationYetToWindBack);
                    double constrainedAllocation = Carbon.TotalFixationSupplyAllocated - AllocationToWindBack;
                    
                    foreach (Organ o in PlantOrgans) //Redo Fixation with constrained amount
                    {
                        if (o.Carbon.Supplies.Fixation > 0)
                        {
                            o.Carbon.SuppliesAllocated.Fixation = calcAllocated(constrainedAllocation, o.Carbon.Supplies.Fixation, Carbon.TotalFixationSupply);
                        }
                    }
                    PotentialAllocationYetToWindBack -= AllocationToWindBack;
                }

                //Finally wind back reallocation
                if ((Carbon.TotalReAllocationSupplyAllocated > 0)&& (PotentialAllocationYetToWindBack > 0))
                {
                    double AllocationToWindBack = Math.Min(Carbon.TotalReAllocationSupplyAllocated, PotentialAllocationYetToWindBack);
                    double constrainedAllocation = Carbon.TotalReAllocationSupplyAllocated - AllocationToWindBack;
                    
                    foreach (Organ o in PlantOrgans) //Redo allocation with constrained amount
                    {
                        o.Carbon.SuppliesAllocated.ReAllocation = new NutrientPoolsState(
                                0.0,
                                calcAllocated(constrainedAllocation, o.Carbon.Supplies.ReAllocation.Metabolic, Carbon.TotalReAllocationSupply),
                                calcAllocated(constrainedAllocation, o.Carbon.Supplies.ReAllocation.Storage, Carbon.TotalReAllocationSupply));
                    }
                    PotentialAllocationYetToWindBack -= AllocationToWindBack;
                }

                if (PotentialAllocationYetToWindBack > 0)
                    throw new Exception("Problem with nutrient constrained supply allocation");
            }
        }

        private double calcAllocated(double totalAllocated, double organSupply, double totalSupply)
        {
            if (totalAllocated - totalSupply > tolerence)
                throw new Exception("Allocation greater than supply");
            double relativeShare = organSupply / totalSupply;
            double retVal = totalAllocated * relativeShare;
            if (Double.IsNaN(retVal))
                throw new Exception("Allocation of supplies gave a Nan");
            if (retVal < -tolerence)
                throw new Exception("Allocation was negative");
            return Math.Max(0, retVal); //Constrained to zero to wipe floating point errors
        }

        /// <summary>Relatives the allocation.</summary>
        /// <param name="TotalSupply">The amount of nutrient to allocate</param>
        /// <param name="PRS">The supply and demand info for that nutrient</param>
        public double DoAllocation(double TotalSupply, PlantNutrientsDelta PRS)
        {
            double initialDemand = PRS.TotalPlantDemand - PRS.TotalPlantDemandsAllocated;
            double totalAllocated = 0;
            if (TotalSupply > tolerence)
            {
                double notAllocated = TotalSupply;

                ////First time round allocate with priority factors applied so higher priority sinks get more allocation
                foreach (OrganNutrientDelta o in PRS.ArbitratingOrgans)
                {
                    if (o.OutstandingDemands.Total > 0.0)
                    {
                        double totalPriorityDemand = PRS.TotalPlantPriorityScalledDemand;
                        NutrientPoolsState allocation = new NutrientPoolsState
                        (
                            Math.Min(o.OutstandingDemands.Structural, TotalSupply * MathUtilities.Divide(o.PriorityScaledDemand.Structural, totalPriorityDemand, 0)),
                            Math.Min(o.OutstandingDemands.Metabolic, TotalSupply * MathUtilities.Divide(o.PriorityScaledDemand.Metabolic, totalPriorityDemand, 0)),
                            Math.Min(o.OutstandingDemands.Storage, TotalSupply * MathUtilities.Divide(o.PriorityScaledDemand.Storage, totalPriorityDemand, 0))
                        );

                        o.DemandsAllocated += allocation;
                        notAllocated -= allocation.Total;
                        totalAllocated += allocation.Total;
                    }
                }
                double RemainingDemand = PRS.TotalPlantDemand - PRS.TotalPlantDemandsAllocated;
                if (RemainingDemand < -tolerence) //Throw if really negative
                    throw new Exception(FullPath + ".Storage was set to negative value");
                if (RemainingDemand < 0)  // if negative in floating point tollerence, zero the pool
                    RemainingDemand = 0.0;
                // Second time round if there is still biomass to allocate do it based on relative demands so lower priority organs have the change to be allocated full demand
                foreach (OrganNutrientDelta o in PRS.ArbitratingOrgans)
                {
                    if (o.OutstandingDemands.Total > 0.0)
                    {
                        NutrientPoolsState allocation = new NutrientPoolsState
                        (
                            Math.Min(o.OutstandingDemands.Structural, notAllocated * MathUtilities.Divide(o.OutstandingDemands.Structural, RemainingDemand, 0)),
                            Math.Min(o.OutstandingDemands.Metabolic, notAllocated * MathUtilities.Divide(o.OutstandingDemands.Metabolic, RemainingDemand, 0)),
                            Math.Min(o.OutstandingDemands.Storage, notAllocated * MathUtilities.Divide(o.OutstandingDemands.Storage, RemainingDemand, 0))
                        );

                        o.DemandsAllocated += allocation;
                        totalAllocated += allocation.Total;
                    }
                }
            }
            return totalAllocated;
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        virtual protected void OnPlantEnding(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Clears this instance.</summary>
        virtual protected void Clear()
        {
            Carbon.Clear();
            Nitrogen.Clear();
        }
    }
}
