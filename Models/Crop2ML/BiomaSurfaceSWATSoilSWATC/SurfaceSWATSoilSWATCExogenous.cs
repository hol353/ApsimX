using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.BiomaSurfaceSWATSoilSWATC;

/// <summary>
/// exogenous variables class of the SurfaceSWATSoilSWATC component
/// </summary>
public class SurfaceSWATSoilSWATCExogenous
{
    private double _AirTemperatureMaximum;
    private double _AirTemperatureMinimum;
    private double _GlobalSolarRadiation;
    private double _WaterEquivalentOfSnowPack;
    private double _Albedo;
    private double[] _VolumetricWaterContent;

    /// <summary>
    /// Constructor SurfaceSWATSoilSWATCExogenous domain class
    /// </summary>
    public SurfaceSWATSoilSWATCExogenous() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SurfaceSWATSoilSWATCExogenous(SurfaceSWATSoilSWATCExogenous toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            AirTemperatureMaximum = toCopy.AirTemperatureMaximum;
            AirTemperatureMinimum = toCopy.AirTemperatureMinimum;
            GlobalSolarRadiation = toCopy.GlobalSolarRadiation;
            WaterEquivalentOfSnowPack = toCopy.WaterEquivalentOfSnowPack;
            Albedo = toCopy.Albedo;
            VolumetricWaterContent = new double[toCopy.VolumetricWaterContent.Length];
            for (int i = 0; i < toCopy.VolumetricWaterContent.Length; i++)
                { VolumetricWaterContent[i] = toCopy.VolumetricWaterContent[i]; }
    
        }
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
    /// Gets and sets the Water equivalent of snow pack
    /// </summary>
    [Description("Water equivalent of snow pack")] 
    [Units("mm")] 
    public double WaterEquivalentOfSnowPack
    {
        get { return this._WaterEquivalentOfSnowPack; }
        set { this._WaterEquivalentOfSnowPack= value; } 
    }

    /// <summary>
    /// Gets and sets the Albedo of soil
    /// </summary>
    [Description("Albedo of soil")] 
    [Units("unitless")] 
    public double Albedo
    {
        get { return this._Albedo; }
        set { this._Albedo= value; } 
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