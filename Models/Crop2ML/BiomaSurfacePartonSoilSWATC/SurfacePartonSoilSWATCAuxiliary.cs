using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.BiomaSurfacePartonSoilSWATC;

/// <summary>
/// auxiliary variables class of the SurfacePartonSoilSWATC component
/// </summary>
public class SurfacePartonSoilSWATCAuxiliary
{
    private double _SurfaceTemperatureMinimum;
    private double _SurfaceTemperatureMaximum;
    private double _SurfaceSoilTemperature;

    /// <summary>
    /// Constructor SurfacePartonSoilSWATCAuxiliary domain class
    /// </summary>
    public SurfacePartonSoilSWATCAuxiliary() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SurfacePartonSoilSWATCAuxiliary(SurfacePartonSoilSWATCAuxiliary toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            SurfaceTemperatureMinimum = toCopy.SurfaceTemperatureMinimum;
            SurfaceTemperatureMaximum = toCopy.SurfaceTemperatureMaximum;
            SurfaceSoilTemperature = toCopy.SurfaceSoilTemperature;
        }
    }

    /// <summary>
    /// Gets and sets the Minimum surface soil temperature
    /// </summary>
    [Description("Minimum surface soil temperature")] 
    [Units("degC")] 
    public double SurfaceTemperatureMinimum
    {
        get { return this._SurfaceTemperatureMinimum; }
        set { this._SurfaceTemperatureMinimum= value; } 
    }

    /// <summary>
    /// Gets and sets the Maximum surface soil temperature
    /// </summary>
    [Description("Maximum surface soil temperature")] 
    [Units("degC")] 
    public double SurfaceTemperatureMaximum
    {
        get { return this._SurfaceTemperatureMaximum; }
        set { this._SurfaceTemperatureMaximum= value; } 
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