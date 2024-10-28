using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;

namespace Models.Soils
{
    /// <summary>
    /// Calculates the average soil temperature at the centre of each layer, based on the soil temperature model of EPIC (Williams et al 1984)
    /// This code was separated from old SoilN - tidied up but not updated (RCichota, sep/2012)
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    public class CERESSoilTemperature : Model, ISoilTemperature
    {
        [Link]
        IWeather weather = null;

        [Link]
        IClock clock = null;

        /// <summary>The water balance model</summary>
        [Link]
        ISoilWater waterBalance = null;

        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private IPhysical soilPhysical = null;

        /// <summary>Invoke when the soil temperature has changed.</summary>
        public event EventHandler SoilTemperatureChanged;

        #region Parameters and inputs provided by the user or APSIM

        #region Parameters used on initialisation only

        // local latitude
        private double _latitude = -999.0;

        // annual average ambient air temperature (oC)
        private double _tav = -999.0;

        // annual amplitude of the mean monthly air temperature (oC)
        private double _amp = -999.0;

        #endregion

        #region Parameters that do or may change during simulation

        // today's net solar radiation
        private double _radn = 0.0;

        // today's maximun air temperature
        private double _maxt = 0.0;

        // today's minimun air temperature
        private double _mint = 0.0;

        // soil albedo (0-1)
        private double _salb;

        // values passed via SoilTemperature()

        private DateTime _today;

        private double[] _dlayer;

        private double[] _bd;

        private double[] _ll15_dep;

        private double[] _sw_dep;

        #endregion

        #endregion

        #region Internal variables

        // estimated soil surface temperatures (oC)
        private double[] surf_temp;

        // estimated soil temperature profile (oC)
        private double[] st;

        // average air temperature (oC)
        private double ave_temp;

        #endregion

        #region Internal constants

        // Maximum number of days in a year
        private const int MaxDaysInYear = 366;

        // day of year of nthrn summer solstice
        private const double nth_solst = 173.0;

        // delay from solstice to warmest day (days)
        private const double temp_delay = 27.0;

        // warmest day of year in nth hemisphere
        private const double nth_hot = nth_solst + temp_delay;

        // day of year of sthrn summer solstice
        private const double sth_solst = nth_solst + 365.25 / 2.0;

        // warmest day of year of sth hemisphere
        private const double sth_hot = sth_solst + temp_delay;

        //
        private const double my_pi = 3.14159;

        // length of one day in radians
        //private const double ang = (2.0 * Math.PI) / 365.25;
        private const double ang = (2.0 * my_pi) / 365.25;

        // number of days to compute moving average of surface soil temperature
        private const int ndays = 5;


        #endregion

        /// <summary>Clears this instance.</summary>
        private void Clear()
        {
            // need to initialise some values for surf_temp, repeat the value of ave_temp for the first day (RCichota: why not tav?)
            surf_temp = new double[MaxDaysInYear];
            for (int day = 0; day < MaxDaysInYear; day++)
                surf_temp[day] = ave_temp;
        }

        /// <summary>Soil temperature for each layer (oC)</summary>
        public double[] Value { get { return st; } }

        /// <summary>Average soil temperature for each layer (oC)</summary>
        public double[] AverageSoilTemperature { get { return st; } }

        /// <summary>Average soil temperature for soil surface (oC)</summary>
        public double AverageSoilSurfaceTemperature { get { return double.NaN; } }

        /// <summary>Average soil temperature for each layer (oC)</summary>
        public double[] MinimumSoilTemperature { get { return st; } }

        /// <summary>Average soil temperature for soil surface (oC)</summary>
        public double MinimumSoilSurfaceTemperature { get { return double.NaN; } }

        /// <summary>Average soil temperature for each layer (oC)</summary>
        public double[] MaximumSoilTemperature { get { return st; } }

        /// <summary>Average soil temperature for soil surface (oC)</summary>
        public double MaximumSoilSurfaceTemperature { get { return double.NaN; } }

        /// <summary>Called to perform soil temperature calculations</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilTemperature")]
        private void OnDoSoilTemperature(object sender, EventArgs e)
        {
            _today = clock.Today;
            _mint = weather.MinT;
            _maxt = weather.MaxT;
            _radn = weather.Radn;
            _salb = waterBalance.Salb;
            _dlayer = soilPhysical.Thickness;
            _bd = soilPhysical.BD;
            _ll15_dep = soilPhysical.LL15mm;
            _sw_dep = waterBalance.SWmm;
            _latitude = weather.Latitude;
            _tav = weather.Tav;
            _amp = weather.Amp;

            st = new double[soilPhysical.Thickness.Length];

            ave_temp = (_maxt + _mint) * 0.5;

            if (clock.Today.Equals(clock.StartDate))
                Clear();

            // Calculate "normal" soil temperature from the day of year assumed to have the warmest average soil temperature
            // The normal soil temperature varies as a cosine function of alx, with average tav and amplitude amp

            // get the time of year, in radians, from hottest instance to current day
            double alx;
            if (_latitude >= 0)
                alx = ang * _today.AddDays(-(int)nth_hot).DayOfYear;
            else
                alx = ang * _today.AddDays(-(int)sth_hot).DayOfYear;

            // RCichota: had to cast nth_hot and sth_hot to integer due to differences in how DayOfYear are handled in here as compared to the fortran version
            //  there was a one day offset for sth_hot. Ex.:
            //  if today = 1-Jan and sht_hot = 382.625, fortran gives day of year = 349, while here, without casting, we get 348

            if (alx < 0.0 || alx > 6.31)
                throw new Exception("Value for alx is out of range");

            // get the change in soil temperature since hottest day of the year
            double TempChange = dlt_temp(alx);

            // get temperature dumping depth (mm per radian of a year)
            double DampingDepth = DampDepth();

            // compute soil temp for each of the remaining layers
            double cum_depth = 0.0;
            for (int layer = 0; layer < _dlayer.Length; layer++)
            {
                // cumulative depth to the bottom of current layer
                cum_depth += _dlayer[layer];

                // depth lag factor - This reduces changes in soil temperature with depth (radians of a year)
                double depth_lag = MathUtilities.Divide(cum_depth, DampingDepth, 0.0);

                // soil temperature
                st[layer] = LayerTemp(depth_lag, alx, TempChange);
                if (st[layer] < -50.0 || st[layer] > 80.0)
                    throw new Exception("Value for soil_temp is out of range");
            }

            SoilTemperatureChanged?.Invoke(this, EventArgs.Empty);
        }

        private double dlt_temp(double alx)
        {
            // + Purpose
            //     Calculates the rate of change in soil surface temperature with time.
            //       This is a correction to adjust today's normal sinusoidal soil surface temperature to the current temperature conditions.

            // estimate today's top layer temperature from yesterdays temp and today's weather conditions.
            int todayDoY = _today.DayOfYear - 1;
            int yesterdayDoY = _today.AddDays(-1).DayOfYear - 1;
            double TodayTempAmp = _maxt - ave_temp;
            double SunEffect = Math.Sqrt(_radn * 23.8846 / 800.0);
            surf_temp[todayDoY] = (1.0 - _salb) * (ave_temp + TodayTempAmp * SunEffect) + _salb * surf_temp[yesterdayDoY];

            // average of soil surface temperature over last ndays
            double ave_surf_temp = 0;
            for (int day = 0; day < ndays; day++)
            {
                int dayDoY = _today.AddDays(-day).DayOfYear - 1;
                ave_surf_temp += surf_temp[dayDoY];
            }
            ave_surf_temp = MathUtilities.Divide(ave_surf_temp, ndays, 0.0);

            // Calculate today's normal surface soil temperature.
            // There is no depth lag, being the surface, and there is no adjustment for the current temperature conditions
            //  as we want the "normal" sinusoidal temperature for this time of year.
            double normal_temp = LayerTemp(0.0, alx, 0.0);

            // Estimate the rate of change in soil surface temperature with time.
            // This is the difference between a five-day moving average and today's normal surface soil temperature.
            double result = ave_surf_temp - normal_temp;

            // check output
            if (result < -100.0 || result > 100.0)
                throw new Exception("Value for simpleSoilTemp dlt_temp is out of range");

            return result;
        }

        private double LayerTemp(double depth_lag, double alx, double dlt_temp)
        {
            // + Purpose
            //      Calculate the average soil temperature for a given layer.
            //      The difference in temperature between surface and layers is an exponential function of the ratio of
            //        the depth to the bottom of the layer and the temperature damping depth of the soil.

            return _tav + ((_amp / 2.0) * Math.Cos(alx - depth_lag) + dlt_temp) * Math.Exp(-depth_lag);
        }

        private double DampDepth()
        {
            // + Purpose
            //      Now get the temperature damping depth.
            //       This is a function of the average soil bulk density and the amount of water above the lower limit.

            //+  Notes
            //     241091 consulted Brian Wall.  For soil temperature an estimate of the water content of the total profile is required,
            //      not the plant extractable soil water.  Hence the method used here - difference between total lower limit and total soil
            //      water.  Here the use of lower limit is of no significance - it is merely a reference point, just as 0.0 could have been used.  jngh

            const double sw_avail_tot_min = 0.01;

            // get average bulk density and its factor
            double cum_depth = 0.0;
            double ave_bd = 0.0;
            double ll_tot = 0.0;
            double sw_dep_tot = 0.0;
            for (int layer = 0; layer < _dlayer.Length; layer++)
            {
                ave_bd += _bd[layer] * _dlayer[layer];
                ll_tot += _ll15_dep[layer];
                sw_dep_tot += _sw_dep[layer];
                cum_depth += _dlayer[layer];
            }

            ave_bd = MathUtilities.Divide(ave_bd, cum_depth, 0.0);

            // bd factor ranges from almost 0 to almost 1 (sigmoid funtion)
            double favbd = ave_bd / (ave_bd + 686.0 * Math.Exp(-5.63 * ave_bd));

            // damp_depth_max ranges from 1000 to almost 3500
            // It seems damp_depth_max is the damping depth potential.
            double damp_depth_max = Math.Max(0.0, 1000.0 + 2500.0 * favbd);

            // Potential sw above lower limit - mm water/mm soil depth
            //   Note that this function says that average bulk density can't go above 2.47222,
            //    otherwise potential sw becomes negative.  This function allows potential (ww) to go from 0 to .356
            double ww = Math.Max(0.0, 0.356 - 0.144 * ave_bd);

            // calculate amount of soil water, using lower limit as the reference point.
            double sw_avail_tot = Math.Max(sw_dep_tot - ll_tot, sw_avail_tot_min);

            // get fractional water content
            // wc can range from 0 to 1 while wcf ranges from 1 to 0
            double wc = MathUtilities.Divide(sw_avail_tot, ww * cum_depth, 1.0);
            wc = Math.Max(0.0, Math.Min(1.0, wc));
            double wcf = (1.0 - wc) / (1.0 + wc);

            // Here b can range from -.69314 to -1.94575 and f ranges from 1 to  0.142878
            // When wc is 0, wcf=1 and f=500/damp_depth_max and soiln2_SoilTemp_DampDepth=500
            // When wc is 1, wcf=0 and f=1 and soiln2_SoilTemp_DampDepth=damp_depth_max
            //   and that damp_depth_max is the maximum.
            //double b = Math.Log(MathUtilities.Divide(500.0, damp_depth_max, 1.0e10));
            double b = Math.Log(500.0 / damp_depth_max);
            double f = Math.Exp(b * wcf * wcf);

            // Get the temperature damping depth. (mm soil/radian of a g_year)
            //   discount the potential damping depth by the soil water deficit.
            // Here soiln2_SoilTemp_DampDepth ranges from 500 to almost 3500 mm/58 days.
            return f * damp_depth_max;
        }
    }

}