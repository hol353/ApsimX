using System;
using System.Collections.Generic;
using System.Linq;    
using Models.Core;   
namespace Models.Crop2ML.DSSAT_EPICST_standalone;

/// <summary>
///- Name: STEMP_EPIC -Version:  1.0, -Time step:  1
///- Description:
///            * Title: Model of STEMP_EPIC
///            * Authors: Kenneth N. Potter , Jimmy R. Williams 
///            * Reference: https://doi.org/10.2134/agronj1994.00021962008600060014x
///            * Institution: USDA-ARS, USDA-ARS
///            * ExtendedDescription: None
///            * ShortDescription: Determines soil temperature by layer test encore
///- inputs:
///            * name: NL
///                          ** description : Number of soil layers
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : INT
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : dimensionless
///            * name: ISWWAT
///                          ** description : Water simulation control switch (Y or N)
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : STRING
///                          ** max : 
///                          ** min : 
///                          ** default : Y
///                          ** unit : dimensionless
///            * name: BD
///                          ** description : Bulk density, soil layer NL
///                          ** inputtype : parameter
///                          ** parametercategory : soil
///                          ** datatype : DOUBLEARRAY
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : g [soil] / cm3 [soil]
///            * name: DLAYR
///                          ** description : Thickness of soil layer NL
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLEARRAY
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : cm
///            * name: DS
///                          ** description : Cumulative depth in soil layer NL
///                          ** inputtype : parameter
///                          ** parametercategory : soil
///                          ** datatype : DOUBLEARRAY
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : cm
///            * name: DUL
///                          ** description : Volumetric soil water content at Drained Upper Limit in soil layer L
///                          ** inputtype : parameter
///                          ** parametercategory : soil
///                          ** datatype : DOUBLEARRAY
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : cm3[water]/cm3[soil]
///            * name: LL
///                          ** description : Volumetric soil water content in soil layer NL at lower limit
///                          ** inputtype : parameter
///                          ** parametercategory : soil
///                          ** datatype : DOUBLEARRAY
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : cm3 [water] / cm3 [soil]
///            * name: NLAYR
///                          ** description : Actual number of soil layers
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : INT
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : dimensionless
///            * name: TAMP
///                          ** description : Annual amplitude of the average air temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : degC
///            * name: RAIN
///                          ** description : daily rainfall
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : mm
///            * name: SW
///                          ** description : Volumetric soil water content in layer NL
///                          ** inputtype : parameter
///                          ** parametercategory : soil
///                          ** datatype : DOUBLEARRAY
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : cm3 [water] / cm3 [soil]
///            * name: TAVG
///                          ** description : Average daily temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : degC
///            * name: TMAX
///                          ** description : Maximum daily temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : degC
///            * name: TMIN
///                          ** description : Minimum Temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : degC
///            * name: TAV
///                          ** description : Average annual soil temperature, used with TAMP to calculate soil temperature.
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : degC
///            * name: CUMDPT
///                          ** description : Cumulative depth of soil profile
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : mm
///            * name: DSMID
///                          ** description : Depth to midpoint of soil layer NL
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : cm
///            * name: TDL
///                          ** description : Total water content of soil at drained upper limit
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : cm
///            * name: TMA
///                          ** description : Array of previous 5 days of average soil temperatures.
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : 5
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : degC
///            * name: NDays
///                          ** description : Number of days ...
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : INT
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : day
///            * name: WetDay
///                          ** description : Wet Days
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : INTARRAY
///                          ** len : 30
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : day
///            * name: X2_PREV
///                          ** description : Temperature of soil surface at precedent timestep
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : degC
///            * name: SRFTEMP
///                          ** description : Temperature of soil surface litter
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : degC
///            * name: ST
///                          ** description : Soil temperature in soil layer NL
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : degC
///            * name: DEPIR
///                          ** description : Depth of irrigation
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : mm
///            * name: BIOMAS
///                          ** description : Biomass
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : kg/ha
///            * name: MULCHMASS
///                          ** description : Mulch Mass
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : kg/ha
///            * name: SNOW
///                          ** description : Snow cover
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : mm
///- outputs:
///            * name: CUMDPT
///                          ** description : Cumulative depth of soil profile
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 
///                          ** min : 
///                          ** unit : mm
///            * name: DSMID
///                          ** description : Depth to midpoint of soil layer NL
///                          ** datatype : DOUBLEARRAY
///                          ** variablecategory : state
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** unit : cm
///            * name: TDL
///                          ** description : Total water content of soil at drained upper limit
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 
///                          ** min : 
///                          ** unit : cm
///            * name: TMA
///                          ** description : Array of previous 5 days of average soil temperatures.
///                          ** datatype : DOUBLEARRAY
///                          ** variablecategory : state
///                          ** len : 5
///                          ** max : 
///                          ** min : 
///                          ** unit : degC
///            * name: NDays
///                          ** description : Number of days ...
///                          ** datatype : INT
///                          ** variablecategory : state
///                          ** max : 
///                          ** min : 
///                          ** unit : day
///            * name: WetDay
///                          ** description : Wet Days
///                          ** datatype : INTARRAY
///                          ** variablecategory : state
///                          ** len : 30
///                          ** max : 
///                          ** min : 
///                          ** unit : day
///            * name: X2_PREV
///                          ** description : Temperature of soil surface at precedent timestep
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 
///                          ** min : 
///                          ** unit : degC
///            * name: SRFTEMP
///                          ** description : Temperature of soil surface litter
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 
///                          ** min : 
///                          ** unit : degC
///            * name: ST
///                          ** description : Soil temperature in soil layer NL
///                          ** datatype : DOUBLEARRAY
///                          ** variablecategory : state
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** unit : degC
/// </summary>
public class STEMP_EPIC
{

