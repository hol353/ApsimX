using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.DSSAT_EPICST_standalone;

/// <summary>
/// state variables class of the STEMP_EPIC_ component
/// </summary>
public class STEMP_EPIC_State
{
    private int[] _WetDay = new int[30];
    private double _SRFTEMP;
    private int _NDays;
    private double[] _ST;
    private double[] _TMA = new double[5];
    private double _TDL;
    private double _X2_PREV;
    private double[] _DSMID;
    private double _CUMDPT;

    /// <summary>
    /// Constructor STEMP_EPIC_State domain class
    /// </summary>
    public STEMP_EPIC_State() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public STEMP_EPIC_State(STEMP_EPIC_State toCopy, bool copyAll) // copy constructor 
    {
        if (copyAll)
        {
            WetDay = new int[30];
            for (int i = 0; i < 30; i++)
                { WetDay[i] = toCopy.WetDay[i]; }
    
            SRFTEMP = toCopy.SRFTEMP;
            NDays = toCopy.NDays;
            ST = new double[toCopy.ST.Length];
            for (int i = 0; i < toCopy.ST.Length; i++)
                { ST[i] = toCopy.ST[i]; }
    
            TMA = new double[5];
            for (int i = 0; i < 5; i++)
                { TMA[i] = toCopy.TMA[i]; }
    
            TDL = toCopy.TDL;
            X2_PREV = toCopy.X2_PREV;
            DSMID = new double[toCopy.DSMID.Length];
            for (int i = 0; i < toCopy.DSMID.Length; i++)
                { DSMID[i] = toCopy.DSMID[i]; }
    
            CUMDPT = toCopy.CUMDPT;
        }
    }

    /// <summary>
    /// Gets and sets the Wet Days
    /// </summary>
    [Description("Wet Days")] 
    [Units("day")] 
    public int[] WetDay
    {
        get { return this._WetDay; }
        set { this._WetDay= value; } 
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
    /// Gets and sets the Number of days ...
    /// </summary>
    [Description("Number of days ...")] 
    [Units("day")] 
    public int NDays
    {
        get { return this._NDays; }
        set { this._NDays= value; } 
    }

    /// <summary>
    /// Gets and sets the Soil temperature in soil layer NL
    /// </summary>
    [Description("Soil temperature in soil layer NL")] 
    [Units("degC")] 
    public double[] ST
    {
        get { return this._ST; }
        set { this._ST= value; } 
    }

    /// <summary>
    /// Gets and sets the Array of previous 5 days of average soil temperatures.
    /// </summary>
    [Description("Array of previous 5 days of average soil temperatures.")] 
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
    /// Gets and sets the Temperature of soil surface at precedent timestep
    /// </summary>
    [Description("Temperature of soil surface at precedent timestep")] 
    [Units("degC")] 
    public double X2_PREV
    {
        get { return this._X2_PREV; }
        set { this._X2_PREV= value; } 
    }

    /// <summary>
    /// Gets and sets the Depth to midpoint of soil layer NL
    /// </summary>
    [Description("Depth to midpoint of soil layer NL")] 
    [Units("cm")] 
    public double[] DSMID
    {
        get { return this._DSMID; }
        set { this._DSMID= value; } 
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

}