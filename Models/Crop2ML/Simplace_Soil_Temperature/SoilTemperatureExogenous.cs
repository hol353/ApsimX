using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML;

/// <summary>
/// exogenous variables class of the SoilTemperature component
/// </summary>
public class SoilTemperatureExogenous
{
    private double _iAirTemperatureMax;
    private double _iTempMax;
    private double _iAirTemperatureMin;
    private double _iTempMin;
    private double _iGlobalSolarRadiation;
    private double _iRadiation;
    private double _iRAIN;
    private double _iCropResidues;
    private double _iPotentialSoilEvaporation;
    private double _iLeafAreaIndex;
    private double[] _SoilTempArray;
    private double[] _iSoilTempArray;
    private double _iSoilWaterContent;
    private double _iSoilSurfaceTemperature;

    /// <summary>
    /// Constructor SoilTemperatureExogenous domain class
    /// </summary>
    public SoilTemperatureExogenous() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SoilTemperatureExogenous(SoilTemperatureExogenous toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            iAirTemperatureMax = toCopy.iAirTemperatureMax;
            iTempMax = toCopy.iTempMax;
            iAirTemperatureMin = toCopy.iAirTemperatureMin;
            iTempMin = toCopy.iTempMin;
            iGlobalSolarRadiation = toCopy.iGlobalSolarRadiation;
            iRadiation = toCopy.iRadiation;
            iRAIN = toCopy.iRAIN;
            iCropResidues = toCopy.iCropResidues;
            iPotentialSoilEvaporation = toCopy.iPotentialSoilEvaporation;
            iLeafAreaIndex = toCopy.iLeafAreaIndex;
            SoilTempArray = new double[toCopy.SoilTempArray.Length];
        for (int i = 0; i < toCopy.SoilTempArray.Length; i++)
        { SoilTempArray[i] = toCopy.SoilTempArray[i]; }
    
            iSoilTempArray = new double[toCopy.iSoilTempArray.Length];
        for (int i = 0; i < toCopy.iSoilTempArray.Length; i++)
        { iSoilTempArray[i] = toCopy.iSoilTempArray[i]; }
    
            iSoilWaterContent = toCopy.iSoilWaterContent;
            iSoilSurfaceTemperature = toCopy.iSoilSurfaceTemperature;
        }
    }

    /// <summary>
    /// Gets and sets the Daily maximum air temperature
    /// </summary>
    [Description("Daily maximum air temperature")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")] 
    public double iAirTemperatureMax
    {
        get { return this._iAirTemperatureMax; }
        set { this._iAirTemperatureMax= value; } 
    }

    /// <summary>
    /// Gets and sets the Daily maximum air temperature
    /// </summary>
    [Description("Daily maximum air temperature")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")] 
    public double iTempMax
    {
        get { return this._iTempMax; }
        set { this._iTempMax= value; } 
    }

    /// <summary>
    /// Gets and sets the Daily minimum air temperature
    /// </summary>
    [Description("Daily minimum air temperature")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")] 
    public double iAirTemperatureMin
    {
        get { return this._iAirTemperatureMin; }
        set { this._iAirTemperatureMin= value; } 
    }

    /// <summary>
    /// Gets and sets the Daily minimum air temperature
    /// </summary>
    [Description("Daily minimum air temperature")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")] 
    public double iTempMin
    {
        get { return this._iTempMin; }
        set { this._iTempMin= value; } 
    }

    /// <summary>
    /// Gets and sets the Global Solar radiation
    /// </summary>
    [Description("Global Solar radiation")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/megajoule_per_square_metre")] 
    public double iGlobalSolarRadiation
    {
        get { return this._iGlobalSolarRadiation; }
        set { this._iGlobalSolarRadiation= value; } 
    }

    /// <summary>
    /// Gets and sets the Global Solar radiation
    /// </summary>
    [Description("Global Solar radiation")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/megajoule_per_square_metre")] 
    public double iRadiation
    {
        get { return this._iRadiation; }
        set { this._iRadiation= value; } 
    }

    /// <summary>
    /// Gets and sets the Rain amount
    /// </summary>
    [Description("Rain amount")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/millimetre")] 
    public double iRAIN
    {
        get { return this._iRAIN; }
        set { this._iRAIN= value; } 
    }

    /// <summary>
    /// Gets and sets the Crop residues plus above ground biomass
    /// </summary>
    [Description("Crop residues plus above ground biomass")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/gram_per_square_metre")] 
    public double iCropResidues
    {
        get { return this._iCropResidues; }
        set { this._iCropResidues= value; } 
    }

    /// <summary>
    /// Gets and sets the Potenial Evaporation
    /// </summary>
    [Description("Potenial Evaporation")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/millimetre")] 
    public double iPotentialSoilEvaporation
    {
        get { return this._iPotentialSoilEvaporation; }
        set { this._iPotentialSoilEvaporation= value; } 
    }

    /// <summary>
    /// Gets and sets the Leaf area index
    /// </summary>
    [Description("Leaf area index")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/square_metre_per_square_metre")] 
    public double iLeafAreaIndex
    {
        get { return this._iLeafAreaIndex; }
        set { this._iLeafAreaIndex= value; } 
    }

    /// <summary>
    /// Gets and sets the Soil Temp array of last day
    /// </summary>
    [Description("Soil Temp array of last day")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")] 
    public double[] SoilTempArray
    {
        get { return this._SoilTempArray; }
        set { this._SoilTempArray= value; } 
    }

    /// <summary>
    /// Gets and sets the Soil Temp array of last day
    /// </summary>
    [Description("Soil Temp array of last day")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")] 
    public double[] iSoilTempArray
    {
        get { return this._iSoilTempArray; }
        set { this._iSoilTempArray= value; } 
    }

    /// <summary>
    /// Gets and sets the Water content, sum of whole soil profile
    /// </summary>
    [Description("Water content, sum of whole soil profile")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/millimetre")] 
    public double iSoilWaterContent
    {
        get { return this._iSoilWaterContent; }
        set { this._iSoilWaterContent= value; } 
    }

    /// <summary>
    /// Gets and sets the Temperature at soil surface
    /// </summary>
    [Description("Temperature at soil surface")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")] 
    public double iSoilSurfaceTemperature
    {
        get { return this._iSoilSurfaceTemperature; }
        set { this._iSoilSurfaceTemperature= value; } 
    }

}