using System;
using System.Collections.Generic;
using System.Linq;    
using Models.Core;   
namespace Models.Crop2ML;

/// <summary>
///- Name: SnowCoverCalculator -Version: 001, -Time step: 1
///- Description:
///            * Title: SnowCoverCalculator model
///            * Authors: Gunther Krauss
///            * Reference: ('http://www.simplace.net/doc/simplace_modules/',)
///            * Institution: INRES Pflanzenbau, Uni Bonn
///            * ExtendedDescription: as given in the documentation
///            * ShortDescription: None
///- inputs:
///            * name: cCarbonContent
///                          ** description : Carbon content of upper soil layer
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 20.0
///                          ** min : 0.5
///                          ** default : 0.5
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/percent
///            * name: cInitialAgeOfSnow
///                          ** description : Initial age of snow
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : INT
///                          ** max : 
///                          ** min : 0
///                          ** default : 0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/percent
///            * name: cInitialSnowWaterContent
///                          ** description : Initial snow water content
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 1500.0
///                          ** min : 0.0
///                          ** default : 0.0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/percent
///            * name: Albedo
///                          ** description : Albedo
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 1.0
///                          ** min : 0.0
///                          ** default : 
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/one
///            * name: pInternalAlbedo
///                          ** description : Albedo privat
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 1.0
///                          ** min : 0.0
///                          ** default : 
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/one
///            * name: cSnowIsolationFactorA
///                          ** description : Static part of the snow isolation index calculation
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 10.0
///                          ** min : 0.0
///                          ** default : 2.3
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/one
///            * name: cSnowIsolationFactorB
///                          ** description : Dynamic part of the snow isolation index calculation
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 1.0
///                          ** min : 0.0
///                          ** default : 0.22
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/one
///            * name: iTempMax
///                          ** description : Daily maximum air temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 50.0
///                          ** min : -40.0
///                          ** default : 
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius
///            * name: iTempMin
///                          ** description : Daily minimum air temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 50.0
///                          ** min : -40.0
///                          ** default : 
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius
///            * name: iRadiation
///                          ** description : Global Solar radiation
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 2000.0
///                          ** min : 0.0
///                          ** default : 
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/megajoule_per_square_metre
///            * name: iRAIN
///                          ** description : Rain amount
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 60.0
///                          ** min : 0.0
///                          ** default : 0.0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/millimetre
///            * name: iCropResidues
///                          ** description : Crop residues plus above ground biomass
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 20000.0
///                          ** min : 0.0
///                          ** default : 
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/gram_per_square_metre
///            * name: iPotentialSoilEvaporation
///                          ** description : Potenial Evaporation
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 12.0
///                          ** min : 0.0
///                          ** default : 0.0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/millimetre
///            * name: iLeafAreaIndex
///                          ** description : Leaf area index
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 10.0
///                          ** min : 0.0
///                          ** default : 
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/square_metre_per_square_metre
///            * name: iSoilTempArray
///                          ** description : Soil Temp array of last day
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLEARRAY
///                          ** len : 
///                          ** max : 35.0
///                          ** min : -15.0
///                          ** default : 
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius
///            * name: SnowWaterContent
///                          ** description : Snow water content
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 1500.0
///                          ** min : 0.0
///                          ** default : 0.0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/millimetre
///            * name: SoilSurfaceTemperature
///                          ** description : Soil surface temperature
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 70.0
///                          ** min : -40.0
///                          ** default : 0.0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius
///            * name: AgeOfSnow
///                          ** description : Age of snow
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : INT
///                          ** max : 
///                          ** min : 0
///                          ** default : 0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/day
///- outputs:
///            * name: SnowWaterContent
///                          ** description : Snow water content
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 1500.0
///                          ** min : 0.0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/millimetre
///            * name: SoilSurfaceTemperature
///                          ** description : Soil surface temperature
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 70.0
///                          ** min : -40.0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius
///            * name: AgeOfSnow
///                          ** description : Age of snow
///                          ** datatype : INT
///                          ** variablecategory : state
///                          ** max : 
///                          ** min : 0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/day
///            * name: rSnowWaterContentRate
///                          ** description : daily snow water content change rate
///                          ** datatype : DOUBLE
///                          ** variablecategory : rate
///                          ** max : 1500.0
///                          ** min : -1500.0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/millimetre_per_day
///            * name: rSoilSurfaceTemperatureRate
///                          ** description : daily soil surface temperature change rate
///                          ** datatype : DOUBLE
///                          ** variablecategory : rate
///                          ** max : 70.0
///                          ** min : -40.0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius_per_day
///            * name: rAgeOfSnowRate
///                          ** description : daily age of snow change rate
///                          ** datatype : INT
///                          ** variablecategory : rate
///                          ** max : 
///                          ** min : 
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/one
///            * name: SnowIsolationIndex
///                          ** description : Snow isolation index
///                          ** datatype : DOUBLE
///                          ** variablecategory : auxiliary
///                          ** max : 1.0
///                          ** min : 0.0
///                          ** unit : http://www.wurvoc.org/vocabularies/om-1.8/one
/// </summary>
public class SnowCoverCalculator
{

