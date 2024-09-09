using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML;

/// <summary>
/// state variables class of the SoilTemperature component
/// </summary>
public class SoilTemperatureState
{
    private double _pInternalAlbedo;
    private double _SnowWaterContent;
    private double _SoilSurfaceTemperature;
    private int _AgeOfSnow;
    private double[] _rSoilTempArrayRate;
    private double[] _pSoilLayerDepth;
    private double[] _SoilTempArray;

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
            pInternalAlbedo = toCopy.pInternalAlbedo;
            SnowWaterContent = toCopy.SnowWaterContent;
            SoilSurfaceTemperature = toCopy.SoilSurfaceTemperature;
            AgeOfSnow = toCopy.AgeOfSnow;
            rSoilTempArrayRate = new double[toCopy.rSoilTempArrayRate.Length];
        for (int i = 0; i < toCopy.rSoilTempArrayRate.Length; i++)
        { rSoilTempArrayRate[i] = toCopy.rSoilTempArrayRate[i]; }
    
            pSoilLayerDepth = new double[toCopy.pSoilLayerDepth.Length];
        for (int i = 0; i < toCopy.pSoilLayerDepth.Length; i++)
        { pSoilLayerDepth[i] = toCopy.pSoilLayerDepth[i]; }
    
            SoilTempArray = new double[toCopy.SoilTempArray.Length];
        for (int i = 0; i < toCopy.SoilTempArray.Length; i++)
        { SoilTempArray[i] = toCopy.SoilTempArray[i]; }
    
        }
    }

    /// <summary>
    /// Gets and sets the Albedo privat
    /// </summary>
    [Description("Albedo privat")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/one")] 
    public double pInternalAlbedo
    {
        get { return this._pInternalAlbedo; }
        set { this._pInternalAlbedo= value; } 
    }

    /// <summary>
    /// Gets and sets the Snow water content
    /// </summary>
    [Description("Snow water content")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/millimetre")] 
    public double SnowWaterContent
    {
        get { return this._SnowWaterContent; }
        set { this._SnowWaterContent= value; } 
    }

    /// <summary>
    /// Gets and sets the Soil surface temperature
    /// </summary>
    [Description("Soil surface temperature")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")] 
    public double SoilSurfaceTemperature
    {
        get { return this._SoilSurfaceTemperature; }
        set { this._SoilSurfaceTemperature= value; } 
    }

    /// <summary>
    /// Gets and sets the Age of snow
    /// </summary>
    [Description("Age of snow")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/day")] 
    public int AgeOfSnow
    {
        get { return this._AgeOfSnow; }
        set { this._AgeOfSnow= value; } 
    }

    /// <summary>
    /// Gets and sets the Array of daily temperature change
    /// </summary>
    [Description("Array of daily temperature change")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius_per_day")] 
    public double[] rSoilTempArrayRate
    {
        get { return this._rSoilTempArrayRate; }
        set { this._rSoilTempArrayRate= value; } 
    }

    /// <summary>
    /// Gets and sets the Depth of soil layer plus additional depth
    /// </summary>
    [Description("Depth of soil layer plus additional depth")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/metre")] 
    public double[] pSoilLayerDepth
    {
        get { return this._pSoilLayerDepth; }
        set { this._pSoilLayerDepth= value; } 
    }

    /// <summary>
    /// Gets and sets the Array of soil temperatures in layers 
    /// </summary>
    [Description("Array of soil temperatures in layers ")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")] 
    public double[] SoilTempArray
    {
        get { return this._SoilTempArray; }
        set { this._SoilTempArray= value; } 
    }

}