    /// <summary>
    /// initialization of the STEMP_EPIC component
    /// </summary>
    public void Init(STEMP_EPIC_State s, STEMP_EPIC_State s1, STEMP_EPIC_Rate r, STEMP_EPIC_Auxiliary a, STEMP_EPIC_Exogenous ex)
    {
        double TAMP = ex.TAMP;
        double RAIN = ex.RAIN;
        double TAVG = ex.TAVG;
        double TMAX = ex.TMAX;
        double TMIN = ex.TMIN;
        double TAV = ex.TAV;
        double DEPIR = ex.DEPIR;
        double BIOMAS = ex.BIOMAS;
        double MULCHMASS = ex.MULCHMASS;
        double SNOW = ex.SNOW;
        double CUMDPT;
        double[] DSMID =  new double [NL];
        double TDL;
        double[] TMA =  new double [5];
        int NDays;
        int[] WetDay =  new int [30];
        double X2_PREV;
        double SRFTEMP;
        double[] ST =  new double [NL];
        CUMDPT = 0.0;
        DSMID = new double[NL];
        TDL = 0.0;
        TMA = new double[5];
        NDays = 0;
        WetDay = new int[30];
        X2_PREV = 0.0;
        SRFTEMP = 0.0;
        ST = new double[NL];
        int I;
        int L;
        double ABD;
        double B;
        double DP;
        double FX;
        double PESW;
        double TBD;
        double WW;
        double TLL;
        double TSW;
        double X2_AVG;
        double WFT;
        double BCV;
        double CV;
        double BCV1;
        double BCV2;
        double[] SWI =  new double [NL];
        SWI = SW;
        TBD = 0.0;
        TLL = 0.0;
        TSW = 0.0;
        TDL = 0.0;
        CUMDPT = 0.0;
        for (L=1 ; L!=NLAYR + 1 ; L+=1)
        {
            DSMID[L - 1] = CUMDPT + (DLAYR[(L - 1)] * 5.0);
            CUMDPT = CUMDPT + (DLAYR[(L - 1)] * 10.0);
            TBD = TBD + (BD[(L - 1)] * DLAYR[(L - 1)]);
            TLL = TLL + (LL[(L - 1)] * DLAYR[(L - 1)]);
            TSW = TSW + (SWI[(L - 1)] * DLAYR[(L - 1)]);
            TDL = TDL + (DUL[(L - 1)] * DLAYR[(L - 1)]);
        }
        if (ISWWAT == "Y")
        {
            PESW = Math.Max(0.0, TSW - TLL);
        }
        else
        {
            PESW = Math.Max(0.0, TDL - TLL);
        }
        ABD = TBD / DS[(NLAYR - 1)];
        FX = ABD / (ABD + (686.0 * Math.Exp(-(5.63 * ABD))));
        DP = 1000.0 + (2500.0 * FX);
        WW = 0.356 - (0.144 * ABD);
        B = Math.Log(500.0 / DP);
        for (I=1 ; I!=5 + 1 ; I+=1)
        {
            TMA[I - 1] = (int)(TAVG * 10000.0d) / 10000.0d;
        }
        X2_AVG = TMA[(1 - 1)] * 5.0;
        for (L=1 ; L!=NLAYR + 1 ; L+=1)
        {
            ST[L - 1] = TAVG;
        }
        WFT = 0.1;
        for (var i = 0; i < 30; i++){WetDay[i] = 0;}
        NDays = 0;
        CV = MULCHMASS / 1000.0d;
        BCV1 = CV / (CV + Math.Exp(5.3396 - (2.3951 * CV)));
        BCV2 = SNOW / (SNOW + Math.Exp(2.303 - (0.2197 * SNOW)));
        BCV = Math.Max(BCV1, BCV2);
        for (I=1 ; I!=8 + 1 ; I+=1)
        {
            SOILT_EPIC(NL, B, BCV, CUMDPT, DP, DSMID, NLAYR, PESW, TAV, TAVG, TMAX, TMIN, 0, WFT, WW, ref TMA, ref ST, ref X2_PREV, out SRFTEMP, out X2_AVG);
        }
        s.CUMDPT= CUMDPT;
        s.DSMID= DSMID;
        s.TDL= TDL;
        s.TMA= TMA;
        s.NDays= NDays;
        s.WetDay= WetDay;
        s.X2_PREV= X2_PREV;
        s.SRFTEMP= SRFTEMP;
        s.ST= ST;
    }

