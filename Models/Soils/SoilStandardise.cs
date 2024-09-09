using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Interfaces;
using Models.Soils;
using Models.Soils.SoilTemp;
using Models.WaterModel;
using static Models.Soils.Chemical;
using static Models.Soils.Solute;
namespace Models.Soils;

/// <summary>
/// Encapsulates code to sanitise a soil to make it ready for running.
/// </summary>
public static class SoilSanitise
{
    /// <summary>
    /// Sanitise a soil.
    /// </summary>
    /// <param name="soil"></param>
    public static void Sanitise(this Soil soil)
    {
        var physical = soil.FindChild<Physical>();
        var chemical = soil.FindChild<Chemical>();
        var layerStructure = soil.FindChild<LayerStructure>();
        var organic = soil.FindChild<Organic>();
        var water = soil.FindChild<Water>();
        var waterBalance = soil.FindInScope<ISoilWater>();
        var temperature = soil.FindInScope<Models.Soils.SoilTemp.SoilTemperature>();

        // Determine the target layer structure.
        var targetThickness = physical.Thickness;
        if (layerStructure != null)
            targetThickness = layerStructure.Thickness;

        if (physical != null)
            SanitisePhysical(physical, targetThickness);
        if (chemical != null)
            SanitiseChemical(chemical, targetThickness);
        if (organic != null)
            SanitiseOrganic(organic, targetThickness);
        if (water != null && physical != null)
            SanitiseWater(water, physical, targetThickness);
        if (waterBalance != null)
        {
            if (waterBalance is WaterBalance wb)
                SanitiseWaterBalance(wb, physical, targetThickness);
            else if (waterBalance is WEIRDO weirdo)
                SanitiseWeirdo(weirdo, targetThickness);
        }
        if (temperature != null)
            SanitiseSoilTemperature(temperature, targetThickness);

        foreach (var solute in soil.FindAllChildren<Solute>())
            SanitiseSolute(solute, targetThickness);
    }

    /// <summary>Sanitises the chemical model ready for running in a simulation.</summary>
    /// <param name="chemical">The chemical node</param>
    /// <param name="targetThickness">Target thickness.</param>
    private static void SanitiseChemical(Chemical chemical, double[] targetThickness)
    {
        if (!MathUtilities.AreEqual(targetThickness, chemical.Thickness))
        {
            chemical.PH = SoilUtilities.MapConcentration(chemical.PH, chemical.Thickness, targetThickness, 7.0);
            chemical.EC = SoilUtilities.MapConcentration(chemical.EC, chemical.Thickness, targetThickness, MathUtilities.LastValue(chemical.EC));
            chemical.ESP = SoilUtilities.MapConcentration(chemical.ESP, chemical.Thickness, targetThickness, MathUtilities.LastValue(chemical.ESP));
            chemical.CEC = SoilUtilities.MapConcentration(chemical.CEC, chemical.Thickness, targetThickness, MathUtilities.LastValue(chemical.CEC));
            chemical.Thickness = targetThickness;
        }
        if (chemical.PHUnits == PHUnitsEnum.CaCl2)
        {
            chemical.PH = SoilUtilities.PHCaCl2ToWater(chemical.PH);
            chemical.PHUnits = PHUnitsEnum.Water;
        }

        chemical.EC = MathUtilities.FillMissingValues(chemical.EC, chemical.Thickness.Length, 0);
        chemical.ESP = MathUtilities.FillMissingValues(chemical.ESP, chemical.Thickness.Length, 0);
        chemical.PH = MathUtilities.FillMissingValues(chemical.PH, chemical.Thickness.Length, 7.0);
        chemical.CEC = MathUtilities.FillMissingValues(chemical.CEC, chemical.Thickness.Length, 0);
    }

    /// <summary>Sanitises the organic model ready for running in a simulation.</summary>
    /// <param name="organic">The organic instance to sanitise</param>
    /// <param name="targetThickness">Target thickness.</param>
    private static void SanitiseOrganic(Organic organic, double[] targetThickness)
    {
        if (!MathUtilities.AreEqual(targetThickness, organic.Thickness))
        {
            organic.FBiom = SoilUtilities.MapConcentration(organic.FBiom, organic.Thickness, targetThickness, MathUtilities.LastValue(organic.FBiom));
            organic.FInert = SoilUtilities.MapConcentration(organic.FInert, organic.Thickness, targetThickness, MathUtilities.LastValue(organic.FInert));
            organic.Carbon = SoilUtilities.MapConcentration(organic.Carbon, organic.Thickness, targetThickness, MathUtilities.LastValue(organic.Carbon));
            organic.SoilCNRatio = SoilUtilities.MapConcentration(organic.SoilCNRatio, organic.Thickness, targetThickness, MathUtilities.LastValue(organic.SoilCNRatio));
            organic.FOM = SoilUtilities.MapMass(organic.FOM, organic.Thickness, targetThickness, false);
            organic.Thickness = targetThickness;
        }

        if (organic.FBiom != null)
            MathUtilities.ReplaceMissingValues(organic.FBiom, MathUtilities.LastValue(organic.FBiom));
        if (organic.FInert != null)
            MathUtilities.ReplaceMissingValues(organic.FInert, MathUtilities.LastValue(organic.FInert));
        if (organic.Carbon != null)
            MathUtilities.ReplaceMissingValues(organic.Carbon, MathUtilities.LastValue(organic.Carbon));

        if (organic.CarbonUnits == Organic.CarbonUnitsEnum.WalkleyBlack)
        {
            organic.Carbon = SoilUtilities.OCWalkleyBlackToTotal(organic.Carbon);
            organic.CarbonUnits = Organic.CarbonUnitsEnum.Total;
        }

        if (!MathUtilities.ValuesInArray(organic.Carbon))
            organic.Carbon = null;
        if (organic.Carbon != null)
            organic.Carbon = MathUtilities.FixArrayLength(organic.Carbon, organic.Thickness.Length);
    }

