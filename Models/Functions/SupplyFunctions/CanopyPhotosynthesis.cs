using System;
using Models.Climate;
using Models.Core;
using Models.Interfaces;
using Models.PMF;

namespace Models.Functions.SupplyFunctions
{
    /// <summary>
    /// Daily gross CO2 assimilation and biomass growth is simulated using a canopy photosynthesis model adopted from SPASS, which was a modified version of the original SUCROS model.
    /// The daily gross photosynthesis is called in reponse to event DoPotentialPlantGrowth.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ILeaf))]

    public class CanopyPhotosynthesis : Model, IFunction
    {
        /// <summary>The Plant</summary>
        [Link]
        public Plant Plant = null;
        /// <summary>The Canopy</summary>
        [Link]
        public ICanopy Canopy = null;
        /// <summary>The Weather file</summary>
        [Link]
        public Weather Weather = null;
        /// <summary>The Weather file</summary>
        [Link]
        public IClock Clock = null;

        /// <summary>Link to the hourly photosynthesis model </summary>
        [Link]
        private CanopyGrossPhotosynthesisHourly HourlyGrossCanopyPhotosythesis = null;
        /// <summary>Link to the light use efficiency at low light </summary>
        [Link]
        private LeafLightUseEfficiency LightUseEfficiencyLeaf = null;
        /// <summary>Link to the leaf maximum gross photosynthesis rate</summary>
        [Link]
        private LeafMaxGrossPhotosynthesis MaxGrossPhotosynthesisLeaf = null;


        /// <summary> The amount of DM that is fixed by photosynthesis </summary>
        public double GrossPhotosynthesis { get; set; }

