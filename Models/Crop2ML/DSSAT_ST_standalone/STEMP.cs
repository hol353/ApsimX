using System;
using System.Collections.Generic;
using System.Linq;    
using Models.Core;   
namespace Models.Crop2ML.DSSAT_ST_standalone;

/// <summary>
///- Name: STEMP -Version:  1.0, -Time step:  1
///- Description:
///            * Title: Model of STEMP
///            * Authors: DSSAT 
///            * Reference: https://doi.org/10.2134/agronj1994.00021962008600060014x
///            * Institution: DSSAT Florida
///            * ExtendedDescription: None
///            * ShortDescription: Determines soil temperature by layer
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
///                          ** description : Water simulation control switch
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
///                          ** description : Thickness of soil layer L
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLEARRAY
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : cm
///            * name: DS
///                          ** description : Cumulative depth in soil layer L
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
///                          ** description : Volumetric soil water content in soil layer L at lower limit
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
///            * name: MSALB
///                          ** description : Soil albedo with mulch and soil water effects
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : dimensionless
///            * name: SRAD
///                          ** description : Solar radiation
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : MJ/m2-d
///            * name: SW
///                          ** description : Volumetric soil water content in layer L
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
///            * name: XLAT
///                          ** description : Latitude
///                          ** inputtype : parameter
///                          ** parametercategory : constant
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
///            * name: TAMP
///                          ** description : Amplitude of temperature function used to calculate soil temperatures
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
///                          ** description : Depth to midpoint of soil layer L
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
///                          ** description : Array of previous 5 days of average soil temperatures
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : 5
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : degC
///            * name: ATOT
///                          ** description : Sum of TMA array (last 5 days soil temperature)
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
///                          ** description : Soil temperature in soil layer L
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : degC
///            * name: DOY
///                          ** description : Current day of simulation
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : INT
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : d
///            * name: HDAY
///                          ** description : Haverst day
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : day
///- outputs:
///            * name: CUMDPT
///                          ** description : Cumulative depth of soil profile
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 
///                          ** min : 
///                          ** unit : mm
///            * name: DSMID
///                          ** description : Depth to midpoint of soil layer L
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
///                          ** description : Array of previous 5 days of average soil temperatures
///                          ** datatype : DOUBLEARRAY
///                          ** variablecategory : state
///                          ** len : 5
///                          ** max : 
///                          ** min : 
///                          ** unit : degC
///            * name: ATOT
///                          ** description : Sum of TMA array (last 5 days soil temperature)
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
///                          ** description : Soil temperature in soil layer L
///                          ** datatype : DOUBLEARRAY
///                          ** variablecategory : state
///                          ** len : NL
///                          ** max : 
///                          ** min : 
///                          ** unit : degC
/// </summary>
public class STEMP
{