    /// <summary>Sanitises the physical model ready for running in a simulation.</summary>
    /// <param name="physical">The physical model to sanitise</param>
    /// <param name="targetThickness"></param>
    private static void SanitisePhysical(Physical physical, double[] targetThickness)
    {
        if (!MathUtilities.AreEqual(targetThickness, physical.Thickness))
        {
            foreach (var crop in (physical as IModel).FindAllChildren<SoilCrop>())
            {
                crop.KL = SoilUtilities.MapConcentration(crop.KL, physical.Thickness, targetThickness, MathUtilities.LastValue(crop.KL));
                crop.XF = SoilUtilities.MapConcentration(crop.XF, physical.Thickness, targetThickness, MathUtilities.LastValue(crop.XF));
                crop.LL = SoilUtilities.MapConcentration(crop.LL, physical.Thickness, targetThickness, MathUtilities.LastValue(crop.LL));
            }

            physical.BD = SoilUtilities.MapConcentration(physical.BD, physical.Thickness, targetThickness, MathUtilities.LastValue(physical.BD));
            physical.AirDry = SoilUtilities.MapConcentration(physical.AirDry, physical.Thickness, targetThickness, MathUtilities.LastValue(physical.AirDry));
            physical.LL15 = SoilUtilities.MapConcentration(physical.LL15, physical.Thickness, targetThickness, MathUtilities.LastValue(physical.LL15));
            physical.DUL = SoilUtilities.MapConcentration(physical.DUL, physical.Thickness, targetThickness, MathUtilities.LastValue(physical.DUL));
            physical.SAT = SoilUtilities.MapConcentration(physical.SAT, physical.Thickness, targetThickness, MathUtilities.LastValue(physical.SAT));
            physical.KS = SoilUtilities.MapConcentration(physical.KS, physical.Thickness, targetThickness, MathUtilities.LastValue(physical.KS));
            if (physical.ParticleSizeClay != null && physical.ParticleSizeClay.Length > 0 && physical.ParticleSizeClay.Length != targetThickness.Length)
                physical.ParticleSizeClay = SoilUtilities.MapConcentration(physical.ParticleSizeClay, physical.Thickness, targetThickness, MathUtilities.LastValue(physical.ParticleSizeClay));
            if (physical.ParticleSizeSand != null && physical.ParticleSizeSand.Length > 0 && physical.ParticleSizeSand.Length != targetThickness.Length)
                physical.ParticleSizeSand = SoilUtilities.MapConcentration(physical.ParticleSizeSand, physical.Thickness, targetThickness, MathUtilities.LastValue(physical.ParticleSizeSand));
            if (physical.ParticleSizeSilt != null && physical.ParticleSizeSilt.Length > 0 && physical.ParticleSizeSilt.Length != targetThickness.Length)
                physical.ParticleSizeSilt = SoilUtilities.MapConcentration(physical.ParticleSizeSilt, physical.Thickness, targetThickness, MathUtilities.LastValue(physical.ParticleSizeSilt));
            if (physical.Rocks != null && physical.Rocks.Length > 0 && physical.Rocks.Length != targetThickness.Length)
                physical.Rocks = SoilUtilities.MapConcentration(physical.Rocks, physical.Thickness, targetThickness, MathUtilities.LastValue(physical.Rocks));
            physical.Thickness = targetThickness;

            foreach (var crop in (physical as IModel).FindAllChildren<SoilCrop>())
            {
                var soilCrop = crop as SoilCrop;
                // Ensure crop LL are between Airdry and DUL.
                for (int i = 0; i < soilCrop.LL.Length; i++)
                    soilCrop.LL = MathUtilities.Constrain(soilCrop.LL, physical.AirDry, physical.DUL);
            }
        }

        // Add soil/crop parameterisations is on a vertosol soil.
        AddPredictedCrops(physical);

        physical.InFill();
    }

