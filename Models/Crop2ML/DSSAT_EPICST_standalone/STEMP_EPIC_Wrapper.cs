using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Core;
using Models.Interfaces;
using Models.PMF;
using Models.Soils;
using Models.WaterModel;
using Models.Surface;
using System;
using System.Collections.Generic;
using System.Linq;
using Models.Factorial;
namespace Models.Crop2ML.DSSAT_EPICST_standalone;

/// <summary>
///  This class encapsulates the STEMP_EPIC_Component
/// </summary>
[Serializable]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
[ViewName("UserInterface.Views.PropertyView")]
[ValidParent(ParentType = typeof(Zone))]
[ValidParent(ParentType = typeof(CompositeFactor))]

public class STEMP_EPIC_Wrapper :  Model, ISoilTemperature
{
    [Link] Clock clock = null;
    [Link] Weather weather = null;
    [Link] Physical physical = null;
    [Link] Plant[] plants = null;
    [Link] Water water = null;
    [Link] SurfaceOrganicMatter surfaceOrganicMatter = null;

    /// <summary>Event invoke when the soil temperature has changed</summary>
    public event EventHandler SoilTemperatureChanged;

    private STEMP_EPIC_State s;
    private STEMP_EPIC_State s1;
    private STEMP_EPIC_Rate r;
    private STEMP_EPIC_Auxiliary a;
    private STEMP_EPIC_Exogenous ex;
    private STEMP_EPIC_Component stemp_epic_Component;

    /// <summary>
    ///  The constructor of the Wrapper of the STEMP_EPIC_Component
    /// </summary>
    public STEMP_EPIC_Wrapper()
    {
        s = new STEMP_EPIC_State();
        s1 = new STEMP_EPIC_State();
        r = new STEMP_EPIC_Rate();
        a = new STEMP_EPIC_Auxiliary();
        ex = new STEMP_EPIC_Exogenous();
        stemp_epic_Component = new STEMP_EPIC_Component();
    }

    /// <summary>
    ///  The get method of the Wet Days output variable
    /// </summary>
    [Description("Wet Days")]
    [Units("day")]
    public int[] WetDay{ get { return s.WetDay;}}


    /// <summary>
    ///  The get method of the Temperature of soil surface litter output variable
    /// </summary>
    [Description("Temperature of soil surface litter")]
    [Units("degC")]
    public double SRFTEMP{ get { return s.SRFTEMP;}}


    /// <summary>
    ///  The get method of the Number of days ... output variable
    /// </summary>
    [Description("Number of days ...")]
    [Units("day")]
    public int NDays{ get { return s.NDays;}}


    /// <summary>
    ///  The get method of the Soil temperature in soil layer NL output variable
    /// </summary>
    [Description("Soil temperature in soil layer NL")]
    [Units("degC")]
    public double[] ST{ get { return s.ST;}}


    /// <summary>
    ///  The get method of the Array of previous 5 days of average soil temperatures. output variable
    /// </summary>
    [Description("Array of previous 5 days of average soil temperatures.")]
    [Units("degC")]
    public double[] TMA{ get { return s.TMA;}}


    /// <summary>
    ///  The get method of the Total water content of soil at drained upper limit output variable
    /// </summary>
    [Description("Total water content of soil at drained upper limit")]
    [Units("cm")]
    public double TDL{ get { return s.TDL;}}


    /// <summary>
    ///  The get method of the Temperature of soil surface at precedent timestep output variable
    /// </summary>
    [Description("Temperature of soil surface at precedent timestep")]
    [Units("degC")]
    public double X2_PREV{ get { return s.X2_PREV;}}


    /// <summary>
    ///  The get method of the Depth to midpoint of soil layer NL output variable
    /// </summary>
    [Description("Depth to midpoint of soil layer NL")]
    [Units("cm")]
    public double[] DSMID{ get { return s.DSMID;}}


    /// <summary>
    ///  The get method of the Cumulative depth of soil profile output variable
    /// </summary>
    [Description("Cumulative depth of soil profile")]
    [Units("mm")]
    public double CUMDPT{ get { return s.CUMDPT;}}

