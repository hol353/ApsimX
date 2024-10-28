using System;
using Models.Core;

namespace Models.Functions.SupplyFunctions
{
    ///<summary>
    ///%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    ///float DLL CanopyGrossPhotosynthesis(float fPgMax, float fLUE, float fLAI,
    ///                                float fLatitude,int nDay,float fHour, float fPARdir,float fPARdif)
    ///Author:   Enli Wang
    ///Date:      10.11.1996
    ///Purpose:   This function calculates the canopy gross photosynthesis rate for a given crop with fPgMax,fLUE,fLAI
    ///         at latitude (fLatitude) on day (julian day fDay) at fHour
    ///Inputs:   1. fPgMax   - Maximum leaf gross photosynthesis rate at light saturation (kgCO2/ha.hr)
    ///         2. fLUE      - Light use efficiency of the leaf at current conditions ((kgCO2/ha leaf.hr)/(W/m2))
    ///         3. fLAI      - Effective LAI (-)
    ///         4. fLatitude- Location latitude (Degree)
    ///         5. nDay      - Julain day (-)
    ///         6. fHour   - Current time (Hour)
    ///         7. fPARdir   - Direct component of incident photosynthetic active radiation (W/m2)
    ///         7. fPARdif   - Diffuse component of incident photosynthetic active radiation (W/m2)
    ///Outputs:   1. Return   - Calculated canopy photosynthesis rate (kgCO2/ha.hr)
    ///Functions Called:
    ///         None
    ///Comments:   The input variable fPgMax and fLUE should be calculated using the following functions:
    ///         fPgMax = LeafMaxGrossPhotosynthesis(...);
    ///         fLUE   = LeafLightUseEfficiency(...)
    ///Reference:1. Wang,Enli. xxxx.
    ///%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%</summary>
    /// <summary>Daily growth increment of total plant biomass</summary>
    /// <returns>g dry matter/m2 soil/day</returns>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CanopyPhotosynthesis))]

    public class CanopyGrossPhotosynthesisHourly : Model
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("KDIF")]
        public double KDIF { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fPgMax"></param>
        /// <param name="fLUE"></param>
        /// <param name="fLAI"></param>
        /// <param name="fLatitude"></param>
        /// <param name="nDay"></param>
        /// <param name="fHour"></param>
        /// <param name="fPARdir"></param>
        /// <param name="fPARdif"></param>
        /// <returns></returns>
        public double Value(double fPgMax, double fLUE, double fLAI,
                                        double fLatitude, int nDay, double fHour, double fPARdir, double fPARdif)
        {
            int i, j;
            double PMAX, EFF, vLAI, PARDIR, PARDIF, SINB;
            double SQV, REFH, REFS, CLUSTF, KDIRBL, KDIRT, FGROS, LAIC, VISDF, VIST, VISD, VISSHD, FGRSH;
            double FGRSUN, VISSUN, VISPP, FGRS, FSLLA, FGL, LAT, DAY, HOUR, DEC, vSin, vCos;
            int nGauss = 5;
            double[] xGauss = { 0.0469101, 0.2307534, 0.5000000, 0.7692465, 0.9530899 };
            double[] wGauss = { 0.1184635, 0.2393144, 0.2844444, 0.2393144, 0.1184635 };

            double PI = 3.1415926;
            double RAD = PI / 180.0;

            double SCV = 0.20;    //Scattering coefficient of leaves for visible radiation (PAR)

            PMAX = fPgMax;
            EFF = fLUE;
            vLAI = fLAI;
            PARDIR = fPARdir;
            PARDIF = fPARdif;
            LAT = fLatitude;
            HOUR = fHour;
            DAY = nDay;

            //===========================================================================================
            //Sine of the solar height
            //===========================================================================================
            //Declination of the sun as function of Daynumber (vDay)
            DEC = -Math.Asin(Math.Sin(23.45 * RAD) * Math.Cos(2.0 * PI * (DAY + 10.0) / 365.0));

            //vSin, vCos and vRsc are intermediate variables
            vSin = Math.Sin(RAD * LAT) * Math.Sin(DEC);
            vCos = Math.Cos(RAD * LAT) * Math.Cos(DEC);

            SINB = Math.Max(0.0, vSin + vCos * Math.Cos(2.0 * PI * (HOUR + 12.0) / 24.0));

            //===========================================================================================
            //Reflection of horizontal and spherical leaf angle distribution
            SQV = Math.Sqrt(1.0 - SCV);
            REFH = (1.0 - SQV) / (1.0 + SQV);
            REFS = REFH * 2.0 / (1.0 + 2.0 * SINB);

            //Extinction coefficient for direct radiation and total direct flux
            CLUSTF = KDIF / (0.8 * SQV);
            KDIRBL = (0.5 / SINB) * CLUSTF;
            KDIRT = KDIRBL * SQV;

            //===========================================================================================
            //Selection of depth of canopy, canopy assimilation is set to zero
            FGROS = 0;
            for (i = 0; i < nGauss; i++)
            {
                LAIC = vLAI * xGauss[i];

                //Absorbed fluxes per unit leaf area: diffuse flux, total direct
                //flux, direct component of direct flux.
                VISDF = (1.0 - REFH) * PARDIF * KDIF * Math.Exp(-KDIF * LAIC);
                VIST = (1.0 - REFS) * PARDIR * KDIRT * Math.Exp(-KDIRT * LAIC);
                VISD = (1.0 - SCV) * PARDIR * KDIRBL * Math.Exp(-KDIRBL * LAIC);

                //Absorbed flux (J/M2 leaf/s) for shaded leaves and assimilation of shaded leaves
                VISSHD = VISDF + VIST - VISD;
                if (PMAX > 0.0)
                    FGRSH = PMAX * (1.0 - Math.Exp(-VISSHD * EFF / PMAX));
                else
                    FGRSH = 0.0;

                //Direct flux absorbed by leaves perpendicular on direct beam and
                //assimilation of sunlit leaf area
                VISPP = (1.0 - SCV) * PARDIR / SINB;

                FGRSUN = 0.0;
                for (j = 0; j < nGauss; j++)
                {
                    VISSUN = VISSHD + VISPP * xGauss[j];

                    if (PMAX > 0.0)
                        FGRS = PMAX * (1.0 - Math.Exp(-VISSUN * EFF / PMAX));
                    else
                        FGRS = 0.0;

                    FGRSUN = FGRSUN + FGRS * wGauss[j];
                }

                //Fraction sunlit leaf area (FSLLA) and local assimilation rate (FGL)
                FSLLA = CLUSTF * Math.Exp(-KDIRBL * LAIC);
                FGL = FSLLA * FGRSUN + (1.0 - FSLLA) * FGRSH;

                //Integration of local assimilation rate to canopy assimilation (FGROS)
                FGROS = FGROS + FGL * wGauss[i];
            }

            FGROS = FGROS * vLAI;

            return FGROS;
        }
    }
}