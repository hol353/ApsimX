using System;
using System.Collections.Generic;
using System.Linq;    
using Models.Core;   
namespace Models.Crop2ML.BiomaSurfacePartonSoilSWATC;

/// <summary>
///- Name: SoilTemperatureSWAT -Version: 001, -Time step: 1
///- Description:
///            * Title: SoilTemperatureSWAT model
///            * Authors: simone.bregaglio
///            * Reference: ('http://bioma.jrc.ec.europa.eu/ontology/JRC_MARS_biophysical_domain.owl',)
///            * Institution: University Of Milan
///            * ExtendedDescription: Strategy for the calculation of soil temperature with SWAT method. Reference: Neitsch,S.L., Arnold, J.G., Kiniry, J.R., Williams, J.R., King, K.W. Soil and Water Assessment Tool. Theoretical documentation. Version 2000. http://swatmodel.tamu.edu/media/1290/swat2000theory.pdf
///            * ShortDescription: None
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
/// </summary>
public class SoilTemperatureSWAT
{

    /// <summary>
    /// initialization of the SoilTemperatureSWAT component
    /// </summary>
    public void Init(SurfacePartonSoilSWATCState s, SurfacePartonSoilSWATCState s1, SurfacePartonSoilSWATCRate r, SurfacePartonSoilSWATCAuxiliary a, SurfacePartonSoilSWATCExogenous ex)
    {
        double[] VolumetricWaterContent = ex.VolumetricWaterContent;
        double[] SoilTemperatureByLayers ;
        int i;
        SoilTemperatureByLayers = new double[LayerThickness.Length];
        for (i=0 ; i!=LayerThickness.Length ; i+=1)
        {
            SoilTemperatureByLayers[i] = (double)(15);
        }
        s.SoilTemperatureByLayers= SoilTemperatureByLayers;
    }

