using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
namespace APSIM.Shared.Utilities
{
    /// <summary>Various soil utilities.</summary>
    public class SoilUtilities
    {
        /// <summary>Convert the specified thicknesses to mid points for plotting.</summary>
        /// <param name="Thickness">The thicknesses.</param>
        static public double[] ToMidPoints(double[] Thickness)
        {
            double[] CumThickness = ToCumThickness(Thickness);
            double[] MidPoints = new double[CumThickness.Length];
            for (int Layer = 0; Layer != CumThickness.Length; Layer++)
            {
                if (Layer == 0)
                    MidPoints[Layer] = CumThickness[Layer] / 2.0;
                else
                    MidPoints[Layer] = (CumThickness[Layer] + CumThickness[Layer - 1]) / 2.0;
            }
            return MidPoints;
        }

        /// <summary>Returns a cumulative thickness based on the specified thickness.</summary>
        /// <param name="Thickness">The thickness.</param>
        static public double[] ToCumThickness(double[] Thickness)
        {
            // ------------------------------------------------
            // Return cumulative thickness for each layer - mm
            // ------------------------------------------------
            double[] CumThickness = new double[Thickness.Length];
            if (Thickness.Length > 0)
            {
                CumThickness[0] = Thickness[0];
                for (int Layer = 1; Layer != Thickness.Length; Layer++)
                    CumThickness[Layer] = Thickness[Layer] + CumThickness[Layer - 1];
            }
            return CumThickness;
        }

        /// <summary>Returns an array that gives the proportion of each layer contributing to a given depth.</summary>
        /// <param name="Thickness">The thickness.</param>
        /// <param name="GivenDepth">The supplied depth (mm).</param>
        static public double[] ProportionOfCumThickness(double[] Thickness, double GivenDepth)
        {
            // ------------------------------------------------
            // Return the proportion of the layer contributing to Given Depth - mm/mm
            // ------------------------------------------------
            
            // find the layer in which GivenDepth lies
            int GivenDepthLayer = LayerIndexOfClosestDepth(Thickness, GivenDepth);
            
            double[] ProportionOfCumThickness = new double[Thickness.Length];
            double[] CumThickness = ToCumThickness(Thickness);

            for (int i = 0; i < Thickness.Length; i++)
            {
                if (i < GivenDepthLayer)
                    ProportionOfCumThickness[i] = Thickness[i] / GivenDepth;  // the entire layer in in the target depth
                else if (i == GivenDepthLayer)
                    if (i == 0)
                        ProportionOfCumThickness[i] = (GivenDepth - 0) / GivenDepth;
                    else
                        ProportionOfCumThickness[i] = (GivenDepth - CumThickness[i-1]) / GivenDepth;  
                else
                    ProportionOfCumThickness[i] = 0.0;
            }
            return ProportionOfCumThickness;
        }

        /// <summary>Return the index of the layer that contains the specified depth.</summary>
        /// <param name="thickness">The soil layer thicknesses.</param>
        /// <param name="depth">The depth to search for.</param>
        /// <returns></returns>
        static public int LayerIndexOfDepth(double[] thickness, double depth)
        {
            if (depth > thickness.Sum())
                throw new Exception("Depth deeper than bottom of soil profile");
            else
                return LayerIndexOfClosestDepth(thickness, depth);
        }

        /// <summary>Return the index of the closest layer that contains the specified depth.</summary>
        /// <param name="thickness">The soil layer thicknesses.</param>
        /// <param name="depth">The depth to search for.</param>
        /// <returns></returns>
        static public int LayerIndexOfClosestDepth(double[] thickness, double depth)
        {
            double CumDepth = 0;
            for (int i = 0; i < thickness.Length; i++)
            {
                CumDepth = CumDepth + thickness[i];
                if (CumDepth >= depth) { return i; }
            }
            throw new Exception("Depth deeper than bottom of soil profile");
        }

        /// <summary>Returns the proportion that 'depth' is through the layer.</summary>
        /// <param name="thickness">Soil layer thickness.</param>
        /// <param name="layerIndex">The layer index</param>
        /// <param name="depth">The depth</param>
        public static double ProportionThroughLayer(double[] thickness, int layerIndex, double depth)
        {
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            for (int i = 0; i <= layerIndex; i++)
                depth_to_layer_bottom += thickness[i];

            double depth_to_layer_top = depth_to_layer_bottom - thickness[layerIndex];
            double depth_to_root = Math.Min(depth_to_layer_bottom, depth);
            double depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);