    /// <summary>
    /// initialization of the SnowCoverCalculator component
    /// </summary>
    public void Init(SoilTemperatureState s, SoilTemperatureState s1, SoilTemperatureRate r, SoilTemperatureAuxiliary a, SoilTemperatureExogenous ex)
    {
        double iTempMax = ex.iTempMax;
        double iTempMin = ex.iTempMin;
        double iRadiation = ex.iRadiation;
        double iRAIN = ex.iRAIN;
        double iCropResidues = ex.iCropResidues;
        double iPotentialSoilEvaporation = ex.iPotentialSoilEvaporation;
        double iLeafAreaIndex = ex.iLeafAreaIndex;
        double[] iSoilTempArray = ex.iSoilTempArray;
        double pInternalAlbedo;
        double SnowWaterContent = 0.0;
        double SoilSurfaceTemperature = 0.0;
        int AgeOfSnow = 0;
        pInternalAlbedo = 0.00d;
        double TMEAN;
        double TAMPL;
        double DST;
        if (Albedo == (double)(0))
        {
            pInternalAlbedo = 0.02260d * Math.Log(cCarbonContent, 10) + 0.15020d;
        }
        else
        {
            pInternalAlbedo = Albedo;
        }
        TMEAN = 0.50d * (iTempMax + iTempMin);
        TAMPL = 0.50d * (iTempMax - iTempMin);
        DST = TMEAN + (TAMPL * (iRadiation * (1 - pInternalAlbedo) - 14) / 20);
        SoilSurfaceTemperature = DST;
        AgeOfSnow = cInitialAgeOfSnow;
        SnowWaterContent = cInitialSnowWaterContent;
        s.pInternalAlbedo= pInternalAlbedo;
        s.SnowWaterContent= SnowWaterContent;
        s.SoilSurfaceTemperature= SoilSurfaceTemperature;
        s.AgeOfSnow= AgeOfSnow;
    }

