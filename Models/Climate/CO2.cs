using System;
using System.Collections.Generic;
using System.Data;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;

namespace Models.Climate
{
    /// <summary>
    /// A model to allow the user to modify CO2 values in a simulation, either by a constant value or
    /// by reading values from a .csv file.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Weather))]
    public class CO2Value : Model
    {
        private Dictionary<int, double> co2Values = new();


        [Link]
        private Simulation simulation = null;

        [Link]
        private IWeather weather = null;

        [Link]
        private Clock clock = null;


        /// <summary>Name of csv file to read co2 values from.</summary>

        [Separator("Specify either a filename containing CO2 values or a constant value")]

        [Description("Name of csv file to read co2 values from")]
        [Display]
        public string FileName { get; set; } = "%root%\\Examples\\WeatherFiles\\CO2.csv";

        /// <summary>Constant CO2 value (ppm).</summary>
        [Description("Constant CO2 value (ppm)")]
        public double ConstantValue { get; set; }

        /// <summary>
        /// Invoked when a simulation starts.
        /// </summary>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                // Ensure filename is relative to the directory where the .apsimx file is located.
                string fullFileName = PathUtilities.GetAbsolutePath(this.FileName, simulation.FileName);

                var data = ApsimTextFile.ToTable(fullFileName);
                if (!data.Columns.Contains("Year"))
                    throw new Exception("Cannot find a Year column in co2 file");
                if (!data.Columns.Contains("CO2"))
                    throw new Exception("Cannot find a CO2 column in co2 file");

                foreach (DataRow row in data.Rows)
                {
                    int year = Convert.ToInt32(row["Year"]);
                    double value = Convert.ToDouble(row["CO2"]);
                    co2Values.Add(year, value);
                }
            }
            else if (ConstantValue == 0)
                throw new Exception("You need to specify a CO2 file name or a constant CO2 value");
        }


        /// <summary>
        /// Invoked by weather to allow models to set weather data.
        /// </summary>
        [EventSubscribe("PreparingNewWeatherData")]
        private void OnPreparingNewWeatherData(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                if (!co2Values.TryGetValue(clock.Today.Year, out double co2Value))
                    throw new Exception($"Cannot find a co2 value in file {FileName} for year {clock.Today.Year}");
                weather.CO2 = co2Value;
            }
            else
                weather.CO2 = ConstantValue;
        }
    }
}
