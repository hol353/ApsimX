using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
namespace Models.Crop2ML.Stics_soil_temperature;

/// <summary>
///- Name: canopy_temp_avg -Version: 1.0, -Time step: 1
///- Description:
///            * Title: canopy_temp_avg model
///            * Authors: None
///            * Reference: doi:http://dx.doi.org/10.1016/j.agrformet.2014.05.002
///            * Institution: INRAE
///            * ExtendedDescription: None
///            * ShortDescription: None
///- inputs:
///            * name: min_canopy_temp
///                          ** description : current minimum temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 50.0
///                          ** min : -50.0
///                          ** default : 0.0
///                          ** unit : degC
///            * name: max_canopy_temp
///                          ** description : current maximum temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 50.0
///                          ** min : -50.0
///                          ** default : 0.0
///                          ** unit : degC
///- outputs:
///            * name: canopy_temp_avg
///                          ** description : current temperature amplitude
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 100.0
///                          ** min : 0.0
///                          ** unit : degC
/// </summary>
public class Canopy_temp_avg
{


    /// <summary>
    /// Constructor of the Canopy_temp_avg component")
    /// </summary>
    public Canopy_temp_avg() { }

    /// <summary>
    /// Algorithm of the Canopy_temp_avg component
    /// </summary>
    public void  CalculateModel(Soil_tempState s, Soil_tempState s1, Soil_tempRate r, Soil_tempAuxiliary a, Soil_tempExogenous ex)
    {
        double min_canopy_temp = ex.min_canopy_temp;
        double max_canopy_temp = ex.max_canopy_temp;
        double canopy_temp_avg;
        canopy_temp_avg = (max_canopy_temp + min_canopy_temp) / 2;
        s.canopy_temp_avg= canopy_temp_avg;
    }
}