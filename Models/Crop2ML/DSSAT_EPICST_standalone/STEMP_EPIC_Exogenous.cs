using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.DSSAT_EPICST_standalone;

/// <summary>
/// exogenous variables class of the STEMP_EPIC_ component
/// </summary>
public class STEMP_EPIC_Exogenous
{
    private double _TAV;
    private double _RAIN;
    private double _BIOMAS;
    private double _SNOW;
    private double _TAVG;
    private double _DEPIR;
    private double _MULCHMASS;
    private double _TMAX;
    private double _TMIN;
    private double _TAMP;

    /// <summary>
    /// Constructor STEMP_EPIC_Exogenous domain class
    /// </summary>
    public STEMP_EPIC_Exogenous() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public STEMP_EPIC_Exogenous(STEMP_EPIC_Exogenous toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            TAV = toCopy.TAV;
            RAIN = toCopy.RAIN;
            BIOMAS = toCopy.BIOMAS;
            SNOW = toCopy.SNOW;
            TAVG = toCopy.TAVG;
            DEPIR = toCopy.DEPIR;
            MULCHMASS = toCopy.MULCHMASS;
            TMAX = toCopy.TMAX;
            TMIN = toCopy.TMIN;
            TAMP = toCopy.TAMP;
        }
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
    /// Gets and sets the daily rainfall
    /// </summary>
    [Description("daily rainfall")] 
    [Units("mm")] 
    public double RAIN
    {
        get { return this._RAIN; }
        set { this._RAIN= value; } 
    }

    /// <summary>
    /// Gets and sets the Biomass
    /// </summary>
    [Description("Biomass")] 
    [Units("kg/ha")] 
    public double BIOMAS
    {
        get { return this._BIOMAS; }
        set { this._BIOMAS= value; } 
    }

    /// <summary>
    /// Gets and sets the Snow cover
    /// </summary>
    [Description("Snow cover")] 
    [Units("mm")] 
    public double SNOW
    {
        get { return this._SNOW; }
        set { this._SNOW= value; } 
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
    /// Gets and sets the Depth of irrigation
    /// </summary>
    [Description("Depth of irrigation")] 
    [Units("mm")] 
    public double DEPIR
    {
        get { return this._DEPIR; }
        set { this._DEPIR= value; } 
    }

    /// <summary>
    /// Gets and sets the Mulch Mass
    /// </summary>
    [Description("Mulch Mass")] 
    [Units("kg/ha")] 
    public double MULCHMASS
    {
        get { return this._MULCHMASS; }
        set { this._MULCHMASS= value; } 
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
    /// Gets and sets the Minimum Temperature
    /// </summary>
    [Description("Minimum Temperature")] 
    [Units("degC")] 
    public double TMIN
    {
        get { return this._TMIN; }
        set { this._TMIN= value; } 
    }

    /// <summary>
    /// Gets and sets the Annual amplitude of the average air temperature
    /// </summary>
    [Description("Annual amplitude of the average air temperature")] 
    [Units("degC")] 
    public double TAMP
    {
        get { return this._TAMP; }
        set { this._TAMP= value; } 
    }

}