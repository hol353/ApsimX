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
namespace Models.Crop2ML.SQ_Soil_Temperature;

/// <summary>
///  This class encapsulates the SoilTemperatureComponent
/// </summary>
[Serializable]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
[ViewName("UserInterface.Views.PropertyView")]
[ValidParent(ParentType = typeof(Zone))]
[ValidParent(ParentType = typeof(CompositeFactor))]

public class SoilTemperatureWrapper :  Model, ISoilTemperature
{
    [Link] Clock clock = null;
    [Link] Weather weather = null;
    [Link] Physical physical = null;

    private SoilTemperatureState s;
    private SoilTemperatureState s1;
    private SoilTemperatureRate r;
    private SoilTemperatureAuxiliary a;
    private SoilTemperatureExogenous ex;
    private SoilTemperatureComponent soiltemperatureComponent;

    /// <summary>Event invoke when the soil temperature has changed</summary>
    public event EventHandler SoilTemperatureChanged;

    /// <summary>
    ///  The constructor of the Wrapper of the SoilTemperatureComponent
    /// </summary>
    public SoilTemperatureWrapper()
    {
        s = new SoilTemperatureState();
        s1 = new SoilTemperatureState();
        r = new SoilTemperatureRate();
        a = new SoilTemperatureAuxiliary();
        ex = new SoilTemperatureExogenous();
        soiltemperatureComponent = new SoilTemperatureComponent();
    }

    /// <summary>
    ///  The get method of the Minimum Soil Temperature output variable
    /// </summary>
    [Description("Minimum Soil Temperature")]
    [Units("Â°C")]
    public double minTSoil{ get { return s.minTSoil;}}


    /// <summary>
    ///  The get method of the Temperature of the last soil layer output variable
    /// </summary>
    [Description("Temperature of the last soil layer")]
    [Units("Â°C")]
    public double deepLayerT{ get { return s.deepLayerT;}}


    /// <summary>
    ///  The get method of the Maximum Soil Temperature output variable
    /// </summary>
    [Description("Maximum Soil Temperature")]
    [Units("Â°C")]
    public double maxTSoil{ get { return s.maxTSoil;}}


    /// <summary>
    ///  The get method of the Hourly Soil Temperature output variable
    /// </summary>
    [Description("Hourly Soil Temperature")]
    [Units("Â°C")]
    public double[] hourlySoilT{ get { return s.hourlySoilT;}}

    /// <summary>
    /// Soil temperature by layer
    /// </summary>
    public double[] Value => Enumerable.Repeat(deepLayerT, physical.Thickness.Length).ToArray();

    /// <summary>
    /// Surface soil temperature.
    /// </summary>
    public double SurfaceSoilTemperature => deepLayerT;


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
    ///  The Constructor copy of the wrapper of the SoilTemperatureComponent
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SoilTemperatureWrapper(SoilTemperatureWrapper toCopy, bool copyAll)
    {
        s = (toCopy.s != null) ? new SoilTemperatureState(toCopy.s, copyAll) : null;
        r = (toCopy.r != null) ? new SoilTemperatureRate(toCopy.r, copyAll) : null;
        a = (toCopy.a != null) ? new SoilTemperatureAuxiliary(toCopy.a, copyAll) : null;
        ex = (toCopy.ex != null) ? new SoilTemperatureExogenous(toCopy.ex, copyAll) : null;
        if (copyAll)
        {
            soiltemperatureComponent = (toCopy.soiltemperatureComponent != null) ? new SoilTemperatureComponent(toCopy.soiltemperatureComponent) : null;
        }
    }

    /// <summary>
    ///  The Initialization method of the wrapper of the SoilTemperatureComponent
    /// </summary>
    public void Init(){
        setExogenous();
        loadParameters();
        soiltemperatureComponent.Init(s, s1, r, a, ex);
    }

    /// <summary>
    ///  Load parameters of the wrapper of the SoilTemperatureComponent
    /// </summary>
    private void loadParameters()
    {
        soiltemperatureComponent.lambda_ = 2.454;
        soiltemperatureComponent.b = 1.81;
        soiltemperatureComponent.c = 0.49;
        soiltemperatureComponent.a = 0.5;
    }

    /// <summary>
    ///  Set exogenous variables of the wrapper of the SoilTemperatureComponent
    /// </summary>
    private void setExogenous()
    {
        ex.meanTAir = weather.MeanT;
        ex.minTAir = weather.MinT;
        ex.meanAnnualAirTemp = weather.Tav;
        ex.maxTAir = weather.MaxT;
        ex.dayLength = weather.DayLength;
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
        soiltemperatureComponent.CalculateModel(s,s1, r, a, ex);
        SoilTemperatureChanged?.Invoke(this, EventArgs.Empty);
    }

}