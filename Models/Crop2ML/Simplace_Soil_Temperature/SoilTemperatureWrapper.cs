using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Core;
using Models.Factorial;
using Models.Interfaces;
using Models.PMF;
using Models.Soils;
using Models.Surface;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Models.Crop2ML.Simplace_Soil_Temperature;

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
    [Link] IPlant crop = null;
    [Link] SurfaceOrganicMatter SurfaceOM = null;
    [Link] IPhysical soilPhysical = null;
    [Link] Organic soilOrganic = null;
    [Link] ISoilWater waterBalance = null;
    /// <summary>Event invoke when the soil temperature has changed</summary>
    public event EventHandler SoilTemperatureChanged;

    private SoilTemperatureState s;
    private SoilTemperatureState s1;
    private SoilTemperatureRate r;
    private SoilTemperatureAuxiliary a;
    private SoilTemperatureExogenous ex;
    private SoilTemperatureComponent soiltemperatureComponent;

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
    ///  The get method of the Snow water content output variable
    /// </summary>
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/millimetre")]
    public double SnowWaterContent{ get { return s.SnowWaterContent;}}


    /// <summary>
    ///  The get method of the Soil surface temperature output variable
    /// </summary>
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")]
    public double SoilSurfaceTemperature{ get { return s.SoilSurfaceTemperature;}}


    /// <summary>
    ///  The get method of the Age of snow output variable
    /// </summary>
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/day")]
    public int AgeOfSnow{ get { return s.AgeOfSnow;}}


    /// <summary>
    ///  The get method of the Array of daily temperature change output variable
    /// </summary>
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius_per_day")]
    public double[] rSoilTempArrayRate{ get { return s.rSoilTempArrayRate;}}


    /// <summary>
    ///  The get method of the Array of soil temperatures in layers  output variable
    /// </summary>
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius")]
    public double[] SoilTempArray{ get { return s.SoilTempArray;}}


    /// <summary>
    ///  The get method of the daily snow water content change rate output variable
    /// </summary>
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/millimetre_per_day")]
    public double rSnowWaterContentRate{ get { return r.rSnowWaterContentRate;}}


    /// <summary>
    ///  The get method of the daily soil surface temperature change rate output variable
    /// </summary>
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/degree_Celsius_per_day")]
    public double rSoilSurfaceTemperatureRate{ get { return r.rSoilSurfaceTemperatureRate;}}


    /// <summary>
    ///  The get method of the daily age of snow change rate output variable
    /// </summary>
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/one")]
    public int rAgeOfSnowRate{ get { return r.rAgeOfSnowRate;}}


    /// <summary>
    ///  The get method of the Snow isolation index output variable
    /// </summary>
    [Units("http://www.wurvoc.org/vocabularies/om-1.8/one")]
    public double SnowIsolationIndex{ get { return a.SnowIsolationIndex;}}


    /// <summary>
    ///
    /// </summary>
    [Units("mm")]
    public double cDampingDepth { get; set; } = 6000.0;

    /// <summary>
    /// Soil temperature by layer.
    /// </summary>
    public double[] Value => SoilTempArray;

    /// <summary>
    ///
    /// </summary>
    public double SurfaceSoilTemperature => SoilSurfaceTemperature;

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
        soiltemperatureComponent.cCarbonContent = soilOrganic.Carbon[0];
        soiltemperatureComponent.cAlbedo = waterBalance.Salb;
        soiltemperatureComponent.cSoilLayerDepth = MathUtilities.Divide_Value(soilPhysical.Thickness, 1000.0);
        soiltemperatureComponent.cFirstDayMeanTemp = weather.MeanT;
        soiltemperatureComponent.cAverageGroundTemperature = weather.Tav;
        soiltemperatureComponent.cAverageBulkDensity = MathUtilities.Multiply(soilPhysical.BD, soilPhysical.Thickness).Sum() / soilPhysical.Thickness.Sum();
        soiltemperatureComponent.cDampingDepth = cDampingDepth / 1000.0;
    }

    /// <summary>
    ///  Set exogenous variables of the wrapper of the SoilTemperatureComponent
    /// </summary>
    private void setExogenous()
    {
        ex.iAirTemperatureMax = weather.MaxT;
        ex.iAirTemperatureMin = weather.MinT;
        ex.iGlobalSolarRadiation = weather.Radn;
        ex.iRAIN = weather.Rain;
        ex.iCropResidues = SurfaceOM.Wt;
        ex.iPotentialSoilEvaporation = waterBalance.Eos;
        ex.iLeafAreaIndex = (crop as Plant).LAI;
        ex.iSoilWaterContent = waterBalance.SWmm.Sum();
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