using System;
using System.Collections.Generic;
using System.Linq;    
using Models.Core;   
namespace Models.Crop2ML.Monica_SoilTemp;

/// <summary>
///- Name: SoilTemperature -Version: 1, -Time step: 1
///- Description:
///            * Title: Model of soil temperature
///            * Authors: Michael Berg-Mohnicke
///            * Reference: None
///            * Institution: ZALF e.V.
///            * ExtendedDescription: None
///            * ShortDescription: Calculates the soil temperature at all soil layers
///- inputs:
///            * name: noOfSoilLayers
///                          ** description : noOfSoilLayers
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : INT
///                          ** max : 
///                          ** min : 
///                          ** default : 20
///                          ** unit : dimensionless
///            * name: noOfTempLayers
///                          ** description : noOfTempLayers=noOfSoilLayers+2
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : INT
///                          ** max : 
///                          ** min : 
///                          ** default : 22
///                          ** unit : dimensionless
///            * name: noOfTempLayersPlus1
///                          ** description : for matrixSecondaryDiagonal
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : INT
///                          ** max : 
///                          ** min : 
///                          ** default : 23
///                          ** unit : dimensionless
///            * name: soilSurfaceTemperature
///                          ** description : current soilSurfaceTemperature
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLE
///                          ** max : 80.0
///                          ** min : -50.0
///                          ** default : 0.0
///                          ** unit : °C
///            * name: timeStep
///                          ** description : timeStep
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 1.0
///                          ** unit : dimensionless
///            * name: soilMoistureConst
///                          ** description : constant soilmoisture during the model run
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfSoilLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : m**3/m**3
///            * name: baseTemp
///                          ** description : baseTemperature
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 9.5
///                          ** unit : °C
///            * name: initialSurfaceTemp
///                          ** description : initialSurfaceTemperature
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 10.0
///                          ** unit : °C
///            * name: densityAir
///                          ** description : DensityAir
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 1.25
///                          ** unit : kg/m**3
///            * name: specificHeatCapacityAir
///                          ** description : SpecificHeatCapacityAir
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 1005.0
///                          ** unit : J/kg/K
///            * name: densityHumus
///                          ** description : DensityHumus
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 1300.0
///                          ** unit : kg/m**3
///            * name: specificHeatCapacityHumus
///                          ** description : SpecificHeatCapacityHumus
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 1920.0
///                          ** unit : J/kg/K
///            * name: densityWater
///                          ** description : DensityWater
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 1000.0
///                          ** unit : kg/m**3
///            * name: specificHeatCapacityWater
///                          ** description : SpecificHeatCapacityWater
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 4192.0
///                          ** unit : J/kg/K
///            * name: quartzRawDensity
///                          ** description : QuartzRawDensity
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 2650.0
///                          ** unit : kg/m**3
///            * name: specificHeatCapacityQuartz
///                          ** description : SpecificHeatCapacityQuartz
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 750.0
///                          ** unit : J/kg/K
///            * name: nTau
///                          ** description : NTau
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLE
///                          ** max : 
///                          ** min : 
///                          ** default : 0.65
///                          ** unit : ?
///            * name: layerThickness
///                          ** description : layerThickness
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : m
///            * name: soilBulkDensity
///                          ** description : bulkDensity
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfSoilLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : kg/m**3
///            * name: saturation
///                          ** description : saturation
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfSoilLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : m**3/m**3
///            * name: soilOrganicMatter
///                          ** description : soilOrganicMatter
///                          ** inputtype : parameter
///                          ** parametercategory : constant
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfSoilLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : m**3/m**3
///            * name: soilTemperature
///                          ** description : soilTemperature
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: V
///                          ** description : V
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: B
///                          ** description : B
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: volumeMatrix
///                          ** description : volumeMatrix
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: volumeMatrixOld
///                          ** description : volumeMatrixOld
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: matrixPrimaryDiagonal
///                          ** description : matrixPrimaryDiagonal
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: matrixSecondaryDiagonal
///                          ** description : matrixSecondaryDiagonal
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayersPlus1
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: heatConductivity
///                          ** description : heatConductivity
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: heatConductivityMean
///                          ** description : heatConductivityMean
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: heatCapacity
///                          ** description : heatCapacity
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: solution
///                          ** description : solution
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: matrixDiagonal
///                          ** description : matrixDiagonal
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: matrixLowerTriangle
///                          ** description : matrixLowerTriangle
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///            * name: heatFlow
///                          ** description : heatFlow
///                          ** inputtype : variable
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : noOfTempLayers
///                          ** max : 
///                          ** min : 
///                          ** default : 
///                          ** unit : °C
///- outputs:
///            * name: soilTemperature
///                          ** description : soilTemperature next day
///                          ** variablecategory : state
///                          ** datatype : DOUBLEARRAY
///                          ** len : 22
///                          ** max : 
///                          ** min : 
///                          ** unit : °C
/// </summary>
public class SoilTemperature
{

