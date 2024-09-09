using Models.Core;
using Models.Utilities;
using System; 
namespace Models.Crop2ML.BiomaSurfaceSWATSoilSWATC;
     

/// <summary>
///  SurfaceSWATSoilSWATC component
/// </summary>
public class SurfaceSWATSoilSWATCComponent 
{

    /// <summary>
    ///  constructor of SurfaceSWATSoilSWATC component
    /// </summary>
    public SurfaceSWATSoilSWATCComponent() {}

    //Declaration of the associated strategies
    SurfaceTemperatureSWAT _SurfaceTemperatureSWAT = new SurfaceTemperatureSWAT();
    SoilTemperatureSWAT _SoilTemperatureSWAT = new SoilTemperatureSWAT();

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
    /// Algorithm of SurfaceSWATSoilSWATC component
    /// </summary>
    public void CalculateModel(SurfaceSWATSoilSWATCState s,SurfaceSWATSoilSWATCState s1,SurfaceSWATSoilSWATCRate r,SurfaceSWATSoilSWATCAuxiliary a,SurfaceSWATSoilSWATCExogenous ex)
    {
        _SurfaceTemperatureSWAT.CalculateModel(s,s1, r, a, ex);
        _SoilTemperatureSWAT.CalculateModel(s,s1, r, a, ex);
    }

    /// <summary>
    /// Initialization of SurfaceSWATSoilSWATC component
    /// </summary>
    public void Init(SurfaceSWATSoilSWATCState s, SurfaceSWATSoilSWATCState s1, SurfaceSWATSoilSWATCRate r, SurfaceSWATSoilSWATCAuxiliary a, SurfaceSWATSoilSWATCExogenous ex)
    {
        _SoilTemperatureSWAT.Init(s, s1, r, a, ex);
    }

    /// <summary>
    /// constructor copy of SurfaceSWATSoilSWATC component
    /// </summary>
    /// <param name="toCopy"></param>
    public SurfaceSWATSoilSWATCComponent(SurfaceSWATSoilSWATCComponent toCopy): this() // copy constructor 
    {
            
            for (int i = 0; i < toCopy.BulkDensity.Length; i++)
                { BulkDensity[i] = toCopy.BulkDensity[i]; }
    
            AirTemperatureAnnualAverage = toCopy.AirTemperatureAnnualAverage;
            SoilProfileDepth = toCopy.SoilProfileDepth;
            LagCoefficient = toCopy.LagCoefficient;
            
            for (int i = 0; i < toCopy.LayerThickness.Length; i++)
                { LayerThickness[i] = toCopy.LayerThickness[i]; }
    
    }
}