    /// <summary>
    /// initialization of the STEMP component
    /// </summary>
    public void Init(STEMP_State s, STEMP_State s1, STEMP_Rate r, STEMP_Auxiliary a, STEMP_Exogenous ex)
    {
        double SRAD = ex.SRAD;
        double TAVG = ex.TAVG;
        double TMAX = ex.TMAX;
        double TAV = ex.TAV;
        double TAMP = ex.TAMP;
        int DOY = ex.DOY;
        double CUMDPT;
        double[] DSMID =  new double [NL];
        double TDL;
        double[] TMA =  new double [5];
        double ATOT;
        double SRFTEMP;
        double[] ST =  new double [NL];
        double HDAY;
        CUMDPT = 0.0;
        DSMID = new double[NL];
        TDL = 0.0;
        TMA = new double[5];
        ATOT = 0.0;
        SRFTEMP = 0.0;
        ST = new double[NL];
        HDAY = 0.0;
        int I;
        int L;
        double ABD;
        double ALBEDO;
        double B;
        double DP;
        double FX;
        double PESW;
        double TBD;
        double WW;
        double TLL;
        double TSW;
        double[] DLI =  new double [NL];
        double[] DSI =  new double [NL];
        double[] SWI =  new double [NL];
        SWI = SW;
        DSI = DS;
        if (XLAT < 0.0)
        {
            HDAY = 20.0;
        }
        else
        {
            HDAY = 200.0;
        }
        TBD = 0.0;
        TLL = 0.0;
        TSW = 0.0;
        TDL = 0.0;
        CUMDPT = 0.0;
        for (L=1 ; L!=NLAYR + 1 ; L+=1)
        {
            if (L == 1)
            {
                DLI[L - 1] = DSI[L - 1];
            }
            else
            {
                DLI[L - 1] = DSI[L - 1] - DSI[L - 1 - 1];
            }
            DSMID[L - 1] = CUMDPT + (DLI[(L - 1)] * 5.0);
            CUMDPT = CUMDPT + (DLI[(L - 1)] * 10.0);
            TBD = TBD + (BD[(L - 1)] * DLI[(L - 1)]);
            TLL = TLL + (LL[(L - 1)] * DLI[(L - 1)]);
            TSW = TSW + (SWI[(L - 1)] * DLI[(L - 1)]);
            TDL = TDL + (DUL[(L - 1)] * DLI[(L - 1)]);
        }
        if (ISWWAT == "Y")
        {
            PESW = Math.Max(0.0, TSW - TLL);
        }
        else
        {
            PESW = Math.Max(0.0, TDL - TLL);
        }
        ABD = TBD / DSI[(NLAYR - 1)];
        FX = ABD / (ABD + (686.0 * Math.Exp(-(5.63 * ABD))));
        DP = 1000.0 + (2500.0 * FX);
        WW = 0.356 - (0.144 * ABD);
        B = Math.Log(500.0 / DP);
        ALBEDO = MSALB;
        for (I=1 ; I!=5 + 1 ; I+=1)
        {
            TMA[I - 1] = (int)(TAVG * 10000.0d) / 10000.0d;
        }
        ATOT = TMA[(1 - 1)] * 5.0;
        for (L=1 ; L!=NLAYR + 1 ; L+=1)
        {
            ST[L - 1] = TAVG;
        }
        for (I=1 ; I!=8 + 1 ; I+=1)
        {
            SOILT(NL, ALBEDO, B, CUMDPT, DOY, DP, HDAY, NLAYR, PESW, SRAD, TAMP, TAV, TAVG, TMAX, WW, DSMID, ref ATOT, ref TMA, out SRFTEMP, out ST);
        }
        s.CUMDPT= CUMDPT;
        s.DSMID= DSMID;
        s.TDL= TDL;
        s.TMA= TMA;
        s.ATOT= ATOT;
        s.SRFTEMP= SRFTEMP;
        s.ST= ST;
        s.HDAY= HDAY;
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
    /// Gets and sets the Water simulation control switch
    /// </summary>
    [Description("Water simulation control switch")] 
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
    /// Gets and sets the Thickness of soil layer L
    /// </summary>
    [Description("Thickness of soil layer L")] 
    [Units("cm")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=constant, inputtype="parameter")] 
    public double[] DLAYR
    {
        get { return this._DLAYR; }
        set { this._DLAYR= value; } 
    }

    private double[] _DS;
    /// <summary>
    /// Gets and sets the Cumulative depth in soil layer L
    /// </summary>
    [Description("Cumulative depth in soil layer L")] 
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
    /// Gets and sets the Volumetric soil water content in soil layer L at lower limit
    /// </summary>
    [Description("Volumetric soil water content in soil layer L at lower limit")] 
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

