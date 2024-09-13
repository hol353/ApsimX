using System;
using Models.Core;
namespace Models.Crop2ML.Bioma;

/// <summary>
///- Name: SurfaceTemperatureParton -Version: 001, -Time step: 1
///- Description:
///            * Title: SurfaceTemperatureParton model
///            * Authors: simone.bregaglio
///            * Reference: ('http://bioma.jrc.ec.europa.eu/ontology/JRC_MARS_biophysical_domain.owl',)
///            * Institution: University Of Milan
///            * ExtendedDescription: Strategy for the calculation of soil surface temperature with Parton's method. Reference: Parton, W. J. 1984. Predicting soil temperatures in a shortgrass steppe. Soil Science 138:93-101.
///            * ShortDescription: None
/// </summary>
/// <remarks>
///- inputs:
///            * name: DayLength
///                          ** description : Length of the day
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 24
///                          ** min : 0
///                          ** default : 10
///                          ** unit : h
///            * name: AirTemperatureMaximum
///                          ** description : Maximum daily air temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 60
///                          ** min : -40
///                          ** default : 15
///                          ** unit :
///            * name: AirTemperatureMinimum
///                          ** description : Minimum daily air temperature
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 50
///                          ** min : -60
///                          ** default : 5
///                          ** unit :
///            * name: AboveGroundBiomass
///                          ** description : Above ground biomass
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 60
///                          ** min : 0
///                          ** default : 3
///                          ** unit : Kg ha-1
///            * name: GlobalSolarRadiation
///                          ** description : Daily global solar radiation
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLE
///                          ** max : 50
///                          ** min : 0
///                          ** default : 15
///                          ** unit : Mj m-2 d-1
///- outputs:
///            * name: SurfaceTemperatureMinimum
///                          ** description : Minimum surface soil temperature
///                          ** datatype : DOUBLE
///                          ** variablecategory : auxiliary
///                          ** max : 60
///                          ** min : -60
///                          ** unit : degC
///            * name: SurfaceTemperatureMaximum
///                          ** description : Maximum surface soil temperature
///                          ** datatype : DOUBLE
///                          ** variablecategory : auxiliary
///                          ** max : 60
///                          ** min : -60
///                          ** unit : degC
///            * name: SurfaceSoilTemperature
///                          ** description : Average surface soil temperature
///                          ** datatype : DOUBLE
///                          ** variablecategory : auxiliary
///                          ** max : 60
///                          ** min : -60
///                          ** unit : degC
/// </remarks>
[Serializable]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
[ViewName("UserInterface.Views.PropertyView")]
[ValidParent(ParentType = typeof(Zone))]
public class NewSurfaceTemperatureParton : Model
{
    [Link] SurfaceTemperaturePartonAuxiliary a = null;
    [Link] Crop2MLExogenous ex = null;

    /// <summary>Algorithm of the SurfaceTemperatureParton component</summary>
    [EventSubscribe("Crop2MLCalculateModel")]
    public void  CalculateModel()
    {
        double DayLength = ex.DayLength;
        double AirTemperatureMaximum = ex.AirTemperatureMaximum;
        double AirTemperatureMinimum = ex.AirTemperatureMinimum;
        double AboveGroundBiomass = ex.AboveGroundBiomass;
        double GlobalSolarRadiation = ex.GlobalSolarRadiation;
        double SurfaceTemperatureMinimum;
        double SurfaceTemperatureMaximum;
        double SurfaceSoilTemperature;
        double _AGB;
        double _AirTMax;
        double _AirTmin;
        double _SolarRad;
        _AGB = AboveGroundBiomass / 10000;
        _AirTMax = AirTemperatureMaximum;
        _AirTmin = AirTemperatureMinimum;
        _SolarRad = GlobalSolarRadiation;
        if (_AGB > 0.15)
        {
            SurfaceTemperatureMaximum = _AirTMax + ((24 * (1 - Math.Exp(-0.038 * _SolarRad)) + (0.35 * _AirTMax)) * (Math.Exp(-4.8 * _AGB) - 0.13));
            SurfaceTemperatureMinimum = _AirTmin + (6 * _AGB) - 1.82;
        }
        else
        {
            SurfaceTemperatureMaximum = AirTemperatureMaximum;
            SurfaceTemperatureMinimum = AirTemperatureMinimum;
        }
        SurfaceSoilTemperature = 0.41 * SurfaceTemperatureMaximum + (0.59 * SurfaceTemperatureMinimum);
        if (DayLength != (double)(0))
        {
            SurfaceSoilTemperature = DayLength / 24 * _AirTMax + ((1 - (DayLength / 24)) * _AirTmin);
        }
        a.SurfaceTemperatureMinimum= SurfaceTemperatureMinimum;
        a.SurfaceTemperatureMaximum= SurfaceTemperatureMaximum;
        a.SurfaceSoilTemperature= SurfaceSoilTemperature;
    }
}