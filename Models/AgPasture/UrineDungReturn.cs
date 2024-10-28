using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Drawing.Charts;
using Models.Core;
using Models.ForageDigestibility;
using Models.Soils;
using Models.Surface;


namespace Models.AgPasture;

/// <summary>
/// This class encapsulates urine and dung deposition to the soil and lost to the simulation.
/// </summary>4

public class UrineDungReturn
{
    /// <summary>
    /// Perform urine return to soil.
    /// </summary>
    /// <param name="grazedForages">Grazed forages.</param>
    /// <param name="fractionDefoliatedBiomassToSoil"></param>
    /// <param name="fractionDefoliatedNToSoil"></param>
    /// <param name="fractionExcretedNToDung"></param>
    /// <param name="CNRatioDung"></param>
    /// <param name="fractionUrineLostToSimulation">Fraction of the urine lost to simulation.</param>
    /// <param name="fractionDungLostToSimulation">Fraction of the dung lost to simulation.</param>
    /// <returns>The amount of urine and dung added to soil and lost from the simulation</returns>
    public static UrineDung CalculateUrineDungReturn(List<Forages.MaterialRemoved> grazedForages,
                                                     double fractionDefoliatedBiomassToSoil,
                                                     double fractionDefoliatedNToSoil,
                                                     double fractionExcretedNToDung,
                                                     double CNRatioDung,
                                                     double fractionUrineLostToSimulation,
                                                     double fractionDungLostToSimulation)
    {
        if (grazedForages.Any())
        {
            double returnedToSoilWt = 0;
            double returnedToSoilN = 0;
            foreach (var grazedForage in grazedForages)
            {
                returnedToSoilWt += fractionDefoliatedBiomassToSoil *
                                    (1 - grazedForage.Digestibility) * grazedForage.Wt;
                returnedToSoilN += fractionDefoliatedNToSoil * grazedForage.N;
            }

            double dungNReturned;
            if (double.IsNaN(fractionExcretedNToDung))
            {
                const double CToDMRatio = 0.4; // 0.4 is C:DM ratio.
                dungNReturned = Math.Min(returnedToSoilN, returnedToSoilWt * CToDMRatio / CNRatioDung);
            }
            else
                dungNReturned = fractionExcretedNToDung * returnedToSoilN;

            UrineDung deposition = new()
            {
                DungWtToSoil = returnedToSoilWt * (1.0 - fractionDungLostToSimulation),
                DungNToSoil = dungNReturned * (1.0 - fractionDungLostToSimulation),
                UrineNToSoil = (returnedToSoilN - dungNReturned) * (1 - fractionUrineLostToSimulation),
                DungWtLostFromSimulation = returnedToSoilWt * fractionDungLostToSimulation,
                DungNLostFromSimulation = dungNReturned * fractionDungLostToSimulation,
                UrineNLostFromSimulation = (returnedToSoilN - dungNReturned) * fractionUrineLostToSimulation
            };

            return deposition;
        }
        return null;
    }

    /// <summary>
    /// Perform urine return.
    /// </summary>
    /// <param name="deposition">The urine, dung deposition to the soil.</param>
    /// <param name="thickness">The layer thickness (mm).</param>
    /// <param name="urea">The urea solute.</param>
    /// <param name="depthUrineIsAdded">The depth to add the urine.</param>
    public static void DoUrineReturn(UrineDung deposition, double[] thickness, ISolute urea, double depthUrineIsAdded)
    {
        if (deposition.UrineNToSoil > 0)
        {
            //int layer = SoilUtilities.LayerIndexOfDepth(thickness, depthUrineIsAdded);
            double[] ProportionOfCumThickness = new double[thickness.Length];
            ProportionOfCumThickness = SoilUtilities.ProportionOfCumThickness(thickness, depthUrineIsAdded);
            var ureaDelta = new double[thickness.Length];
            for (int i = 0; i < thickness.Length; i++)
                ureaDelta[i] = deposition.UrineNToSoil * ProportionOfCumThickness[i];
            urea.AddKgHaDelta(SoluteSetterType.Fertiliser, ureaDelta);
        }
    }

    /// <summary>
    /// Perform dung return.
    /// </summary>
    /// <param name="deposition">The urine, dung deposition to the soil.</param>
    /// <param name="surfaceOrganicMatter">The SurfaceOrganicMatter model.</param>
    public static void DoDungReturn(UrineDung deposition,
                                    SurfaceOrganicMatter surfaceOrganicMatter)   
    {
        if (deposition.DungWtToSoil > 0)
        {
            var cropType = "RuminantDung_PastureFed";
            surfaceOrganicMatter.Add(deposition.DungWtToSoil, deposition.DungNToSoil, 0, cropType, null);
        }
    }

    /// <summary>
    /// Perform trampling.
    /// </summary>
    /// <param name="surfaceOrganicMatter">The surface organic matter model.</param>
    /// <param name="fractionResidueIncorporated">The fraction of residues incorporated by trampling.</param>
    public static void DoTrampling(SurfaceOrganicMatter surfaceOrganicMatter, double fractionResidueIncorporated = 0.1)
    {
        surfaceOrganicMatter.Incorporate(fractionResidueIncorporated, depth: 100);
    }


    /// <summary>Encapsulates calculated urine and dung return to soil</summary>
    public class UrineDung
    {
        /// <summary>The amount of N in dung returned to soil (kg/ha)</summary>
        public double DungNToSoil { get; set; }
        /// <summary>The amount of biomass in dung returned to soil (kg/ha)</summary>
        public double DungWtToSoil { get; set; }
        /// <summary>The amount of urine N returned to soil (kg/ha)</summary>
        public  double UrineNToSoil { get; set; }

        /// <summary>The amount of N lost to the simulation (kg/ha)</summary>
        public double DungNLostFromSimulation { get; set; }
        /// <summary>The amount of biomass lost to the simulation (kg/ha)</summary>
        public double DungWtLostFromSimulation { get; set; }
        /// <summary>The amount of urine N  lost to the simulation (kg/ha)</summary>
        public  double UrineNLostFromSimulation { get; set; }
    }

}