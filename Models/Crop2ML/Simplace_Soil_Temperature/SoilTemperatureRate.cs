using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML;

/// <summary>
/// rate variables class of the SoilTemperature component
/// </summary>
public class SoilTemperatureRate
{
    private double _rSnowWaterContentRate;
    private double _rSoilSurfaceTemperatureRate;
    private int _rAgeOfSnowRate;

    /// <summary>
    /// Constructor SoilTemperatureRate domain class
    /// </summary>
    public SoilTemperatureRate() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SoilTemperatureRate(SoilTemperatureRate toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            rSnowWaterContentRate = toCopy.rSnowWaterContentRate;
            rSoilSurfaceTemperatureRate = toCopy.rSoilSurfaceTemperatureRate;
            rAgeOfSnowRate = toCopy.rAgeOfSnowRate;
        }
    }

    /// <summary>
    /// Gets and sets the daily snow water content change rate
    /// </summary>
    [Description("daily snow water content change rate")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/millimetre_per_day")] 
    public double rSnowWaterContentRate
    {
        get { return this._rSnowWaterContentRate; }
        set { this._rSnowWaterContentRate= value; } 
    }

    /// <summary>
    /// Gets and sets the daily soil surface temperature change rate
    /// </summary>
    [Description("daily soil surface temperature change rate")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius_per_day")] 
    public double rSoilSurfaceTemperatureRate
    {
        get { return this._rSoilSurfaceTemperatureRate; }
        set { this._rSoilSurfaceTemperatureRate= value; } 
    }

    /// <summary>
    /// Gets and sets the daily age of snow change rate
    /// </summary>
    [Description("daily age of snow change rate")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/one")] 
    public int rAgeOfSnowRate
    {
        get { return this._rAgeOfSnowRate; }
        set { this._rAgeOfSnowRate= value; } 
    }

}