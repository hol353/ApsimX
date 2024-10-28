using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models.Functions
{
    /// <summary>
    /// Firstly 3-hourly estimates of air temperature (Ta) are interpolated 
    /// using the method of [jones_ceres-maize:_1986] which assumes a sinusoidal temperature 
    /// pattern between Tmax and Tmin.  
    /// </summary>
    [Serializable]
    [Description("A value is calculated at 3-hourly estimates using air temperature based on daily max and min temperatures\n\n" +
        "Eight interpolations of the air temperature are calculated using a three-hour correction factor." +
        "For each air three-hour air temperature, a value is calculated.")]
    [ValidParent(ParentType = typeof(SubDailyInterpolation))]
    public class ThreeHourAirTemperature : Model, IInterpolationMethod
    {
        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>Factors used to multiply daily range to give diurnal pattern of temperatures between Tmax and Tmin</summary>
        public List<double> TempRangeFactors = null;

        /// <summary>The type of variable for sub-daily values</summary>
        [JsonIgnore]
        public string OutputValueType { get; set; } = "air temperature";

        /// <summary>
        /// Calculate temperatures at 3 hourly intervals from min and max using sin curve
        /// </summary>
        /// <returns>list of 8 temperature estimates for 3 hourly periods</returns>
        public List<double> SubDailyValues()
        {
            List<double> sdts = new List<Double>();
            double diurnal_range = MetData.MaxT - MetData.MinT;
            foreach (double trf in TempRangeFactors)
            {
                sdts.Add(MetData.MinT + trf * diurnal_range);
            }
            return sdts;
        }

        /// <summary> Set the sub daily temperature range factor values at sowing</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            TempRangeFactors = t_range_fract();
        }

        /// <summary>Fraction_of of day's range_of for this 3 hr period</summary>
        public List<double> t_range_fract()
        {
            List<int> periods = Enumerable.Range(1, 8).ToList();
            List<double> trfs = new List<double>();
            // pre calculate t_range_fract for speed reasons
            foreach (int period in periods)
            {
                trfs.Add(0.92105
                        + 0.1140 * period
                        - 0.0703 * Math.Pow(period, 2)
                        + 0.0053 * Math.Pow(period, 3));
            }
            if (trfs.Count != 8)
                throw new Exception("Incorrect number of subdaily temperature estimations in " + this.Name + " temperature interpolation");
            return trfs;
        }
    }
}
