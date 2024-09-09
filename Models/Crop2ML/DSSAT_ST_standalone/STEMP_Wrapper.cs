using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Core;
using Models.Factorial;
using Models.Interfaces;
using Models.PMF;
using Models.Soils;
using Models.Surface;
using Models.WaterModel;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Models.Crop2ML.DSSAT_ST_standalone;

/// <summary>
///  This class encapsulates the STEMP_Component
/// </summary>
[Serializable]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
[ViewName("UserInterface.Views.PropertyView")]
[ValidParent(ParentType = typeof(CompositeFactor))]

[ValidParent(ParentType = typeof(Zone))]
public class STEMP_Wrapper :  Model, ISoilTemperature
{
    [Link] Clock clock = null;
    [Link] Weather weather = null;
    [Link] Physical physical = null;
    [Link] Water water = null;
    [Link] WaterBalance waterBalance = null;
    /// <summary>Event invoke when the soil temperature has changed</summary>
    public event EventHandler SoilTemperatureChanged;
    private STEMP_State s;
    private STEMP_State s1;
    private STEMP_Rate r;
    private STEMP_Auxiliary a;
    private STEMP_Exogenous ex;
    private STEMP_Component stemp_Component;

    /// <summary>
    ///  The constructor of the Wrapper of the STEMP_Component
    /// </summary>
    public STEMP_Wrapper()
    {
        s = new STEMP_State();
        s1 = new STEMP_State();
        r = new STEMP_Rate();
        a = new STEMP_Auxiliary();
        ex = new STEMP_Exogenous();
        stemp_Component = new STEMP_Component();
    }

    /// <summary>
    ///  The get method of the Temperature of soil surface litter output variable
    /// </summary>
    [Description("Temperature of soil surface litter")]
    [Units("degC")]
    public double SRFTEMP{ get { return s.SRFTEMP;}}


    /// <summary>
    ///  The get method of the Soil temperature in soil layer L output variable
    /// </summary>
    [Description("Soil temperature in soil layer L")]
    [Units("degC")]
    public double[] ST{ get { return s.ST;}}


    /// <summary>
    ///  The get method of the Array of previous 5 days of average soil temperatures output variable
    /// </summary>
    [Description("Array of previous 5 days of average soil temperatures")]
    [Units("degC")]
    public double[] TMA{ get { return s.TMA;}}


    /// <summary>
    ///  The get method of the Total water content of soil at drained upper limit output variable
    /// </summary>
    [Description("Total water content of soil at drained upper limit")]
    [Units("cm")]
    public double TDL{ get { return s.TDL;}}


    /// <summary>
    ///  The get method of the Cumulative depth of soil profile output variable
    /// </summary>
    [Description("Cumulative depth of soil profile")]
    [Units("mm")]
    public double CUMDPT{ get { return s.CUMDPT;}}


    /// <summary>
    ///  The get method of the Sum of TMA array (last 5 days soil temperature) output variable
    /// </summary>
    [Description("Sum of TMA array (last 5 days soil temperature)")]
    [Units("degC")]
    public double ATOT{ get { return s.ATOT;}}


    /// <summary>
    ///  The get method of the Depth to midpoint of soil layer L output variable
    /// </summary>
    [Description("Depth to midpoint of soil layer L")]
    [Units("cm")]
    public double[] DSMID{ get { return s.DSMID;}}

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
    ///  The Constructor copy of the wrapper of the STEMP_Component
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public STEMP_Wrapper(STEMP_Wrapper toCopy, bool copyAll)
    {
        s = (toCopy.s != null) ? new STEMP_State(toCopy.s, copyAll) : null;
        r = (toCopy.r != null) ? new STEMP_Rate(toCopy.r, copyAll) : null;
        a = (toCopy.a != null) ? new STEMP_Auxiliary(toCopy.a, copyAll) : null;
        ex = (toCopy.ex != null) ? new STEMP_Exogenous(toCopy.ex, copyAll) : null;
        if (copyAll)
        {
            stemp_Component = (toCopy.stemp_Component != null) ? new STEMP_Component(toCopy.stemp_Component) : null;
        }
    }

    /// <summary>
    ///  The Initialization method of the wrapper of the STEMP_Component
    /// </summary>
    public void Init(){
        setExogenous();
        loadParameters();
        stemp_Component.Init(s, s1, r, a, ex);
    }

    /// <summary>
    ///  Load parameters of the wrapper of the STEMP_Component
    /// </summary>
    private void loadParameters()
    {
        stemp_Component.MSALB = waterBalance.Salb;
        stemp_Component.NL = physical.Thickness.Length;
        stemp_Component.LL = physical.LL15;
        stemp_Component.NLAYR = physical.Thickness.Length;
        stemp_Component.DS = SoilUtilities.ToCumThickness(MathUtilities.Divide_Value(physical.Thickness, 10));
        stemp_Component.DLAYR = MathUtilities.Divide_Value(physical.Thickness, 10.0); // to cm
        stemp_Component.ISWWAT = "Y";
        stemp_Component.BD = physical.BD;
        stemp_Component.SW = water.Volumetric;
        stemp_Component.XLAT = weather.Latitude;
        stemp_Component.DUL = physical.DUL;
    }

    /// <summary>
    ///  Set exogenous variables of the wrapper of the STEMP_Component
    /// </summary>
    private void setExogenous()
    {
        ex.TMAX = weather.MaxT;
        ex.SRAD = weather.Radn;
        ex.TAMP = weather.Amp;
        ex.TAVG = weather.MeanT;
        ex.TAV = weather.Tav;
        ex.DOY = clock.Today.DayOfYear;
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
        stemp_Component.CalculateModel(s,s1, r, a, ex);
        SoilTemperatureChanged?.Invoke(this, EventArgs.Empty);
    }

}