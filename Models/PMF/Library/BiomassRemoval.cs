using System;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.Soils.Nutrients;
using Models.Surface;

namespace Models.PMF.Library
{

    /// <summary>
    /// This organ will respond to certain management actions by either removing some
    /// of its biomass from the system or transferring some of its biomass to the soil
    /// surface residues. The following table describes the default proportions of live
    /// and dead biomass that are transferred out of the simulation using "Removed" or
    /// to soil surface residue using "To Residue" for a range of management actions.
    /// The total percentage removed for live or dead must not exceed 100%. The
    /// difference between the total and 100% gives the biomass remaining on the plant.
    /// These can be changed during a simulation using a manager script.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IOrgan))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.CompositePropertyPresenter")]
    public class BiomassRemoval : Model
    {
        [Link]
        Plant plant = null;

        [Link]
        ISurfaceOrganicMatter surfaceOrganicMatter = null;

        [Link]
        Summary summary = null;

        [Link]
        INutrient nutrient = null;

        /// <summary>Fraction of live biomass to remove from plant at harvest (remove from the system)</summary>
        [Description("Fraction of live biomass to remove from plant at harvest (remove from the system)")]
        public double HarvestFractionLiveToRemove { get; set; }

        /// <summary>Fraction of dead biomass to remove from plant  at harvest (remove from the system)</summary>
        [Description("Fraction of dead biomass to remove from plant  at harvest (remove from the system)")]
        public double HarvestFractionDeadToRemove { get; set; }

        /// <summary>Fraction of live biomass to remove from plant at harvest (send to surface organic matter</summary>
        [Description("Fraction of live biomass to remove from plant at harvest (send to surface organic matter")]
        public double HarvestFractionLiveToResidue { get; set; }

        /// <summary>Fraction of dead biomass to remove from plant at harvest (send to surface organic matter</summary>
        [Description("Fraction of dead biomass to remove from plant at harvest (send to surface organic matter")]
        public double HarvestFractionDeadToResidue { get; set; }

        /// <summary>Removes biomass from live and dead biomass pools, may send to surface organic matter</summary>
        /// <param name="liveToRemove">Fraction of live biomass to remove from simulation (0-1).</param>
        /// <param name="deadToRemove">Fraction of dead biomass to remove from simulation (0-1).</param>
        /// <param name="liveToResidue">Fraction of live biomass to remove and send to residue pool(0-1).</param>
        /// <param name="deadToResidue">Fraction of dead biomass to remove and send to residue pool(0-1).</param>
        /// <param name="live">Live biomass pool</param>
        /// <param name="dead">Dead biomass pool</param>
        /// <param name="removed">The removed pool to add to.</param>
        /// <param name="detached">The detached pool to add to.</param>
        /// <param name="writeToSummary">Write the biomass removal to summary file?</param>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        public double RemoveBiomass(double liveToRemove, double deadToRemove, double liveToResidue, double deadToResidue,
                                    Biomass live, Biomass dead,
                                    Biomass removed, Biomass detached,
                                    bool writeToSummary = true)
        {
            if (liveToRemove + liveToResidue > 1.0)
                throw new Exception($"The sum of FractionToResidue and FractionToRemove for {Parent.Name} is greater than one for live biomass.");

            if (deadToRemove + deadToResidue > 1.0)
                throw new Exception($"The sum of FractionToResidue and FractionToRemove for {Parent.Name} is greater than one for dead biomass");

            double liveFractionToRemove = liveToRemove + liveToResidue;
            double deadFractionToRemove = deadToRemove + deadToResidue;

            if (liveFractionToRemove + deadFractionToRemove > 0.0)
            {
                double totalBiomass = live.Wt + dead.Wt;
                if (totalBiomass > 0)
                {
                    RemoveBiomassFromLiveAndDead(liveToRemove, deadToRemove, liveToResidue, deadToResidue, 
                                                 live, dead, out Biomass removing, out Biomass detaching);

                    // Add the detaching biomass to total removed and detached
                    removed.Add(removing);
                    detached.Add(detaching);

                    // Pass the detaching biomass to surface organic matter model.
                    //TODO: in reality, the dead material is different from the live, so it would be better to add them as separate pools to SurfaceOM
                    if (plant.PlantType == null)
                        throw new Exception($"PlantType is null in plant {plant.Name}. The most likely cause is the use of an unofficial/unreleased plant model.");

                    if (writeToSummary && (removing.Wt > 0 || detaching.Wt > 0))
                    {
                        double totalFractionToRemove = (removed.Wt + detaching.Wt) * 100.0 / totalBiomass;
                        double toResidue = detaching.Wt * 100.0 / (removed.Wt + detaching.Wt);
                        double removedOff = removed.Wt * 100.0 / (removed.Wt + detaching.Wt);
                        summary.WriteMessage(Parent, $"Removing {totalFractionToRemove:F1}% of {Parent.Name.ToLower()} biomass from {plant.Name}. " +
                                                     $"{removedOff:F1}% is removed from the system and {toResidue:F1}% is returned to the surface", MessageType.Diagnostic);
                    }

                    surfaceOrganicMatter.Add(detaching.Wt * 10.0, detaching.N * 10.0, 0.0, plant.PlantType, Name);

                    return removing.Wt + detaching.Wt; ;
                }
            }

            return 0.0;
        }


