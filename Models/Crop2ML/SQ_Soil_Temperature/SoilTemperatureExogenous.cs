using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.SQ_Soil_Temperature;

/// <summary>
/// exogenous variables class of the SoilTemperature component
/// </summary>
public class SoilTemperatureExogenous
{
    private double _meanTAir;
    private double _minTAir;
    private double _meanAnnualAirTemp;
    private double _maxTAir;
    private double _dayLength;

    /// <summary>
    /// Constructor SoilTemperatureExogenous domain class
    /// </summary>
    public SoilTemperatureExogenous() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SoilTemperatureExogenous(SoilTemperatureExogenous toCopy, bool copyAll) // copy constructor
    {
        if (copyAll)
        {
            meanTAir = toCopy.meanTAir;
            minTAir = toCopy.minTAir;
            meanAnnualAirTemp = toCopy.meanAnnualAirTemp;
            maxTAir = toCopy.maxTAir;
            dayLength = toCopy.dayLength;
        }
    }

    /// <summary>
    /// Gets and sets the Mean Air Temperature
    /// </summary>
    [Description("Mean Air Temperature")]
    [Units("Â°C")]
    public double meanTAir
    {
        get { return this._meanTAir; }
        set { this._meanTAir= value; }
    }

    /// <summary>
    /// Gets and sets the Minimum Air Temperature from Weather files
    /// </summary>
    [Description("Minimum Air Temperature from Weather files")]
    [Units("Â°C")]
    public double minTAir
    {
        get { return this._minTAir; }
        set { this._minTAir= value; }
    }

    /// <summary>
    /// Gets and sets the Annual Mean Air Temperature
    /// </summary>
    [Description("Annual Mean Air Temperature")]
    [Units("Â°C")]
    public double meanAnnualAirTemp
    {
        get { return this._meanAnnualAirTemp; }
        set { this._meanAnnualAirTemp= value; }
    }

    /// <summary>
    /// Gets and sets the Maximum Air Temperature from Weather Files
    /// </summary>
    [Description("Maximum Air Temperature from Weather Files")]
    [Units("Â°C")]
    public double maxTAir
    {
        get { return this._maxTAir; }
        set { this._maxTAir= value; }
    }

    /// <summary>
    /// Gets and sets the Length of the day
    /// </summary>
    [Description("Length of the day")]
    [Units("hour")]
    public double dayLength
    {
        get { return this._dayLength; }
        set { this._dayLength= value; }
    }

}