using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.BiomaSurfaceSWATSoilSWATC;

/// <summary>
/// state variables class of the SurfaceSWATSoilSWATC component
/// </summary>
public class SurfaceSWATSoilSWATCState
{
    private double[] _SoilTemperatureByLayers;

    /// <summary>
    /// Constructor SurfaceSWATSoilSWATCState domain class
    /// </summary>
    public SurfaceSWATSoilSWATCState() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SurfaceSWATSoilSWATCState(SurfaceSWATSoilSWATCState toCopy, bool copyAll) // copy constructor 
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