    private double _MSALB;
    /// <summary>
    /// Gets and sets the Soil albedo with mulch and soil water effects
    /// </summary>
    [Description("Soil albedo with mulch and soil water effects")] 
    [Units("dimensionless")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=, parametercategory=constant, inputtype="parameter")] 
    public double MSALB
    {
        get { return this._MSALB; }
        set { this._MSALB= value; } 
    }

    private double[] _SW;
    /// <summary>
    /// Gets and sets the Volumetric soil water content in layer L
    /// </summary>
    [Description("Volumetric soil water content in layer L")] 
    [Units("cm3 [water] / cm3 [soil]")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=soil, inputtype="parameter")] 
    public double[] SW
    {
        get { return this._SW; }
        set { this._SW= value; } 
    }

    private double _XLAT;
    /// <summary>
    /// Gets and sets the Latitude
    /// </summary>
    [Description("Latitude")] 
    [Units("degC")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=, parametercategory=constant, inputtype="parameter")] 
    public double XLAT
    {
        get { return this._XLAT; }
        set { this._XLAT= value; } 
    }

    
    /// <summary>
    /// Constructor of the STEMP component")
    /// </summary>  
    public STEMP() { }
    
    /// <summary>
    /// Algorithm of the STEMP component
    /// </summary>
    public void  CalculateModel(STEMP_State s, STEMP_State s1, STEMP_Rate r, STEMP_Auxiliary a, STEMP_Exogenous ex)
    {
        double SRAD = ex.SRAD;
        double TAVG = ex.TAVG;
        double TMAX = ex.TMAX;
        double TAV = ex.TAV;
        double TAMP = ex.TAMP;
        double CUMDPT = s.CUMDPT;
        double[] DSMID = s.DSMID;
        double TDL = s.TDL;
        double[] TMA = s.TMA;
        double ATOT = s.ATOT;
        double SRFTEMP = s.SRFTEMP;
        double[] ST = s.ST;
        int DOY = ex.DOY;
        double HDAY = s.HDAY;
        int L;
        double ABD;
        double ALBEDO;
        double B;
        double DP;
        double FX;
        double PESW;
        double TBD;
        double WW;
        double TLL;
        double TSW;
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
        ALBEDO = MSALB;
        if (ISWWAT == "Y")
        {
            PESW = Math.Max(0.0, TSW - TLL);
        }
        else
        {
            PESW = Math.Max(0.0, TDL - TLL);
        }
        SOILT(NL, ALBEDO, B, CUMDPT, DOY, DP, HDAY, NLAYR, PESW, SRAD, TAMP, TAV, TAVG, TMAX, WW, DSMID, ref ATOT, ref TMA, out SRFTEMP, out ST);
        s.CUMDPT= CUMDPT;
        s.DSMID= DSMID;
        s.TDL= TDL;
        s.TMA= TMA;
        s.ATOT= ATOT;
        s.SRFTEMP= SRFTEMP;
        s.ST= ST;
    }
    /// <summary>
    /// 
    /// </summary>
    public static void  SOILT(int NL, double ALBEDO, double B, double CUMDPT, int DOY, double DP, double HDAY, int NLAYR, double PESW, double SRAD, double TAMP, double TAV, double TAVG, double TMAX, double WW, double[] DSMID, ref double ATOT, ref double[] TMA, out double SRFTEMP, out double[] ST)
    {
        int K;
        int L;
        double ALX;
        double DD;
        double DT;
        double FX;
        double TA;
        double WC;
        double ZD;
        ST =  new double [NL];
        ALX = ((double)(DOY) - HDAY) * 0.0174;
        ATOT = ATOT - TMA[5 - 1];
        for (K=5 ; K!=2 - 1 ; K+=-1)
        {
            TMA[K - 1] = TMA[K - 1 - 1];
        }
        TMA[1 - 1] = TAVG;
        TMA[1 - 1] = (int)(TMA[(1 - 1)] * 10000.0d) / 10000.0d;
        ATOT = ATOT + TMA[1 - 1];
        WC = Math.Max(0.01, PESW) / (WW * CUMDPT) * 10.0;
        FX = Math.Exp(B * Math.Pow((1.0 - WC) / (1.0 + WC), 2));
        DD = FX * DP;
        TA = TAV + (TAMP * Math.Cos(ALX) / 2.0);
        DT = ATOT / 5.0 - TA;
        for (L=1 ; L!=NLAYR + 1 ; L+=1)
        {
            ZD = -(DSMID[(L - 1)] / DD);
            ST[L - 1] = TAV + ((TAMP / 2.0 * Math.Cos((ALX + ZD)) + DT) * Math.Exp(ZD));
            ST[L - 1] = (int)(ST[(L - 1)] * 1000.0d) / 1000.0d;
        }
        SRFTEMP = TAV + (TAMP / 2.0d * Math.Cos(ALX) + DT);
    }
}