    /// <summary>
    /// initialization of the SoilTemperature component
    /// </summary>
    public void Init(SoilTemperatureCompState s, SoilTemperatureCompState s1, SoilTemperatureCompRate r, SoilTemperatureCompAuxiliary a, SoilTemperatureCompExogenous ex)
    {
        double soilSurfaceTemperature = 0.0;
        double[] soilTemperature =  new double [noOfTempLayers];
        double[] V =  new double [noOfTempLayers];
        double[] B =  new double [noOfTempLayers];
        double[] volumeMatrix =  new double [noOfTempLayers];
        double[] volumeMatrixOld =  new double [noOfTempLayers];
        double[] matrixPrimaryDiagonal =  new double [noOfTempLayers];
        double[] matrixSecondaryDiagonal =  new double [noOfTempLayersPlus1];
        double[] heatConductivity =  new double [noOfTempLayers];
        double[] heatConductivityMean =  new double [noOfTempLayers];
        double[] heatCapacity =  new double [noOfTempLayers];
        double[] solution =  new double [noOfTempLayers];
        double[] matrixDiagonal =  new double [noOfTempLayers];
        double[] matrixLowerTriangle =  new double [noOfTempLayers];
        double[] heatFlow =  new double [noOfTempLayers];
        soilTemperature = new double[noOfTempLayers];
        V = new double[noOfTempLayers];
        B = new double[noOfTempLayers];
        volumeMatrix = new double[noOfTempLayers];
        volumeMatrixOld = new double[noOfTempLayers];
        matrixPrimaryDiagonal = new double[noOfTempLayers];
        matrixSecondaryDiagonal = new double[noOfTempLayersPlus1];
        heatConductivity = new double[noOfTempLayers];
        heatConductivityMean = new double[noOfTempLayers];
        heatCapacity = new double[noOfTempLayers];
        solution = new double[noOfTempLayers];
        matrixDiagonal = new double[noOfTempLayers];
        matrixLowerTriangle = new double[noOfTempLayers];
        heatFlow = new double[noOfTempLayers];
        int groundLayer;
        int bottomLayer;
        double lti_1;
        double lti;
        double ts;
        double dw;
        double cw;
        double dq;
        double cq;
        double da;
        double ca;
        double dh;
        double ch;
        double sbdi;
        double smi;
        double sati;
        double somi;
        double hci_1;
        double hci;
        int i;
        for (i=0 ; i!=noOfSoilLayers ; i+=1)
        {
            soilTemperature[i] = (1.00d - ((double)(i) / noOfSoilLayers)) * initialSurfaceTemp + ((double)(i) / noOfSoilLayers * baseTemp);
        }
        groundLayer = noOfTempLayers - 2;
        bottomLayer = noOfTempLayers - 1;
        layerThickness[groundLayer] = 2.00d * layerThickness[(groundLayer - 1)];
        layerThickness[bottomLayer] = 1.00d;
        soilTemperature[groundLayer] = (soilTemperature[groundLayer - 1] + baseTemp) * 0.50d;
        soilTemperature[bottomLayer] = baseTemp;
        V[0] = layerThickness[0];
        B[0] = 2.00d / layerThickness[0];
        for (i=1 ; i!=noOfTempLayers ; i+=1)
        {
            lti_1 = layerThickness[i - 1];
            lti = layerThickness[i];
            B[i] = 2.00d / (lti + lti_1);
            V[i] = lti * nTau;
        }
        ts = timeStep;
        dw = densityWater;
        cw = specificHeatCapacityWater;
        dq = quartzRawDensity;
        cq = specificHeatCapacityQuartz;
        da = densityAir;
        ca = specificHeatCapacityAir;
        dh = densityHumus;
        ch = specificHeatCapacityHumus;
        for (i=0 ; i!=noOfSoilLayers ; i+=1)
        {
            sbdi = soilBulkDensity[i];
            smi = soilMoistureConst[i];
            heatConductivity[i] = (3.00d * (sbdi / 1000.00d) - 1.70d) * 0.0010d / (1.00d + ((11.50d - (5.00d * (sbdi / 1000.00d))) * Math.Exp(-50.00d * Math.Pow(smi / (sbdi / 1000.00d), 1.50d)))) * 86400.00d * ts * 100.00d * 4.1840d;
            sati = saturation[i];
            somi = soilOrganicMatter[i] / da * sbdi;
            heatCapacity[i] = smi * dw * cw + ((sati - smi) * da * ca) + (somi * dh * ch) + ((1.00d - sati - somi) * dq * cq);
        }
        heatCapacity[groundLayer] = heatCapacity[groundLayer - 1];
        heatCapacity[bottomLayer] = heatCapacity[groundLayer];
        heatConductivity[groundLayer] = heatConductivity[groundLayer - 1];
        heatConductivity[bottomLayer] = heatConductivity[groundLayer];
        soilSurfaceTemperature = initialSurfaceTemp;
        heatConductivityMean[0] = heatConductivity[0];
        for (i=1 ; i!=noOfTempLayers ; i+=1)
        {
            lti_1 = layerThickness[i - 1];
            lti = layerThickness[i];
            hci_1 = heatConductivity[i - 1];
            hci = heatConductivity[i];
            heatConductivityMean[i] = (lti_1 * hci_1 + (lti * hci)) / (lti + lti_1);
        }
        for (i=0 ; i!=noOfTempLayers ; i+=1)
        {
            volumeMatrix[i] = V[i] * heatCapacity[i];
            volumeMatrixOld[i] = volumeMatrix[i];
            matrixSecondaryDiagonal[i] = -B[i] * heatConductivityMean[i];
        }
        matrixSecondaryDiagonal[bottomLayer + 1] = 0.00d;
        for (i=0 ; i!=noOfTempLayers ; i+=1)
        {
            matrixPrimaryDiagonal[i] = volumeMatrix[i] - matrixSecondaryDiagonal[i] - matrixSecondaryDiagonal[i + 1];
        }
        s.soilSurfaceTemperature= soilSurfaceTemperature;
        s.soilTemperature= soilTemperature;
        s.V= V;
        s.B= B;
        s.volumeMatrix= volumeMatrix;
        s.volumeMatrixOld= volumeMatrixOld;
        s.matrixPrimaryDiagonal= matrixPrimaryDiagonal;
        s.matrixSecondaryDiagonal= matrixSecondaryDiagonal;
        s.heatConductivity= heatConductivity;
        s.heatConductivityMean= heatConductivityMean;
        s.heatCapacity= heatCapacity;
        s.solution= solution;
        s.matrixDiagonal= matrixDiagonal;
        s.matrixLowerTriangle= matrixLowerTriangle;
        s.heatFlow= heatFlow;
    }