    /// <summary>
    /// Fill missing values with default values.
    /// </summary>
    /// <param name="physical">The physical model.</param>
    public static void InFill(this Physical physical)
    {
        // Fill in missing XF values.
        foreach (var crop in (physical as IModel).FindAllChildren<SoilCrop>())
        {
            if (crop.KL == null)
                FillInKLForCrop(crop);

            var (cropValues, cropMetadata) = SoilUtilities.FillMissingValues(crop.LL, crop.LLMetadata, physical.Thickness.Length, (i) => i < physical.LL15.Length ? physical.LL15[i] : physical.LL15.Last());
            crop.LL = cropValues;
            crop.LLMetadata = cropMetadata;

            (cropValues, cropMetadata) = SoilUtilities.FillMissingValues(crop.KL, crop.KLMetadata, physical.Thickness.Length, (i) => 0.06);
            crop.KL = cropValues;
            crop.KLMetadata = cropMetadata;

            (cropValues, cropMetadata) = SoilUtilities.FillMissingValues(crop.XF, crop.XFMetadata, physical.Thickness.Length, (i) => 1.0);
            crop.XF = cropValues;
            crop.XFMetadata = cropMetadata;

            // Modify wheat crop for sub soil constraints.
            //if (crop.Name.Equals("WheatSoil", StringComparison.InvariantCultureIgnoreCase))
            //    ModifyKLForSubSoilConstraints(crop);
        }

        // Make sure there are the correct number of KS values.
        if (physical.KS != null && physical.KS.Length > 0)
            physical.KS = MathUtilities.FillMissingValues(physical.KS, physical.Thickness.Length, 0.0);

        physical.ParticleSizeClay = MathUtilities.SetArrayOfCorrectSize(physical.ParticleSizeClay, physical.Thickness.Length);
        physical.ParticleSizeClayMetadata = MathUtilities.SetArrayOfCorrectSize(physical.ParticleSizeClayMetadata, physical.Thickness.Length);
        physical.ParticleSizeSand = MathUtilities.SetArrayOfCorrectSize(physical.ParticleSizeSand, physical.Thickness.Length);
        physical.ParticleSizeSandMetadata = MathUtilities.SetArrayOfCorrectSize(physical.ParticleSizeSandMetadata, physical.Thickness.Length);
        physical.ParticleSizeSilt = MathUtilities.SetArrayOfCorrectSize(physical.ParticleSizeSilt, physical.Thickness.Length);
        physical.ParticleSizeSiltMetadata = MathUtilities.SetArrayOfCorrectSize(physical.ParticleSizeSiltMetadata, physical.Thickness.Length);

        // Fill in missing particle size values.
        for (int i = 0; i < physical.Thickness.Length; i++)
        {
            bool clayIsSupplied = !double.IsNaN(physical.ParticleSizeClay[i]);
            bool siltIsSupplied = !double.IsNaN(physical.ParticleSizeSilt[i]);
            bool sandIsSupplied = !double.IsNaN(physical.ParticleSizeSand[i]);

            if (!clayIsSupplied && !siltIsSupplied && !sandIsSupplied)
            {
                SetDefaultClay(physical, i, 30);
                SetDefaultSilt(physical, i, 65);
                SetDefaultSand(physical, i, 5);
            }
            else if (clayIsSupplied && !siltIsSupplied && !sandIsSupplied)
            {
                SetDefaultSilt(physical, i, 0.65 * (100 - physical.ParticleSizeClay[i]));
                SetDefaultSand(physical, i, 100 - physical.ParticleSizeClay[i] - physical.ParticleSizeSilt[i]);
            }
            else if (siltIsSupplied && !clayIsSupplied && !sandIsSupplied)
            {
                SetDefaultClay(physical, i, 0.3 * (100 - physical.ParticleSizeSilt[i]));
                SetDefaultSand(physical, i, 100 - physical.ParticleSizeClay[i] - physical.ParticleSizeSilt[i]);
            }
            else if (sandIsSupplied && !clayIsSupplied && !siltIsSupplied)
            {
                SetDefaultClay(physical, i, 0.3 * (100 - physical.ParticleSizeSilt[i]));
                SetDefaultSilt(physical, i, 100 - physical.ParticleSizeClay[i] - physical.ParticleSizeSand[i]);
            }
            else if (clayIsSupplied && siltIsSupplied && !sandIsSupplied)
                SetDefaultSand(physical, i, 100 - physical.ParticleSizeClay[i] - physical.ParticleSizeSilt[i]);
            else if (clayIsSupplied && sandIsSupplied && !siltIsSupplied)
                SetDefaultSilt(physical, i, 100 - physical.ParticleSizeClay[i] - physical.ParticleSizeSand[i]);
            else if (siltIsSupplied && sandIsSupplied && !clayIsSupplied)
                SetDefaultClay(physical, i, 100 - physical.ParticleSizeSilt[i] - physical.ParticleSizeSand[i]);
        }

        // Fill in missing rocks.
        physical.Rocks = MathUtilities.SetArrayOfCorrectSize(physical.Rocks, physical.Thickness.Length);
        physical.RocksMetadata = MathUtilities.SetArrayOfCorrectSize(physical.RocksMetadata, physical.Thickness.Length);
        var (values, metadata) = SoilUtilities.FillMissingValues(physical.Rocks, physical.RocksMetadata, physical.Thickness.Length, (i) =>
        {
            double bd = i < physical.BD.Length ? physical.BD[i] : physical.BD.Last();
            double sat = i < physical.SAT.Length ? physical.SAT[i] : physical.SAT.Last();
            double particleDensity = 2.65;
            double totalPorosity = (1 - bd / particleDensity) * 0.93;
            double rocksFraction = 1 - sat / totalPorosity;
            if (rocksFraction > 0.1)
                return rocksFraction;
            else
                return 0;
        });
        physical.Rocks = values;
        physical.RocksMetadata = metadata;
    }