    private int _NL;
    /// <summary>
    /// Gets and sets the Number of soil layers
    /// </summary>
    [Description("Number of soil layers")] 
    [Units("dimensionless")] 
    //[Crop2ML(datatype="INT", min=null, max=null, default=, parametercategory=constant, inputtype="parameter")] 
    public int NL
    {
        get { return this._NL; }
        set { this._NL= value; } 
    }

    private string _ISWWAT;
    /// <summary>
    /// Gets and sets the Water simulation control switch (Y or N)
    /// </summary>
    [Description("Water simulation control switch (Y or N)")] 
    [Units("dimensionless")] 
    //[Crop2ML(datatype="STRING", min=null, max=null, default=Y, parametercategory=constant, inputtype="parameter")] 
    public string ISWWAT
    {
        get { return this._ISWWAT; }
        set { this._ISWWAT= value; } 
    }

    private double[] _BD;
    /// <summary>
    /// Gets and sets the Bulk density, soil layer NL
    /// </summary>
    [Description("Bulk density, soil layer NL")] 
    [Units("g [soil] / cm3 [soil]")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=soil, inputtype="parameter")] 
    public double[] BD
    {
        get { return this._BD; }
        set { this._BD= value; } 
    }

    private double[] _DLAYR;
    /// <summary>
    /// Gets and sets the Thickness of soil layer NL
    /// </summary>
    [Description("Thickness of soil layer NL")] 
    [Units("cm")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=constant, inputtype="parameter")] 
    public double[] DLAYR
    {
        get { return this._DLAYR; }
        set { this._DLAYR= value; } 
    }

    private double[] _DS;
    /// <summary>
    /// Gets and sets the Cumulative depth in soil layer NL
    /// </summary>
    [Description("Cumulative depth in soil layer NL")] 
    [Units("cm")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=soil, inputtype="parameter")] 
    public double[] DS
    {
        get { return this._DS; }
        set { this._DS= value; } 
    }

    private double[] _DUL;
    /// <summary>
    /// Gets and sets the Volumetric soil water content at Drained Upper Limit in soil layer L
    /// </summary>
    [Description("Volumetric soil water content at Drained Upper Limit in soil layer L")] 
    [Units("cm3[water]/cm3[soil]")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=soil, inputtype="parameter")] 
    public double[] DUL
    {
        get { return this._DUL; }
        set { this._DUL= value; } 
    }

    private double[] _LL;
    /// <summary>
    /// Gets and sets the Volumetric soil water content in soil layer NL at lower limit
    /// </summary>
    [Description("Volumetric soil water content in soil layer NL at lower limit")] 
    [Units("cm3 [water] / cm3 [soil]")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=soil, inputtype="parameter")] 
    public double[] LL
    {
        get { return this._LL; }
        set { this._LL= value; } 
    }

    private int _NLAYR;
    /// <summary>
    /// Gets and sets the Actual number of soil layers
    /// </summary>
    [Description("Actual number of soil layers")] 
    [Units("dimensionless")] 
    //[Crop2ML(datatype="INT", min=null, max=null, default=, parametercategory=constant, inputtype="parameter")] 
    public int NLAYR
    {
        get { return this._NLAYR; }
        set { this._NLAYR= value; } 
    }

    private double[] _SW;
    /// <summary>
    /// Gets and sets the Volumetric soil water content in layer NL
    /// </summary>
    [Description("Volumetric soil water content in layer NL")] 
    [Units("cm3 [water] / cm3 [soil]")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=soil, inputtype="parameter")] 
    public double[] SW
    {
        get { return this._SW; }
        set { this._SW= value; } 
    }

    
    /// <summary>
    /// Constructor of the STEMP_EPIC component")
    /// </summary>  
    public STEMP_EPIC() { }
    
    /// <summary>
    /// Algorithm of the STEMP_EPIC component
    /// </summary>
    public void  CalculateModel(STEMP_EPIC_State s, STEMP_EPIC_State s1, STEMP_EPIC_Rate r, STEMP_EPIC_Auxiliary a, STEMP_EPIC_Exogenous ex)
    {
        double TAMP = ex.TAMP;
        double RAIN = ex.RAIN;
        double TAVG = ex.TAVG;
        double TMAX = ex.TMAX;
        double TMIN = ex.TMIN;
        double TAV = ex.TAV;
        double CUMDPT = s.CUMDPT;
        double[] DSMID = s.DSMID;
        double TDL = s.TDL;
        double[] TMA = s.TMA;
        int NDays = s.NDays;
        int[] WetDay = s.WetDay;
        double X2_PREV = s.X2_PREV;
        double SRFTEMP = s.SRFTEMP;
        double[] ST = s.ST;
        double DEPIR = ex.DEPIR;
        double BIOMAS = ex.BIOMAS;
        double MULCHMASS = ex.MULCHMASS;
        double SNOW = ex.SNOW;
        int I;
        int L;
        int NWetDays;
        double ABD;
        double B;
        double DP;
        double FX;
        double PESW;
        double TBD;
        double WW;
        double TLL;
        double TSW;
        double X2_AVG;
        double WFT;
        double BCV;
        double CV;
        double BCV1;
        double BCV2;
        TBD = 0.0;
        TLL = 0.0;
        TSW = 0.0;
        for (L=1 ; L!=NLAYR + 1 ; L+=1)
        {
            TBD = TBD + (BD[(L - 1)] * DLAYR[(L - 1)]);
            TDL = TDL + (DUL[(L - 1)] * DLAYR[(L - 1)]);
            TLL = TLL + (LL[(L - 1)] * DLAYR[(L - 1)]);
            TSW = TSW + (SW[(L - 1)] * DLAYR[(L - 1)]);
        }
        ABD = TBD / DS[(NLAYR - 1)];
        FX = ABD / (ABD + (686.0 * Math.Exp(-(5.63 * ABD))));
        DP = 1000.0 + (2500.0 * FX);
        WW = 0.356 - (0.144 * ABD);
        B = Math.Log(500.0 / DP);
        if (ISWWAT == "Y")
        {
            PESW = Math.Max(0.0, TSW - TLL);
        }
        else
        {
            PESW = Math.Max(0.0, TDL - TLL);
        }
        if (NDays == 30)
        {
            for (I=1 ; I!=29 + 1 ; I+=1)
            {
                WetDay[I - 1] = WetDay[I + 1 - 1];
            }
        }
        else
        {
            NDays = NDays + 1;
        }
        if (RAIN + DEPIR > 1.0E-6)
        {
            WetDay[NDays - 1] = 1;
        }
        else
        {
            WetDay[NDays - 1] = 0;
        }
        NWetDays = WetDay.Sum();
        WFT = (double)(NWetDays) / (double)(NDays);
        CV = (BIOMAS + MULCHMASS) / 1000.0d;
        BCV1 = CV / (CV + Math.Exp(5.3396 - (2.3951 * CV)));
        BCV2 = SNOW / (SNOW + Math.Exp(2.303 - (0.2197 * SNOW)));
        BCV = Math.Max(BCV1, BCV2);
        SOILT_EPIC(NL, B, BCV, CUMDPT, DP, DSMID, NLAYR, PESW, TAV, TAVG, TMAX, TMIN, WetDay[NDays - 1], WFT, WW, ref TMA, ref ST, ref X2_PREV, out SRFTEMP, out X2_AVG);
        s.CUMDPT= CUMDPT;
        s.DSMID= DSMID;
        s.TDL= TDL;
        s.TMA= TMA;
        s.NDays= NDays;
        s.WetDay= WetDay;
        s.X2_PREV= X2_PREV;
        s.SRFTEMP= SRFTEMP;
        s.ST= ST;
    }
    /// <summary>
    /// 
    /// </summary>
    public static void  SOILT_EPIC(int NL, double B, double BCV, double CUMDPT, double DP, double[] DSMID, int NLAYR, double PESW, double TAV, double TAVG, double TMAX, double TMIN, int WetDay, double WFT, double WW, ref double[] TMA, ref double[] ST, ref double X2_PREV, out double SRFTEMP, out double X2_AVG)
    {
        int K;
        int L;
        double DD;
        double FX;
        double WC;
        double ZD;
        double X1;
        double X2;
        double X3;
        double F;
        double LAG;
        LAG = 0.5;
        WC = Math.Max(0.01, PESW) / (WW * CUMDPT) * 10.0;
        FX = Math.Exp(B * Math.Pow((1.0 - WC) / (1.0 + WC), 2));
        DD = FX * DP;
        if (WetDay > 0)
        {
            X2 = WFT * (TAVG - TMIN) + TMIN;
        }
        else
        {
            X2 = WFT * (TMAX - TAVG) + TAVG + 2.0d;
        }
        TMA[1 - 1] = X2;
        for (K=5 ; K!=2 - 1 ; K+=-1)
        {
            TMA[K - 1] = TMA[K - 1 - 1];
        }
        X2_AVG = TMA.Sum() / 5.0;
        X3 = (1.0d - BCV) * X2_AVG + (BCV * X2_PREV);
        SRFTEMP = Math.Min(X2_AVG, X3);
        X1 = TAV - X3;
        for (L=1 ; L!=NLAYR + 1 ; L+=1)
        {
            ZD = DSMID[(L - 1)] / DD;
            F = ZD / (ZD + Math.Exp(-.8669 - (2.0775 * ZD)));
            ST[L - 1] = LAG * ST[(L - 1)] + ((1.0d - LAG) * (F * X1 + X3));
        }
        X2_PREV = X2_AVG;
    }
}