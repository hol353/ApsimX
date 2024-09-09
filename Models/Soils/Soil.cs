using System;
using System.Text;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Soils
{

    /// <summary>
    /// The soil class encapsulates a soil characterisation and 0 or more soil samples.
    /// the methods in this class that return double[] always return using the
    /// "Standard layer structure" i.e. the layer structure as defined by the Water child object.
    /// method. Mapping will occur to achieve this if necessary.
    /// To obtain the "raw", unmapped, values use the child classes e.g. SoilWater, Analysis and Sample.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Zones.CircularZone))]
    [ValidParent(ParentType = typeof(Zones.RectangularZone))]
    public class Soil : Model
    {
        [Link]
        private ISummary summary = null;

        /// <summary>Gets or sets the record number.</summary>
        [Summary]
        [Description("Record number")]
        public int RecordNumber { get; set; }

        /// <summary>Gets or sets the asc order.</summary>
        [Summary]
        [Description("Australian Soil Classification Order")]
        public string ASCOrder { get; set; }

        /// <summary>Gets or sets the asc sub order.</summary>
        [Summary]
        [Description("Australian Soil Classification Sub-Order")]
        public string ASCSubOrder { get; set; }

        /// <summary>Gets or sets the type of the soil.</summary>
        [Summary]
        [Description("Soil texture or other descriptor")]
        public string SoilType { get; set; }

        /// <summary>Gets or sets the name of the local.</summary>
        [Summary]
        [Description("Local name")]
        public string LocalName { get; set; }

        /// <summary>Gets or sets the site.</summary>
        [Summary]
        [Description("Site")]
        public string Site { get; set; }

        /// <summary>Gets or sets the nearest town.</summary>
        [Summary]
        [Description("Nearest town")]
        public string NearestTown { get; set; }

        /// <summary>Gets or sets the region.</summary>
        [Summary]
        [Description("Region")]
        public string Region { get; set; }

        /// <summary>Gets or sets the state.</summary>
        [Summary]
        [Description("State")]
        public string State { get; set; }

        /// <summary>Gets or sets the country.</summary>
        [Summary]
        [Description("Country")]
        public string Country { get; set; }

        /// <summary>Gets or sets the natural vegetation.</summary>
        [Summary]
        [Description("Natural vegetation")]
        public string NaturalVegetation { get; set; }

        /// <summary>Gets or sets the apsoil number.</summary>
        [Summary]
        [Description("APSoil number")]
        public string ApsoilNumber { get; set; }

        /// <summary>Gets or sets the latitude.</summary>
        [Summary]
        [Description("Latitude (WGS84)")]
        public double Latitude { get; set; }

        /// <summary>Gets or sets the longitude.</summary>
        [Summary]
        [Description("Longitude (WGS84)")]
        public double Longitude { get; set; }

        /// <summary>Gets or sets the location accuracy.</summary>
        [Summary]
        [Description("Location accuracy")]
        public string LocationAccuracy { get; set; }

        /// <summary>Gets or sets the year of sampling.</summary>
        [Summary]
        [Description("Year of sampling")]
        public string YearOfSampling { get; set; }

        /// <summary>Gets or sets the data source.</summary>
        [Summary]
        [Description("Data source")]
        public string DataSource { get; set; }

        /// <summary>Gets or sets the comments.</summary>
        [Summary]
        [Description("Comments")]
        public string Comments { get; set; }

        /// <summary>Event handler to perform error checks at start of simulation.</summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event data.</param>
        [EventSubscribe("DoInitialSummary")]
        private void OnDoInitialSummary(object sender, EventArgs e)
        {
            Check(summary);
        }

        /// <summary>
        /// Checks validity of soil parameters. Throws if soil is invalid.
        /// Standardises the soil before performing tests.
        /// </summary>
        /// <param name="summary">A summary instance to write warning messages to.</param>
        public void CheckWithStandardisation(ISummary summary)
        {
            var soil = Apsim.Clone(this) as Soil;
            soil.Sanitise();

            Check(summary);
        }

        /// <summary>
        /// Checks validity of soil parameters. Throws if soil is invalid.
        /// Does not standardise the soil before performing tests.
        /// </summary>
        /// <param name="summary">A summary instance to write warning messages to.</param>
        public void Check(ISummary summary)
        {
            var weirdo = FindChild<WEIRDO>();
            var water = FindChild<Water>();
            var organic = FindChild<Organic>();
            var chemical = FindChild<Chemical>();
            var physical = FindChild<IPhysical>();
            const double min_sw = 0.0;
            const double specific_bd = 2.65; // (g/cc)
            StringBuilder message = new StringBuilder();

            //Weirdo is an experimental soil water model that does not have the same soil water parameters
            //so don't do any of these tests if Weirdo is plugged into this simulation.
            if (weirdo == null)
            {
                var crops = FindAllDescendants<SoilCrop>();
                foreach (var soilCrop in crops)
                {
                    if (soilCrop != null)
                    {
                        double[] LL = soilCrop.LL;
                        double[] KL = soilCrop.KL;
                        double[] XF = soilCrop.XF;

                        if (!MathUtilities.ValuesInArray(LL) || !MathUtilities.ValuesInArray(KL) || !MathUtilities.ValuesInArray(XF))
                            message.AppendLine($"Values for LL, KL or XF are missing for crop {soilCrop.Name}");
                        else
                        {
                            for (int layer = 0; layer < physical.Thickness.Length; layer++)
                            {
                                int layerNumber = layer + 1;

                                if (KL[layer] == MathUtilities.MissingValue || double.IsNaN(KL[layer]))
                                    message.AppendLine($"{soilCrop.Name} KL value missing in layer {layerNumber}");
                                else if (MathUtilities.GreaterThan(KL[layer], 1, 3))
                                    message.AppendLine($"{soilCrop.Name} KL value of {KL[layer].ToString("f3")} in layer {layerNumber} is greater than 1");

                                if (XF[layer] == MathUtilities.MissingValue || double.IsNaN(XF[layer]))
                                    message.AppendLine($"{soilCrop.Name} XF value missing in layer {layerNumber}");
                                else if (MathUtilities.GreaterThan(XF[layer], 1, 3))
                                    message.AppendLine($"{soilCrop.Name} XF value of {XF[layer].ToString("f3")} in layer {layerNumber} is greater than 1");

                                if (LL[layer] == MathUtilities.MissingValue || double.IsNaN(LL[layer]))
                                    message.AppendLine($"{soilCrop.Name} LL value missing in layer {layerNumber}");
                                else if (MathUtilities.LessThan(LL[layer], physical.AirDry[layer], 3))
                                    message.AppendLine($"{soilCrop.Name} LL of {LL[layer].ToString("f3")} in layer {layerNumber} is below air dry value of {physical.AirDry[layer].ToString("f3")}");
                                else if (MathUtilities.GreaterThan(LL[layer], physical.DUL[layer], 3))
                                    message.AppendLine($"{soilCrop.Name} LL of {LL[layer].ToString("f3")} in layer {layerNumber} is above drained upper limit of {physical.DUL[layer].ToString("f3")}");
                            }
                        }
                    }
                }

                // Check other profile variables.
                for (int layer = 0; layer < physical.Thickness.Length; layer++)
                {
                    double max_sw = MathUtilities.Round(1.0 - physical.BD[layer] / specific_bd, 3);
                    int layerNumber = layer + 1;

                    if (physical.AirDry[layer] == MathUtilities.MissingValue || double.IsNaN(physical.AirDry[layer]))
                        message.AppendLine($"Air dry value missing in layer {layerNumber}");
                    else if (MathUtilities.LessThan(physical.AirDry[layer], min_sw, 3))
                        message.AppendLine($"Air dry lower limit of {physical.AirDry[layer].ToString("f3")} in layer {layerNumber} is below acceptable value of {min_sw.ToString("f3")}");

                    if (physical.LL15[layer] == MathUtilities.MissingValue || double.IsNaN(physical.LL15[layer]))
                        message.AppendLine($"15 bar lower limit value missing in layer {layerNumber}");
                    else if (MathUtilities.LessThan(physical.LL15[layer], physical.AirDry[layer], 3))
                        message.AppendLine($"15 bar lower limit of {physical.LL15[layer].ToString("f3")} in layer {layerNumber} is below air dry value of {physical.AirDry[layer].ToString("f3")}");

                    if (physical.DUL[layer] == MathUtilities.MissingValue || double.IsNaN(physical.DUL[layer]))
                        message.AppendLine($"Drained upper limit value missing in layer {layerNumber}");
                    else if (MathUtilities.LessThan(physical.DUL[layer], physical.LL15[layer], 3))
                        message.AppendLine($"Drained upper limit of {physical.DUL[layer].ToString("f3")} in layer {layerNumber} is at or below lower limit of {physical.LL15[layer].ToString("f3")}");

                    if (physical.SAT[layer] == MathUtilities.MissingValue || double.IsNaN(physical.SAT[layer]))
                        message.AppendLine($"Saturation value missing in layer {layerNumber}");
                    else if (MathUtilities.LessThan(physical.SAT[layer], physical.DUL[layer], 3))
                        message.AppendLine($"Saturation of {physical.SAT[layer].ToString("f3")} in layer {layerNumber} is at or below drained upper limit of {physical.DUL[layer].ToString("f3")}");
                    else if (MathUtilities.GreaterThan(physical.SAT[layer], max_sw, 3))
                    {
                        double max_bd = (1.0 - physical.SAT[layer]) * specific_bd;
                        message.AppendLine($"Saturation of {physical.SAT[layer].ToString("f3")} in layer {layerNumber} is above acceptable value of {max_sw.ToString("f3")}. You must adjust bulk density to below {max_bd.ToString("f3")} OR saturation to below {max_sw.ToString("f3")}");
                    }

                    if (physical.BD[layer] == MathUtilities.MissingValue || double.IsNaN(physical.BD[layer]))
                        message.AppendLine($"BD value missing in layer {layerNumber}");
                    else if (MathUtilities.GreaterThan(physical.BD[layer], specific_bd, 3))
                        message.AppendLine($"BD value of {physical.BD[layer].ToString("f3")} in layer {layerNumber} is greater than the theoretical maximum of 2.65");
                }

                if (organic.Carbon.Length == 0)
                    message.AppendLine("Cannot find OC values in soil");
                else
                    for (int layer = 0; layer != physical.Thickness.Length; layer++)
                    {
                        int layerNumber = layer + 1;
                        if (organic.Carbon[layer] == MathUtilities.MissingValue || double.IsNaN(organic.Carbon[layer]))
                            message.AppendLine($"OC value missing in layer {layerNumber}");
                        else if (MathUtilities.LessThan(organic.Carbon[layer], 0.01, 3))
                            summary.WriteMessage(null, $"OC value of {organic.Carbon[layer].ToString("f3")} in layer {layerNumber} is less than 0.01", MessageType.Warning);
                    }

                if (!MathUtilities.ValuesInArray(water.InitialValues))
                    message.AppendLine("No starting soil water values found.");
                else
                    for (int layer = 0; layer != physical.Thickness.Length; layer++)
                    {
                        int layerNumber = layer + 1;

                        if (water.InitialValues[layer] == MathUtilities.MissingValue || double.IsNaN(water.InitialValues[layer]))
                            message.AppendLine($"Soil water value missing in layer {layerNumber}");
                        else if (MathUtilities.GreaterThan(water.InitialValues[layer], physical.SAT[layer], 3))
                            message.AppendLine($"Soil water of {water.InitialValues[layer].ToString("f3")} in layer {layerNumber} is above saturation of {physical.SAT[layer].ToString("f3")}");
                        else if (MathUtilities.LessThan(water.InitialValues[layer], physical.AirDry[layer], 3))
                            message.AppendLine($"Soil water of {water.InitialValues[layer].ToString("f3")} in layer {layerNumber} is below air-dry value of {physical.AirDry[layer].ToString("f3")}");
                    }

                for (int layer = 0; layer != physical.Thickness.Length; layer++)
                {
                    int layerNumber = layer + 1;
                    if (chemical.PH[layer] == MathUtilities.MissingValue || double.IsNaN(chemical.PH[layer]))
                        message.AppendLine($"PH value missing in layer {layerNumber}");
                    else if (MathUtilities.LessThan(chemical.PH[layer], 3.5, 3))
                        message.AppendLine($"PH value of {chemical.PH[layer].ToString("f3")} in layer {layerNumber} is less than 3.5");
                    else if (MathUtilities.GreaterThan(chemical.PH[layer], 11, 3))
                        message.AppendLine($"PH value of {chemical.PH[layer].ToString("f3")} in layer {layerNumber} is greater than 11");
                }

                var no3 = FindChild<Solute>("NO3");
                if (!MathUtilities.ValuesInArray(no3.InitialValues))
                    message.AppendLine("No starting NO3 values found.");
                var nh4 = FindChild<Solute>("NH4");
                if (!MathUtilities.ValuesInArray(nh4.InitialValues))
                    message.AppendLine("No starting NH4 values found.");
            }

            if (message.Length > 0)
                throw new Exception(message.ToString());
        }
    }
}