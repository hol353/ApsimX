using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.SQ_Soil_Temperature;

/// <summary>
/// rate variables class of the SoilTemperature component
/// </summary>
public class SoilTemperatureRate
{
    private double _heatFlux;

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
            heatFlux = toCopy.heatFlux;
        }
    }

    /// <summary>
    /// Gets and sets the Soil Heat Flux from Energy Balance Component
    /// </summary>
    [Description("Soil Heat Flux from Energy Balance Component")]
    [Units("g m-2 d-1")]
    public double heatFlux
    {
        get { return this._heatFlux; }
        set { this._heatFlux= value; }
    }

}