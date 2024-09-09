using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.Stics_soil_temperature;

/// <summary>
/// state variables class of the soil_temp component
/// </summary>
public class Soil_tempState
{
    private List<double> _prev_temp_profile = new List<double>();
    private double _prev_canopy_temp;
    private double _temp_amp;
    private List<double> _temp_profile = new List<double>();
    private List<double> _layer_temp = new List<double>();
    private double _canopy_temp_avg;

    /// <summary>
    /// Constructor Soil_tempState domain class
    /// </summary>
    public Soil_tempState() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public Soil_tempState(Soil_tempState toCopy, bool copyAll) // copy constructor
    {
        if (copyAll)
        {
            prev_temp_profile = new List<double>();
            for (int i = 0; i < toCopy.prev_temp_profile.Count; i++)
                { prev_temp_profile.Add(toCopy.prev_temp_profile[i]); }

            prev_canopy_temp = toCopy.prev_canopy_temp;
            temp_amp = toCopy.temp_amp;
            temp_profile = new List<double>();
            for (int i = 0; i < toCopy.temp_profile.Count; i++)
                { temp_profile.Add(toCopy.temp_profile[i]); }

            layer_temp = new List<double>();
            for (int i = 0; i < toCopy.layer_temp.Count; i++)
                { layer_temp.Add(toCopy.layer_temp[i]); }

            canopy_temp_avg = toCopy.canopy_temp_avg;
        }
    }

    /// <summary>
    /// Gets and sets the previous soil temperature profile (for 1 cm layers)
    /// </summary>
    [Description("previous soil temperature profile (for 1 cm layers)")]
    [Units("degC")]
    public List<double> prev_temp_profile
    {
        get { return this._prev_temp_profile; }
        set { this._prev_temp_profile= value; }
    }

    /// <summary>
    /// Gets and sets the previous crop temperature
    /// </summary>
    [Description("previous crop temperature")]
    [Units("degC")]
    public double prev_canopy_temp
    {
        get { return this._prev_canopy_temp; }
        set { this._prev_canopy_temp= value; }
    }

    /// <summary>
    /// Gets and sets the current temperature amplitude
    /// </summary>
    [Description("current temperature amplitude")]
    [Units("degC")]
    public double temp_amp
    {
        get { return this._temp_amp; }
        set { this._temp_amp= value; }
    }

    /// <summary>
    /// Gets and sets the current soil profile temperature (for 1 cm layers)
    /// </summary>
    [Description("current soil profile temperature (for 1 cm layers)")]
    [Units("degC")]
    public List<double> temp_profile
    {
        get { return this._temp_profile; }
        set { this._temp_profile= value; }
    }

    /// <summary>
    /// Gets and sets the soil layers temperature
    /// </summary>
    [Description("soil layers temperature")]
    [Units("degC")]
    public List<double> layer_temp
    {
        get { return this._layer_temp; }
        set { this._layer_temp= value; }
    }

    /// <summary>
    /// Gets and sets the current temperature amplitude
    /// </summary>
    [Description("current temperature amplitude")]
    [Units("degC")]
    public double canopy_temp_avg
    {
        get { return this._canopy_temp_avg; }
        set { this._canopy_temp_avg= value; }
    }

}