using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.Stics_soil_temperature;

/// <summary>
/// exogenous variables class of the soil_temp component
/// </summary>
public class Soil_tempExogenous
{
    private double _min_temp;
    private double _max_temp;
    private double _min_air_temp;
    private double _min_canopy_temp;
    private double _max_canopy_temp;

    /// <summary>
    /// Constructor Soil_tempExogenous domain class
    /// </summary>
    public Soil_tempExogenous() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public Soil_tempExogenous(Soil_tempExogenous toCopy, bool copyAll) // copy constructor
    {
        if (copyAll)
        {
            min_temp = toCopy.min_temp;
            max_temp = toCopy.max_temp;
            min_air_temp = toCopy.min_air_temp;
            min_canopy_temp = toCopy.min_canopy_temp;
            max_canopy_temp = toCopy.max_canopy_temp;
        }
    }

    /// <summary>
    /// Gets and sets the current minimum temperature
    /// </summary>
    [Description("current minimum temperature")]
    [Units("degC")]
    public double min_temp
    {
        get { return this._min_temp; }
        set { this._min_temp= value; }
    }

    /// <summary>
    /// Gets and sets the current maximum temperature
    /// </summary>
    [Description("current maximum temperature")]
    [Units("degC")]
    public double max_temp
    {
        get { return this._max_temp; }
        set { this._max_temp= value; }
    }

    /// <summary>
    /// Gets and sets the current minimum air temperature
    /// </summary>
    [Description("current minimum air temperature")]
    [Units("degC")]
    public double min_air_temp
    {
        get { return this._min_air_temp; }
        set { this._min_air_temp= value; }
    }

    /// <summary>
    /// Gets and sets the current minimum temperature
    /// </summary>
    [Description("current minimum temperature")]
    [Units("degC")]
    public double min_canopy_temp
    {
        get { return this._min_canopy_temp; }
        set { this._min_canopy_temp= value; }
    }

    /// <summary>
    /// Gets and sets the current maximum temperature
    /// </summary>
    [Description("current maximum temperature")]
    [Units("degC")]
    public double max_canopy_temp
    {
        get { return this._max_canopy_temp; }
        set { this._max_canopy_temp= value; }
    }

}