    private double[] _LayerThickness;
    /// <summary>
    /// Gets and sets the Soil layer thickness
    /// </summary>
    [Description("Soil layer thickness")] 
    [Units("m")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=0.005, max=3, default=0.05, parametercategory=constant, inputtype="parameter")] 
    public double[] LayerThickness
    {
        get { return this._LayerThickness; }
        set { this._LayerThickness= value; } 
    }

    private double _LagCoefficient;
    /// <summary>
    /// Gets and sets the Lag coefficient that controls the influence of the previous day's temperature on the current day's temperature
    /// </summary>
    [Description("Lag coefficient that controls the influence of the previous day's temperature on the current day's temperature")] 
    [Units("dimensionless")] 
    //[Crop2ML(datatype="DOUBLE", min=0, max=1, default=0.8, parametercategory=constant, inputtype="parameter")] 
    public double LagCoefficient
    {
        get { return this._LagCoefficient; }
        set { this._LagCoefficient= value; } 
    }

    private double _AirTemperatureAnnualAverage;
    /// <summary>
    /// Gets and sets the Annual average air temperature
    /// </summary>
    [Description("Annual average air temperature")] 
    [Units("degC")] 
    //[Crop2ML(datatype="DOUBLE", min=-40, max=50, default=15, parametercategory=constant, inputtype="parameter")] 
    public double AirTemperatureAnnualAverage
    {
        get { return this._AirTemperatureAnnualAverage; }
        set { this._AirTemperatureAnnualAverage= value; } 
    }

    private double[] _BulkDensity;
    /// <summary>
    /// Gets and sets the Bulk density
    /// </summary>
    [Description("Bulk density")] 
    [Units("t m-3")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=0.9, max=1.8, default=1.3, parametercategory=constant, inputtype="parameter")] 
    public double[] BulkDensity
    {
        get { return this._BulkDensity; }
        set { this._BulkDensity= value; } 
    }

    private double _SoilProfileDepth;
    /// <summary>
    /// Gets and sets the Soil profile depth
    /// </summary>
    [Description("Soil profile depth")] 
    [Units("m")] 
    //[Crop2ML(datatype="DOUBLE", min=0, max=50, default=3, parametercategory=constant, inputtype="parameter")] 
    public double SoilProfileDepth
    {
        get { return this._SoilProfileDepth; }
        set { this._SoilProfileDepth= value; } 
    }

    
    /// <summary>
    /// Constructor of the SoilTemperatureSWAT component")
    /// </summary>  
    public SoilTemperatureSWAT() { }
    
    /// <summary>
    /// Algorithm of the SoilTemperatureSWAT component
    /// </summary>
    public void  CalculateModel(SurfacePartonSoilSWATCState s, SurfacePartonSoilSWATCState s1, SurfacePartonSoilSWATCRate r, SurfacePartonSoilSWATCAuxiliary a, SurfacePartonSoilSWATCExogenous ex)
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
        _SoilProfileDepthmm = SoilProfileDepth * 1000;
        _TotalWaterContentmm = (double)(0);
        for (i=0 ; i!=LayerThickness.Length ; i+=1)
        {
            _TotalWaterContentmm = _TotalWaterContentmm + (VolumetricWaterContent[i] * LayerThickness[i]);
        }
        _TotalWaterContentmm = _TotalWaterContentmm * 1000;
        _MaximumDumpingDepth = (double)(0);
        _DumpingDepth = (double)(0);
        _ScalingFactor = (double)(0);
        _DepthBottom = (double)(0);
        _RatioCenter = (double)(0);
        _DepthFactor = (double)(0);
        _DepthCenterLayer = LayerThickness[0] * 1000 / 2;
        _MaximumDumpingDepth = 1000 + (2500 * BulkDensity[0] / (BulkDensity[0] + (686 * Math.Exp(-5.63 * BulkDensity[0]))));
        _ScalingFactor = _TotalWaterContentmm / ((0.356 - (0.144 * BulkDensity[0])) * _SoilProfileDepthmm);
        _DumpingDepth = _MaximumDumpingDepth * Math.Exp(Math.Log(500 / _MaximumDumpingDepth) * Math.Pow((1 - _ScalingFactor) / (1 + _ScalingFactor), 2));
        _RatioCenter = _DepthCenterLayer / _DumpingDepth;
        _DepthFactor = _RatioCenter / (_RatioCenter + Math.Exp(-0.867 - (2.078 * _RatioCenter)));
        SoilTemperatureByLayers[0] = LagCoefficient * SoilTemperatureByLayers[0] + ((1 - LagCoefficient) * (_DepthFactor * (AirTemperatureAnnualAverage - SurfaceSoilTemperature) + SurfaceSoilTemperature));
        for (i=1 ; i!=LayerThickness.Length ; i+=1)
        {
            _DepthBottom = _DepthBottom + (LayerThickness[(i - 1)] * 1000);
            _DepthCenterLayer = _DepthBottom + (LayerThickness[i] * 1000 / 2);
            _MaximumDumpingDepth = 1000 + (2500 * BulkDensity[i] / (BulkDensity[i] + (686 * Math.Exp(-5.63 * BulkDensity[i]))));
            _ScalingFactor = _TotalWaterContentmm / ((0.356 - (0.144 * BulkDensity[i])) * _SoilProfileDepthmm);
            _DumpingDepth = _MaximumDumpingDepth * Math.Exp(Math.Log(500 / _MaximumDumpingDepth) * Math.Pow((1 - _ScalingFactor) / (1 + _ScalingFactor), 2));
            _RatioCenter = _DepthCenterLayer / _DumpingDepth;
            _DepthFactor = _RatioCenter / (_RatioCenter + Math.Exp(-0.867 - (2.078 * _RatioCenter)));
            SoilTemperatureByLayers[i] = LagCoefficient * SoilTemperatureByLayers[i] + ((1 - LagCoefficient) * (_DepthFactor * (AirTemperatureAnnualAverage - SurfaceSoilTemperature) + SurfaceSoilTemperature));
        }
        s.SoilTemperatureByLayers= SoilTemperatureByLayers;
    }
}