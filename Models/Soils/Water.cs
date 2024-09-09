using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.APSoil;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models.Soils
{

    /// <summary>
    /// This class encapsulates the water content (initial and current) in the simulation.
    /// </summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.WaterView.glade")]
    [PresenterName("UserInterface.Presenters.WaterPresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class Water : Model
    {
        private double[] volumetric;

        /// <summary>Last initialisation event.</summary>
        public event EventHandler WaterChanged;


        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Display]
        [Summary]
        [Units("mm")]
        [JsonIgnore]
        public string[] Depth
        {
            get
            {
                return SoilUtilities.ToDepthStrings(Thickness);
            }
            set
            {
                Thickness = SoilUtilities.ToThickness(value);
            }
        }

        /// <summary>Thickness</summary>
        public double[] Thickness { get; set; }

        /// <summary>Initial water values</summary>
        [Description("Initial values")]
        [Summary]
        [Units("mm/mm")]
        [Display(Format = "N3")]
        public double[] InitialValues { get; set; }

        /// <summary>Initial values total mm</summary>
        [Summary]
        [Units("mm")]
        public double[] InitialValuesMM => InitialValues == null ? null : MathUtilities.Multiply(InitialValues, Thickness);

        /// <summary>Amount water (mm)</summary>
        [Units("mm")]
        public double[] MM => Volumetric == null ? null : MathUtilities.Multiply(Volumetric, Thickness);

        /// <summary>Amount (mm/mm)</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] Volumetric
        {
            get
            {
                return volumetric;
            }
            set
            {
                volumetric = value;
                WaterChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Soil water potential (kPa)</summary>
        [Units("kPa")]
        [JsonIgnore]
        public double[] Potential
        {
            get
            {
                return MathUtilities.Multiply_Value(WaterModel.PSI, 0.1);
            }
        }

        /// <summary>Soil water potential (kPa)</summary>
        [Units("-")]
        [JsonIgnore]
        public double[] pF
        {
            get
            {
                double[] psi = WaterModel.PSI;
                double[] value = new double[WaterModel.PSI.Length];
                for (int layer = 0; layer < Thickness.Length; layer++)
                {
                    if (psi[layer] < 0.0)
                        value[layer] = Math.Log10(-psi[layer]);
                    else
                        value[layer] = 0;
                }
                return value;
            }
        }

        /// <summary>Soil hydraulic conductivity (mm/d)</summary>
        [Units("mm/d")]
        [JsonIgnore]
        public double[] HydraulicConductivity
        {
            get
            {
                return MathUtilities.Multiply_Value(WaterModel.K, 10 * 24);
            }
        }

        /// <summary>Plant available water (mm).</summary>
        [Units("mm")]
        public double InitialPAWmm
        {
            get
            {
                if (InitialValues == null)
                    return 0;
                double[] values =  MathUtilities.Subtract(InitialValuesMM, RelativeToLLMM);
                if (values != null)
                    return values.Sum();
                return 0;
            }
            set
            {
                if (Physical != null)
                {
                    double[] airdry = SoilUtilities.MapConcentration(Physical.AirDry, Physical.Thickness, Thickness, Physical.AirDry.Last());
                    double[] dul = SoilUtilities.MapConcentration(Physical.DUL, Physical.Thickness, Thickness, Physical.DUL.Last());
                    double[] sat = SoilUtilities.MapConcentration(Physical.SAT, Physical.Thickness, Thickness, Physical.SAT.Last());
                    double[] thickness = Physical.Thickness;

                    if (FilledFromTop)
                        InitialValues = DistributeAmountWaterFromTop(value, thickness, airdry, RelativeToLL, dul, sat, RelativeToXF);
                    else
                        InitialValues = DistributeAmountWaterEvenly(value, thickness, airdry, RelativeToLL, dul, sat, RelativeToXF);
                }
            }
        }

        /// <summary>Plant available water SW-LL15 (mm/mm).</summary>
        [Units("mm/mm")]
        public double[] PAW => APSoilUtilities.CalcPAWC(Physical.Thickness, Physical.LL15, Volumetric, null);

        /// <summary>Plant available water SW-LL15 (mm).</summary>
        [Units("mm")]
        public double[] PAWmm => MathUtilities.Multiply(PAW, Physical.Thickness);

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Reset();
        }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndOfSimulation")]
        private void OnSimulationEnding(object sender, EventArgs e)
        {
            Reset();
        }

        /// <summary>
        /// Set solute to initialisation state
        /// </summary>
        public void Reset()
        {
            if (InitialValues == null)
                throw new Exception("No initial soil water specified.");
            Volumetric = (double[])InitialValues.Clone();
        }

        [JsonIgnore]
        private string relativeToCheck = "LL15";

        /// <summary>The crop name (or LL15) that fraction full is relative to</summary>
        public string RelativeTo
        {
            get => relativeToCheck;
            set
            {
                string newValue = value;
                if (newValue == null)
                    newValue = "LL15";
                // This structure is required to create a 'source of truth' to ensure
                // a stack overflow does not occurs.
                if (relativeToCheck != newValue)
                {
                    double percent = FractionFull;
                    relativeToCheck = newValue;
                    UpdateInitialValuesFromFractionFull(percent);
                }
                else
                {
                    relativeToCheck = newValue;
                }
            }
        }

        /// <summary>Allowed strings in 'RelativeTo' property.</summary>
        public IEnumerable<string> AllowedRelativeTo => (GetAllowedRelativeToStrings());

        [JsonIgnore]
        private bool filledFromTop = false;

        /// <summary>Distribute the water at the top of the profile when setting fraction full.</summary>
        public bool FilledFromTop
        {
            get => filledFromTop;
            set
            {
                double percent = FractionFull;
                filledFromTop = value;
                if(Physical != null)
                    UpdateInitialValuesFromFractionFull(percent);
            }
        }

        /// <summary>Calculate the fraction of the profile that is full.</summary>
        [JsonIgnore]
        public double FractionFull
        {
            get
            {
                if (Physical != null)
                {
                    double newFractionFull;
                    double[] dul = SoilUtilities.MapConcentration(Physical.DUL, Physical.Thickness, Thickness, Physical.DUL.Last());
                    double[] dulMM = MathUtilities.Multiply(dul, Thickness);
                    if (InitialValues == null)
                        return 0;
                    else
                    {
                        if (RelativeTo != "LL15")
                        {
                            //Get layer indices that have a XF as 0.
                            var plantCrop = GetCropSoil();

                            double[] initialValuesMMMinusEmptyXFLayers = MathUtilities.Multiply(plantCrop.XF, InitialValuesMM);
                            double[] relativeToLLMMMinusEmptyXFLayers = MathUtilities.Multiply(plantCrop.XF, RelativeToLLMM);
                            double[] dulMMMinusEmptyXFLayers = MathUtilities.Multiply(plantCrop.XF, dulMM);

                            newFractionFull = MathUtilities.Subtract(initialValuesMMMinusEmptyXFLayers, relativeToLLMMMinusEmptyXFLayers).Sum() /
                                                MathUtilities.Subtract(dulMMMinusEmptyXFLayers, relativeToLLMMMinusEmptyXFLayers).Sum();
                        }
                        else
                        {

                            var paw = MathUtilities.Subtract(InitialValuesMM, RelativeToLLMM);
                            if (paw == null)
                                newFractionFull = 0;
                            else
                                newFractionFull = paw.Sum() / MathUtilities.Subtract(dulMM, RelativeToLLMM).Sum();
                        }

                        return newFractionFull;
                    }
                }
                else return 0;
            }
            set
            {
                UpdateInitialValuesFromFractionFull(value);
            }
        }

        /// <summary>
        /// Updates InitialValues from FractionFull.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void UpdateInitialValuesFromFractionFull(double value)
        {
            if (Physical != null)
            {
                double[] airdry = SoilUtilities.MapConcentration(Physical.AirDry, Physical.Thickness, Thickness, Physical.AirDry.Last());
                double[] dul = SoilUtilities.MapConcentration(Physical.DUL, Physical.Thickness, Thickness, Physical.DUL.Last());
                double[] sat = SoilUtilities.MapConcentration(Physical.DUL, Physical.Thickness, Thickness, Physical.SAT.Last());
                if (FilledFromTop)
                    InitialValues = DistributeWaterFromTop(value, Thickness, airdry, RelativeToLL, dul, sat, RelativeToXF);
                else
                    InitialValues = DistributeWaterEvenly(value, Thickness, airdry, RelativeToLL, dul, sat, RelativeToXF);

                double fraction = FractionFull;
            }
        }

        /// <summary>Calculate the depth of wet soil (mm).</summary>
        [JsonIgnore]
        public double DepthWetSoil
        {
            get
            {
                if (InitialValues == null || InitialValues.Length != Thickness.Length)
                    return 0;
                var ll = RelativeToLL;
                double[] dul = SoilUtilities.MapConcentration(Physical.DUL, Physical.Thickness, Thickness, Physical.DUL.Last());

                double depthSoFar = 0;
                for (int layer = 0; layer < Thickness.Length; layer++)
                {
                    double prop = 0;
                    if (dul[layer] - ll[layer] != 0)
                        prop = (InitialValues[layer] - ll[layer]) / (dul[layer] - ll[layer]);

                    if (MathUtilities.IsGreaterThanOrEqual(prop, 1.0))
                        depthSoFar += Thickness[layer];
                    else
                        depthSoFar += Thickness[layer] * prop;
                }
                return depthSoFar;
            }
            set
            {
                double[] dul = SoilUtilities.MapConcentration(Physical.DUL, Physical.Thickness, Thickness, Physical.DUL.Last());
                InitialValues = DistributeToDepthOfWetSoil(value, Thickness, RelativeToLL, dul);
            }
        }

        /// <summary>Finds the 'Physical' node.</summary>
        public IPhysical Physical => FindAncestor<Soil>()?.FindDescendant<IPhysical>() ?? FindInScope<IPhysical>();

        /// <summary>Finds the 'SoilWater' node.</summary>
        public ISoilWater WaterModel => FindAncestor<Soil>()?.FindDescendant<ISoilWater>() ?? FindInScope<ISoilWater>();

        /// <summary>Find LL values (mm) for the RelativeTo property.</summary>
        public double[] RelativeToLL
        {
            get
            {
                double[] values;
                if (RelativeTo == "LL15")
                    values = Physical.LL15;
                else
                {
                    var plantCrop = GetCropSoil();
                    if (plantCrop == null)
                    {
                        RelativeTo = "LL15";
                        values = Physical.LL15;
                    }
                    else
                        values = plantCrop.LL;
                }

                return SoilUtilities.MapConcentration(values, Physical.Thickness, Thickness, values.Last());
            }
        }

        /// <summary>Find LL values (mm) for the RelativeTo property.</summary>
        private double[] RelativeToLLMM => MathUtilities.Multiply(RelativeToLL, Thickness);

        /// <summary>Find LL values (mm) for the RelativeTo property.</summary>
        private double[] RelativeToXF
        {
            get
            {
                if (RelativeTo != "LL15")
                {
                    SoilCrop plantCrop = GetCropSoil();
                    if (plantCrop != null)
                        return SoilUtilities.MapConcentration(plantCrop.XF, Physical.Thickness, Thickness, plantCrop.XF.Last());
                }
                return Enumerable.Repeat(1.0, Thickness.Length).ToArray();
            }
        }

        /// <summary>Distribute water from the top of the profile using a fraction full.</summary>
        /// <param name="fractionFull">The fraction to fill the profile to.</param>
        /// <param name="thickness">Layer thickness (mm).</param>
        /// <param name="airdry">Airdry</param>
        /// <param name="ll">Relative ll (ll15 or crop ll).</param>
        /// <param name="dul">Drained upper limit.</param>
        /// <param name="xf">XF.</param>
        /// <param name="sat">SAT figures from Water's Physical model sibling.</param>
        /// <returns>A double array of volumetric soil water values (mm/mm)</returns>
        public static double[] DistributeWaterFromTop(double fractionFull, double[] thickness, double[] airdry, double[] ll, double[] dul, double[] sat, double[] xf)
        {
            double[] pawcmm = MathUtilities.Multiply(MathUtilities.Subtract(dul, ll), thickness);
            pawcmm = MathUtilities.Multiply(xf, pawcmm);

            double amountWater = MathUtilities.Sum(pawcmm) * fractionFull;
            return DistributeAmountWaterFromTop(amountWater, thickness, airdry, ll, dul, sat, xf);
        }

        private enum FillFlag { AirDry, DUL, SAT }

        /// <summary>Distribute amount of water from the top of the profile.</summary>
        /// <param name="amountWater">The amount of water to fill the profile to.</param>
        /// <param name="thickness">Layer thickness (mm).</param>
        /// <param name="airdry"></param>
        /// <param name="ll">Relative ll (ll15 or crop ll).</param>
        /// <param name="dul">Drained upper limit.</param>
        /// <param name="xf">XF.</param>
        /// <param name="sat">SATmm figures from Physical model.</param>
        /// <param name="sw">Pass in an optional sw table</param>
        /// <returns>A double array of volumetric soil water values (mm/mm)</returns>
        private static double[] DistributeAmountWaterFromTop(double amountWater, double[] thickness, double[] airdry, double[] ll, double[] dul, double[] sat, double[] xf, double[] sw = null)
        {
            double waterAmount = amountWater;
            double[] soilWater = new double[thickness.Length];

            double[] airDryToLL = MathUtilities.Subtract(ll, airdry);
            double[] llToDul = MathUtilities.Subtract(dul, ll);
            double[] dulToSat = MathUtilities.Subtract(sat, dul);

            FillFlag flag = FillFlag.DUL;

            if (sw != null) //this means we are filling past DUL
            {
                soilWater = sw;
                flag = FillFlag.SAT;
            }
            else if (amountWater < 0) //filling to airdry
            {
                waterAmount = -waterAmount;
                flag = FillFlag.AirDry;
            }

            double[] pawcmm = new double[thickness.Length];
            if (flag == FillFlag.AirDry)
                pawcmm = MathUtilities.Multiply(airDryToLL, thickness);
            else if (flag == FillFlag.DUL)
                pawcmm = MathUtilities.Multiply(llToDul, thickness);
            else if (flag == FillFlag.SAT)
                pawcmm = MathUtilities.Multiply(dulToSat, thickness);

            pawcmm = MathUtilities.Multiply(xf, pawcmm);

            for (int layer = 0; layer < thickness.Length; layer++)
            {
                double prop = 1;
                if (pawcmm[layer] == 0)
                    prop = 1;
                else if (waterAmount < pawcmm[layer])
                    prop = waterAmount / pawcmm[layer];

                if (flag == FillFlag.AirDry)
                    soilWater[layer] = ll[layer] - (prop * airDryToLL[layer] * xf[layer]);
                else if (flag == FillFlag.DUL)
                    soilWater[layer] = ll[layer] + (prop * llToDul[layer] * xf[layer]);
                else if (flag == FillFlag.SAT)
                    soilWater[layer] = ll[layer] + (llToDul[layer] * xf[layer]) + (prop * dulToSat[layer] * xf[layer]);

                waterAmount = waterAmount - pawcmm[layer];
                if (waterAmount < 0)
                    waterAmount = 0;
            }
            // If there is still water left fill the layers to SAT, starting from the top.
            if (flag == FillFlag.DUL && waterAmount > 0)
                soilWater = DistributeAmountWaterFromTop(waterAmount, thickness, airdry, ll, dul, sat, xf, soilWater);

            return soilWater;
        }


        /// <summary>
        /// Calculate a layered soil water using a FractionFull and evenly distributed. Units: mm/mm
        /// </summary>
        /// <param name="amountWater"></param>
        /// <param name="thickness"></param>
        /// <param name="airdry"></param>
        /// <param name="ll">Relative ll (ll15 or crop ll).</param>
        /// <param name="dul">Drained upper limit.</param>
        /// <param name="sat"></param>
        /// <param name="xf"></param>
        /// <returns>A double array of volumetric soil water values (mm/mm)</returns>
        public static double[] DistributeAmountWaterEvenly(double amountWater, double[] thickness, double[] airdry, double[] ll, double[] dul, double[] sat, double[] xf)
        {
            //returned array
            double[] sw = new double[ll.Length];

            double[] airdryThick = MathUtilities.Multiply(airdry, thickness);
            double[] llThick = MathUtilities.Multiply(ll, thickness);
            double[] dulThick = MathUtilities.Multiply(dul, thickness);
            double[] satThick = MathUtilities.Multiply(sat, thickness);

            double[] airdryToll = MathUtilities.Subtract(llThick, airdryThick);
            airdryToll = MathUtilities.Multiply(xf, airdryToll);
            double[] airdryThickInverse = MathUtilities.Add(llThick, airdryToll);

            double[] lltosat = MathUtilities.Subtract(satThick, llThick);
            lltosat = MathUtilities.Multiply(xf, lltosat);
            satThick = MathUtilities.Add(llThick, lltosat);

            double[] llToDul = MathUtilities.Subtract(dulThick, llThick);
            dulThick = MathUtilities.Multiply(xf, llToDul);

            //variables so same code can be used for both SAT and Airdry
            FillFlag flag = FillFlag.DUL;
            double[] max = satThick;
            double waterAmount = amountWater;
            if (waterAmount < 0)
            {
                waterAmount = -waterAmount;
                flag = FillFlag.AirDry;
                max = airdryThickInverse;
            }

            //store excess water over SAT or under airdry
            double excessWater = 0;

            //fill to DUL or airdry based on how much water is held in ll to dul
            for (int layer = 0; layer < sw.Length; layer++)
            {
                double waterForLayer = waterAmount * (dulThick[layer] / MathUtilities.Sum(dulThick));
                sw[layer] = llThick[layer] + waterForLayer;
                if (sw[layer] > max[layer])
                {
                    excessWater += sw[layer] - max[layer];
                    sw[layer] = max[layer];
                }
            }

            //if there is more water than a ll to dul layer can hold, spread the excess out across the other layers
            while (excessWater > 0)
            {
                //determine how many layers are full to SAT
                int fullLayers = 0;
                for (int layer = 0; layer < sw.Length; layer++)
                    if (sw[layer] >= max[layer])
                        fullLayers += 1;

                //put excess water into layers that aren't full
                if (fullLayers < sw.Length)
                {
                    //spilt water across non-full layers
                    double water = (excessWater / (sw.Length - fullLayers));

                    //reset excess water
                    excessWater = 0;
                    for (int layer = 0; layer < sw.Length; layer++)
                    {
                        if (sw[layer] < max[layer]) //only do unfilled layers
                        {
                            sw[layer] += water;
                            if (sw[layer] > max[layer])
                            {
                                excessWater += sw[layer] - max[layer];
                                sw[layer] = max[layer];
                            }
                        }
                    }
                }
                else
                {
                    excessWater = 0;
                }
            }

            //if going to airdry, invert the result back to the left again
            if (flag == FillFlag.AirDry)
                sw = MathUtilities.Subtract(llThick, MathUtilities.Subtract(sw, llThick));

             return MathUtilities.Divide(sw, thickness);
        }

        /// <summary>
        /// Calculate a layered soil water using an amount of water and evenly distributed. Units: mm/mm
        /// </summary>
        /// <param name="fractionFull"></param>
        /// <param name="thickness">Layer thickness (mm).</param>
        /// <param name="airdry"></param>
        /// <param name="ll">Relative ll (ll15 or crop ll).</param>
        /// <param name="dul">Drained upper limit.</param>
        /// <param name="sat"></param>
        /// <param name="xf"></param>
        /// <returns>A double array of volumetric soil water values (mm/mm)</returns>
        public static double[] DistributeWaterEvenly(double fractionFull, double[] thickness, double[] airdry, double[] ll, double[] dul, double[] sat, double[] xf)
        {
            double[] pawcmm = MathUtilities.Multiply(MathUtilities.Subtract(dul, ll), thickness);
            pawcmm = MathUtilities.Multiply(xf, pawcmm);

            double amountWater = MathUtilities.Sum(pawcmm) * fractionFull;
            return DistributeAmountWaterEvenly(amountWater, thickness, airdry, ll, dul, sat, xf);
        }

        /// <summary>
        /// Calculate a layered soil water using a depth of wet soil.
        /// </summary>
        /// <param name="depthOfWetSoil">Depth of wet soil (mm)</param>
        /// <param name="thickness">Layer thickness (mm).</param>
        /// <param name="ll">Relative ll (ll15 or crop ll).</param>
        /// <param name="dul">Drained upper limit.</param>
        /// <returns>A double array of volumetric soil water values (mm/mm)</returns>
        public static double[] DistributeToDepthOfWetSoil(double depthOfWetSoil, double[] thickness, double[] ll, double[] dul)
        {
            double[] sw = new double[thickness.Length];
            double depthSoFar = 0;
            for (int layer = 0; layer < thickness.Length; layer++)
            {
                if (depthOfWetSoil > depthSoFar + thickness[layer])
                {
                    sw[layer] = dul[layer];
                }
                else
                {
                    double prop = Math.Max(depthOfWetSoil - depthSoFar, 0) / thickness[layer];
                    sw[layer] = (prop * (dul[layer] - ll[layer])) + ll[layer];
                }

                depthSoFar += thickness[layer];
            }
            return sw;
        }


        /// <summary>
        /// Get all soil crop names as strings from the relevant Soil this water node is a child of as well as LL15 (default value).
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetAllowedRelativeToStrings()
        {
            IEnumerable<string> results = new List<string>();
            IEnumerable<SoilCrop> ancestorSoilCropLists = new List<SoilCrop>();
            // LL15 is here as this is the default value.
            List<string> newSoilCropNames = new List<string> { "LL15" };
            Soil ancestorSoil = FindAncestor<Soil>();
            if (ancestorSoil != null)
            {
                ancestorSoilCropLists = ancestorSoil.FindAllDescendants<SoilCrop>();
                newSoilCropNames.AddRange(ancestorSoilCropLists.Select(s => s.Name.Replace("Soil", "")));
            }
            return newSoilCropNames;
        }

        /// <summary>
        /// Checks to make sure every InitialValue value is within airdry and SAT values.
        /// </summary>
        public bool AreInitialValuesWithinPhysicalBoundaries()
        {
            if (this.Physical == null)
                throw new Exception("To check boundaries of InitialValues Physical must not be null.");

            if (InitialValues.Length != Thickness.Length)
                return false;

            var mappedInitialValues = SoilUtilities.MapConcentration(InitialValues, Thickness, Physical.Thickness, MathUtilities.LastValue(Physical.LL15));

            for (int i = 0; i < mappedInitialValues.Length; i++)
            {
                if (mappedInitialValues[i] < Physical.AirDry[i] || mappedInitialValues[i] > Physical.SAT[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Attempts to get an appropriate SoilCrop.
        /// </summary>
        /// <exception cref="Exception"></exception>
        private SoilCrop GetCropSoil()
        {
            var physical = FindSibling<Physical>();
            if (physical == null)
                physical = FindInScope<Physical>();
                if (physical == null)
                    throw new Exception($"Unable to locate a Physical node when updating {this.Name}.");
            var plantCrop = physical.FindChild<SoilCrop>(RelativeTo + "Soil");
            if (plantCrop == null)
                throw new Exception($"Unable to locate an appropriate SoilCrop with the name of {RelativeTo + "Soil"} under {physical.Name}.");
            return plantCrop;
        }
    }
}
