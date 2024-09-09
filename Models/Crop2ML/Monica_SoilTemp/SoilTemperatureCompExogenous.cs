using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.Monica_SoilTemp;

/// <summary>
/// exogenous variables class of the SoilTemperatureComp component
/// </summary>
public class SoilTemperatureCompExogenous
{
    private double _tmin;
    private double _tmax;
    private double _globrad;
    private double _soilCoverage;
    private double _soilSurfaceTemperatureBelowSnow;
    private bool _hasSnowCover;

    /// <summary>
    /// Constructor SoilTemperatureCompExogenous domain class
    /// </summary>
    public SoilTemperatureCompExogenous() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SoilTemperatureCompExogenous(SoilTemperatureCompExogenous toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            tmin = toCopy.tmin;
            tmax = toCopy.tmax;
            globrad = toCopy.globrad;
            soilCoverage = toCopy.soilCoverage;
            soilSurfaceTemperatureBelowSnow = toCopy.soilSurfaceTemperatureBelowSnow;
            hasSnowCover = toCopy.hasSnowCover;
        }
    }

    /// <summary>
    /// Gets and sets the the days min air temperature
    /// </summary>
    [Description("the days min air temperature")] 
    [Units("°C")] 
    public double tmin
    {
        get { return this._tmin; }
        set { this._tmin= value; } 
    }

    /// <summary>
    /// Gets and sets the the days max air temperature
    /// </summary>
    [Description("the days max air temperature")] 
    [Units("°C")] 
    public double tmax
    {
        get { return this._tmax; }
        set { this._tmax= value; } 
    }

    /// <summary>
    /// Gets and sets the the days global radiation
    /// </summary>
    [Description("the days global radiation")] 
    [Units("MJ/m**2/d")] 
    public double globrad
    {
        get { return this._globrad; }
        set { this._globrad= value; } 
    }

    /// <summary>
    /// Gets and sets the soilCoverage
    /// </summary>
    [Description("soilCoverage")] 
    [Units("dimensionless")] 
    public double soilCoverage
    {
        get { return this._soilCoverage; }
        set { this._soilCoverage= value; } 
    }

    /// <summary>
    /// Gets and sets the soilSurfaceTemperature below snow cover
    /// </summary>
    [Description("soilSurfaceTemperature below snow cover")] 
    [Units("°C")] 
    public double soilSurfaceTemperatureBelowSnow
    {
        get { return this._soilSurfaceTemperatureBelowSnow; }
        set { this._soilSurfaceTemperatureBelowSnow= value; } 
    }

    /// <summary>
    /// Gets and sets the is soil covered by snow
    /// </summary>
    [Description("is soil covered by snow")] 
    [Units("dimensionless")] 
    public bool hasSnowCover
    {
        get { return this._hasSnowCover; }
        set { this._hasSnowCover= value; } 
    }

}