        ///<summary>%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        ///float DLL DailyCanopyGrossPhotosythesis(LPSTR pCrop,float fLAI, float fLatitude,int nDay,
        ///                                        float fRad,float fTmpMax,float fTmpMin,float fCO2,
        ///                                        PZN pLfN,PZRESPONSE pTmpRsp)
        ///Author:   Enli Wang
        ///Date:      10.11.1996
        ///Purpose:   This function calculates the daily canopy photosynthesis rate under optimal water condition
        ///Inputs:   1. pCROP      - Pointer to a string containing the crop name,use the following names:
        ///                       WHEAT,BARLEY,MAIZE,MILLET,SOGHUM,POTATO,SUGARBEET,SOYBEAN,COTTON,C3,C4,CAM
        ///         2. fLAI         - Effective leaf area index (-)
        ///          3. fLatitude   - Location latitude (Degree)
        ///          4. nDay         - Julain day (-)
        ///          5. fRad         - Daily global radiation (MJ/d)
        ///          6. fTmpMax      - Daily maximum air temperature(C)
        ///          7. fTmpMin      - Daily minimum air temperature(C)
        ///          8. fCO2         - Current CO2 concentration in the air (vppm)
        ///         9. pLfN      - Pointer to a ORGANNC structure containing leaf nitrogen concentration
        ///         10. pTmpRsp      - Pointer to a ZRESPONSE structure containing temperature response data for photosynthesis
        ///Outputs:   1. Return      - Calculated daily gross photosynthesis rate of unit leaf area (kgCO2/ha.day)
        ///Functions Called:
        ///         LeafMaxGrossPhotosynthesis
        ///         LeafLightUseEfficiency
        ///         CanopyGrossPhotosynthesis
        ///Comments:   This function checks at first the data contained under pResp. If these data are valid, they will be
        ///         used to construct the temperature response function for photosynthesis. If not, the cardinal temperatures
        ///         at pCardTemp will be used to construct the temperature response function. If pCardTemp equals NULL,
        ///         a minimum, optimum and maximum temperature of 0, 22 and 35C will be assumed respectively.
        ///         If pLfN equals NULL, no nitrogen stress will be considered.
        ///Reference:1. Wang,Enli. xxxx.
        ///%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%% </summary>
        public double DailyCanopyGrossPhotosythesis(double LAI, double Latitude, int Day,
                                                double Radn, double Tmax, double Tmin, double CO2,
                                                double DifFr,
                                                double Fact)
        {
            int i;
            double AveGrossPs;
            double GrossPs;
            double DailyGrossPs;
            double PAR;
            double PARDIF;
            double PARDIR;
            //double vAveGrossPsMax;

            //double GlobalRadiation, SinHeight, Dayl, AtmTrans, DifFr;
            double GlobalRadiation, SinHeight, Dayl, AtmTrans;
            double Dec, Sin, Cos, Rsc, SolarConst, DailySin, DailySinE, Hour, RadExt;
            double LUE, PgMax, Temp;

            int nGauss = 5;
            double[] xGauss = { 0.0469101, 0.2307534, 0.5000000, 0.7692465, 0.9530899 };
            double[] wGauss = { 0.1184635, 0.2393144, 0.2844444, 0.2393144, 0.1184635 };
            double PI = 3.1415926;
            double RAD = PI / 180.0;

            GlobalRadiation = (double)Radn * 1E6;    //J/m2.d


            //===========================================================================================
            //Dailenght, Solar constant and daily extraterrestrial radiation
            //===========================================================================================
            //Declination of the sun as function of Daynumber (vDay)

            Dec = -Math.Asin(Math.Sin(23.45 * RAD) * Math.Cos(2.0 * PI * ((double)Day + 10.0) / 365.0));

            //vSin, vCos and vRsc are intermediate variables
            Sin = Math.Sin(RAD * Latitude) * Math.Sin(Dec);
            Cos = Math.Cos(RAD * Latitude) * Math.Cos(Dec);
            Rsc = Sin / Cos;

            //Astronomical daylength (hr)
            Dayl = 12.0 * (1.0 + 2.0 * Math.Asin(Sin / Cos) / PI);

            //Sine of solar height(vDailySin), inegral of vDailySin(vDailySin) and integrel of vDailySin
            //with correction for lower atmospheric transmission at low solar elevations (vDailySinE)
            DailySin = 3600.0 * (Dayl * Sin + 24.0 * Cos * Math.Sqrt(1.0 - Rsc * Rsc) / PI);
            DailySinE = 3600.0 * (Dayl * (Sin + 0.4 * (Sin * Sin + Cos * Cos * 0.5))
                     + 12.0 * Cos * (2.0 + 3.0 * 0.4 * Sin) * Math.Sqrt(1.0 - Rsc * Rsc) / PI);

            //Solar constant(vSolarConst) and daily extraterrestrial (vRadExt)
            SolarConst = 1370.0 * (1.0 + 0.033 * Math.Cos(2.0 * PI * (double)Day / 365.0));   //J/m2.d
            RadExt = SolarConst * DailySin * 1E-6;               //MJ/m2.d

            //===========================================================================================
            //Daily photosynthesis
            //===========================================================================================
            //Assimilation set to zero and three different times of the Day (vHour)
            AveGrossPs = 0;
            //vAveGrossPsMax = 0;

            //Daytime temperature
            Temp = 0.71 * Tmax + 0.29 * Tmin;

            for (i = 0; i < nGauss; i++)
            {
                //At the specified vHour, radiation is computed and used to compute assimilation
                Hour = 12.0 + Dayl * 0.5 * xGauss[i];
                //Sine of solar elevation
                SinHeight = Math.Max(0.0, Sin + Cos * Math.Cos(2.0 * PI * (Hour + 12.0) / 24.0));
                //Diffuse light fraction (vDifFr) from atmospheric transmission (vAtmTrans)
                PAR = 0.5 * GlobalRadiation * SinHeight * (1.0 + 0.4 * SinHeight) / DailySinE;
                AtmTrans = PAR / (0.5 * SolarConst * SinHeight);

                if (DifFr < 0)
                {

                    if (AtmTrans <= 0.22)
                        DifFr = 1.0;
                    else
                    {
                        if ((AtmTrans > 0.22) && (AtmTrans <= 0.35))
                            DifFr = 1.0 - 6.4 * (AtmTrans - 0.22) * (AtmTrans - 0.22);
                        else
                            DifFr = 1.47 - 1.66 * AtmTrans;
                    }
                    DifFr = Math.Max(DifFr, 0.15 + 0.85 * (1.0 - Math.Exp(-0.1 / SinHeight)));
                }
                if (DifFr < 0 | DifFr > 1)
                {
                    throw new Exception("Diffuse fraction should be between 0 and 1.");
                }

                //Diffuse PAR (PARDIF) and direct PAR (PARDIR)
                PARDIF = Math.Min(PAR, SinHeight * DifFr * AtmTrans * 0.5 * SolarConst);
                PARDIR = PAR - PARDIF;

                //Light response parameters
                PgMax = MaxGrossPhotosynthesisLeaf.Value(CO2, Fact);

                LUE = LightUseEfficiencyLeaf.Value(Temp, CO2);

                //Canopy gross photosynthesis
                GrossPs = HourlyGrossCanopyPhotosythesis.Value(PgMax, LUE, LAI, Latitude,
                                     Day, Hour, PARDIR, PARDIF);

                //Integration of assimilation rate to a daily total (vAveGrossPs)
                AveGrossPs += GrossPs * wGauss[i];
            }

            DailyGrossPs = AveGrossPs * Dayl;

            return DailyGrossPs;
        }

        /// <summary>Daily growth increment of total plant biomass</summary>
        /// <returns>g dry matter/m2 soil/day</returns>
        public double Value(int arrayIndex = -1)
        {
            if (Plant.IsEmerged)
            {
                GrossPhotosynthesis = DailyCanopyGrossPhotosythesis(Canopy.LAI,
                                                      Weather.Latitude,
                                                      Clock.Today.DayOfYear,
                                                      Weather.Radn,
                                                      Weather.MaxT,
                                                      Weather.MinT,
                                                      Weather.CO2,
                                                      Weather.DiffuseFraction,
                                                      1.0) * 30 / 44 * 0.1;
                //30/44 converts CO2 to CH2O, 0.1 converts from kg/ha to g/m2

            }
            return GrossPhotosynthesis;
        }
    }
}