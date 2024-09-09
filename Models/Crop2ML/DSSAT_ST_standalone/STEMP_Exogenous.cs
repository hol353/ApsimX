using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.DSSAT_ST_standalone;

/// <summary>
/// exogenous variables class of the STEMP_ component
/// </summary>
public class STEMP_Exogenous
{
    private double _TMAX;
    private double _SRAD;
    private double _TAMP;
    private double _TAVG;
    private double _TAV;
    private int _DOY;

    /// <summary>
    /// Constructor STEMP_Exogenous domain class
    /// </summary>
    public STEMP_Exogenous() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public STEMP_Exogenous(STEMP_Exogenous toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            TMAX = toCopy.TMAX;
            SRAD = toCopy.SRAD;
            TAMP = toCopy.TAMP;
            TAVG = toCopy.TAVG;
            TAV = toCopy.TAV;
            DOY = toCopy.DOY;
        }
    }

    /// <summary>
    /// Gets and sets the Maximum daily temperature
    /// </summary>
    [Description("Maximum daily temperature")] 
    [Units("degC")] 
    public double TMAX
    {
        get { return this._TMAX; }
        set { this._TMAX= value; } 
    }

    /// <summary>
    /// Gets and sets the Solar radiation
    /// </summary>
    [Description("Solar radiation")] 
    [Units("MJ/m2-d")] 
    public double SRAD
    {
        get { return this._SRAD; }
        set { this._SRAD= value; } 
    }

    /// <summary>
    /// Gets and sets the Amplitude of temperature function used to calculate soil temperatures
    /// </summary>
    [Description("Amplitude of temperature function used to calculate soil temperatures")] 
    [Units("degC")] 
    public double TAMP
    {
        get { return this._TAMP; }
        set { this._TAMP= value; } 
    }

    /// <summary>
    /// Gets and sets the Average daily temperature
    /// </summary>
    [Description("Average daily temperature")] 
    [Units("degC")] 
    public double TAVG
    {
        get { return this._TAVG; }
        set { this._TAVG= value; } 
    }

    /// <summary>
    /// Gets and sets the Average annual soil temperature, used with TAMP to calculate soil temperature.
    /// </summary>
    [Description("Average annual soil temperature, used with TAMP to calculate soil temperature.")] 
    [Units("degC")] 
    public double TAV
    {
        get { return this._TAV; }
        set { this._TAV= value; } 
    }

    /// <summary>
    /// Gets and sets the Current day of simulation
    /// </summary>
    [Description("Current day of simulation")] 
    [Units("d")] 
    public int DOY
    {
        get { return this._DOY; }
        set { this._DOY= value; } 
    }

}