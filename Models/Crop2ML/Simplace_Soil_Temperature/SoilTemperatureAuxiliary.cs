using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML;

/// <summary>
/// auxiliary variables class of the SoilTemperature component
/// </summary>
public class SoilTemperatureAuxiliary
{
    private double _SnowIsolationIndex;

    /// <summary>
    /// Constructor SoilTemperatureAuxiliary domain class
    /// </summary>
    public SoilTemperatureAuxiliary() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SoilTemperatureAuxiliary(SoilTemperatureAuxiliary toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            SnowIsolationIndex = toCopy.SnowIsolationIndex;
        }
    }

    /// <summary>
    /// Gets and sets the Snow isolation index
    /// </summary>
    [Description("Snow isolation index")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/one")] 
    public double SnowIsolationIndex
    {
        get { return this._SnowIsolationIndex; }
        set { this._SnowIsolationIndex= value; } 
    }

}