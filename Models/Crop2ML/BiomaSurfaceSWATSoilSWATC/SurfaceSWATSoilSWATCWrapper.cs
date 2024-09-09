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
namespace Models.Crop2ML.BiomaSurfaceSWATSoilSWATC;

/// <summary>
///  This class encapsulates the SurfaceSWATSoilSWATCComponent
/// </summary>
[Serializable]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
[ViewName("UserInterface.Views.PropertyView")]
[ValidParent(ParentType = typeof(Zone))]
[ValidParent(ParentType = typeof(CompositeFactor))]

public class SurfaceSWATSoilSWATCWrapper :  Model, ISoilTemperature
{
    [Link] Clock clock = null;
    [Link] Weather weather = null;
    [Link] Physical physical = null;
    [Link] Water water = null;
    [Link] WaterBalance waterBalance = null;

    private SurfaceSWATSoilSWATCState s;
    private SurfaceSWATSoilSWATCState s1;
    private SurfaceSWATSoilSWATCRate r;
    private SurfaceSWATSoilSWATCAuxiliary a;
    private SurfaceSWATSoilSWATCExogenous ex;
    private SurfaceSWATSoilSWATCComponent surfaceswatsoilswatcComponent;

    /// <summary>Event invoke when the soil temperature has changed</summary>
    public event EventHandler SoilTemperatureChanged;

    /// <summary>
    ///  The constructor of the Wrapper of the SurfaceSWATSoilSWATCComponent
    /// </summary>
    public SurfaceSWATSoilSWATCWrapper()
    {
        s = new SurfaceSWATSoilSWATCState();
        s1 = new SurfaceSWATSoilSWATCState();
        r = new SurfaceSWATSoilSWATCRate();
        a = new SurfaceSWATSoilSWATCAuxiliary();
        ex = new SurfaceSWATSoilSWATCExogenous();
        surfaceswatsoilswatcComponent = new SurfaceSWATSoilSWATCComponent();
    }

    /// <summary>
    ///  The get method of the Soil temperature of each layer output variable
    /// </summary>
    [Description("Soil temperature of each layer")]
    [Units("degC")]
    public double[] SoilTemperatureByLayers{ get { return s.SoilTemperatureByLayers;}}

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
    ///  The Constructor copy of the wrapper of the SurfaceSWATSoilSWATCComponent
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SurfaceSWATSoilSWATCWrapper(SurfaceSWATSoilSWATCWrapper toCopy, bool copyAll)
    {
        s = (toCopy.s != null) ? new SurfaceSWATSoilSWATCState(toCopy.s, copyAll) : null;
        r = (toCopy.r != null) ? new SurfaceSWATSoilSWATCRate(toCopy.r, copyAll) : null;
        a = (toCopy.a != null) ? new SurfaceSWATSoilSWATCAuxiliary(toCopy.a, copyAll) : null;
        ex = (toCopy.ex != null) ? new SurfaceSWATSoilSWATCExogenous(toCopy.ex, copyAll) : null;
        if (copyAll)
        {
            surfaceswatsoilswatcComponent = (toCopy.surfaceswatsoilswatcComponent != null) ? new SurfaceSWATSoilSWATCComponent(toCopy.surfaceswatsoilswatcComponent) : null;
        }
    }

    /// <summary>
    ///  The Initialization method of the wrapper of the SurfaceSWATSoilSWATCComponent
    /// </summary>
    public void Init(){
        setExogenous();
        loadParameters();
        surfaceswatsoilswatcComponent.Init(s, s1, r, a, ex);
    }

    /// <summary>
    ///  Load parameters of the wrapper of the SurfaceSWATSoilSWATCComponent
    /// </summary>
    private void loadParameters()
    {
        surfaceswatsoilswatcComponent.BulkDensity = physical.BD;
        surfaceswatsoilswatcComponent.AirTemperatureAnnualAverage = weather.Tav;
        surfaceswatsoilswatcComponent.SoilProfileDepth = physical.Thickness.Sum();
        surfaceswatsoilswatcComponent.LagCoefficient = 0.8;
        surfaceswatsoilswatcComponent.LayerThickness = physical.Thickness;
    }

    /// <summary>
    ///  Set exogenous variables of the wrapper of the SurfaceSWATSoilSWATCComponent
    /// </summary>
    private void setExogenous()
    {
        ex.AirTemperatureMaximum = weather.MaxT;
        ex.AirTemperatureMinimum = weather.MinT;
        ex.GlobalSolarRadiation = weather.Radn;
        ex.WaterEquivalentOfSnowPack = 10;
        ex.Albedo = waterBalance.Salb;
        ex.VolumetricWaterContent = water.Volumetric;
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
        surfaceswatsoilswatcComponent.CalculateModel(s,s1, r, a, ex);
        SoilTemperatureChanged?.Invoke(this, EventArgs.Empty);
    }

}