using Models.Core;
using Models.Utilities;
using System; 
namespace Models.Crop2ML;
     

/// <summary>
///  SoilTemperature component
/// </summary>
public class SoilTemperatureComponent 
{

    /// <summary>
    ///  constructor of SoilTemperature component
    /// </summary>
    public SoilTemperatureComponent() {}

    //Declaration of the associated strategies
    SnowCoverCalculator _SnowCoverCalculator = new SnowCoverCalculator();
    STMPsimCalculator _STMPsimCalculator = new STMPsimCalculator();

    /// <summary>
    /// Gets and sets the Carbon content of upper soil layer
    /// </summary>
    [Description("Carbon content of upper soil layer")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/percent")] 
    public double cCarbonContent
    {
        get
        {
             return _SnowCoverCalculator.cCarbonContent; 
        }
        set
        {
            _SnowCoverCalculator.cCarbonContent = value;
        }
    }

    /// <summary>
    /// Gets and sets the Albedo
    /// </summary>
    [Description("Albedo")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/one")] 
    public double cAlbedo
    {
        get
        {
             return _SnowCoverCalculator.Albedo; 
        }
        set
        {
            _SnowCoverCalculator.Albedo = value;
        }
    }

    /// <summary>
    /// Gets and sets the Depth of soil layer
    /// </summary>
    [Description("Depth of soil layer")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/metre")] 
    public double[] cSoilLayerDepth
    {
        get
        {
             return _STMPsimCalculator.cSoilLayerDepth; 
        }
        set
        {
            _STMPsimCalculator.cSoilLayerDepth = value;
        }
    }

    /// <summary>
    /// Gets and sets the Mean air temperature on first day
    /// </summary>
    [Description("Mean air temperature on first day")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")] 
    public double cFirstDayMeanTemp
    {
        get
        {
             return _STMPsimCalculator.cFirstDayMeanTemp; 
        }
        set
        {
            _STMPsimCalculator.cFirstDayMeanTemp = value;
        }
    }

    /// <summary>
    /// Gets and sets the Constant Temperature of deepest soil layer - use long term mean air temperature
    /// </summary>
    [Description("Constant Temperature of deepest soil layer - use long term mean air temperature")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")] 
    public double cAverageGroundTemperature
    {
        get
        {
             return _STMPsimCalculator.cAVT; 
        }
        set
        {
            _STMPsimCalculator.cAVT = value;
        }
    }

    /// <summary>
    /// Gets and sets the Mean bulk density
    /// </summary>
    [Description("Mean bulk density")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/tonne_per_cubic_metre")] 
    public double cAverageBulkDensity
    {
        get
        {
             return _STMPsimCalculator.cABD; 
        }
        set
        {
            _STMPsimCalculator.cABD = value;
        }
    }

    /// <summary>
    /// Gets and sets the Initial value for damping depth of soil
    /// </summary>
    [Description("Initial value for damping depth of soil")] 
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/metre")] 
    public double cDampingDepth
    {
        get
        {
             return _STMPsimCalculator.cDampingDepth; 
        }
        set
        {
            _STMPsimCalculator.cDampingDepth = value;
        }
    }

    /// <summary>
    /// Algorithm of SoilTemperature component
    /// </summary>
    public void CalculateModel(SoilTemperatureState s,SoilTemperatureState s1,SoilTemperatureRate r,SoilTemperatureAuxiliary a,SoilTemperatureExogenous ex)
    {
        ex.iTempMax = ex.iAirTemperatureMax;
        ex.iTempMin = ex.iAirTemperatureMin;
        ex.iRadiation = ex.iGlobalSolarRadiation;
        ex.iSoilTempArray = s.SoilTempArray;
        _SnowCoverCalculator.CalculateModel(s,s1, r, a, ex);
        ex.iSoilSurfaceTemperature = s.SoilSurfaceTemperature;
        _STMPsimCalculator.CalculateModel(s,s1, r, a, ex);
    }

    /// <summary>
    /// Initialization of SoilTemperature component
    /// </summary>
    public void Init(SoilTemperatureState s, SoilTemperatureState s1, SoilTemperatureRate r, SoilTemperatureAuxiliary a, SoilTemperatureExogenous ex)
    {
        _SnowCoverCalculator.Init(s, s1, r, a, ex);
        _STMPsimCalculator.Init(s, s1, r, a, ex);
    }

    /// <summary>
    /// constructor copy of SoilTemperature component
    /// </summary>
    /// <param name="toCopy"></param>
    public SoilTemperatureComponent(SoilTemperatureComponent toCopy): this() // copy constructor 
    {
        cCarbonContent = toCopy.cCarbonContent;
        cAlbedo = toCopy.cAlbedo;
        
        for (int i = 0; i < toCopy.cSoilLayerDepth.Length; i++)
        { cSoilLayerDepth[i] = toCopy.cSoilLayerDepth[i]; }
    
        cFirstDayMeanTemp = toCopy.cFirstDayMeanTemp;
        cAverageGroundTemperature = toCopy.cAverageGroundTemperature;
        cAverageBulkDensity = toCopy.cAverageBulkDensity;
        cDampingDepth = toCopy.cDampingDepth;
    }
}