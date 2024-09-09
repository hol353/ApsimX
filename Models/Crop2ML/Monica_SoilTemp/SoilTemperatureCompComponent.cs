using Models.Core;
using Models.Utilities;
using System;
namespace Models.Crop2ML.Monica_SoilTemp;


/// <summary>
///  SoilTemperatureComp component
/// </summary>
public class SoilTemperatureCompComponent
{

    /// <summary>
    ///  constructor of SoilTemperatureComp component
    /// </summary>
    public SoilTemperatureCompComponent() {}

    //Declaration of the associated strategies
    SoilTemperature _SoilTemperature = new SoilTemperature();
    NoSnowSoilSurfaceTemperature _NoSnowSoilSurfaceTemperature = new NoSnowSoilSurfaceTemperature();
    WithSnowSoilSurfaceTemperature _WithSnowSoilSurfaceTemperature = new WithSnowSoilSurfaceTemperature();

    /// <summary>
    /// Gets and sets the dampingFactor
    /// </summary>
    [Description("dampingFactor")]
    [Units("dimensionless")]
    public double dampingFactor
    {
        get
        {
             return _NoSnowSoilSurfaceTemperature.dampingFactor;
        }
        set
        {
            _NoSnowSoilSurfaceTemperature.dampingFactor = value;
        }
    }

    /// <summary>
    /// Gets and sets the timeStep
    /// </summary>
    [Description("timeStep")]
    [Units("dimensionless")]
    public double timeStep
    {
        get
        {
             return _SoilTemperature.timeStep;
        }
        set
        {
            _SoilTemperature.timeStep = value;
        }
    }

    /// <summary>
    /// Gets and sets the constant soilmoisture during the model run
    /// </summary>
    [Description("constant soilmoisture during the model run")]
    [Units("m**3/m**3")]
    public double[] soilMoistureConst
    {
        get
        {
             return _SoilTemperature.soilMoistureConst;
        }
        set
        {
            _SoilTemperature.soilMoistureConst = value;
        }
    }

    /// <summary>
    /// Gets and sets the baseTemperature
    /// </summary>
    [Description("baseTemperature")]
    [Units("°C")]
    public double baseTemp
    {
        get
        {
             return _SoilTemperature.baseTemp;
        }
        set
        {
            _SoilTemperature.baseTemp = value;
        }
    }

    /// <summary>
    /// Gets and sets the initialSurfaceTemperature
    /// </summary>
    [Description("initialSurfaceTemperature")]
    [Units("°C")]
    public double initialSurfaceTemp
    {
        get
        {
             return _SoilTemperature.initialSurfaceTemp;
        }
        set
        {
            _SoilTemperature.initialSurfaceTemp = value;
        }
    }

    /// <summary>
    /// Gets and sets the DensityAir
    /// </summary>
    [Description("DensityAir")]
    [Units("kg/m**3")]
    public double densityAir
    {
        get
        {
             return _SoilTemperature.densityAir;
        }
        set
        {
            _SoilTemperature.densityAir = value;
        }
    }

    /// <summary>
    /// Gets and sets the SpecificHeatCapacityAir
    /// </summary>
    [Description("SpecificHeatCapacityAir")]
    [Units("J/kg/K")]
    public double specificHeatCapacityAir
    {
        get
        {
             return _SoilTemperature.specificHeatCapacityAir;
        }
        set
        {
            _SoilTemperature.specificHeatCapacityAir = value;
        }
    }

    /// <summary>
    /// Gets and sets the DensityHumus
    /// </summary>
    [Description("DensityHumus")]
    [Units("kg/m**3")]
    public double densityHumus
    {
        get
        {
             return _SoilTemperature.densityHumus;
        }
        set
        {
            _SoilTemperature.densityHumus = value;
        }
    }

