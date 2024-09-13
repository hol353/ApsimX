using System;
using Models.Core;
namespace Models.Crop2ML.Bioma;

/// <summary>
///- Name: SoilTemperatureSWAT -Version: 001, -Time step: 1
///- Description:
///            * Title: SoilTemperatureSWAT model
///            * Authors: simone.bregaglio
///            * Reference: ('http://bioma.jrc.ec.europa.eu/ontology/JRC_MARS_biophysical_domain.owl',)
///            * Institution: University Of Milan
///            * ExtendedDescription: Strategy for the calculation of soil temperature with SWAT method. Reference: Neitsch,S.L., Arnold, J.G., Kiniry, J.R., Williams, J.R., King, K.W. Soil and Water Assessment Tool. Theoretical documentation. Version 2000. http://swatmodel.tamu.edu/media/1290/swat2000theory.pdf
///            * ShortDescription: None
/// </summary>
/// <remarks>
///- inputs:
///            * name: VolumetricWaterContent
///                          ** description : Volumetric soil water content
///                          ** inputtype : variable
///                          ** variablecategory : exogenous
///                          ** datatype : DOUBLEARRAY
///                          ** len :
///                          ** max : 0.8
///                          ** min : 0
///                          ** default : 0.25
///                          ** unit : m3 m-3
///            * name: SurfaceSoilTemperature
///                          ** description : Average surface soil temperature
///                          ** inputtype : variable
///                          ** variablecategory : auxiliary
///                          ** datatype : DOUBLE
///                          ** max : 60
///                          ** min : -60
///                          ** default : 25
///                          ** unit : degC
///            * name: LayerThickness
///                          ** description : Soil layer thickness
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLEARRAY
///                          ** len :
///                          ** max : 3
///                          ** min : 0.005
///                          ** default : 0.05
///                          ** unit : m
///            * name: LagCoefficient
///                          ** description : Lag coefficient that controls the influence of the previous day's temperature on the current day's temperature
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 1
///                          ** min : 0
///                          ** default : 0.8
///                          ** unit : dimensionless
///            * name: SoilTemperatureByLayers
///                          ** description : Soil temperature of each layer
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len :
///                          ** max : 60
///                          ** min : -60
///                          ** default : 15
///                          ** unit : degC
///            * name: AirTemperatureAnnualAverage
///                          ** description : Annual average air temperature
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 50
///                          ** min : -40
///                          ** default : 15
///                          ** unit : degC
///            * name: BulkDensity
///                          ** description : Bulk density
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLEARRAY
///                          ** len :
///                          ** max : 1.8
///                          ** min : 0.9
///                          ** default : 1.3
///                          ** unit : t m-3
///            * name: SoilProfileDepth
///                          ** description : Soil profile depth
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 50
///                          ** min : 0
///                          ** default : 3
///                          ** unit : m
///- outputs:
///            * name: SoilTemperatureByLayers
///                          ** description : Soil temperature of each layer
///                          ** datatype : DOUBLEARRAY
///                          ** variablecategory : state
///                          ** len :
///                          ** max : 60
///                          ** min : -60
///                          ** unit : degC
/// </remarks>
[Serializable]
[PresenterName("UserInterface.Presenters.PropertyPresenter")]
[ViewName("UserInterface.Views.PropertyView")]
[ValidParent(ParentType = typeof(Zone))]
public class SoilTemperatureSWAT : Model
{
    [Link] SoilTemperatureSWATState s = null;
    [Link] SurfaceTemperaturePartonAuxiliary a = null;
    [Link] Crop2MLExogenous ex = null;

    /// <summary>Initialization of the SoilTemperatureSWAT component</summary>
    [EventSubscribe("Crop2MLInit")]
    public void Init()
    {
        double[] VolumetricWaterContent = ex.VolumetricWaterContent;
        double[] SoilTemperatureByLayers ;
        int i;
        SoilTemperatureByLayers = new double[ex.LayerThickness.Length];
        for (i=0 ; i!=ex.LayerThickness.Length ; i+=1)
        {
            SoilTemperatureByLayers[i] = (double)(15);
        }
        s.SoilTemperatureByLayers= SoilTemperatureByLayers;
    }

