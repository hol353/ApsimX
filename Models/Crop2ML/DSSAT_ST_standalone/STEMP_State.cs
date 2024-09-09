using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.DSSAT_ST_standalone;

/// <summary>
/// state variables class of the STEMP_ component
/// </summary>
public class STEMP_State
{
    private double _HDAY;
    private double _SRFTEMP;
    private double[] _ST;
    private double[] _TMA = new double[5];
    private double _TDL;
    private double _CUMDPT;
    private double _ATOT;
    private double[] _DSMID;

    /// <summary>
    /// Constructor STEMP_State domain class
    /// </summary>
    public STEMP_State() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public STEMP_State(STEMP_State toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            HDAY = toCopy.HDAY;
            SRFTEMP = toCopy.SRFTEMP;
            ST = new double[toCopy.ST.Length];
            for (int i = 0; i < toCopy.ST.Length; i++)
                { ST[i] = toCopy.ST[i]; }
    
            TMA = new double[5];
            for (int i = 0; i < 5; i++)
                { TMA[i] = toCopy.TMA[i]; }
    
            TDL = toCopy.TDL;
            CUMDPT = toCopy.CUMDPT;
            ATOT = toCopy.ATOT;
            DSMID = new double[toCopy.DSMID.Length];
            for (int i = 0; i < toCopy.DSMID.Length; i++)
                { DSMID[i] = toCopy.DSMID[i]; }
    
        }
    }

    /// <summary>
    /// Gets and sets the Haverst day
    /// </summary>
    [Description("Haverst day")] 
    [Units("day")] 
    public double HDAY
    {
        get { return this._HDAY; }
        set { this._HDAY= value; } 
    }

    /// <summary>
    /// Gets and sets the Temperature of soil surface litter
    /// </summary>
    [Description("Temperature of soil surface litter")] 
    [Units("degC")] 
    public double SRFTEMP
    {
        get { return this._SRFTEMP; }
        set { this._SRFTEMP= value; } 
    }

    /// <summary>
    /// Gets and sets the Soil temperature in soil layer L
    /// </summary>
    [Description("Soil temperature in soil layer L")] 
    [Units("degC")] 
    public double[] ST
    {
        get { return this._ST; }
        set { this._ST= value; } 
    }

    /// <summary>
    /// Gets and sets the Array of previous 5 days of average soil temperatures
    /// </summary>
    [Description("Array of previous 5 days of average soil temperatures")] 
    [Units("degC")] 
    public double[] TMA
    {
        get { return this._TMA; }
        set { this._TMA= value; } 
    }

    /// <summary>
    /// Gets and sets the Total water content of soil at drained upper limit
    /// </summary>
    [Description("Total water content of soil at drained upper limit")] 
    [Units("cm")] 
    public double TDL
    {
        get { return this._TDL; }
        set { this._TDL= value; } 
    }

    /// <summary>
    /// Gets and sets the Cumulative depth of soil profile
    /// </summary>
    [Description("Cumulative depth of soil profile")] 
    [Units("mm")] 
    public double CUMDPT
    {
        get { return this._CUMDPT; }
        set { this._CUMDPT= value; } 
    }

    /// <summary>
    /// Gets and sets the Sum of TMA array (last 5 days soil temperature)
    /// </summary>
    [Description("Sum of TMA array (last 5 days soil temperature)")] 
    [Units("degC")] 
    public double ATOT
    {
        get { return this._ATOT; }
        set { this._ATOT= value; } 
    }

    /// <summary>
    /// Gets and sets the Depth to midpoint of soil layer L
    /// </summary>
    [Description("Depth to midpoint of soil layer L")] 
    [Units("cm")] 
    public double[] DSMID
    {
        get { return this._DSMID; }
        set { this._DSMID= value; } 
    }

}