    private double _cCarbonContent;
    /// <summary>
    /// Gets and sets the Carbon content of upper soil layer
    /// </summary>
    [Description("Carbon content of upper soil layer")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/percent")] 
    //[Crop2ML(datatype="DOUBLE", min=0.5, max=20.0, default=0.5, parametercategory=constant, inputtype="parameter")] 
    public double cCarbonContent
    {
        get { return this._cCarbonContent; }
        set { this._cCarbonContent= value; } 
    }

    private int _cInitialAgeOfSnow;
    /// <summary>
    /// Gets and sets the Initial age of snow
    /// </summary>
    [Description("Initial age of snow")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/percent")] 
    //[Crop2ML(datatype="INT", min=0, max=null, default=0, parametercategory=constant, inputtype="parameter")] 
    public int cInitialAgeOfSnow
    {
        get { return this._cInitialAgeOfSnow; }
        set { this._cInitialAgeOfSnow= value; } 
    }

    private double _cInitialSnowWaterContent;
    /// <summary>
    /// Gets and sets the Initial snow water content
    /// </summary>
    [Description("Initial snow water content")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/percent")] 
    //[Crop2ML(datatype="DOUBLE", min=0.0, max=1500.0, default=0.0, parametercategory=constant, inputtype="parameter")] 
    public double cInitialSnowWaterContent
    {
        get { return this._cInitialSnowWaterContent; }
        set { this._cInitialSnowWaterContent= value; } 
    }

    private double _Albedo;
    /// <summary>
    /// Gets and sets the Albedo
    /// </summary>
    [Description("Albedo")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/one")] 
    //[Crop2ML(datatype="DOUBLE", min=0.0, max=1.0, default=, parametercategory=constant, inputtype="parameter")] 
    public double Albedo
    {
        get { return this._Albedo; }
        set { this._Albedo= value; } 
    }

    private double _cSnowIsolationFactorA;
    /// <summary>
    /// Gets and sets the Static part of the snow isolation index calculation
    /// </summary>
    [Description("Static part of the snow isolation index calculation")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/one")] 
    //[Crop2ML(datatype="DOUBLE", min=0.0, max=10.0, default=2.3, parametercategory=constant, inputtype="parameter")] 
    public double cSnowIsolationFactorA
    {
        get { return this._cSnowIsolationFactorA; }
        set { this._cSnowIsolationFactorA= value; } 
    }

    private double _cSnowIsolationFactorB;
    /// <summary>
    /// Gets and sets the Dynamic part of the snow isolation index calculation
    /// </summary>
    [Description("Dynamic part of the snow isolation index calculation")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/one")] 
    //[Crop2ML(datatype="DOUBLE", min=0.0, max=1.0, default=0.22, parametercategory=constant, inputtype="parameter")] 
    public double cSnowIsolationFactorB
    {
        get { return this._cSnowIsolationFactorB; }
        set { this._cSnowIsolationFactorB= value; } 
    }

    
    /// <summary>
    /// Constructor of the SnowCoverCalculator component")
    /// </summary>  
    public SnowCoverCalculator() { }
    
    /// <summary>
    /// Algorithm of the SnowCoverCalculator component
    /// </summary>
    public void  CalculateModel(SoilTemperatureState s, SoilTemperatureState s1, SoilTemperatureRate r, SoilTemperatureAuxiliary a, SoilTemperatureExogenous ex)
    {
        double pInternalAlbedo = s.pInternalAlbedo;
        double iTempMax = ex.iTempMax;
        double iTempMin = ex.iTempMin;
        double iRadiation = ex.iRadiation;
        double iRAIN = ex.iRAIN;
        double iCropResidues = ex.iCropResidues;
        double iPotentialSoilEvaporation = ex.iPotentialSoilEvaporation;
        double iLeafAreaIndex = ex.iLeafAreaIndex;
        double[] iSoilTempArray = ex.iSoilTempArray;
        double SnowWaterContent = s.SnowWaterContent;
        double SoilSurfaceTemperature = s.SoilSurfaceTemperature;
        int AgeOfSnow = s.AgeOfSnow;
        double rSnowWaterContentRate;
        double rSoilSurfaceTemperatureRate;
        int rAgeOfSnowRate;
        double SnowIsolationIndex;
        double tiCropResidues;
        double tiSoilTempArray;
        double TMEAN;
        double TAMPL;
        double DST;
        double tSoilSurfaceTemperature;
        double tSnowIsolationIndex;
        double SNOWEVAPORATION;
        double SNOWMELT;
        double EAJ;
        double ageOfSnowFactor;
        double SNPKT;
        tiCropResidues = iCropResidues * 10.00d;
        tiSoilTempArray = iSoilTempArray[0];
        TMEAN = 0.50d * (iTempMax + iTempMin);
        TAMPL = 0.50d * (iTempMax - iTempMin);
        DST = TMEAN + (TAMPL * (iRadiation * (1 - pInternalAlbedo) - 14) / 20);
        if (iRAIN > (double)(0) && (tiSoilTempArray < (double)(1) || (SnowWaterContent > (double)(3) || SoilSurfaceTemperature < (double)(0))))
        {
            SnowWaterContent = SnowWaterContent + iRAIN;
        }
        tSnowIsolationIndex = 1.00d;
        if (tiCropResidues < (double)(10))
        {
            tSnowIsolationIndex = tiCropResidues / (tiCropResidues + Math.Exp(5.340d - (2.40d * tiCropResidues)));
        }
        if (SnowWaterContent < 1E-10)
        {
            tSnowIsolationIndex = tSnowIsolationIndex * 0.850d;
            tSoilSurfaceTemperature = 0.50d * (DST + ((1 - tSnowIsolationIndex) * DST) + (tSnowIsolationIndex * tiSoilTempArray));
        }
        else
        {
            tSnowIsolationIndex = Math.Max(SnowWaterContent / (SnowWaterContent + Math.Exp(cSnowIsolationFactorA - (cSnowIsolationFactorB * SnowWaterContent))), tSnowIsolationIndex);
            tSoilSurfaceTemperature = (1 - tSnowIsolationIndex) * DST + (tSnowIsolationIndex * tiSoilTempArray);
        }
        if (SnowWaterContent == (double)(0) && !(iRAIN > (double)(0) && tiSoilTempArray < (double)(1)))
        {
            SnowWaterContent = (double)(0);
        }
        else
        {
            EAJ = .50d;
            if (SnowWaterContent < (double)(5))
            {
                EAJ = Math.Exp(-Math.Max((0.40d * iLeafAreaIndex), (0.10d * (tiCropResidues + 0.10d))));
            }
            SNOWEVAPORATION = iPotentialSoilEvaporation * EAJ;
            ageOfSnowFactor = AgeOfSnow / (AgeOfSnow + Math.Exp(5.340d - (2.3950d * AgeOfSnow)));
            SNPKT = .33330d * (2 * Math.Min(tSoilSurfaceTemperature, tiSoilTempArray) + iTempMax);
            if (TMEAN > (double)(0))
            {
                SNOWMELT = Math.Max(0, Math.Sqrt(iTempMax * iRadiation) * (1.520d + (.540d * ageOfSnowFactor * SNPKT)));
            }
            else
            {
                SNOWMELT = (double)(0);
            }
            if (SNOWMELT + SNOWEVAPORATION > SnowWaterContent)
            {
                SNOWMELT = SNOWMELT / (SNOWMELT + SNOWEVAPORATION) * SnowWaterContent;
                SNOWEVAPORATION = SNOWEVAPORATION / (SNOWMELT + SNOWEVAPORATION) * SnowWaterContent;
            }
            SnowWaterContent = SnowWaterContent - (SNOWMELT + SNOWEVAPORATION);
            if (SnowWaterContent < (double)(0))
            {
                SnowWaterContent = (double)(0);
            }
            if (SnowWaterContent < (double)(5))
            {
                AgeOfSnow = 0;
            }
            else
            {
                AgeOfSnow = AgeOfSnow + 1;
            }
        }
        rSnowWaterContentRate = SnowWaterContent - SnowWaterContent;
        rSoilSurfaceTemperatureRate = tSoilSurfaceTemperature - SoilSurfaceTemperature;
        rAgeOfSnowRate = AgeOfSnow - AgeOfSnow;
        SoilSurfaceTemperature = tSoilSurfaceTemperature;
        SnowIsolationIndex = tSnowIsolationIndex;
        s.SnowWaterContent= SnowWaterContent;
        s.SoilSurfaceTemperature= SoilSurfaceTemperature;
        s.AgeOfSnow= AgeOfSnow;
        r.rSnowWaterContentRate = rSnowWaterContentRate;
        r.rSoilSurfaceTemperatureRate = rSoilSurfaceTemperatureRate;
        r.rAgeOfSnowRate = rAgeOfSnowRate;
        a.SnowIsolationIndex= SnowIsolationIndex;
    }
}