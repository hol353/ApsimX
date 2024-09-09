using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
namespace Models.Crop2ML.Stics_soil_temperature;

/// <summary>
///- Name: update -Version: 1.0, -Time step: 1
///- Description:
///            * Title: update soil temp model
///            * Authors: None
///            * Reference: doi:http://dx.doi.org/10.1016/j.agrformet.2014.05.002
///            * Institution: INRAE
///            * ExtendedDescription: None
///            * ShortDescription: None
///- inputs:
///            * name: canopy_temp_avg
///                          ** description : current canopy mean temperature
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 50.0
///                          ** min : -50.0
///                          ** default : 0.0
///                          ** unit : degC
///            * name: temp_profile
///                          ** description : current soil profile temperature (for 1 cm layers)
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len :
///                          ** max : 50.0
///                          ** min : -50.0
///                          ** default :
///                          ** unit : degC
///- outputs:
///            * name: prev_canopy_temp
///                          ** description : previous crop temperature
///                          ** datatype : DOUBLE
///                          ** variablecategory : state
///                          ** max : 50.0
///                          ** min : 0.0
///                          ** unit : degC
///            * name: prev_temp_profile
///                          ** description : previous soil temperature profile (for 1 cm layers)
///                          ** datatype : DOUBLELIST
///                          ** variablecategory : state
///                          ** max : 50.0
///                          ** min : -50.0
///                          ** unit : degC
/// </summary>
public class Update
{


    /// <summary>
    /// Constructor of the Update component")
    /// </summary>
    public Update() { }

    /// <summary>
    /// Algorithm of the Update component
    /// </summary>
    public void  CalculateModel(Soil_tempState s, Soil_tempState s1, Soil_tempRate r, Soil_tempAuxiliary a, Soil_tempExogenous ex)
    {
        double canopy_temp_avg = s.canopy_temp_avg;
        List<double> temp_profile = s.temp_profile;
        double prev_canopy_temp;
        List<double> prev_temp_profile = new List<double>();
        int n;
        prev_canopy_temp = canopy_temp_avg;
        n = temp_profile.Count;
        prev_temp_profile = new List<double>(n);
        prev_temp_profile = temp_profile;
        s.prev_canopy_temp= prev_canopy_temp;
        s.prev_temp_profile= prev_temp_profile;
    }
}