            return depth_of_root_in_layer / thickness[layerIndex];
        }

        
        /// <summary>Keep the top x mm of soil and zero the rest.</summary>
        /// <param name="values">The layered values.</param>
        /// <param name="thickness">Soil layer thickness.</param>
        /// <param name="depth">The depth of soil to keep</param>
        public static double[] KeepTopXmm(IReadOnlyList<double> values, double[] thickness, double depth)
        {
            double[] returnValues = values.ToArray();
            for (int i = 0; i < thickness.Length; i++)
            {
                double proportion = ProportionThroughLayer(thickness, i, depth);
                returnValues[i] *= proportion;
            }
            return returnValues;
        }        

        /// <summary>Calculate conversion factor from kg/ha to ppm (mg/kg)</summary>
        /// <param name="thickness">Soil layer thickness.</param>
        /// <param name="bd">Bulk density.</param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double[] kgha2ppm(double[] thickness, double[] bd, double[] values)
        {
            if (values == null)
                return null;

            double[] ppm = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
                ppm[i] = values[i] * (100.0 / (bd[i] * thickness[i]));
            return ppm;
        }

        /// <summary>Calculate conversion factor from ppm to kg/ha</summary>
        /// <param name="thickness">Soil layer thickness.</param>
        /// <param name="bd">Bulk density.</param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double[] ppm2kgha(double[] thickness, double[] bd, double[] values)
        {
            if (values == null)
                return null;

            double[] kgha = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
                kgha[i] = values[i] * (bd[i] * thickness[i] / 100);
            return kgha;
        }

        /// <summary>Convert an array of thickness (mm) to depth strings (cm)</summary>
        /// <param name="Thickness">The thickness.</param>
        /// <returns></returns>
        public static string[] ToDepthStringsCM(double[] Thickness)
        {
            return ToDepthStrings(MathUtilities.Divide_Value(Thickness, 10.0));
        }

        /// <summary>Convert an array of thickness (mm) to depth strings (cm)</summary>
        /// <param name="Thickness">The thickness.</param>
        /// <returns></returns>
        public static string[] ToDepthStrings(double[] Thickness)
        {
            if (Thickness == null)
                return null;
            string[] Strings = new string[Thickness.Length];
            double DepthSoFar = 0;
            for (int i = 0; i != Thickness.Length; i++)
            {
                if (Thickness[i] == MathUtilities.MissingValue)
                    Strings[i] = "";
                else
                {
                    double ThisThickness = Thickness[i];
                    double TopOfLayer = DepthSoFar;
                    double BottomOfLayer = DepthSoFar + ThisThickness;

                    TopOfLayer = Math.Round(TopOfLayer, 1);
                    BottomOfLayer = Math.Round(BottomOfLayer, 1);

                    Strings[i] = TopOfLayer.ToString() + "-" + BottomOfLayer.ToString();
                    DepthSoFar = BottomOfLayer;
                }
            }
            return Strings;
        }

        /// <summary>
        /// Convert an array of depth strings (cm) to thickness (mm) e.g.
        /// "0-10", "10-30"
        /// To
        /// 100, 200
        /// </summary>
        /// <param name="depthStrings">The depth strings.</param>
        /// <returns></returns>
        public static double[] ToThicknessCM(string[] depthStrings)
        {
            return MathUtilities.Multiply_Value(ToThickness(depthStrings), 10);
        }

        /// <summary>
        /// Convert an array of depth strings (mm) to thickness (mm) e.g.
        /// "0-100", "10-300"
        /// To
        /// 100, 200
        /// </summary>
        /// <param name="depthStrings">The depth strings.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Invalid layer string:  + DepthStrings[i] +
        ///                                   . String must be of the form: 10-30</exception>
        public static double[] ToThickness(string[] depthStrings)
        {
            if (depthStrings == null)
                return null;
            depthStrings = depthStrings.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            double[] Thickness = new double[depthStrings.Length];
            for (int i = 0; i != depthStrings.Length; i++)
            {
                if (string.IsNullOrEmpty(depthStrings[i]))
                    Thickness[i] = MathUtilities.MissingValue;
                else
                {
                    int PosDash = depthStrings[i].IndexOf('-');
                    if (PosDash == -1)
                        throw new Exception("Invalid layer string: " + depthStrings[i] +
                                  ". String must be of the form: 10-30");
                    double TopOfLayer;
                    double BottomOfLayer;

                    if (!Double.TryParse(depthStrings[i].Substring(0, PosDash), out TopOfLayer))
                        throw new Exception("Invalid string for layer top: '" + depthStrings[i].Substring(0, PosDash) + "'");
                    if (!Double.TryParse(depthStrings[i].Substring(PosDash + 1), out BottomOfLayer))
                        throw new Exception("Invalid string for layer bottom: '" + depthStrings[i].Substring(PosDash + 1) + "'");
                    Thickness[i] = (BottomOfLayer - TopOfLayer);
                }
            }
            return Thickness;
        }

