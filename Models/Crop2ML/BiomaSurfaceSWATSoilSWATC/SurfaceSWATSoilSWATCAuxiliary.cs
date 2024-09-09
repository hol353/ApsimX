using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.BiomaSurfaceSWATSoilSWATC;

/// <summary>
/// auxiliary variables class of the SurfaceSWATSoilSWATC component
/// </summary>
public class SurfaceSWATSoilSWATCAuxiliary
{
    private double _AboveGroundBiomass;
    private double _SurfaceSoilTemperature;

    /// <summary>
    /// Constructor SurfaceSWATSoilSWATCAuxiliary domain class
    /// </summary>
    public SurfaceSWATSoilSWATCAuxiliary() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SurfaceSWATSoilSWATCAuxiliary(SurfaceSWATSoilSWATCAuxiliary toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            AboveGroundBiomass = toCopy.AboveGroundBiomass;
            SurfaceSoilTemperature = toCopy.SurfaceSoilTemperature;
        }
    }

    /// <summary>
    /// Gets and sets the Above ground biomass
    /// </summary>
    [Description("Above ground biomass")] 
    [Units("Kg ha-1")] 
    public double AboveGroundBiomass
    {
        get { return this._AboveGroundBiomass; }
        set { this._AboveGroundBiomass= value; } 
    }

    /// <summary>
    /// Gets and sets the Average surface soil temperature
    /// </summary>
    [Description("Average surface soil temperature")] 
    [Units("degC")] 
    public double SurfaceSoilTemperature
    {
        get { return this._SurfaceSoilTemperature; }
        set { this._SurfaceSoilTemperature= value; } 
    }

}