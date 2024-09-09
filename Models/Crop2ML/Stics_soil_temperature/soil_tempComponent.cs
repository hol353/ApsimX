using Models.Core;
using Models.Utilities;
using System;
namespace Models.Crop2ML.Stics_soil_temperature;


/// <summary>
///  soil_temp component
/// </summary>
public class Soil_tempComponent
{

    /// <summary>
    ///  constructor of Soil_temp component
    /// </summary>
    public Soil_tempComponent() {}

    //Declaration of the associated strategies
    Temp_amp _Temp_amp = new Temp_amp();
    Temp_profile _Temp_profile = new Temp_profile();
    Layers_temp _Layers_temp = new Layers_temp();
    Canopy_temp_avg _Canopy_temp_avg = new Canopy_temp_avg();
    Update _Update = new Update();

    /// <summary>
    /// Gets and sets the Mean temperature on first day
    /// </summary>
    [Description("Mean temperature on first day")]
    [Units("degC")]
    public double air_temp_day1
    {
        get
        {
             return _Temp_profile.air_temp_day1;
        }
        set
        {
            _Temp_profile.air_temp_day1 = value;
        }
    }

    /// <summary>
    /// Gets and sets the layers thickness
    /// </summary>
    [Description("layers thickness")]
    [Units("cm")]
    public int[] layer_thick
    {
        get
        {
             return _Temp_profile.layer_thick;
        }
        set
        {
            _Temp_profile.layer_thick = value;
            _Layers_temp.layer_thick = value;
        }
    }

    /// <summary>
    /// Algorithm of Soil_temp component
    /// </summary>
    public void CalculateModel(Soil_tempState s,Soil_tempState s1,Soil_tempRate r,Soil_tempAuxiliary a,Soil_tempExogenous ex)
    {
        _Temp_amp.CalculateModel(s,s1, r, a, ex);
        _Canopy_temp_avg.CalculateModel(s,s1, r, a, ex);
        _Temp_profile.CalculateModel(s,s1, r, a, ex);
        _Layers_temp.CalculateModel(s,s1, r, a, ex);
        _Update.CalculateModel(s,s1, r, a, ex);
    }

    /// <summary>
    /// Initialization of Soil_temp component
    /// </summary>
    public void Init(Soil_tempState s, Soil_tempState s1, Soil_tempRate r, Soil_tempAuxiliary a, Soil_tempExogenous ex)
    {
        _Temp_profile.Init(s, s1, r, a, ex);
    }

    /// <summary>
    /// constructor copy of Soil_temp component
    /// </summary>
    /// <param name="toCopy"></param>
    public Soil_tempComponent(Soil_tempComponent toCopy): this() // copy constructor
    {
            air_temp_day1 = toCopy.air_temp_day1;

            for (int i = 0; i < toCopy.layer_thick.Length; i++)
                { layer_thick[i] = toCopy.layer_thick[i]; }

    }
}