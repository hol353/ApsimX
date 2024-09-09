using Models.Core;
using Models.Utilities;
using System; 
namespace Models.Crop2ML.DSSAT_ST_standalone;
     

/// <summary>
///  STEMP_ component
/// </summary>
public class STEMP_Component 
{

    /// <summary>
    ///  constructor of STEMP_ component
    /// </summary>
    public STEMP_Component() {}

    //Declaration of the associated strategies
    STEMP _STEMP = new STEMP();

    /// <summary>
    /// Gets and sets the Soil albedo with mulch and soil water effects
    /// </summary>
    [Description("Soil albedo with mulch and soil water effects")] 
    [Units("dimensionless")] 
    public double MSALB
    {
        get
        {
             return _STEMP.MSALB; 
        }
        set
        {
            _STEMP.MSALB = value;
        }
    }

    /// <summary>
    /// Gets and sets the Number of soil layers
    /// </summary>
    [Description("Number of soil layers")] 
    [Units("dimensionless")] 
    public int NL
    {
        get
        {
             return _STEMP.NL; 
        }
        set
        {
            _STEMP.NL = value;
        }
    }

    /// <summary>
    /// Gets and sets the Volumetric soil water content in soil layer L at lower limit
    /// </summary>
    [Description("Volumetric soil water content in soil layer L at lower limit")] 
    [Units("cm3 [water] / cm3 [soil]")] 
    public double[] LL
    {
        get
        {
             return _STEMP.LL; 
        }
        set
        {
            _STEMP.LL = value;
        }
    }

    /// <summary>
    /// Gets and sets the Actual number of soil layers
    /// </summary>
    [Description("Actual number of soil layers")] 
    [Units("dimensionless")] 
    public int NLAYR
    {
        get
        {
             return _STEMP.NLAYR; 
        }
        set
        {
            _STEMP.NLAYR = value;
        }
    }

    /// <summary>
    /// Gets and sets the Cumulative depth in soil layer L
    /// </summary>
    [Description("Cumulative depth in soil layer L")] 
    [Units("cm")] 
    public double[] DS
    {
        get
        {
             return _STEMP.DS; 
        }
        set
        {
            _STEMP.DS = value;
        }
    }

    /// <summary>
    /// Gets and sets the Thickness of soil layer L
    /// </summary>
    [Description("Thickness of soil layer L")] 
    [Units("cm")] 
    public double[] DLAYR
    {
        get
        {
             return _STEMP.DLAYR; 
        }
        set
        {
            _STEMP.DLAYR = value;
        }
    }

    /// <summary>
    /// Gets and sets the Water simulation control switch
    /// </summary>
    [Description("Water simulation control switch")] 
    [Units("dimensionless")] 
    public string ISWWAT
    {
        get
        {
             return _STEMP.ISWWAT; 
        }
        set
        {
            _STEMP.ISWWAT = value;
        }
    }

    /// <summary>
    /// Gets and sets the Bulk density, soil layer NL
    /// </summary>
    [Description("Bulk density, soil layer NL")] 
    [Units("g [soil] / cm3 [soil]")] 
    public double[] BD
    {
        get
        {
             return _STEMP.BD; 
        }
        set
        {
            _STEMP.BD = value;
        }
    }

    /// <summary>
    /// Gets and sets the Volumetric soil water content in layer L
    /// </summary>
    [Description("Volumetric soil water content in layer L")] 
    [Units("cm3 [water] / cm3 [soil]")] 
    public double[] SW
    {
        get
        {
             return _STEMP.SW; 
        }
        set
        {
            _STEMP.SW = value;
        }
    }

    /// <summary>
    /// Gets and sets the Latitude
    /// </summary>
    [Description("Latitude")] 
    [Units("degC")] 
    public double XLAT
    {
        get
        {
             return _STEMP.XLAT; 
        }
        set
        {
            _STEMP.XLAT = value;
        }
    }

    /// <summary>
    /// Gets and sets the Volumetric soil water content at Drained Upper Limit in soil layer L
    /// </summary>
    [Description("Volumetric soil water content at Drained Upper Limit in soil layer L")] 
    [Units("cm3[water]/cm3[soil]")] 
    public double[] DUL
    {
        get
        {
             return _STEMP.DUL; 
        }
        set
        {
            _STEMP.DUL = value;
        }
    }

    /// <summary>
    /// Algorithm of STEMP_ component
    /// </summary>
    public void CalculateModel(STEMP_State s,STEMP_State s1,STEMP_Rate r,STEMP_Auxiliary a,STEMP_Exogenous ex)
    {
        _STEMP.CalculateModel(s,s1, r, a, ex);
    }

    /// <summary>
    /// Initialization of STEMP_ component
    /// </summary>
    public void Init(STEMP_State s, STEMP_State s1, STEMP_Rate r, STEMP_Auxiliary a, STEMP_Exogenous ex)
    {
        _STEMP.Init(s, s1, r, a, ex);
    }

    /// <summary>
    /// constructor copy of STEMP_ component
    /// </summary>
    /// <param name="toCopy"></param>
    public STEMP_Component(STEMP_Component toCopy): this() // copy constructor 
    {
            MSALB = toCopy.MSALB;
            NL = toCopy.NL;
            
            for (int i = 0; i < NL; i++)
                { LL[i] = toCopy.LL[i]; }
    
            NLAYR = toCopy.NLAYR;
            
            for (int i = 0; i < NL; i++)
                { DS[i] = toCopy.DS[i]; }
    
            
            for (int i = 0; i < NL; i++)
                { DLAYR[i] = toCopy.DLAYR[i]; }
    
            ISWWAT = toCopy.ISWWAT;
            
            for (int i = 0; i < NL; i++)
                { BD[i] = toCopy.BD[i]; }
    
            
            for (int i = 0; i < NL; i++)
                { SW[i] = toCopy.SW[i]; }
    
            XLAT = toCopy.XLAT;
            
            for (int i = 0; i < NL; i++)
                { DUL[i] = toCopy.DUL[i]; }
    
    }
}