    /// <summary>
    /// Gets and sets the SpecificHeatCapacityHumus
    /// </summary>
    [Description("SpecificHeatCapacityHumus")]
    [Units("J/kg/K")]
    public double specificHeatCapacityHumus
    {
        get
        {
             return _SoilTemperature.specificHeatCapacityHumus;
        }
        set
        {
            _SoilTemperature.specificHeatCapacityHumus = value;
        }
    }

    /// <summary>
    /// Gets and sets the DensityWater
    /// </summary>
    [Description("DensityWater")]
    [Units("kg/m**3")]
    public double densityWater
    {
        get
        {
             return _SoilTemperature.densityWater;
        }
        set
        {
            _SoilTemperature.densityWater = value;
        }
    }

    /// <summary>
    /// Gets and sets the SpecificHeatCapacityWater
    /// </summary>
    [Description("SpecificHeatCapacityWater")]
    [Units("J/kg/K")]
    public double specificHeatCapacityWater
    {
        get
        {
             return _SoilTemperature.specificHeatCapacityWater;
        }
        set
        {
            _SoilTemperature.specificHeatCapacityWater = value;
        }
    }

    /// <summary>
    /// Gets and sets the QuartzRawDensity
    /// </summary>
    [Description("QuartzRawDensity")]
    [Units("kg/m**3")]
    public double quartzRawDensity
    {
        get
        {
             return _SoilTemperature.quartzRawDensity;
        }
        set
        {
            _SoilTemperature.quartzRawDensity = value;
        }
    }

    /// <summary>
    /// Gets and sets the SpecificHeatCapacityQuartz
    /// </summary>
    [Description("SpecificHeatCapacityQuartz")]
    [Units("J/kg/K")]
    public double specificHeatCapacityQuartz
    {
        get
        {
             return _SoilTemperature.specificHeatCapacityQuartz;
        }
        set
        {
            _SoilTemperature.specificHeatCapacityQuartz = value;
        }
    }

    /// <summary>
    /// Gets and sets the NTau
    /// </summary>
    [Description("NTau")]
    [Units("?")]
    public double nTau
    {
        get
        {
             return _SoilTemperature.nTau;
        }
        set
        {
            _SoilTemperature.nTau = value;
        }
    }

    /// <summary>
    /// Gets and sets the noOfTempLayers=noOfSoilLayers+2
    /// </summary>
    [Description("noOfTempLayers=noOfSoilLayers+2")]
    [Units("dimensionless")]
    public int noOfTempLayers
    {
        get
        {
             return _SoilTemperature.noOfTempLayers;
        }
        set
        {
            _SoilTemperature.noOfTempLayers = value;
        }
    }

    /// <summary>
    /// Gets and sets the for matrixSecondaryDiagonal
    /// </summary>
    [Description("for matrixSecondaryDiagonal")]
    [Units("dimensionless")]
    public int noOfTempLayersPlus1
    {
        get
        {
             return _SoilTemperature.noOfTempLayersPlus1;
        }
        set
        {
            _SoilTemperature.noOfTempLayersPlus1 = value;
        }
    }

    /// <summary>
    /// Gets and sets the noOfSoilLayers
    /// </summary>
    [Description("noOfSoilLayers")]
    [Units("dimensionless")]
    public int noOfSoilLayers
    {
        get
        {
             return _SoilTemperature.noOfSoilLayers;
        }
        set
        {
            _SoilTemperature.noOfSoilLayers = value;
        }
    }

    /// <summary>
    /// Gets and sets the layerThickness
    /// </summary>
    [Description("layerThickness")]
    [Units("m")]
    public double[] layerThickness
    {
        get
        {
             return _SoilTemperature.layerThickness;
        }
        set
        {
            _SoilTemperature.layerThickness = value;
        }
    }

    /// <summary>
    /// Gets and sets the bulkDensity
    /// </summary>
    [Description("bulkDensity")]
    [Units("kg/m**3")]
    public double[] soilBulkDensity
    {
        get
        {
             return _SoilTemperature.soilBulkDensity;
        }
        set
        {
            _SoilTemperature.soilBulkDensity = value;
        }
    }