        /// <summary>
        /// Plant available water for the specified crop. Will throw if crop not found. Units: mm/mm
        /// </summary>
        /// <param name="Thickness">The thickness.</param>
        /// <param name="LL">The ll.</param>
        /// <param name="DUL">The dul.</param>
        /// <param name="XF">The xf.</param>
        /// <returns></returns>
        public static double[] CalcPAWC(double[] Thickness, double[] LL, double[] DUL, double[] XF)
        {
            double[] PAWC = new double[Thickness.Length];
            if (LL == null || DUL == null)
                return PAWC;
            if (Thickness.Length != DUL.Length || Thickness.Length != LL.Length)
                return PAWC;
            for (int layer = 0; layer != Thickness.Length; layer++)
                if (DUL[layer] == MathUtilities.MissingValue ||
                    LL[layer] == MathUtilities.MissingValue)
                    PAWC[layer] = 0;
                else
                    PAWC[layer] = Math.Max(DUL[layer] - LL[layer], 0.0);

            bool ZeroXFFound = false;
            for (int layer = 0; layer != Thickness.Length; layer++)
                if (ZeroXFFound || XF != null && XF[layer] == 0)
                {
                    ZeroXFFound = true;
                    PAWC[layer] = 0;
                }
            return PAWC;
        }

        /// <summary>
        /// Computes the water filled pore space for the entire profile.
        /// </summary>
        /// <param name="sw">Layered sw content.</param>
        /// <param name="sat">Layered sat.</param>
        /// <param name="dul">Layered dul.</param>
        /// <returns>Layered wfps.</returns>
        public static double[] WFPS(double[] sw, double[] sat, double[] dul)
        {
            return sw.Zip(sat, dul).Select(layerWfps).ToArray();

            static double layerWfps((double sw, double sat, double dul) layer)
            {
                if (layer.sw < layer.dul)
                    return 0;
                if (layer.sw > layer.sat)
                    return 1;
                return MathUtilities.Divide(layer.sw - layer.dul, layer.sat - layer.dul, 0.0);
            }
        }

        /// <summary>
        /// Convert organic carbon Walkley Black to Total %.
        /// </summary>
        /// <param name="values">Values to convert.</param>
        public static double[] OCWalkleyBlackToTotal(double[] values)
        {
            return MathUtilities.Multiply_Value(values, 1.3);
        }

        /// <summary>
        /// Convert organic carbon Total % to Walkley Black.
        /// </summary>
        /// <param name="values">Values to convert.</param>
        public static double[] OCTotalToWalkleyBlack(double[] values)
        {
            return MathUtilities.Divide_Value(values, 1.3);
        }

        /// <summary>
        /// Converts PH. CaCl2 to 1:5 water.
        /// </summary>
        /// <param name="values">Values to convert.</param>
        public static double[] PHCaCl2ToWater(double[] values)
        {
            // pH in water = (pH in CaCl X 1.1045) - 0.1375
            return MathUtilities.Subtract_Value(MathUtilities.Multiply_Value(values, 1.1045), 0.1375);
        }

        /// <summary>
        /// Gets PH. Units: (1:5 water)
        /// </summary>
        public static double[] PHWaterToCaCl2(double[] values)
        {
            // pH in CaCl = (pH in water + 0.1375) / 1.1045
            return MathUtilities.Divide_Value(MathUtilities.AddValue(values, 0.1375), 1.1045);
        }