    /// <summary>Gets the model ready for running in a simulation.</summary>
    /// <param name="water">The water node to sanitise</param>
    /// <param name="physcial">The physcial node</param>
    /// <param name="targetThickness">Target thickness.</param>
    private static void SanitiseWater(Water water, Physical physcial, double[] targetThickness)
    {
        if (!MathUtilities.AreEqual(targetThickness, water.Thickness))
        {
            if (water.InitialValues != null)
                water.InitialValues = MapSW(physcial, water.InitialValues, water.Thickness, targetThickness);

            water.Thickness = targetThickness;
        }
        water.Reset();
    }

    /// <summary>Gets the model ready for running in a simulation.</summary>
    /// <param name="solute">The solute model to sanitise</param>
    /// <param name="targetThickness">Target thickness.</param>
    private static void SanitiseSolute(Solute solute, double[] targetThickness)
    {
        // Define default ppm value to use below bottom layer of this solute if necessary.
        double defaultValue = 0;

        if (!MathUtilities.AreEqual(targetThickness, solute.Thickness))
        {
            if (solute.Exco != null)
                solute.Exco = SoilUtilities.MapConcentration(solute.Exco, solute.Thickness, targetThickness, 0.2);
            if (solute.FIP != null)
                solute.FIP = SoilUtilities.MapConcentration(solute.FIP, solute.Thickness, targetThickness, 0.2);

            if (solute.InitialValuesUnits == UnitsEnum.kgha)
                solute.InitialValues = SoilUtilities.kgha2ppm(solute.Thickness, solute.SoluteBD, solute.InitialValues);
            solute.InitialValues = SoilUtilities.MapConcentration(solute.InitialValues, solute.Thickness, targetThickness, defaultValue);
            solute.Thickness = targetThickness;
            if (solute.InitialValuesUnits == UnitsEnum.kgha)
                solute.InitialValues = SoilUtilities.ppm2kgha(solute.Thickness, solute.SoluteBD, solute.InitialValues);
        }

        if (solute.FIP != null) solute.FIP = MathUtilities.FillMissingValues(solute.FIP, solute.Thickness.Length, solute.FIP.Last());
        if (solute.Exco != null) solute.Exco = MathUtilities.FillMissingValues(solute.Exco, solute.Thickness.Length, solute.Exco.Last());
        solute.InitialValues = MathUtilities.FillMissingValues(solute.InitialValues, solute.Thickness.Length, defaultValue);
        solute.Reset();
    }

    /// <summary>Gets the model ready for running in a simulation.</summary>
    /// <param name="waterBalance">The water balance model to sanitise</param>
    /// <param name="physical">The physical model</param>
    /// <param name="targetThickness">Target thickness.</param>
    private static void SanitiseWaterBalance(WaterBalance waterBalance, Physical physical, double[] targetThickness)
    {
        waterBalance.SetPhysical(physical);
        if (!MathUtilities.AreEqual(targetThickness, waterBalance.Thickness))
        {
            waterBalance.KLAT = SoilUtilities.MapConcentration(waterBalance.KLAT, waterBalance.Thickness, targetThickness, MathUtilities.LastValue(waterBalance.KLAT));
            waterBalance.SWCON = SoilUtilities.MapConcentration(waterBalance.SWCON, waterBalance.Thickness, targetThickness, 0.0);

            waterBalance.Thickness = targetThickness;
        }
        if (waterBalance.SWCON == null)
            waterBalance.SWCON = MathUtilities.CreateArrayOfValues(0.3, waterBalance.Thickness.Length);
        MathUtilities.ReplaceMissingValues(waterBalance.SWCON, 0.0);
    }

    /// <summary>Gets the model ready for running in a simulation.</summary>
    /// <param name="weirdo">The weirdo model</param>
    /// <param name="targetThickness">Target thickness.</param>
    private static void SanitiseWeirdo(WEIRDO weirdo, double[] targetThickness)
    {
        weirdo.CFlow = MathUtilities.Multiply_Value(weirdo.CFlow, 1e-10);
        weirdo.CFlow = SoilUtilities.MapConcentration(weirdo.CFlow, weirdo.Thickness, targetThickness, weirdo.CFlow[weirdo.CFlow.Length - 1]);
        weirdo.XFlow = SoilUtilities.MapConcentration(weirdo.XFlow, weirdo.Thickness, targetThickness, weirdo.XFlow[weirdo.XFlow.Length - 1]);
        weirdo.PsiBub = SoilUtilities.MapConcentration(weirdo.PsiBub, weirdo.Thickness, targetThickness, weirdo.PsiBub[weirdo.PsiBub.Length - 1]);
        weirdo.UpperRepellentWC = SoilUtilities.MapConcentration(weirdo.UpperRepellentWC, weirdo.Thickness, targetThickness, weirdo.UpperRepellentWC[weirdo.UpperRepellentWC.Length - 1]);
        weirdo.LowerRepellentWC = SoilUtilities.MapConcentration(weirdo.LowerRepellentWC, weirdo.Thickness, targetThickness, weirdo.LowerRepellentWC[weirdo.LowerRepellentWC.Length - 1]);
        weirdo.MinRepellancyFactor = SoilUtilities.MapConcentration(weirdo.MinRepellancyFactor, weirdo.Thickness, targetThickness, weirdo.MinRepellancyFactor[weirdo.MinRepellancyFactor.Length - 1]);
        weirdo.Thickness = targetThickness;
    }

