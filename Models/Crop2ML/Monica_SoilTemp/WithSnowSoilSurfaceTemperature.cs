using System;
using System.Collections.Generic;
using System.Linq;    
using Models.Core;   
namespace Models.Crop2ML.Monica_SoilTemp;

/// <summary>
///- Name: WithSnowSoilSurfaceTemperature -Version: 1, -Time step: 1
///- Description:
///            * Title: Soil surface temperature with potential snow cover
///            * Authors: Michael Berg-Mohnicke
///            * Reference: None
///            * Institution: ZALF e.V.
///            * ExtendedDescription: None
///            * ShortDescription: It calculates the soil surface temperature taking a potential snow cover into account
///        
///- inputs:
///            * name: noSnowSoilSurfaceTemperature
///                          ** description : soilSurfaceTemperature without snow
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 0.0
///                          ** unit : °C
///            * name: soilSurfaceTemperatureBelowSnow
///                          ** description : soilSurfaceTemperature below snow cover
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 0.0
///                          ** unit : °C
///            * name: hasSnowCover
///                          ** description : is soil covered by snow
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : BOOLEAN
///                          ** max : 
///                          ** min : 
///                          ** default : false
///                          ** unit : dimensionless
///- outputs:
///            * name: soilSurfaceTemperature
///                          ** description : soilSurfaceTemperature
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** unit : °C
/// </summary>
public class WithSnowSoilSurfaceTemperature
{

    
    /// <summary>
    /// Constructor of the WithSnowSoilSurfaceTemperature component")
    /// </summary>  
    public WithSnowSoilSurfaceTemperature() { }
    
    /// <summary>
    /// Algorithm of the WithSnowSoilSurfaceTemperature component
    /// </summary>
    public void  CalculateModel(SoilTemperatureCompState s, SoilTemperatureCompState s1, SoilTemperatureCompRate r, SoilTemperatureCompAuxiliary a, SoilTemperatureCompExogenous ex)
    {
        double noSnowSoilSurfaceTemperature = s.noSnowSoilSurfaceTemperature;
        double soilSurfaceTemperatureBelowSnow = ex.soilSurfaceTemperatureBelowSnow;
        bool hasSnowCover = ex.hasSnowCover;
        double soilSurfaceTemperature;
        if (hasSnowCover)
        {
            soilSurfaceTemperature = soilSurfaceTemperatureBelowSnow;
        }
        else
        {
            soilSurfaceTemperature = noSnowSoilSurfaceTemperature;
        }
        s.soilSurfaceTemperature= soilSurfaceTemperature;
    }
}