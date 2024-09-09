using Models.Core;
using Models.Utilities;
using System; 
namespace Models.Crop2ML.BiomaSurfacePartonSoilSWATC;
     

/// <summary>
///  SurfacePartonSoilSWATC component
/// </summary>
public class SurfacePartonSoilSWATCComponent 
{

    /// <summary>
    ///  constructor of SurfacePartonSoilSWATC component
    /// </summary>
    public SurfacePartonSoilSWATCComponent() {}

    //Declaration of the associated strategies
    SurfaceTemperatureParton _SurfaceTemperatureParton = new SurfaceTemperatureParton();
    SoilTemperatureSWAT _SoilTemperatureSWAT = new SoilTemperatureSWAT();

    /// <summary>
    /// Gets and sets the Soil layer thickness
    /// </summary>
    [Description("Soil layer thickness")] 
    [Units("m")] 
    public double[] LayerThickness
    {
        get
        {
             return _SoilTemperatureSWAT.LayerThickness; 
        }
        set
        {
            _SoilTemperatureSWAT.LayerThickness = value;
        }
    }

    /// <summary>
    /// Gets and sets the Bulk density
    /// </summary>
    [Description("Bulk density")] 
    [Units("t m-3")] 
    public double[] BulkDensity
    {
        get
        {
             return _SoilTemperatureSWAT.BulkDensity; 
        }
        set
        {
            _SoilTemperatureSWAT.BulkDensity = value;
        }
    }

    /// <summary>
    /// Gets and sets the Soil profile depth
    /// </summary>
    [Description("Soil profile depth")] 
    [Units("m")] 
    public double SoilProfileDepth
    {
        get
        {
             return _SoilTemperatureSWAT.SoilProfileDepth; 
        }
        set
        {
            _SoilTemperatureSWAT.SoilProfileDepth = value;
        }
    }

    /// <summary>
    /// Gets and sets the Annual average air temperature
    /// </summary>
    [Description("Annual average air temperature")] 
    [Units("degC")] 
    public double AirTemperatureAnnualAverage
    {
        get
        {
             return _SoilTemperatureSWAT.AirTemperatureAnnualAverage; 
        }
        set
        {
            _SoilTemperatureSWAT.AirTemperatureAnnualAverage = value;
        }
    }

    /// <summary>
    /// Gets and sets the Lag coefficient that controls the influence of the previous day's temperature on the current day's temperature
    /// </summary>
    [Description("Lag coefficient that controls the influence of the previous day's temperature on the current day's temperature")] 
    [Units("dimensionless")] 
    public double LagCoefficient
    {
        get
        {
             return _SoilTemperatureSWAT.LagCoefficient; 
        }
        set
        {
            _SoilTemperatureSWAT.LagCoefficient = value;
        }
    }

    /// <summary>
    /// Algorithm of SurfacePartonSoilSWATC component
    /// </summary>
    public void CalculateModel(SurfacePartonSoilSWATCState s,SurfacePartonSoilSWATCState s1,SurfacePartonSoilSWATCRate r,SurfacePartonSoilSWATCAuxiliary a,SurfacePartonSoilSWATCExogenous ex)
    {
        _SurfaceTemperatureParton.CalculateModel(s,s1, r, a, ex);
        _SoilTemperatureSWAT.CalculateModel(s,s1, r, a, ex);
    }

    /// <summary>
    /// Initialization of SurfacePartonSoilSWATC component
    /// </summary>
    public void Init(SurfacePartonSoilSWATCState s, SurfacePartonSoilSWATCState s1, SurfacePartonSoilSWATCRate r, SurfacePartonSoilSWATCAuxiliary a, SurfacePartonSoilSWATCExogenous ex)
    {
        _SoilTemperatureSWAT.Init(s, s1, r, a, ex);
    }

    /// <summary>
    /// constructor copy of SurfacePartonSoilSWATC component
    /// </summary>
    /// <param name="toCopy"></param>
    public SurfacePartonSoilSWATCComponent(SurfacePartonSoilSWATCComponent toCopy): this() // copy constructor 
    {
            
            for (int i = 0; i < toCopy.LayerThickness.Length; i++)
                { LayerThickness[i] = toCopy.LayerThickness[i]; }
    
            
            for (int i = 0; i < toCopy.BulkDensity.Length; i++)
                { BulkDensity[i] = toCopy.BulkDensity[i]; }
    
            SoilProfileDepth = toCopy.SoilProfileDepth;
            AirTemperatureAnnualAverage = toCopy.AirTemperatureAnnualAverage;
            LagCoefficient = toCopy.LagCoefficient;
    }
}