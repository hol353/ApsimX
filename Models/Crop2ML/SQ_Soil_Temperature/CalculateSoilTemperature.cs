using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
namespace Models.Crop2ML.SQ_Soil_Temperature;

/// <summary>
///- Name: CalculateSoilTemperature -Version: 001, -Time step: 1
///- Description:
///            * Title: CalculateSoilTemperature model
///            * Authors: loic.manceau@inra.fr
///            * Reference: ('http://biomamodelling.org',)
///            * Institution: INRA
///            * ExtendedDescription: Calculation of minimum and maximum Soil temperature, Further used in shoot temperature estimate.
///            * ShortDescription: None
///- inputs:
///            * name: meanTAir
///                          ** description : Mean Air Temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 80
///                          ** min : -30
///                          ** default : 22
///                          ** unit : Â°C
///            * name: minTAir
///                          ** description : Minimum Air Temperature from Weather files
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 80
///                          ** min : -30
///                          ** default : 20
///                          ** unit : Â°C
///            * name: lambda_
///                          ** description : Latente heat of water vaporization at 20Â°C
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 10
///                          ** min : 0
///                          ** default : 2.454
///                          ** unit : MJ.kg-1
///            * name: deepLayerT
///                          ** description : Temperature of the last soil layer
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 80
///                          ** min : -30
///                          ** default : 20
///                          ** unit : Â°C
///            * name: meanAnnualAirTemp
///                          ** description : Annual Mean Air Temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 80
///                          ** min : -30
///                          ** default : 22
///                          ** unit : Â°C
///            * name: heatFlux
///                          ** description : Soil Heat Flux from Energy Balance Component
///                          ** inputtype : variable
///                          ** variablecategory : rate
///                          ** datatype : DOUBLE
///                          ** max : 100
///                          ** min : 0
///                          ** default : 50
///                          ** unit : g m-2 d-1
///            * name: maxTAir
///                          ** description : Maximum Air Temperature from Weather Files
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 80
///                          ** min : -30
///                          ** default : 25
///                          ** unit : Â°C
///- outputs:
///            * name: minTSoil
///                          ** description : Minimum Soil Temperature
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 80
///                          ** min : -30
///                          ** unit : Â°C
///            * name: deepLayerT
///                          ** description : Temperature of the last soil layer
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 80
///                          ** min : -30
///                          ** unit : Â°C
///            * name: maxTSoil
///                          ** description : Maximum Soil Temperature
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 80
///                          ** min : -30
///                          ** unit : Â°C
/// </summary>
public class CalculateSoilTemperature
{

    /// <summary>
    /// initialization of the CalculateSoilTemperature component
    /// </summary>
    public void Init(SoilTemperatureState s, SoilTemperatureState s1, SoilTemperatureRate r, SoilTemperatureAuxiliary a, SoilTemperatureExogenous ex)
    {
        double meanTAir = ex.meanTAir;
        double minTAir = ex.minTAir;
        double meanAnnualAirTemp = ex.meanAnnualAirTemp;
        double maxTAir = ex.maxTAir;
        double deepLayerT = 20.0;
        deepLayerT = meanAnnualAirTemp;
        s.deepLayerT= deepLayerT;
    }

    private double _lambda_;
    /// <summary>
    /// Gets and sets the Latente heat of water vaporization at 20Â°C
    /// </summary>
    [Description("Latente heat of water vaporization at 20Â°C")]
    [Units("MJ.kg-1")]
    //[Crop2ML(datatype="DOUBLE", min=0, max=10, default=2.454, parametercategory=constant, inputtype="parameter")]
    public double lambda_
    {
        get { return this._lambda_; }
        set { this._lambda_= value; }
    }


    /// <summary>
    /// Constructor of the CalculateSoilTemperature component")
    /// </summary>
    public CalculateSoilTemperature() { }

    /// <summary>
    /// Algorithm of the CalculateSoilTemperature component
    /// </summary>
    public void  CalculateModel(SoilTemperatureState s, SoilTemperatureState s1, SoilTemperatureRate r, SoilTemperatureAuxiliary a, SoilTemperatureExogenous ex)
    {
        double meanTAir = ex.meanTAir;
        double minTAir = ex.minTAir;
        double deepLayerT = s.deepLayerT;
        double meanAnnualAirTemp = ex.meanAnnualAirTemp;
        double heatFlux = r.heatFlux;
        double maxTAir = ex.maxTAir;
        double minTSoil;
        double maxTSoil;
        double tmp;
        tmp = meanAnnualAirTemp;
        if (maxTAir == (double)(-999) && minTAir == (double)(999))
        {
            minTSoil = (double)(999);
            maxTSoil = (double)(-999);
            deepLayerT = 0.0;
        }
        else
        {
            minTSoil = SoilMinimumTemperature(maxTAir, meanTAir, minTAir, heatFlux, lambda_, deepLayerT);
            maxTSoil = SoilMaximumTemperature(maxTAir, meanTAir, minTAir, heatFlux, lambda_, deepLayerT);
            deepLayerT = UpdateTemperature(minTSoil, maxTSoil, deepLayerT);
        }
        s.deepLayerT= deepLayerT;
        s.minTSoil= minTSoil;
        s.maxTSoil= maxTSoil;
    }
    /// <summary>
    ///
    /// </summary>
    public static double SoilTempB(double weatherMinTemp, double deepTemperature)
    {
        return (weatherMinTemp + deepTemperature) / 2.0;
    }
    /// <summary>
    ///
    /// </summary>
    public static double SoilTempA(double weatherMaxTemp, double weatherMeanTemp, double soilHeatFlux, double lambda_)
    {
        double TempAdjustment;
        double SoilAvailableEnergy;
        TempAdjustment = weatherMeanTemp < 8.0 ? -0.5 * weatherMeanTemp + 4.0 : (double)(0);
        SoilAvailableEnergy = soilHeatFlux * lambda_ / 1000;
        double result;
        result = weatherMaxTemp + (11.2 * (1.0 - Math.Exp(-0.07 * (SoilAvailableEnergy - 5.5)))) + TempAdjustment;
        return result;
    }
    /// <summary>
    ///
    /// </summary>
    public static double SoilMinimumTemperature(double weatherMaxTemp, double weatherMeanTemp, double weatherMinTemp, double soilHeatFlux, double lambda_, double deepTemperature)
    {
        return Math.Min(SoilTempA(weatherMaxTemp, weatherMeanTemp, soilHeatFlux, lambda_), SoilTempB(weatherMinTemp, deepTemperature));
    }

    /// <summary>
    ///
    /// </summary>
    public static double SoilMaximumTemperature(double weatherMaxTemp, double weatherMeanTemp, double weatherMinTemp, double soilHeatFlux, double lambda_, double deepTemperature)
    {
        return Math.Max(SoilTempA(weatherMaxTemp, weatherMeanTemp, soilHeatFlux, lambda_), SoilTempB(weatherMinTemp, deepTemperature));
    }
    /// <summary>
    ///
    /// </summary>
    public static double UpdateTemperature(double minSoilTemp, double maxSoilTemp, double Temperature)
    {
        double meanTemp;
        meanTemp = (minSoilTemp + maxSoilTemp) / 2.0;
        Temperature = (9.0 * Temperature + meanTemp) / 10.0;
        return Temperature;
    }
}