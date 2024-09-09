using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.BiomaSurfacePartonSoilSWATC;

/// <summary>
/// state variables class of the SurfacePartonSoilSWATC component
/// </summary>
public class SurfacePartonSoilSWATCState
{
    private double[] _SoilTemperatureByLayers;

    /// <summary>
    /// Constructor SurfacePartonSoilSWATCState domain class
    /// </summary>
    public SurfacePartonSoilSWATCState() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SurfacePartonSoilSWATCState(SurfacePartonSoilSWATCState toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            SoilTemperatureByLayers = new double[toCopy.SoilTemperatureByLayers.Length];
            for (int i = 0; i < toCopy.SoilTemperatureByLayers.Length; i++)
                { SoilTemperatureByLayers[i] = toCopy.SoilTemperatureByLayers[i]; }
    
        }
    }

    /// <summary>
    /// Gets and sets the Soil temperature of each layer
    /// </summary>
    [Description("Soil temperature of each layer")] 
    [Units("degC")] 
    public double[] SoilTemperatureByLayers
    {
        get { return this._SoilTemperatureByLayers; }
        set { this._SoilTemperatureByLayers= value; } 
    }

}