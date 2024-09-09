using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.SQ_Soil_Temperature;

/// <summary>
/// state variables class of the SoilTemperature component
/// </summary>
public class SoilTemperatureState
{
    private double _minTSoil;
    private double _deepLayerT;
    private double _maxTSoil;
    private double[] _hourlySoilT = new double[24];

    /// <summary>
    /// Constructor SoilTemperatureState domain class
    /// </summary>
    public SoilTemperatureState() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SoilTemperatureState(SoilTemperatureState toCopy, bool copyAll) // copy constructor
    {
        if (copyAll)
        {
            minTSoil = toCopy.minTSoil;
            deepLayerT = toCopy.deepLayerT;
            maxTSoil = toCopy.maxTSoil;
            hourlySoilT = new double[24];
            for (int i = 0; i < 24; i++)
                { hourlySoilT[i] = toCopy.hourlySoilT[i]; }

        }
    }

    /// <summary>
    /// Gets and sets the Minimum Soil Temperature
    /// </summary>
    [Description("Minimum Soil Temperature")]
    [Units("Â°C")]
    public double minTSoil
    {
        get { return this._minTSoil; }
        set { this._minTSoil= value; }
    }

    /// <summary>
    /// Gets and sets the Temperature of the last soil layer
    /// </summary>
    [Description("Temperature of the last soil layer")]
    [Units("Â°C")]
    public double deepLayerT
    {
        get { return this._deepLayerT; }
        set { this._deepLayerT= value; }
    }

    /// <summary>
    /// Gets and sets the Maximum Soil Temperature
    /// </summary>
    [Description("Maximum Soil Temperature")]
    [Units("Â°C")]
    public double maxTSoil
    {
        get { return this._maxTSoil; }
        set { this._maxTSoil= value; }
    }

    /// <summary>
    /// Gets and sets the Hourly Soil Temperature
    /// </summary>
    [Description("Hourly Soil Temperature")]
    [Units("Â°C")]
    public double[] hourlySoilT
    {
        get { return this._hourlySoilT; }
        set { this._hourlySoilT= value; }
    }

}