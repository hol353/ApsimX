using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
namespace Models.Crop2ML.Stics_soil_temperature;

/// <summary>
///- Name: temp_profile -Version: 1.0, -Time step: 1
///- Description:
///            * Title: tempprofile model
///            * Authors: None
///            * Reference: doi:http://dx.doi.org/10.1016/j.agrformet.2014.05.002
///            * Institution: INRAE
///            * ExtendedDescription: None
///            * ShortDescription: None
///- inputs:
///            * name: temp_amp
///                          ** description : current temperature amplitude
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 100.0
///                          ** min : 0.0
///                          ** default : 0.0
///                          ** unit : degC
///            * name: prev_temp_profile
///                          ** description : previous soil temperature profile (for 1 cm layers)
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLELIST
///                          ** max : 50.0
///                          ** min : -50.0
///                          ** default :
///                          ** unit : degC
///            * name: prev_canopy_temp
///                          ** description : previous crop temperature
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 50.0
///                          ** min : 0.0
///                          ** default :
///                          ** unit : degC
///            * name: min_air_temp
///                          ** description : current minimum air temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 50.0
///                          ** min : -50.0
///                          ** default :
///                          ** unit : degC
///            * name: air_temp_day1
///                          ** description : Mean temperature on first day
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 100.0
///                          ** min : 0.0
///                          ** default : 0.0
///                          ** unit : degC
///            * name: layer_thick
///                          ** description : layers thickness
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : INTARRAY
///                          ** len :
///                          ** max :
///                          ** min :
///                          ** default :
///                          ** unit : cm
///- outputs:
///            * name: temp_profile
///                          ** description : current soil profile temperature (for 1 cm layers)
///                          ** datatype : DOUBLELIST
///                          ** variablecategory : state
///                          ** max : 50.0
///                          ** min : -50.0
///                          ** unit : degC
/// </summary>
public class Temp_profile
{

    /// <summary>
    /// initialization of the Temp_profile component
    /// </summary>
    public void Init(Soil_tempState s, Soil_tempState s1, Soil_tempRate r, Soil_tempAuxiliary a, Soil_tempExogenous ex)
    {
        double min_air_temp = ex.min_air_temp;
        double temp_amp = 0.0;
        List<double> prev_temp_profile = new List<double>();
        double prev_canopy_temp;
        prev_temp_profile = new List<double>{};
        prev_canopy_temp = 0.0;
        int soil_depth;
        soil_depth = layer_thick.Sum();
        prev_temp_profile = new List<double>(soil_depth);
        for (var i = 0; i < soil_depth; i++){prev_temp_profile.Add(air_temp_day1);}
        prev_canopy_temp = air_temp_day1;
        s.temp_amp= temp_amp;
        s.prev_temp_profile= prev_temp_profile;
        s.prev_canopy_temp= prev_canopy_temp;
    }

    private double _air_temp_day1;
    /// <summary>
    /// Gets and sets the Mean temperature on first day
    /// </summary>
    [Description("Mean temperature on first day")]
    [Units("degC")]
    //[Crop2ML(datatype="DOUBLE", min=0.0, max=100.0, default=0.0, parametercategory=constant, inputtype="parameter")]
    public double air_temp_day1
    {
        get { return this._air_temp_day1; }
        set { this._air_temp_day1= value; }
    }

    private int[] _layer_thick;
    /// <summary>
    /// Gets and sets the layers thickness
    /// </summary>
    [Description("layers thickness")]
    [Units("cm")]
    //[Crop2ML(datatype="INTARRAY", min=null, max=null, default=, parametercategory=constant, inputtype="parameter")]
    public int[] layer_thick
    {
        get { return this._layer_thick; }
        set { this._layer_thick= value; }
    }


    /// <summary>
    /// Constructor of the Temp_profile component")
    /// </summary>
    public Temp_profile() { }

    /// <summary>
    /// Algorithm of the Temp_profile component
    /// </summary>
    public void  CalculateModel(Soil_tempState s, Soil_tempState s1, Soil_tempRate r, Soil_tempAuxiliary a, Soil_tempExogenous ex)
    {
        double temp_amp = s.temp_amp;
        List<double> prev_temp_profile = s.prev_temp_profile;
        double prev_canopy_temp = s.prev_canopy_temp;
        double min_air_temp = ex.min_air_temp;
        double[] temp_profile;
        int z;
        int n;
        double[] vexp;
        double therm_diff = 5.37e-3;
        double temp_freq = 7.272e-5;
        double therm_amp;
        n = prev_temp_profile.Count;
        temp_profile = new double[n];
        vexp = new double[n];
        therm_amp = Math.Sqrt(temp_freq / 2 / therm_diff);
        for (z=1 ; z!=n + 1 ; z+=1)
        {
            vexp[z - 1] = Math.Exp(-(z * therm_amp));
        }
        for (z=1 ; z!=n + 1 ; z+=1)
        {
            temp_profile[z - 1] = prev_temp_profile[z - 1] - (vexp[(z - 1)] * (prev_canopy_temp - min_air_temp)) + (0.1 * (prev_canopy_temp - prev_temp_profile[z - 1])) + (temp_amp * vexp[(z - 1)] / 2);
        }
        s.temp_profile= temp_profile.ToList();
    }
}