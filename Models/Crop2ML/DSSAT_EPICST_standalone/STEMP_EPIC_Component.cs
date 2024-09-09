using Models.Core;
using Models.Utilities;
using System; 
namespace Models.Crop2ML.DSSAT_EPICST_standalone;
     

/// <summary>
///  STEMP_EPIC_ component
/// </summary>
public class STEMP_EPIC_Component 
{

    /// <summary>
    ///  constructor of STEMP_EPIC_ component
    /// </summary>
    public STEMP_EPIC_Component() {}

    //Declaration of the associated strategies
    STEMP_EPIC _STEMP_EPIC = new STEMP_EPIC();

    /// <summary>
    /// Gets and sets the Volumetric soil water content at Drained Upper Limit in soil layer L
    /// </summary>
    [Description("Volumetric soil water content at Drained Upper Limit in soil layer L")] 
    [Units("cm3[water]/cm3[soil]")] 
    public double[] DUL
    {
        get
        {
             return _STEMP_EPIC.DUL; 
        }
        set
        {
            _STEMP_EPIC.DUL = value;
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
             return _STEMP_EPIC.NL; 
        }
        set
        {
            _STEMP_EPIC.NL = value;
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
             return _STEMP_EPIC.NLAYR; 
        }
        set
        {
            _STEMP_EPIC.NLAYR = value;
        }
    }

    /// <summary>
    /// Gets and sets the Cumulative depth in soil layer NL
    /// </summary>
    [Description("Cumulative depth in soil layer NL")] 
    [Units("cm")] 
    public double[] DS
    {
        get
        {
             return _STEMP_EPIC.DS; 
        }
        set
        {
            _STEMP_EPIC.DS = value;
        }
    }

    /// <summary>
    /// Gets and sets the Water simulation control switch (Y or N)
    /// </summary>
    [Description("Water simulation control switch (Y or N)")] 
    [Units("dimensionless")] 
    public string ISWWAT
    {
        get
        {
             return _STEMP_EPIC.ISWWAT; 
        }
        set
        {
            _STEMP_EPIC.ISWWAT = value;
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
             return _STEMP_EPIC.BD; 
        }
        set
        {
            _STEMP_EPIC.BD = value;
        }
    }

    /// <summary>
    /// Gets and sets the Volumetric soil water content in soil layer NL at lower limit
    /// </summary>
    [Description("Volumetric soil water content in soil layer NL at lower limit")] 
    [Units("cm3 [water] / cm3 [soil]")] 
    public double[] LL
    {
        get
        {
             return _STEMP_EPIC.LL; 
        }
        set
        {
            _STEMP_EPIC.LL = value;
        }
    }

    /// <summary>
    /// Gets and sets the Thickness of soil layer NL
    /// </summary>
    [Description("Thickness of soil layer NL")] 
    [Units("cm")] 
    public double[] DLAYR
    {
        get
        {
             return _STEMP_EPIC.DLAYR; 
        }
        set
        {
            _STEMP_EPIC.DLAYR = value;
        }
    }

    /// <summary>
    /// Gets and sets the Volumetric soil water content in layer NL
    /// </summary>
    [Description("Volumetric soil water content in layer NL")] 
    [Units("cm3 [water] / cm3 [soil]")] 
    public double[] SW
    {
        get
        {
             return _STEMP_EPIC.SW; 
        }
        set
        {
            _STEMP_EPIC.SW = value;
        }
    }

    /// <summary>
    /// Algorithm of STEMP_EPIC_ component
    /// </summary>
    public void CalculateModel(STEMP_EPIC_State s,STEMP_EPIC_State s1,STEMP_EPIC_Rate r,STEMP_EPIC_Auxiliary a,STEMP_EPIC_Exogenous ex)
    {
        _STEMP_EPIC.CalculateModel(s,s1, r, a, ex);
    }

    /// <summary>
    /// Initialization of STEMP_EPIC_ component
    /// </summary>
    public void Init(STEMP_EPIC_State s, STEMP_EPIC_State s1, STEMP_EPIC_Rate r, STEMP_EPIC_Auxiliary a, STEMP_EPIC_Exogenous ex)
    {
        _STEMP_EPIC.Init(s, s1, r, a, ex);
    }

    /// <summary>
    /// constructor copy of STEMP_EPIC_ component
    /// </summary>
    /// <param name="toCopy"></param>
    public STEMP_EPIC_Component(STEMP_EPIC_Component toCopy): this() // copy constructor 
    {
            
            for (int i = 0; i < NL; i++)
                { DUL[i] = toCopy.DUL[i]; }
    
            NL = toCopy.NL;
            NLAYR = toCopy.NLAYR;
            
            for (int i = 0; i < NL; i++)
                { DS[i] = toCopy.DS[i]; }
    
            ISWWAT = toCopy.ISWWAT;
            
            for (int i = 0; i < NL; i++)
                { BD[i] = toCopy.BD[i]; }
    
            
            for (int i = 0; i < NL; i++)
                { LL[i] = toCopy.LL[i]; }
    
            
            for (int i = 0; i < NL; i++)
                { DLAYR[i] = toCopy.DLAYR[i]; }
    
            
            for (int i = 0; i < NL; i++)
                { SW[i] = toCopy.SW[i]; }
    
    }
}