    /// <summary>Gets the model ready for running in a simulation.</summary>
    /// <param name="soilTemperature">Soil temperature model</param>
    /// <param name="targetThickness">Target thickness.</param>
    private static void SanitiseSoilTemperature(SoilTemperature soilTemperature, double[] targetThickness)
    {
        soilTemperature.InitialValues = SoilUtilities.MapInterpolation(soilTemperature.InitialValues, soilTemperature.Thickness, targetThickness, allowMissingValues:true);
    }

    /// <summary>Map soil water from one layer structure to another.</summary>
    /// <param name="physical">The physical model</param>
    /// <param name="fromValues">The from values.</param>
    /// <param name="fromThickness">The from thickness.</param>
    /// <param name="toThickness">To thickness.</param>
    /// <returns></returns>
    private static double[] MapSW(Physical physical, double[] fromValues, double[] fromThickness, double[] toThickness)
    {
        if (fromValues == null || fromThickness == null)
            return null;

        // convert from values to a mass basis with a dummy bottom layer.
        List<double> values = new List<double>();
        values.AddRange(fromValues);
        values.Add(MathUtilities.LastValue(fromValues) * 0.8);
        values.Add(MathUtilities.LastValue(fromValues) * 0.4);
        values.Add(0.0);
        List<double> thickness = new List<double>();
        thickness.AddRange(fromThickness);
        thickness.Add(MathUtilities.LastValue(fromThickness));
        thickness.Add(MathUtilities.LastValue(fromThickness));
        thickness.Add(3000);

        // Get the first crop ll or ll15.
        var firstCrop = (physical as IModel).FindChild<SoilCrop>();
        double[] LowerBound;
        if (physical != null && firstCrop != null)
            LowerBound = SoilUtilities.MapConcentration(firstCrop.LL, physical.Thickness, thickness.ToArray(), MathUtilities.LastValue(firstCrop.LL));
        else
            LowerBound = SoilUtilities.MapConcentration(physical.LL15, physical.Thickness, thickness.ToArray(), physical.LL15.Last());
        if (LowerBound == null)
            throw new Exception("Cannot find crop lower limit or LL15 in soil");

        // Make sure all SW values below LastIndex don't go below CLL.
        int bottomLayer = fromThickness.Length - 1;
        for (int i = bottomLayer + 1; i < thickness.Count; i++)
            values[i] = Math.Max(values[i], LowerBound[i]);

        double[] massValues = MathUtilities.Multiply(values.ToArray(), thickness.ToArray());

        // Convert mass back to concentration and return
        return MathUtilities.Divide(SoilUtilities.MapMass(massValues, thickness.ToArray(), toThickness), toThickness);
    }

    /// <summary>Set the default clay content.</summary>
    /// <param name="physical">The physical model</param>
    /// <param name="i">Layer index.</param>
    /// <param name="value">The value.</param>
    private static void SetDefaultClay(Physical physical, int i, double value)
    {
        physical.ParticleSizeClay[i] = value;
        physical.ParticleSizeClayMetadata[i] = "Calculated";
    }

    /// <summary>Set the default silt content.</summary>
    /// <param name="physical">The physical model</param>
    /// <param name="i">Layer index.</param>
    /// <param name="value">The value.</param>
    private static void SetDefaultSilt(Physical physical, int i, double value)
    {
        physical.ParticleSizeSilt[i] = value;
        physical.ParticleSizeSiltMetadata[i] = "Calculated";
    }

    /// <summary>Set the default sand content.</summary>
    /// <param name="physical">The physical model</param>
    /// <param name="i">Layer index.</param>
    /// <param name="value">The value.</param>
    private static void SetDefaultSand(Physical physical, int i, double value)
    {
        physical.ParticleSizeSand[i] = value;
        physical.ParticleSizeSandMetadata[i] = "Calculated";
    }

    /// <summary>Fills in KL for crop.</summary>
    /// <param name="crop">The crop.</param>
    private static void FillInKLForCrop(SoilCrop crop)
    {
        if (crop.Name == null)
            throw new Exception("Crop has no name");
        int i = StringUtilities.IndexOfCaseInsensitive(cropNames, crop.Name + "Soil");
        if (i != -1)
        {
            var water = crop.Parent as Physical;

            double[] KLs = GetRowOfArray(defaultKLs, i);

            double[] cumThickness = SoilUtilities.ToCumThickness(water.Thickness);
            crop.KL = new double[water.Thickness.Length];
            for (int l = 0; l < water.Thickness.Length; l++)
            {
                bool didInterpolate;
                crop.KL[l] = MathUtilities.LinearInterpReal(cumThickness[l], defaultKLThickness, KLs, out didInterpolate);
                crop.KLMetadata = Enumerable.Repeat("Calculated", water.Thickness.Length).ToArray();
            }
        }
    }