    /// <summary>
    /// Gets and sets the saturation
    /// </summary>
    [Description("saturation")]
    [Units("m**3/m**3")]
    public double[] saturation
    {
        get
        {
             return _SoilTemperature.saturation;
        }
        set
        {
            _SoilTemperature.saturation = value;
        }
    }

    /// <summary>
    /// Gets and sets the soilOrganicMatter
    /// </summary>
    [Description("soilOrganicMatter")]
    [Units("m**3/m**3")]
    public double[] soilOrganicMatter
    {
        get
        {
             return _SoilTemperature.soilOrganicMatter;
        }
        set
        {
            _SoilTemperature.soilOrganicMatter = value;
        }
    }

    /// <summary>
    /// Algorithm of SoilTemperatureComp component
    /// </summary>
    public void CalculateModel(SoilTemperatureCompState s,SoilTemperatureCompState s1,SoilTemperatureCompRate r,SoilTemperatureCompAuxiliary a,SoilTemperatureCompExogenous ex)
    {
        _NoSnowSoilSurfaceTemperature.CalculateModel(s,s1, r, a, ex);
        s.noSnowSoilSurfaceTemperature = s.soilSurfaceTemperature;
        _WithSnowSoilSurfaceTemperature.CalculateModel(s,s1, r, a, ex);
        _SoilTemperature.CalculateModel(s,s1, r, a, ex);
    }

    /// <summary>
    /// Initialization of SoilTemperatureComp component
    /// </summary>
    public void Init(SoilTemperatureCompState s, SoilTemperatureCompState s1, SoilTemperatureCompRate r, SoilTemperatureCompAuxiliary a, SoilTemperatureCompExogenous ex)
    {
        _SoilTemperature.Init(s, s1, r, a, ex);
    }

    /// <summary>
    /// constructor copy of SoilTemperatureComp component
    /// </summary>
    /// <param name="toCopy"></param>
    public SoilTemperatureCompComponent(SoilTemperatureCompComponent toCopy): this() // copy constructor
    {
        dampingFactor = toCopy.dampingFactor;
        timeStep = toCopy.timeStep;

        for (int i = 0; i < noOfSoilLayers; i++)
        { soilMoistureConst[i] = toCopy.soilMoistureConst[i]; }

        baseTemp = toCopy.baseTemp;
        initialSurfaceTemp = toCopy.initialSurfaceTemp;
        densityAir = toCopy.densityAir;
        specificHeatCapacityAir = toCopy.specificHeatCapacityAir;
        densityHumus = toCopy.densityHumus;
        specificHeatCapacityHumus = toCopy.specificHeatCapacityHumus;
        densityWater = toCopy.densityWater;
        specificHeatCapacityWater = toCopy.specificHeatCapacityWater;
        quartzRawDensity = toCopy.quartzRawDensity;
        specificHeatCapacityQuartz = toCopy.specificHeatCapacityQuartz;
        nTau = toCopy.nTau;
        noOfTempLayers = toCopy.noOfTempLayers;
        noOfTempLayersPlus1 = toCopy.noOfTempLayersPlus1;
        noOfSoilLayers = toCopy.noOfSoilLayers;

        for (int i = 0; i < noOfTempLayers; i++)
        { layerThickness[i] = toCopy.layerThickness[i]; }


        for (int i = 0; i < noOfSoilLayers; i++)
        { soilBulkDensity[i] = toCopy.soilBulkDensity[i]; }


        for (int i = 0; i < noOfSoilLayers; i++)
        { saturation[i] = toCopy.saturation[i]; }


        for (int i = 0; i < noOfSoilLayers; i++)
        { soilOrganicMatter[i] = toCopy.soilOrganicMatter[i]; }

    }
}