using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using Newtonsoft.Json;

namespace Models.PMF
{
    ///<summary>
    /// interface between soil arbitrator and plant root objects
    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IPlant))]
    public class RootUptakesArbitrator : Model, IUptake
    {
        ///1. Links
        ///------------------------------------------------------------------------------------------------

        /// <summary>The top level plant object in the Plant Modelling Framework</summary>
        [Link]
        private Plant plant = null;

        /// <summary>The zone.</summary>
        [Link(Type = LinkType.Ancestor)]
        protected IZone zone = null;


        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>The kgha2gsm</summary>
        protected const double kgha2gsm = 0.1;

        /// <summary>The list of organs</summary>
        protected List<IWaterNitrogenUptake> uptakingOrgans = new List<IWaterNitrogenUptake>();

        /// <summary>A list of organs or suborgans that have watardemands</summary>
        protected List<IHasWaterDemand> waterDemandingOrgans = new List<IHasWaterDemand>();

        private BiomassArbitrator biomassArbitrator { get; set; }

        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        [JsonIgnore]
        public double WDemand { get; protected set; }

        /// <summary>Gets the water Supply.</summary>
        /// <value>The water supply.</value>
        [JsonIgnore]
        public double WSupply { get; protected set; }

        /// <summary>Gets the water allocated in the plant (taken up).</summary>
        /// <value>The water uptake.</value>
        [JsonIgnore]
        public double WAllocated { get; protected set; }

        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            List<IHasWaterDemand> OrgansToDemandWater = new List<IHasWaterDemand>();
            foreach (Model hwd in plant.FindAllInScope<IHasWaterDemand>())
                OrgansToDemandWater.Add(hwd as IHasWaterDemand);
            waterDemandingOrgans = OrgansToDemandWater;

            List<IWaterNitrogenUptake> OrgansToUptakeWaterAndN = new List<IWaterNitrogenUptake>();
            foreach (Model wnu in plant.FindAllInScope<IWaterNitrogenUptake>())
                OrgansToUptakeWaterAndN.Add(wnu as IWaterNitrogenUptake);
            uptakingOrgans = OrgansToUptakeWaterAndN;

            biomassArbitrator = plant.FindChild<BiomassArbitrator>();
        }

        /// <summary>
        /// Calculate the potential sw uptake for today
        /// </summary>
        public List<ZoneWaterAndN> GetWaterUptakeEstimates(SoilState soilstate)
        {
            if (plant.IsAlive)
            {
                // Get all water supplies.
                double waterSupply = 0;  //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.

                List<double[]> supplies = new List<double[]>();
                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
                foreach (ZoneWaterAndN zone in soilstate.Zones)
                    foreach (IWaterNitrogenUptake u in uptakingOrgans)
                    {
                        double[] organSupply = u.CalculateWaterSupply(zone);
                        if (organSupply != null)
                        {
                            supplies.Add(organSupply);
                            zones.Add(zone);
                            waterSupply += MathUtilities.Sum(organSupply) * zone.Zone.Area;
                        }
                    }


                // Calculate total water demand.
                double waterDemand = 0; //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.

                foreach (IHasWaterDemand WD in waterDemandingOrgans)
                    waterDemand += WD.CalculateWaterDemand() * zone.Area;

                // Calculate demand / supply ratio.
                double fractionUsed = 0;
                if (waterSupply > 0)
                    fractionUsed = Math.Min(1.0, waterDemand / waterSupply);

                // Apply demand supply ratio to each zone and create a ZoneWaterAndN structure
                // to return to caller.
                List<ZoneWaterAndN> ZWNs = new List<ZoneWaterAndN>();
                for (int i = 0; i < supplies.Count; i++)
                {
                    // Just send uptake from my zone
                    ZoneWaterAndN uptake = new ZoneWaterAndN(zones[i]);
                    uptake.Water = MathUtilities.Multiply_Value(supplies[i], fractionUsed);
                    uptake.NO3N = new double[uptake.Water.Length];
                    uptake.NH4N = new double[uptake.Water.Length];
                    ZWNs.Add(uptake);
                }
                return ZWNs;
            }
            return null;
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public virtual void SetActualWaterUptake(List<ZoneWaterAndN> zones)
        {
            // Calculate the total water supply across all zones.
            double waterSupply = 0;   //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
            foreach (ZoneWaterAndN Z in zones)
            {
                waterSupply += MathUtilities.Sum(Z.Water) * Z.Zone.Area;
            }
            // Calculate total plant water demand.
            WDemand = 0.0; //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.

            foreach (IHasWaterDemand d in waterDemandingOrgans)
            {
                WDemand += d.CalculateWaterDemand() * zone.Area;
            }

            // Calculate the fraction of water demand that has been given to us.
            double fraction = 1;
            if (WDemand > 0)
                fraction = Math.Min(1.0, waterSupply / WDemand);

            // Proportionally allocate supply across organs.
            WAllocated = 0.0;

            foreach (IHasWaterDemand WD in waterDemandingOrgans)
            {
                double demand = WD.CalculateWaterDemand();
                double allocation = fraction * demand;
                WD.WaterAllocation = allocation;
                WAllocated += allocation;
            }
            //WDemand = waterUptakeMethod.WDemand;
            //WAllocated = waterUptakeMethod.WAllocated;

            // Give the water uptake for each zone to Root so that it can perform the uptake
            // i.e. Root will do pass the uptake to the soil water balance.
            foreach (ZoneWaterAndN Z in zones)
                foreach (IWaterNitrogenUptake u in uptakingOrgans)
                    u.DoWaterUptake(Z.Water, Z.Zone.Name);

        }

        /// <summary>
        /// Calculate the potential N uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        public virtual List<Soils.Arbitrator.ZoneWaterAndN> GetNitrogenUptakeEstimates(SoilState soilstate)
        {
            if (plant.IsEmerged)
            {
                double NSupply = 0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

                foreach (Organ o in biomassArbitrator.PlantOrgans)
                    o.Nitrogen.Supplies.Uptake = 0;

                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
                foreach (ZoneWaterAndN zone in soilstate.Zones)
                {
                    ZoneWaterAndN UptakeDemands = new ZoneWaterAndN(zone);

                    UptakeDemands.NO3N = new double[zone.NO3N.Length];
                    UptakeDemands.NH4N = new double[zone.NH4N.Length];
                    UptakeDemands.Water = new double[UptakeDemands.NO3N.Length];

                    //Get Nuptake supply from each organ and set the PotentialUptake parameters that are passed to the soil arbitrator
                    foreach (Organ o in biomassArbitrator.PlantOrgans)
                    {
                        if (o.WaterNitrogenUptakeObject != null)
                        {
                            double[] organNO3Supply = new double[zone.NO3N.Length];
                            double[] organNH4Supply = new double[zone.NH4N.Length];
                            o.WaterNitrogenUptakeObject.CalculateNitrogenSupply(zone, ref organNO3Supply, ref organNH4Supply);
                            UptakeDemands.NO3N = MathUtilities.Add(UptakeDemands.NO3N, organNO3Supply); //Add uptake supply from each organ to the plants total to tell the Soil arbitrator
                            UptakeDemands.NH4N = MathUtilities.Add(UptakeDemands.NH4N, organNH4Supply);
                            double organSupply = organNH4Supply.Sum() + organNO3Supply.Sum();
                            o.Nitrogen.Supplies.Uptake += organSupply * kgha2gsm * zone.Zone.Area / this.zone.Area;
                            NSupply += organSupply * zone.Zone.Area;
                        }
                    }
                    zones.Add(UptakeDemands);
                }

                double NDemand = (biomassArbitrator.Nitrogen.TotalPlantDemand - biomassArbitrator.Nitrogen.TotalPlantDemandsAllocated) / kgha2gsm * zone.Area; //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
                if (NDemand < 0) NDemand = 0;  //NSupply should be zero if Reallocation can meet all demand (including small rounding errors which can make this -ve)

                if (NSupply > NDemand)
                {
                    //Reduce the PotentialUptakes that we pass to the soil arbitrator
                    double ratio = Math.Min(1.0, NDemand / NSupply);
                    foreach (ZoneWaterAndN UptakeDemands in zones)
                    {
                        UptakeDemands.NO3N = MathUtilities.Multiply_Value(UptakeDemands.NO3N, ratio);
                        UptakeDemands.NH4N = MathUtilities.Multiply_Value(UptakeDemands.NH4N, ratio);
                    }
                }
                return zones;
            }
            return null;
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public virtual void SetActualNitrogenUptakes(List<ZoneWaterAndN> zones)
        {
            if (plant.IsEmerged)
            {
                // Calculate the total no3 and nh4 across all zones.
                double NSupply = 0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
                foreach (ZoneWaterAndN Z in zones)
                    NSupply += (Z.NO3N.Sum() + Z.NH4N.Sum()) * Z.Zone.Area;

                //Reset actual uptakes to each organ based on uptake allocated by soil arbitrator and the organs proportion of potential uptake
                //NUptakeSupply units should be g/m^2
                biomassArbitrator.AllocateNUptake(NSupply * kgha2gsm);

                IWaterNitrogenUptake u = plant.FindDescendant<IWaterNitrogenUptake>();
                {
                    // Note: This does the actual nitrogen extraction from the soil.
                    // If there are multiple organs with IWaterNitorgenUptake it will send all the N uptake through the first 
                    // This seems wrong at first uptakes and allocations from each organ have been accounted for, this is just
                    // setting the delta in the soil
                    u.DoNitrogenUptake(zones);
                }
            }
        }
    }
}