        /// <summary>Removes biomass from live and dead biomass pools and send to soil</summary>
        /// <param name="liveToRemove">Fraction of live biomass to remove from simulation (0-1).</param>
        /// <param name="liveToResidue">Fraction of live biomass to remove and send to residue pool(0-1).</param>
        /// <param name="Live">Live biomass pool</param>
        /// <param name="Dead">Dead biomass pool</param>
        /// <param name="Removed">The removed pool to add to.</param>
        /// <param name="Detached">The detached pool to add to.</param>
        public double RemoveBiomassToSoil(double liveToRemove, double liveToResidue,
                                          Biomass[] Live, Biomass[] Dead,
                                          Biomass Removed, Biomass Detached)
        {
            //NOTE: roots don't have dead biomass
            double totalFractionToRemove = liveToRemove + liveToResidue;

            if (totalFractionToRemove > 0)
            {
                Biomass removing = new Biomass();
                Biomass detaching = new Biomass();

                //NOTE: at the moment Root has no Dead pool
                FOMLayerLayerType[] FOMLayers = new FOMLayerLayerType[Live.Length];
                double remainingFraction = 1.0 - (liveToResidue + liveToRemove);
                for (int layer = 0; layer < Live.Length; layer++)
                {
                    RemoveBiomassFromLiveAndDead(liveToRemove, deadToRemove: 0.0, liveToResidue, deadToResidue: 0.0,
                                                 Live[layer], Dead[layer], out removing, out detaching);

                    // Add the detaching biomass to total removed and detached
                    Removed.Add(removing);
                    Detached.Add(detaching);

                    // Pass the detaching biomass to surface organic matter model.
                    FOMType fom = new FOMType();
                    fom.amount = (float)(detaching.Wt * 10);
                    fom.N = (float)(detaching.N * 10);
                    fom.C = (float)(0.40 * detaching.Wt * 10);
                    fom.P = 0.0;
                    fom.AshAlk = 0.0;

                    FOMLayerLayerType Layer = new FOMLayerLayerType();
                    Layer.FOM = fom;
                    Layer.CNR = 0.0;
                    Layer.LabileP = 0.0;
                    FOMLayers[layer] = Layer;
                }
                FOMLayerType FomLayer = new FOMLayerType();
                FomLayer.Type = plant.PlantType;
                FomLayer.Layer = FOMLayers;
                nutrient.DoIncorpFOM(FomLayer);

                return removing.Wt + detaching.Wt;
            }
            return 0.0;
        }


        /// <summary>Removes biomass from live and dead biomass pools</summary>
        /// <param name="liveToRemove">Fraction of live biomass to remove from simulation (0-1).</param>
        /// <param name="deadToRemove">Fraction of dead biomass to remove from simulation (0-1).</param>
        /// <param name="liveToResidue">Fraction of live biomass to remove and send to residue pool(0-1).</param>
        /// <param name="deadToResidue">Fraction of dead biomass to remove and send to residue pool(0-1).</param>
        /// <param name="live">Live biomass pool</param>
        /// <param name="dead">Dead biomass pool</param>
        /// <param name="removing">The removed pool to add to.</param>
        /// <param name="detaching">The amount of detaching material</param>
        private static void RemoveBiomassFromLiveAndDead(double liveToRemove, double deadToRemove, double liveToResidue, double deadToResidue, 
                                                           Biomass live, Biomass dead, out Biomass removing, out Biomass detaching)
        {
            double remainingLiveFraction = 1.0 - (liveToResidue + liveToRemove);
            double remainingDeadFraction = 1.0 - (deadToResidue + deadToRemove);

            detaching = live * liveToResidue + dead * deadToResidue;
            removing = live * liveToRemove + dead * deadToRemove;

            live.Multiply(remainingLiveFraction);
            dead.Multiply(remainingDeadFraction);
        }
    }
}
