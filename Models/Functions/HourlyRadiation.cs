using System;
using System.Collections.Generic;
using Models.Core;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models.Functions
{
    /// <summary>
    /// Firstly hourly estimates of solar radiation are interpolated from solar daily radiation
    /// </summary>
    [Serializable]
    [Description("Calculates the ground solar incident radiation per hour")]
    [ValidParent(ParentType = typeof(SubDailyInterpolation))]
    public class HourlyRadiation : Model, IInterpolationMethod
    {
        /// <summary>
        /// Link to the weather object
        /// </summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>
        /// Link to the clock object
        /// </summary>
        [Link]
        protected IClock clock = null;

        /// <summary>The type of variable for sub-daily values</summary>
        [JsonIgnore]
        public string OutputValueType { get; set; } = "solar radiation";

        // Calculates the ground solar incident radiation per hour and scales it to the actual radiation
        // Developed by Greg McLean and adapted/modified by Behnam (Ben) Ababaei.

        /// <summary>
        /// Hourly radiation estimation ported from https://github.com/BrianCollinss/ApsimX/blob/12a89f9981e2636f13251b0faa30200a98b713ce/Models/Functions/SupplyFunctions/MaximumHourlyTrModel.cs#L260
        /// by Hamish
        /// Note, this has not been tested and probably has errors as it is not a dirrect port
        /// </summary>
        /// <returns></returns>
        public List<double> SubDailyValues()
        {
            List<double> hourlyRad = new List<double>();

            double latR = Math.PI / 180.0 * MetData.Latitude;       // convert latitude (degrees) to radians
            double GlobalRadiation = MetData.Radn * 1e6;     // solar radiation
            double PI = Math.PI;
            double RAD = PI / 180.0;

            //Declination of the sun as function of Daynumber (vDay)
            double Dec = -Math.Asin(Math.Sin(23.45 * RAD) * Math.Cos(2.0 * PI * ((double)clock.Today.DayOfYear + 10.0) / 365.0));

            //vSin, vCos and vRsc are intermediate variables
            double Sin = Math.Sin(latR) * Math.Sin(Dec);
            double Cos = Math.Cos(latR) * Math.Cos(Dec);
            double Rsc = Sin / Cos;

            //Astronomical daylength (hr)
            double DayL = MetData.CalculateDayLength(-6);
            double DailySinE = 3600.0 * (DayL * (Sin + 0.4 * (Sin * Sin + Cos * Cos * 0.5))
                     + 12.0 * Cos * (2.0 + 3.0 * 0.4 * Sin) * Math.Sqrt(1.0 - Rsc * Rsc) / PI);

            double riseHour = MetData.CalculateSunRise();
            double setHour = MetData.CalculateSunSet();

            for (int t = 0; t <= 23; t++)
            {
                double Hour1 = Math.Min(setHour, Math.Max(riseHour, t));
                double SinHeight1 = Math.Max(0.0, Sin + Cos * Math.Cos(2.0 * PI * (Hour1 - 12.0) / 24.0));
                double Hour2 = Math.Min(setHour, Math.Max(riseHour, t + 1));
                double SinHeight2 = Math.Max(0.0, Sin + Cos * Math.Cos(2.0 * PI * (Hour2 - 12.0) / 24.0));
                double SinHeight = 0.5 * (SinHeight1 + SinHeight2);
                hourlyRad.Add(Math.Max(0, GlobalRadiation * SinHeight * (1.0 + 0.4 * SinHeight) / DailySinE));
                hourlyRad[t] *= HourlyWeight(t + 1, riseHour, setHour);
            }

            return hourlyRad;
        }

        private double HourlyWeight(int t, double riseHour, double setHour)
        {
            double weight = new double();
            if (t < riseHour) weight = 0;
            else if (t > riseHour && t - 1 < riseHour) weight = t - riseHour;
            else if (t >= riseHour && t < setHour) weight = 1;
            else if (t > setHour && t - 1 < setHour) weight = 1 - (t - setHour);
            else if (t > setHour) weight = 0;
            return weight;
        }
    }

}
