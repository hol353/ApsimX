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
namespace Models.Crop2ML.BiomaSurfacePartonSoilSWATC;

/// <summary>
///  This class encapsulates the SurfacePartonSoilSWATCComponent
/// </summary>
[Serializable]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
[ViewName("UserInterface.Views.PropertyView")]
[ValidParent(ParentType = typeof(Zone))]
[ValidParent(ParentType = typeof(CompositeFactor))]
public class SurfacePartonSoilSWATCWrapper :  Model, ISoilTemperature
{
    [Link] Clock clock = null;
    [Link] Weather weather = null;
    [Link] Physical physical = null;
    [Link] Plant[] plants = null;
    [Link] Water water = null;

    private SurfacePartonSoilSWATCState s;
    private SurfacePartonSoilSWATCState s1;
    private SurfacePartonSoilSWATCRate r;
    private SurfacePartonSoilSWATCAuxiliary a;
    private SurfacePartonSoilSWATCExogenous ex;
    private SurfacePartonSoilSWATCComponent surfacepartonsoilswatcComponent;

    /// <summary>Event invoke when the soil temperature has changed</summary>
    public event EventHandler SoilTemperatureChanged;

    /// <summary>
    ///  The constructor of the Wrapper of the SurfacePartonSoilSWATCComponent
    /// </summary>
    public SurfacePartonSoilSWATCWrapper()
    {
        s = new SurfacePartonSoilSWATCState();
        s1 = new SurfacePartonSoilSWATCState();
        r = new SurfacePartonSoilSWATCRate();
        a = new SurfacePartonSoilSWATCAuxiliary();
        ex = new SurfacePartonSoilSWATCExogenous();
        surfacepartonsoilswatcComponent = new SurfacePartonSoilSWATCComponent();
    }

    /// <summary>
    ///  The get method of the Soil temperature of each layer output variable
    /// </summary>
    [Description("Soil temperature of each layer")]
    [Units("degC")]
    public double[] SoilTemperatureByLayers{ get { return s.SoilTemperatureByLayers;}}


    /// <summary>
    ///  The get method of the Minimum surface soil temperature output variable
    /// </summary>
    [Description("Minimum surface soil temperature")]
    [Units("degC")]
    public double SurfaceTemperatureMinimum{ get { return a.SurfaceTemperatureMinimum;}}

    /// <summary>
    ///  The get method of the Maximum surface soil temperature output variable
    /// </summary>
    [Description("Maximum surface soil temperature")]
    [Units("degC")]
    public double SurfaceTemperatureMaximum{ get { return a.SurfaceTemperatureMaximum;}}

    /// <summary>
    ///  The get method of the Average surface soil temperature output variable
    /// </summary>
    [Description("Average surface soil temperature")]
    [Units("degC")]
    public double SurfaceSoilTemperature{ get { return a.SurfaceSoilTemperature;}}

    /// <summary>
    ///  The get method of the Soil temperature of each layer output variable
    /// </summary>
    public double[] Value{ get { return s.SoilTemperatureByLayers;}}

    /// <summary>
    ///
    /// </summary>
    public double AverageSoilSurfaceTemperature => double.NaN;

    /// <summary>
    ///
    /// </summary>
    public double MinimumSoilSurfaceTemperature => a.SurfaceTemperatureMinimum;

    /// <summary>
    ///
    /// </summary>
    public double MaximumSoilSurfaceTemperature => a.SurfaceTemperatureMaximum;

    /// <summary>
    ///
    /// </summary>
    public double[] AverageSoilTemperature => Enumerable.Repeat(double.NaN, s.SoilTemperatureByLayers.Length).ToArray();

    /// <summary>
    ///
    /// </summary>
    public double[] MinimumSoilTemperature => Enumerable.Repeat(double.NaN, s.SoilTemperatureByLayers.Length).ToArray();

    /// <summary>
    ///
    /// </summary>
    public double[] MaximumSoilTemperature => Enumerable.Repeat(double.NaN, s.SoilTemperatureByLayers.Length).ToArray();




    /// <summary>
    ///  The Constructor copy of the wrapper of the SurfacePartonSoilSWATCComponent
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SurfacePartonSoilSWATCWrapper(SurfacePartonSoilSWATCWrapper toCopy, bool copyAll)
    {
        s = (toCopy.s != null) ? new SurfacePartonSoilSWATCState(toCopy.s, copyAll) : null;
        r = (toCopy.r != null) ? new SurfacePartonSoilSWATCRate(toCopy.r, copyAll) : null;
        a = (toCopy.a != null) ? new SurfacePartonSoilSWATCAuxiliary(toCopy.a, copyAll) : null;
        ex = (toCopy.ex != null) ? new SurfacePartonSoilSWATCExogenous(toCopy.ex, copyAll) : null;
        if (copyAll)
        {
            surfacepartonsoilswatcComponent = (toCopy.surfacepartonsoilswatcComponent != null) ? new SurfacePartonSoilSWATCComponent(toCopy.surfacepartonsoilswatcComponent) : null;
        }
    }

    /// <summary>
    ///  The Initialization method of the wrapper of the SurfacePartonSoilSWATCComponent
    /// </summary>
    public void Init(){
        setExogenous();
        loadParameters();
        surfacepartonsoilswatcComponent.Init(s, s1, r, a, ex);
    }

    /// <summary>
    ///  Load parameters of the wrapper of the SurfacePartonSoilSWATCComponent
    /// </summary>
    private void loadParameters()
    {
        surfacepartonsoilswatcComponent.LayerThickness = physical.Thickness;
        surfacepartonsoilswatcComponent.BulkDensity = physical.BD;
        surfacepartonsoilswatcComponent.SoilProfileDepth = physical.Thickness.Sum();
        surfacepartonsoilswatcComponent.AirTemperatureAnnualAverage = weather.Tav;
        surfacepartonsoilswatcComponent.LagCoefficient = 0.8;
    }

    /// <summary>
    ///  Set exogenous variables of the wrapper of the SurfacePartonSoilSWATCComponent
    /// </summary>
    private void setExogenous()
    {
        ex.DayLength = weather.DayLength;
        ex.GlobalSolarRadiation = weather.Radn;
        ex.AboveGroundBiomass = plants.Sum(p => p.AboveGround.Wt);
        ex.AirTemperatureMinimum = weather.MinT;
        ex.AirTemperatureMaximum = weather.MaxT;
        ex.VolumetricWaterContent  = water.Volumetric;
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
        surfacepartonsoilswatcComponent.CalculateModel(s,s1, r, a, ex);
        SoilTemperatureChanged?.Invoke(this, EventArgs.Empty);
    }

}