    private int _noOfSoilLayers;
    /// <summary>
    /// Gets and sets the noOfSoilLayers
    /// </summary>
    [Description("noOfSoilLayers")] 
    [Units("dimensionless")] 
    //[Crop2ML(datatype="INT", min=null, max=null, default=20, parametercategory=constant, inputtype="parameter")] 
    public int noOfSoilLayers
    {
        get { return this._noOfSoilLayers; }
        set { this._noOfSoilLayers= value; } 
    }

    private int _noOfTempLayers;
    /// <summary>
    /// Gets and sets the noOfTempLayers=noOfSoilLayers+2
    /// </summary>
    [Description("noOfTempLayers=noOfSoilLayers+2")] 
    [Units("dimensionless")] 
    //[Crop2ML(datatype="INT", min=null, max=null, default=22, parametercategory=constant, inputtype="parameter")] 
    public int noOfTempLayers
    {
        get { return this._noOfTempLayers; }
        set { this._noOfTempLayers= value; } 
    }

    private int _noOfTempLayersPlus1;
    /// <summary>
    /// Gets and sets the for matrixSecondaryDiagonal
    /// </summary>
    [Description("for matrixSecondaryDiagonal")] 
    [Units("dimensionless")] 
    //[Crop2ML(datatype="INT", min=null, max=null, default=23, parametercategory=constant, inputtype="parameter")] 
    public int noOfTempLayersPlus1
    {
        get { return this._noOfTempLayersPlus1; }
        set { this._noOfTempLayersPlus1= value; } 
    }