        /// <summary>Map soil variables (using concentration) from one layer structure to another.</summary>
        /// <param name="fromValues">The from values.</param>
        /// <param name="fromThickness">The from thickness.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <param name="defaultValueForBelowProfile">The default value for below profile.</param>
        /// <param name="allowMissingValues">Tolerate missing values (double.NaN)?</param>
        /// <returns></returns>
        public static double[] MapConcentration(double[] fromValues, double[] fromThickness,
                                                  double[] toThickness,
                                                  double defaultValueForBelowProfile,
                                                  bool allowMissingValues = false)
        {
            if (fromValues != null && !MathUtilities.AreEqual(fromThickness, toThickness))
            {
                if (fromValues.Length != fromThickness.Length && !allowMissingValues)
                    throw new Exception($"In MapConcentration, the number of values ({fromValues.Length}) doesn't match the number of thicknesses ({fromThickness.Length}).");
                if (fromValues == null || fromThickness == null)
                    return null;

                // convert from values to a mass basis with a dummy bottom layer.
                List<double> values = new List<double>();
                List<double> thickness = new List<double>();
                for (int i = 0; i < fromValues.Length; i++)
                {
                    if (!allowMissingValues && double.IsNaN(fromValues[i]))
                        break;

                    values.Add(fromValues[i]);
                    thickness.Add(fromThickness[i]);
                }

                values.Add(defaultValueForBelowProfile);
                thickness.Add(30000);
                double[] massValues = MathUtilities.Multiply(values.ToArray(), thickness.ToArray());

                double[] newValues = MapMass(massValues, thickness.ToArray(), toThickness, allowMissingValues);

                // Convert mass back to concentration and return
                if (newValues != null)
                    newValues = MathUtilities.Divide(newValues, toThickness);
                return newValues;
            }
            return fromValues;
        }

        /// <summary>Map soil variables from one layer structure to another.</summary>
        /// <param name="fromValues">The f values.</param>
        /// <param name="fromThickness">The f thickness.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <param name="allowMissingValues">Tolerate missing values (double.NaN)?</param>
        /// <returns>The from values mapped to the specified thickness</returns>
        public static double[] MapMass(IReadOnlyList<double> fromValues, double[] fromThickness, double[] toThickness,
                                       bool allowMissingValues = false)
        {
            if (fromValues == null || fromThickness == null || toThickness == null)
                return null;

            double[] FromThickness = MathUtilities.RemoveMissingValuesFromBottom((double[])fromThickness.Clone());
            double[] FromValues = fromValues.ToArray();

            if (FromValues == null)
                return null;

            if (!allowMissingValues)
            {
                // remove missing layers.
                for (int i = 0; i < FromValues.Length; i++)
                {
                    if (double.IsNaN(FromValues[i]) || i >= FromThickness.Length || double.IsNaN(FromThickness[i]))
                    {
                        FromValues[i] = double.NaN;
                        if (i == FromThickness.Length)
                            Array.Resize(ref FromThickness, i + 1);
                        FromThickness[i] = double.NaN;
                    }
                }
                FromValues = MathUtilities.RemoveMissingValuesFromBottom(FromValues);
                FromThickness = MathUtilities.RemoveMissingValuesFromBottom(FromThickness);
            }

            if (MathUtilities.AreEqual(FromThickness, toThickness))
                return FromValues;

            if (FromValues.Length != FromThickness.Length)
                return null;

            // Remapping is achieved by first constructing a map of
            // cumulative mass vs depth
            // The new values of mass per layer can be linearly
            // interpolated back from this shape taking into account
            // the rescaling of the profile.

            double[] CumDepth = new double[FromValues.Length + 1];
            double[] CumMass = new double[FromValues.Length + 1];
            CumDepth[0] = 0.0;
            CumMass[0] = 0.0;
            for (int Layer = 0; Layer < FromThickness.Length; Layer++)
            {
                CumDepth[Layer + 1] = CumDepth[Layer] + FromThickness[Layer];
                CumMass[Layer + 1] = CumMass[Layer] + FromValues[Layer];
            }

            //look up new mass from interpolation pairs
            double[] ToMass = new double[toThickness.Length];
            for (int Layer = 1; Layer <= toThickness.Length; Layer++)
            {
                double LayerBottom = MathUtilities.Sum(toThickness, 0, Layer, 0.0);
                double LayerTop = LayerBottom - toThickness[Layer - 1];
                bool DidInterpolate;
                double CumMassTop = MathUtilities.LinearInterpReal(LayerTop, CumDepth,
                    CumMass, out DidInterpolate);
                double CumMassBottom = MathUtilities.LinearInterpReal(LayerBottom, CumDepth,
                    CumMass, out DidInterpolate);
                ToMass[Layer - 1] = CumMassBottom - CumMassTop;
            }

            if (!allowMissingValues)
            {
                for (int i = 0; i < ToMass.Length; i++)
                    if (double.IsNaN(ToMass[i]))
                        ToMass[i] = 0.0;
            }

            return ToMass;
        }