    /// <summary>Gets the row of a 2 dimensional array.</summary>
    /// <param name="array">The array.</param>
    /// <param name="row">The row index</param>
    /// <returns>The values in the specified row.</returns>
    private static double[] GetRowOfArray(double[,] array, int row)
    {
        List<double> values = new List<double>();
        for (int col = 0; col < array.GetLength(1); col++)
            values.Add(array[row, col]);

        return values.ToArray();
    }

    private static string[] cropNames = {"Wheat", "Oats",
                                            "Sorghum", "Barley", "Chickpea", "Mungbean", "Cotton", "Canola",
                                            "PigeonPea", "Maize", "Cowpea", "Sunflower", "Fababean", "Lucerne",
                                            "Lupin", "Lentil", "Triticale", "Millet", "Soybean" };

    private static double[] defaultKLThickness = new double[] { 150, 300, 600, 900, 1200, 1500, 1800 };
    private static double[,] defaultKLs =  {{0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                            {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                            {0.07,   0.07,   0.07,   0.05,   0.05,   0.04,   0.03},
                                            {0.07,   0.07,   0.07,   0.05,   0.05,   0.03,   0.02},
                                            {0.06,   0.06,   0.06,   0.06,   0.06,   0.06,   0.06},
                                            {0.06,   0.06,   0.06,   0.04,   0.04,   0.00,   0.00},
                                            {0.10,   0.10,   0.10,   0.10,   0.09,   0.07,   0.05},
                                            {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                            {0.06,   0.06,   0.06,   0.05,   0.04,   0.02,   0.01},
                                            {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                            {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                            {0.10,   0.10,   0.08,   0.06,   0.04,   0.02,   0.01},
                                            {0.08,   0.08,   0.08,   0.08,   0.06,   0.04,   0.03},
                                            {0.10,   0.10,   0.10,   0.10,   0.09,   0.09,   0.09},
                                            {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                            {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                            {0.07,   0.07,   0.07,   0.04,   0.02,   0.01,   0.01},
                                            {0.07,   0.07,   0.07,   0.05,   0.05,   0.04,   0.03},
                                            {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01}};

    /// <summary>
    /// The black vertosol crop list
    /// </summary>
    private static string[] BlackVertosolCropList = new string[] { "Wheat", "Sorghum", "Cotton" };
    /// <summary>
    /// The grey vertosol crop list
    /// </summary>
    private static string[] GreyVertosolCropList = new string[] { "Wheat", "Sorghum", "Cotton", "Barley", "Chickpea", "Fababean", "Mungbean" };
    /// <summary>
    /// The predicted thickness
    /// </summary>
    private static double[] PredictedThickness = new double[] { 150, 150, 300, 300, 300, 300, 300 };
    /// <summary>
    /// The predicted xf
    /// </summary>
    private static double[] PredictedXF = new double[] { 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00 };
    /// <summary>
    /// The wheat kl
    /// </summary>
    private static double[] WheatKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
    /// <summary>
    /// The sorghum kl
    /// </summary>
    private static double[] SorghumKL = new double[] { 0.07, 0.07, 0.07, 0.05, 0.05, 0.04, 0.03 };
    /// <summary>
    /// The barley kl
    /// </summary>
    private static double[] BarleyKL = new double[] { 0.07, 0.07, 0.07, 0.05, 0.05, 0.03, 0.02 };
    /// <summary>
    /// The chickpea kl
    /// </summary>
    private static double[] ChickpeaKL = new double[] { 0.06, 0.06, 0.06, 0.06, 0.06, 0.06, 0.06 };
    /// <summary>
    /// The mungbean kl
    /// </summary>
    private static double[] MungbeanKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.00, 0.00 };
    /// <summary>
    /// The cotton kl
    /// </summary>
    private static double[] CottonKL = new double[] { 0.10, 0.10, 0.10, 0.10, 0.09, 0.07, 0.05 };
    /// <summary>
    /// The canola kl
    /// </summary>
    /// private static double[] CanolaKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
    /// <summary>
    /// The pigeon pea kl
    /// </summary>
    /// private static double[] PigeonPeaKL = new double[] { 0.06, 0.06, 0.06, 0.05, 0.04, 0.02, 0.01 };
    /// <summary>
    /// The maize kl
    /// </summary>
    /// private static double[] MaizeKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
    /// <summary>
    /// The cowpea kl
    /// </summary>
    /// private static double[] CowpeaKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
    /// <summary>
    /// The sunflower kl
    /// </summary>
    /// private static double[] SunflowerKL = new double[] { 0.01, 0.01, 0.08, 0.06, 0.04, 0.02, 0.01 };
    /// <summary>
    /// The fababean kl
    /// </summary>
    private static double[] FababeanKL = new double[] { 0.08, 0.08, 0.08, 0.08, 0.06, 0.04, 0.03 };
    /// <summary>
    /// The lucerne kl
    /// </summary>
    /// private static double[] LucerneKL = new double[] { 0.01, 0.01, 0.01, 0.01, 0.09, 0.09, 0.09 };
    /// <summary>
    /// The perennial kl
    /// </summary>
    /// private static double[] PerennialKL = new double[] { 0.01, 0.01, 0.01, 0.01, 0.09, 0.07, 0.05 };

    /// <summary>
    ///
    /// </summary>
    private class BlackVertosol
    {
        /// <summary>
        /// The cotton a
        /// </summary>
        internal static double[] CottonA = new double[] { 0.832, 0.868, 0.951, 0.988, 1.043, 1.095, 1.151 };
        /// <summary>
        /// The sorghum a
        /// </summary>
        internal static double[] SorghumA = new double[] { 0.699, 0.802, 0.853, 0.907, 0.954, 1.003, 1.035 };
        /// <summary>
        /// The wheat a
        /// </summary>
        internal static double[] WheatA = new double[] { 0.124, 0.049, 0.024, 0.029, 0.146, 0.246, 0.406 };

        /// <summary>
        /// The cotton b
        /// </summary>
        internal static double CottonB = -0.0070;
        /// <summary>
        /// The sorghum b
        /// </summary>
        internal static double SorghumB = -0.0038;
        /// <summary>
        /// The wheat b
        /// </summary>
        internal static double WheatB = 0.0116;

    }
    /// <summary>
    ///
    /// </summary>
    private class GreyVertosol
    {
        /// <summary>
        /// The cotton a
        /// </summary>
        internal static double[] CottonA = new double[] { 0.853, 0.851, 0.883, 0.953, 1.022, 1.125, 1.186 };
        /// <summary>
        /// The sorghum a
        /// </summary>
        internal static double[] SorghumA = new double[] { 0.818, 0.864, 0.882, 0.938, 1.103, 1.096, 1.172 };
        /// <summary>
        /// The wheat a
        /// </summary>
        internal static double[] WheatA = new double[] { 0.660, 0.655, 0.701, 0.745, 0.845, 0.933, 1.084 };
        /// <summary>
        /// The barley a
        /// </summary>
        internal static double[] BarleyA = new double[] { 0.847, 0.866, 0.835, 0.872, 0.981, 1.036, 1.152 };
        /// <summary>
        /// The chickpea a
        /// </summary>
        internal static double[] ChickpeaA = new double[] { 0.435, 0.452, 0.481, 0.595, 0.668, 0.737, 0.875 };
        /// <summary>
        /// The fababean a
        /// </summary>
        internal static double[] FababeanA = new double[] { 0.467, 0.451, 0.396, 0.336, 0.190, 0.134, 0.084 };
        /// <summary>
        /// The mungbean a
        /// </summary>
        internal static double[] MungbeanA = new double[] { 0.779, 0.770, 0.834, 0.990, 1.008, 1.144, 1.150 };
        /// <summary>
        /// The cotton b
        /// </summary>
        internal static double CottonB = -0.0082;
        /// <summary>
        /// The sorghum b
        /// </summary>
        internal static double SorghumB = -0.007;
        /// <summary>
        /// The wheat b
        /// </summary>
        internal static double WheatB = -0.0032;
        /// <summary>
        /// The barley b
        /// </summary>
        internal static double BarleyB = -0.0051;
        /// <summary>
        /// The chickpea b
        /// </summary>
        internal static double ChickpeaB = 0.0029;
        /// <summary>
        /// The fababean b
        /// </summary>
        internal static double FababeanB = 0.02455;
        /// <summary>
        /// The mungbean b
        /// </summary>
        internal static double MungbeanB = -0.0034;
    }

    /// <summary>
    /// Return a list of predicted crop names or an empty string[] if none found.
    /// </summary>
    /// <returns></returns>
    private static void AddPredictedCrops(IPhysical physical)
    {
        var soil = (physical as IModel).Parent as Soil;
        if (soil.SoilType != null)
        {
            string[] predictedCropNames = null;
            if (soil.ASCOrder == "Vertosol" && soil.ASCSubOrder == "Black")
                soil.SoilType = "Black Vertosol";
            else if (soil.ASCOrder == "Vertosol" && soil.ASCSubOrder == "Grey")
                soil.SoilType = "Grey Vertosol";

            if (soil.SoilType.Equals("Black Vertosol", StringComparison.CurrentCultureIgnoreCase))
                predictedCropNames = BlackVertosolCropList;
            else if (soil.SoilType.Equals("Grey Vertosol", StringComparison.CurrentCultureIgnoreCase))
                predictedCropNames = GreyVertosolCropList;

            if (predictedCropNames != null)
            {
                var water = soil.FindChild<Physical>();
                var crops = water.FindAllChildren<SoilCrop>().ToList();

                foreach (string cropName in predictedCropNames)
                {
                    // if a crop parameterisation already exists for this crop then don't add a predicted one.
                    if (crops.Find(c => c.Name.Equals(cropName + "Soil", StringComparison.InvariantCultureIgnoreCase)) == null)
                        Structure.Add(PredictedCrop(soil, cropName), water);
                }
            }
        }
    }

    /// <summary>
    /// Return a predicted SoilCrop for the specified crop name or null if not found.
    /// </summary>
    /// <param name="soil">The soil.</param>
    /// <param name="CropName">Name of the crop.</param>
    /// <returns></returns>
    private static SoilCrop PredictedCrop(Soil soil, string CropName)
    {
        double[] A = null;
        double B = double.NaN;
        double[] KL = null;

        if (soil.SoilType == null)
            return null;

        if (soil.SoilType.Equals("Black Vertosol", StringComparison.CurrentCultureIgnoreCase))
        {
            if (CropName.Equals("Cotton", StringComparison.CurrentCultureIgnoreCase))
            {
                A = BlackVertosol.CottonA;
                B = BlackVertosol.CottonB;
                KL = CottonKL;
            }
            else if (CropName.Equals("Sorghum", StringComparison.CurrentCultureIgnoreCase))
            {
                A = BlackVertosol.SorghumA;
                B = BlackVertosol.SorghumB;
                KL = SorghumKL;
            }
            else if (CropName.Equals("Wheat", StringComparison.CurrentCultureIgnoreCase))
            {
                A = BlackVertosol.WheatA;
                B = BlackVertosol.WheatB;
                KL = WheatKL;
            }
        }
        else if (soil.SoilType.Equals("Grey Vertosol", StringComparison.CurrentCultureIgnoreCase))
        {
            if (CropName.Equals("Cotton", StringComparison.CurrentCultureIgnoreCase))
            {
                A = GreyVertosol.CottonA;
                B = GreyVertosol.CottonB;
                KL = CottonKL;
            }
            else if (CropName.Equals("Sorghum", StringComparison.CurrentCultureIgnoreCase))
            {
                A = GreyVertosol.SorghumA;
                B = GreyVertosol.SorghumB;
                KL = SorghumKL;
            }
            else if (CropName.Equals("Wheat", StringComparison.CurrentCultureIgnoreCase))
            {
                A = GreyVertosol.WheatA;
                B = GreyVertosol.WheatB;
                KL = WheatKL;
            }
            else if (CropName.Equals("Barley", StringComparison.CurrentCultureIgnoreCase))
            {
                A = GreyVertosol.BarleyA;
                B = GreyVertosol.BarleyB;
                KL = BarleyKL;
            }
            else if (CropName.Equals("Chickpea", StringComparison.CurrentCultureIgnoreCase))
            {
                A = GreyVertosol.ChickpeaA;
                B = GreyVertosol.ChickpeaB;
                KL = ChickpeaKL;
            }
            else if (CropName.Equals("Fababean", StringComparison.CurrentCultureIgnoreCase))
            {
                A = GreyVertosol.FababeanA;
                B = GreyVertosol.FababeanB;
                KL = FababeanKL;
            }
            else if (CropName.Equals("Mungbean", StringComparison.CurrentCultureIgnoreCase))
            {
                A = GreyVertosol.MungbeanA;
                B = GreyVertosol.MungbeanB;
                KL = MungbeanKL;
            }
        }


        if (A == null)
            return null;

        var physical = soil.FindChild<IPhysical>();
        double[] LL = PredictedLL(physical, A, B);
        LL = SoilUtilities.MapConcentration(LL, PredictedThickness, physical.Thickness, LL.Last());
        KL = SoilUtilities.MapConcentration(KL, PredictedThickness, physical.Thickness, KL.Last());
        double[] XF = SoilUtilities.MapConcentration(PredictedXF, PredictedThickness, physical.Thickness, PredictedXF.Last());
        string[] Metadata = StringUtilities.CreateStringArray("Estimated", physical.Thickness.Length);
        LL = MathUtilities.Constrain(LL, physical.LL15, physical.DUL);

        return new SoilCrop()
        {
            Name = CropName + "Soil",
            LL = LL,
            LLMetadata = Metadata,
            KL = KL,
            KLMetadata = Metadata,
            XF = XF,
            XFMetadata = Metadata
        };
    }

    /// <summary>
    /// Calculate and return a predicted LL from the specified A and B values.
    /// </summary>
    /// <param name="physical">The soil physical properties.</param>
    /// <param name="A">a.</param>
    /// <param name="B">The b.</param>
    /// <returns></returns>
    private static double[] PredictedLL(IPhysical physical, double[] A, double B)
    {
        double[] LL15 = SoilUtilities.MapConcentration(physical.LL15, physical.Thickness, PredictedThickness, physical.LL15.Last());
        double[] DUL = SoilUtilities.MapConcentration(physical.DUL, physical.Thickness, PredictedThickness, physical.DUL.Last());
        double[] LL = new double[PredictedThickness.Length];
        for (int i = 0; i != PredictedThickness.Length; i++)
        {
            double DULPercent = DUL[i] * 100.0;
            LL[i] = DULPercent * (A[i] + B * DULPercent);
            LL[i] /= 100.0;

            // Bound the predicted LL values.
            LL[i] = Math.Max(LL[i], LL15[i]);
            LL[i] = Math.Min(LL[i], DUL[i]);
        }

        //  make the top 3 layers the same as the top 3 layers of LL15
        if (LL.Length >= 3)
        {
            LL[0] = LL15[0];
            LL[1] = LL15[1];
            LL[2] = LL15[2];
        }
        return LL;
    }


    /// <summary>Standard thicknesses</summary>
    private static readonly double[] StandardThickness = new double[] { 100, 100, 200, 200, 200, 200, 200 };
    /// <summary>Standard Kls</summary>
    private static readonly double[] StandardKL = new double[] { 0.06, 0.06, 0.04, 0.04, 0.04, 0.04, 0.02 };
}