    private double _timeStep;
    /// <summary>
    /// Gets and sets the timeStep
    /// </summary>
    [Description("timeStep")] 
    [Units("dimensionless")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=1.0, parametercategory=constant, inputtype="parameter")] 
    public double timeStep
    {
        get { return this._timeStep; }
        set { this._timeStep= value; } 
    }

    private double[] _soilMoistureConst;
    /// <summary>
    /// Gets and sets the constant soilmoisture during the model run
    /// </summary>
    [Description("constant soilmoisture during the model run")] 
    [Units("m**3/m**3")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=constant, inputtype="parameter")] 
    public double[] soilMoistureConst
    {
        get { return this._soilMoistureConst; }
        set { this._soilMoistureConst= value; } 
    }

    private double _baseTemp;
    /// <summary>
    /// Gets and sets the baseTemperature
    /// </summary>
    [Description("baseTemperature")] 
    [Units("°C")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=9.5, parametercategory=constant, inputtype="parameter")] 
    public double baseTemp
    {
        get { return this._baseTemp; }
        set { this._baseTemp= value; } 
    }

    private double _initialSurfaceTemp;
    /// <summary>
    /// Gets and sets the initialSurfaceTemperature
    /// </summary>
    [Description("initialSurfaceTemperature")] 
    [Units("°C")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=10.0, parametercategory=constant, inputtype="parameter")] 
    public double initialSurfaceTemp
    {
        get { return this._initialSurfaceTemp; }
        set { this._initialSurfaceTemp= value; } 
    }

    private double _densityAir;
    /// <summary>
    /// Gets and sets the DensityAir
    /// </summary>
    [Description("DensityAir")] 
    [Units("kg/m**3")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=1.25, parametercategory=constant, inputtype="parameter")] 
    public double densityAir
    {
        get { return this._densityAir; }
        set { this._densityAir= value; } 
    }

    private double _specificHeatCapacityAir;
    /// <summary>
    /// Gets and sets the SpecificHeatCapacityAir
    /// </summary>
    [Description("SpecificHeatCapacityAir")] 
    [Units("J/kg/K")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=1005.0, parametercategory=constant, inputtype="parameter")] 
    public double specificHeatCapacityAir
    {
        get { return this._specificHeatCapacityAir; }
        set { this._specificHeatCapacityAir= value; } 
    }

    private double _densityHumus;
    /// <summary>
    /// Gets and sets the DensityHumus
    /// </summary>
    [Description("DensityHumus")] 
    [Units("kg/m**3")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=1300.0, parametercategory=constant, inputtype="parameter")] 
    public double densityHumus
    {
        get { return this._densityHumus; }
        set { this._densityHumus= value; } 
    }

    private double _specificHeatCapacityHumus;
    /// <summary>
    /// Gets and sets the SpecificHeatCapacityHumus
    /// </summary>
    [Description("SpecificHeatCapacityHumus")] 
    [Units("J/kg/K")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=1920.0, parametercategory=constant, inputtype="parameter")] 
    public double specificHeatCapacityHumus
    {
        get { return this._specificHeatCapacityHumus; }
        set { this._specificHeatCapacityHumus= value; } 
    }

    private double _densityWater;
    /// <summary>
    /// Gets and sets the DensityWater
    /// </summary>
    [Description("DensityWater")] 
    [Units("kg/m**3")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=1000.0, parametercategory=constant, inputtype="parameter")] 
    public double densityWater
    {
        get { return this._densityWater; }
        set { this._densityWater= value; } 
    }

    private double _specificHeatCapacityWater;
    /// <summary>
    /// Gets and sets the SpecificHeatCapacityWater
    /// </summary>
    [Description("SpecificHeatCapacityWater")] 
    [Units("J/kg/K")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=4192.0, parametercategory=constant, inputtype="parameter")] 
    public double specificHeatCapacityWater
    {
        get { return this._specificHeatCapacityWater; }
        set { this._specificHeatCapacityWater= value; } 
    }

    private double _quartzRawDensity;
    /// <summary>
    /// Gets and sets the QuartzRawDensity
    /// </summary>
    [Description("QuartzRawDensity")] 
    [Units("kg/m**3")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=2650.0, parametercategory=constant, inputtype="parameter")] 
    public double quartzRawDensity
    {
        get { return this._quartzRawDensity; }
        set { this._quartzRawDensity= value; } 
    }

    private double _specificHeatCapacityQuartz;
    /// <summary>
    /// Gets and sets the SpecificHeatCapacityQuartz
    /// </summary>
    [Description("SpecificHeatCapacityQuartz")] 
    [Units("J/kg/K")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=750.0, parametercategory=constant, inputtype="parameter")] 
    public double specificHeatCapacityQuartz
    {
        get { return this._specificHeatCapacityQuartz; }
        set { this._specificHeatCapacityQuartz= value; } 
    }

    private double _nTau;
    /// <summary>
    /// Gets and sets the NTau
    /// </summary>
    [Description("NTau")] 
    [Units("?")] 
    //[Crop2ML(datatype="DOUBLE", min=null, max=null, default=0.65, parametercategory=constant, inputtype="parameter")] 
    public double nTau
    {
        get { return this._nTau; }
        set { this._nTau= value; } 
    }

    private double[] _layerThickness;
    /// <summary>
    /// Gets and sets the layerThickness
    /// </summary>
    [Description("layerThickness")] 
    [Units("m")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=constant, inputtype="parameter")] 
    public double[] layerThickness
    {
        get { return this._layerThickness; }
        set { this._layerThickness= value; } 
    }

    private double[] _soilBulkDensity;
    /// <summary>
    /// Gets and sets the bulkDensity
    /// </summary>
    [Description("bulkDensity")] 
    [Units("kg/m**3")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=constant, inputtype="parameter")] 
    public double[] soilBulkDensity
    {
        get { return this._soilBulkDensity; }
        set { this._soilBulkDensity= value; } 
    }

    private double[] _saturation;
    /// <summary>
    /// Gets and sets the saturation
    /// </summary>
    [Description("saturation")] 
    [Units("m**3/m**3")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=constant, inputtype="parameter")] 
    public double[] saturation
    {
        get { return this._saturation; }
        set { this._saturation= value; } 
    }

    private double[] _soilOrganicMatter;
    /// <summary>
    /// Gets and sets the soilOrganicMatter
    /// </summary>
    [Description("soilOrganicMatter")] 
    [Units("m**3/m**3")] 
    //[Crop2ML(datatype="DOUBLEARRAY", min=null, max=null, default=, parametercategory=constant, inputtype="parameter")] 
    public double[] soilOrganicMatter
    {
        get { return this._soilOrganicMatter; }
        set { this._soilOrganicMatter= value; } 
    }

    
    /// <summary>
    /// Constructor of the SoilTemperature component")
    /// </summary>  
    public SoilTemperature() { }
    
    /// <summary>
    /// Algorithm of the SoilTemperature component
    /// </summary>
    public void  CalculateModel(SoilTemperatureCompState s, SoilTemperatureCompState s1, SoilTemperatureCompRate r, SoilTemperatureCompAuxiliary a, SoilTemperatureCompExogenous ex)
    {
        double soilSurfaceTemperature = s.soilSurfaceTemperature;
        double[] soilTemperature = s.soilTemperature;
        double[] V = s.V;
        double[] B = s.B;
        double[] volumeMatrix = s.volumeMatrix;
        double[] volumeMatrixOld = s.volumeMatrixOld;
        double[] matrixPrimaryDiagonal = s.matrixPrimaryDiagonal;
        double[] matrixSecondaryDiagonal = s.matrixSecondaryDiagonal;
        double[] heatConductivity = s.heatConductivity;
        double[] heatConductivityMean = s.heatConductivityMean;
        double[] heatCapacity = s.heatCapacity;
        double[] solution = s.solution;
        double[] matrixDiagonal = s.matrixDiagonal;
        double[] matrixLowerTriangle = s.matrixLowerTriangle;
        double[] heatFlow = s.heatFlow;
        int groundLayer;
        int bottomLayer;
        int i;
        int j;
        int j_1;
        groundLayer = noOfTempLayers - 2;
        bottomLayer = noOfTempLayers - 1;
        heatFlow[0] = soilSurfaceTemperature * B[0] * heatConductivityMean[0];
        for (i=0 ; i!=noOfTempLayers ; i+=1)
        {
            solution[i] = (volumeMatrixOld[i] + ((volumeMatrix[i] - volumeMatrixOld[i]) / layerThickness[i])) * soilTemperature[i] + heatFlow[i];
        }
        matrixDiagonal[0] = matrixPrimaryDiagonal[0];
        for (i=1 ; i!=noOfTempLayers ; i+=1)
        {
            matrixLowerTriangle[i] = matrixSecondaryDiagonal[i] / matrixDiagonal[(i - 1)];
            matrixDiagonal[i] = matrixPrimaryDiagonal[i] - (matrixLowerTriangle[i] * matrixSecondaryDiagonal[i]);
        }
        for (i=1 ; i!=noOfTempLayers ; i+=1)
        {
            solution[i] = solution[i] - (matrixLowerTriangle[i] * solution[(i - 1)]);
        }
        solution[bottomLayer] = solution[bottomLayer] / matrixDiagonal[bottomLayer];
        for (i=0 ; i!=bottomLayer ; i+=1)
        {
            j = bottomLayer - 1 - i;
            j_1 = j + 1;
            solution[j] = solution[j] / matrixDiagonal[j] - (matrixLowerTriangle[j_1] * solution[j_1]);
        }
        for (i=0 ; i!=noOfTempLayers ; i+=1)
        {
            soilTemperature[i] = solution[i];
        }
        for (i=0 ; i!=noOfSoilLayers ; i+=1)
        {
            volumeMatrixOld[i] = volumeMatrix[i];
        }
        volumeMatrixOld[groundLayer] = volumeMatrix[groundLayer];
        volumeMatrixOld[bottomLayer] = volumeMatrix[bottomLayer];
        s.soilTemperature= soilTemperature;
    }
}