        /// <summary>Map soil variables (using concentration) from one layer structure to another.</summary>
        /// <param name="fromValues">The from values.</param>
        /// <param name="fromThickness">The from thickness.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <param name="allowMissingValues">Tolerate missing values (double.NaN)?</param>
        /// <returns></returns>
        public static double[] MapInterpolation(double[] fromValues, double[] fromThickness,
                                                double[] toThickness,
                                                bool allowMissingValues = false)
        {
            if (fromValues != null && !MathUtilities.AreEqual(fromThickness, toThickness))
            {
                if (fromValues.Length != fromThickness.Length && !allowMissingValues)
                    throw new Exception($"In MapInterpolation, the number of values ({fromValues.Length}) doesn't match the number of thicknesses ({fromThickness.Length}).");
                if (fromValues == null || fromThickness == null)
                    return null;

                double[] fromThicknessMidPoints = SoilUtilities.ToMidPoints(fromThickness);
                List<double> values = new List<double>();
                List<double> thickness = new List<double>();
                for (int i = 0; i < fromValues.Length; i++)
                {
                    if (!allowMissingValues && double.IsNaN(fromValues[i]))
                        break;
                    if (!double.IsNaN(fromValues[i]))
                    {
                        values.Add(fromValues[i]);
                        thickness.Add(fromThicknessMidPoints[i]);
                    }
                }

                double[] toThicknessMidPoints = SoilUtilities.ToMidPoints(toThickness);
                double[] newValues = new double[toThickness.Length];
                for (int i = 0; i != toThickness.Length; i++)
                    newValues[i] = MathUtilities.LinearInterpReal(toThicknessMidPoints[i], thickness.ToArray(), values.ToArray(), out bool didInterpolate);
                return newValues;
            }
            return fromValues;
        }

        /// <summary>
        /// Fill in missing values in an array, updating metadata to reflect any infilled values.
        /// </summary>
        /// <param name="values">The values to check.</param>
        /// <param name="valuesMetadata">The metadata to update.</param>
        /// <param name="numValues">The number of values expected.</param>
        /// <param name="f">The function to call to get a missing value.</param>
        public static (double[] values, string[] metadata) FillMissingValues(double[] values, string[] valuesMetadata, int numValues, Func<int, double> f)
        {
            double[] newValues = MathUtilities.SetArrayOfCorrectSize(values, numValues).ToArray();
            for (int i = 0; i < numValues; i++)
            {
                if (i >= newValues.Length || double.IsNaN(newValues[i])) 
                    newValues[i] = f(i);
            }
            return (newValues, DetermineMetadata(values, valuesMetadata, newValues, "Calculated"));
        }

        /// <summary>
        /// Examine 2 arrays of numbers (values1 and values2) and look for changed values.
        /// If a value is changed then return null metadata for that value. If a value
        /// isn't modified then try and return the metadata1 value, otherwise null.
        /// </summary>
        /// <remarks>
        ///     values1  metadata1  values2
        ///       10         null       10
        ///       20         calc       25
        ///       30         calc       30
        /// 
        ///     metadata2
        ///        null
        ///        null
        ///        calc
        ///        
        /// </remarks>
        /// <param name="values1">The original values.</param>
        /// <param name="metadata1">Metadata for the original values.</param>
        /// <param name="values2">The potentially user modified values.</param>
        /// <param name="metaDataForModifedValue">The metadata to use for modified values</param>
        /// <returns>Metadata for values2.</returns>
        public static string[] DetermineMetadata(double[] values1, string[] metadata1, double[] values2, string metaDataForModifedValue)
        {
            if (values1 == null || values2 == null)
            {
                if (metaDataForModifedValue == null)
                    return null; // All data has been modified so metadata is cleared.
                else
                    return Enumerable.Repeat(metaDataForModifedValue, values2.Length).ToArray();
            }
            else
            {
                // Create a return metadata array.
                List<string> metadataValues = new();

                // Detect if values have been changed and updated metadata accordingly.
                for (int i = 0; i < values2.Length; i++)
                {
                    if (i >= values1.Length)
                        metadataValues.Add(metaDataForModifedValue);  // Extra value has been added to modified.
                    else if (!MathUtilities.FloatsAreEqual(values1[i], values2[i], 0.001))
                        metadataValues.Add(metaDataForModifedValue);  // Value has been changed from original.
                    else
                    {
                        // Value hasn't changed. Try and use existing metadata.
                        if (i < metadata1?.Length)
                            metadataValues.Add(metadata1[i]);
                        else
                            metadataValues.Add(null);
                    }
                }
                // If all metadata is null, return null.
                if (!MathUtilities.ValuesInArray(metadataValues))
                    return null;
                    
                return metadataValues.ToArray();
            }
        }        
    }
}
