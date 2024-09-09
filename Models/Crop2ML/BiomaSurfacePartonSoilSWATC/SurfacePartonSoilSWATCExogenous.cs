using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.BiomaSurfacePartonSoilSWATC;

/// <summary>
/// exogenous variables class of the SurfacePartonSoilSWATC component
/// </summary>
public class SurfacePartonSoilSWATCExogenous
{
    private double _DayLength;
    private double _GlobalSolarRadiation;
    private double _AboveGroundBiomass;
    private double _AirTemperatureMinimum;
    private double _AirTemperatureMaximum;
    private double[] _VolumetricWaterContent;

    /// <summary>
    /// Constructor SurfacePartonSoilSWATCExogenous domain class
    /// </summary>
    public SurfacePartonSoilSWATCExogenous() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SurfacePartonSoilSWATCExogenous(SurfacePartonSoilSWATCExogenous toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            DayLength = toCopy.DayLength;
            GlobalSolarRadiation = toCopy.GlobalSolarRadiation;
            AboveGroundBiomass = toCopy.AboveGroundBiomass;
            AirTemperatureMinimum = toCopy.AirTemperatureMinimum;
            AirTemperatureMaximum = toCopy.AirTemperatureMaximum;
            VolumetricWaterContent = new double[toCopy.VolumetricWaterContent.Length];
            for (int i = 0; i < toCopy.VolumetricWaterContent.Length; i++)
                { VolumetricWaterContent[i] = toCopy.VolumetricWaterContent[i]; }
    
        }
    }

    /// <summary>
    /// Gets and sets the Length of the day
    /// </summary>
    [Description("Length of the day")] 
    [Units("h")] 
    public double DayLength
    {
        get { return this._DayLength; }
        set { this._DayLength= value; } 
    }

    /// <summary>
    /// Gets and sets the Daily global solar radiation
    /// </summary>
    [Description("Daily global solar radiation")] 
    [Units("Mj m-2 d-1")] 
    public double GlobalSolarRadiation
    {
        get { return this._GlobalSolarRadiation; }
        set { this._GlobalSolarRadiation= value; } 
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
    /// Gets and sets the Minimum daily air temperature
    /// </summary>
    [Description("Minimum daily air temperature")] 
    [Units("")] 
    public double AirTemperatureMinimum
    {
        get { return this._AirTemperatureMinimum; }
        set { this._AirTemperatureMinimum= value; } 
    }

    /// <summary>
    /// Gets and sets the Maximum daily air temperature
    /// </summary>
    [Description("Maximum daily air temperature")] 
    [Units("")] 
    public double AirTemperatureMaximum
    {
        get { return this._AirTemperatureMaximum; }
        set { this._AirTemperatureMaximum= value; } 
    }

    /// <summary>
    /// Gets and sets the Volumetric soil water content
    /// </summary>
    [Description("Volumetric soil water content")] 
    [Units("m3 m-3")] 
    public double[] VolumetricWaterContent
    {
        get { return this._VolumetricWaterContent; }
        set { this._VolumetricWaterContent= value; } 
    }

}