    /// <summary>Algorithm of the SoilTemperatureSWAT component</summary>
    [EventSubscribe("Crop2MLCalculateModel")]
    public void  CalculateModel()
    {
        double[] VolumetricWaterContent = ex.VolumetricWaterContent;
        double SurfaceSoilTemperature = a.SurfaceSoilTemperature;
        double[] SoilTemperatureByLayers = s.SoilTemperatureByLayers;
        int i;
        double _SoilProfileDepthmm;
        double _TotalWaterContentmm;
        double _MaximumDumpingDepth;
        double _DumpingDepth;
        double _ScalingFactor;
        double _DepthBottom;
        double _RatioCenter;
        double _DepthFactor;
        double _DepthCenterLayer;
        _SoilProfileDepthmm = ex.SoilProfileDepth * 1000;
        _TotalWaterContentmm = (double)(0);
        for (i=0 ; i!=ex.LayerThickness.Length ; i+=1)
        {
            _TotalWaterContentmm = _TotalWaterContentmm + (VolumetricWaterContent[i] * ex.LayerThickness[i]);
        }
        _TotalWaterContentmm = _TotalWaterContentmm * 1000;
        _MaximumDumpingDepth = (double)(0);
        _DumpingDepth = (double)(0);
        _ScalingFactor = (double)(0);
        _DepthBottom = (double)(0);
        _RatioCenter = (double)(0);
        _DepthFactor = (double)(0);
        _DepthCenterLayer = ex.LayerThickness[0] * 1000 / 2;
        _MaximumDumpingDepth = 1000 + (2500 * ex.BulkDensity[0] / (ex.BulkDensity[0] + (686 * Math.Exp(-5.63 * ex.BulkDensity[0]))));
        _ScalingFactor = _TotalWaterContentmm / ((0.356 - (0.144 * ex.BulkDensity[0])) * _SoilProfileDepthmm);
        _DumpingDepth = _MaximumDumpingDepth * Math.Exp(Math.Log(500 / _MaximumDumpingDepth) * Math.Pow((1 - _ScalingFactor) / (1 + _ScalingFactor), 2));
        _RatioCenter = _DepthCenterLayer / _DumpingDepth;
        _DepthFactor = _RatioCenter / (_RatioCenter + Math.Exp(-0.867 - (2.078 * _RatioCenter)));
        SoilTemperatureByLayers[0] = ex.LagCoefficient * SoilTemperatureByLayers[0] + ((1 - ex.LagCoefficient) * (_DepthFactor * (ex.AirTemperatureAnnualAverage - SurfaceSoilTemperature) + SurfaceSoilTemperature));
        for (i=1 ; i!=ex.LayerThickness.Length ; i+=1)
        {
            _DepthBottom = _DepthBottom + (ex.LayerThickness[(i - 1)] * 1000);
            _DepthCenterLayer = _DepthBottom + (ex.LayerThickness[i] * 1000 / 2);
            _MaximumDumpingDepth = 1000 + (2500 * ex.BulkDensity[i] / (ex.BulkDensity[i] + (686 * Math.Exp(-5.63 * ex.BulkDensity[i]))));
            _ScalingFactor = _TotalWaterContentmm / ((0.356 - (0.144 * ex.BulkDensity[i])) * _SoilProfileDepthmm);
            _DumpingDepth = _MaximumDumpingDepth * Math.Exp(Math.Log(500 / _MaximumDumpingDepth) * Math.Pow((1 - _ScalingFactor) / (1 + _ScalingFactor), 2));
            _RatioCenter = _DepthCenterLayer / _DumpingDepth;
            _DepthFactor = _RatioCenter / (_RatioCenter + Math.Exp(-0.867 - (2.078 * _RatioCenter)));
            SoilTemperatureByLayers[i] = ex.LagCoefficient * SoilTemperatureByLayers[i] + ((1 - ex.LagCoefficient) * (_DepthFactor * (ex.AirTemperatureAnnualAverage - SurfaceSoilTemperature) + SurfaceSoilTemperature));
        }
        s.SoilTemperatureByLayers= SoilTemperatureByLayers;
    }
}