    /// <summary>
    ///  The get method of the Soil temperature of each layer output variable
    /// </summary>
    public double[] Value{ get { return s.ST;}}


    /// <summary>
    ///  Soil surface temperature
    /// </summary>
    public double SurfaceSoilTemperature{ get { return s.SRFTEMP;}}


    /// <summary>
    ///
    /// </summary>
    public double AverageSoilSurfaceTemperature => double.NaN;

    /// <summary>
    ///
    /// </summary>
    public double MinimumSoilSurfaceTemperature => double.NaN;

    /// <summary>
    ///
    /// </summary>
    public double MaximumSoilSurfaceTemperature => double.NaN;

    /// <summary>
    ///
    /// </summary>
    public double[] AverageSoilTemperature => Enumerable.Repeat(double.NaN, Value.Length).ToArray();

    /// <summary>
    ///
    /// </summary>
    public double[] MinimumSoilTemperature => Enumerable.Repeat(double.NaN, Value.Length).ToArray();

    /// <summary>
    ///
    /// </summary>
    public double[] MaximumSoilTemperature => Enumerable.Repeat(double.NaN, Value.Length).ToArray();


    /// <summary>
    ///  The Constructor copy of the wrapper of the STEMP_EPIC_Component
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public STEMP_EPIC_Wrapper(STEMP_EPIC_Wrapper toCopy, bool copyAll)
    {
        s = (toCopy.s != null) ? new STEMP_EPIC_State(toCopy.s, copyAll) : null;
        r = (toCopy.r != null) ? new STEMP_EPIC_Rate(toCopy.r, copyAll) : null;
        a = (toCopy.a != null) ? new STEMP_EPIC_Auxiliary(toCopy.a, copyAll) : null;
        ex = (toCopy.ex != null) ? new STEMP_EPIC_Exogenous(toCopy.ex, copyAll) : null;
        if (copyAll)
        {
            stemp_epic_Component = (toCopy.stemp_epic_Component != null) ? new STEMP_EPIC_Component(toCopy.stemp_epic_Component) : null;
        }
    }

    /// <summary>
    ///  The Initialization method of the wrapper of the STEMP_EPIC_Component
    /// </summary>
    public void Init(){
        setExogenous();
        loadParameters();
        stemp_epic_Component.Init(s, s1, r, a, ex);
    }

    /// <summary>
    ///  Load parameters of the wrapper of the STEMP_EPIC_Component
    /// </summary>
    private void loadParameters()
    {
        stemp_epic_Component.DUL = physical.DUL;
        stemp_epic_Component.NL = physical.Thickness.Length;
        stemp_epic_Component.NLAYR = physical.Thickness.Length;
        stemp_epic_Component.DS = SoilUtilities.ToCumThickness(MathUtilities.Divide_Value(physical.Thickness, 10));
        stemp_epic_Component.ISWWAT = "Y";
        stemp_epic_Component.BD = physical.BD;
        stemp_epic_Component.LL = physical.LL15;
        stemp_epic_Component.DLAYR = MathUtilities.Divide_Value(physical.Thickness, 10.0); // to cm
        stemp_epic_Component.SW = water.Volumetric;
    }

    /// <summary>
    ///  Set exogenous variables of the wrapper of the STEMP_EPIC_Component
    /// </summary>
    private void setExogenous()
    {
        ex.TAV = weather.Tav;
        ex.RAIN = weather.Rain;
        ex.BIOMAS = plants.Sum(p => p.AboveGround.Wt);
        ex.SNOW = 0;
        ex.TAVG = weather.Tav;
        ex.DEPIR = 0;
        ex.MULCHMASS = surfaceOrganicMatter.Wt;
        ex.TMAX = weather.MaxT;
        ex.TMIN = weather.MinT;
        ex.TAMP = weather.Amp;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    [EventSubscribe("Crop2MLProcess")]
    public void CalculateModel(object sender, EventArgs e)
    {
        if (clock.Today == clock.StartDate)
        {
            Init();
        }
        setExogenous();
        stemp_epic_Component.CalculateModel(s,s1, r, a, ex);
        SoilTemperatureChanged?.Invoke(this, EventArgs.Empty);
    }

}