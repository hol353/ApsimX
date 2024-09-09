using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
namespace Models.Crop2ML.Stics_soil_temperature;

/// <summary>
///- Name: temp_amp -Version: 1.0, -Time step: 1
///- Description:
///            * Title: temp_amp model
///            * Authors: None
///            * Reference: doi:http://dx.doi.org/10.1016/j.agrformet.2014.05.002
///            * Institution: INRAE
///            * ExtendedDescription: None
///            * ShortDescription: None
///- inputs:
///            * name: min_temp
///                          ** description : current minimum temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 50.0
///                          ** min : -50.0
///                          ** default : 0.0
///                          ** unit : degC
///            * name: max_temp
///                          ** description : current maximum temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 50.0
///                          ** min : -50.0
///                          ** default : 0.0
///                          ** unit : degC
///- outputs:
///            * name: temp_amp
///                          ** description : current temperature amplitude
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 100.0
///                          ** min : 0.0
///                          ** unit : degC
/// </summary>
public class Temp_amp
{


    /// <summary>
    /// Constructor of the Temp_amp component")
    /// </summary>
    public Temp_amp() { }

    /// <summary>
    /// Algorithm of the Temp_amp component
    /// </summary>
    public void  CalculateModel(Soil_tempState s, Soil_tempState s1, Soil_tempRate r, Soil_tempAuxiliary a, Soil_tempExogenous ex)
    {
        double min_temp = ex.min_temp;
        double max_temp = ex.max_temp;
        double temp_amp;
        temp_amp = max_temp - min_temp;
        s.temp_amp= temp_amp;
    }
}