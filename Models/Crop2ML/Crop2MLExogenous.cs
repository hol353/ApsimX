using Models.Climate;
using Models.Core;
using Models.Factorial;
using Models.PMF;
using Models.Soils;
using System;
using System.Linq;

namespace Models.Crop2ML;

/// <summary>
///  Provides a variable based interface to model in APSIM. Normally models
///  use [Link]s to other models but Crop2ML used variables. This class
///  get's around that issue.
/// </summary>
/// <remarks>
/// This class should be auto-generated. The properties in this class should be called the ICASA variable name
/// rather than the names that each Crop2ML model unit uses.
/// </remarks>
[Serializable]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
[ViewName("UserInterface.Views.PropertyView")]
[ValidParent(ParentType = typeof(Zone))]
[ValidParent(ParentType = typeof(CompositeFactor))]
public class Crop2MLExogenous :  Model
{
    [Link] Weather weather = null;
    [Link] Physical physical = null;
    [Link] Plant[] plants = null;
    [Link] Water water = null;

    // PARAMETERS FROM SOILTEMPERATURESWAT

    /// <summary>Soil layer thickness</summary>
    [Units("m")]
    public double[] LayerThickness => physical.Thickness;

    /// <summary>The Lag coefficient that controls the influence of the previous day's temperature on the current day's temperature</summary>
    [Units("dimensionless")]
    public double LagCoefficient => 0.8;

    /// <summary>Annual average air temperature</summary>
    [Units("degC")]
    public double AirTemperatureAnnualAverage => weather.Tav;

    /// <summary>Bulk density</summary>
    [Units("t m-3")]
    public double[] BulkDensity => physical.BD;

    /// <summary>Soil profile depth</summary>
    [Units("m")]
    public double SoilProfileDepth => physical.Thickness.Sum();

    // EXOGENOUS

    /// <summary>Length of the day</summary>
    [Units("h")]
    public double DayLength => weather.DayLength;

    /// <summary>Daily global solar radiation</summary>
    [Units("Mj m-2 d-1")]
    public double GlobalSolarRadiation => weather.Radn;

    /// <summary>Above ground biomass</summary>
    [Units("Kg ha-1")]
    public double AboveGroundBiomass => plants.Sum(p => p.AboveGround.Wt);

    /// <summary>Minimum daily air temperature</summary>
    [Units("oC")]
    public double AirTemperatureMinimum => weather.MinT;

     /// <summary>Maximum daily air temperature</summary>
    [Units("oC")]
    public double AirTemperatureMaximum => weather.MaxT;

    /// <summary>Volumetric soil water content</summary>
    [Units("m3 m-3")]
    public double[] VolumetricWaterContent => water.Volumetric;
}