using System;
using System.Collections.Generic;
using Models.Core;
namespace Models.Crop2ML.Monica_SoilTemp;

/// <summary>
/// state variables class of the SoilTemperatureComp component
/// </summary>
public class SoilTemperatureCompState
{

    private double[] _V;
    private double[] _B;
    private double[] _volumeMatrix;
    private double[] _volumeMatrixOld;
    private double[] _matrixPrimaryDiagonal;
    private double[] _matrixSecondaryDiagonal;
    private double[] _heatConductivity;
    private double[] _heatConductivityMean;
    private double[] _heatCapacity;
    private double[] _solution;
    private double[] _matrixDiagonal;
    private double[] _matrixLowerTriangle;
    private double[] _heatFlow;
    private double _soilSurfaceTemperature;
    private double[] _soilTemperature = new double[22];
    private double _noSnowSoilSurfaceTemperature;

    /// <summary>
    /// Constructor SoilTemperatureCompState domain class
    /// </summary>
    public SoilTemperatureCompState() { }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toCopy"></param>
    /// <param name="copyAll"></param>
    public SoilTemperatureCompState(SoilTemperatureCompState toCopy, bool copyAll) // copy constructor
    {
        if (copyAll)
        {
            V = new double[toCopy.V.Length];
            for (int i = 0; i < toCopy.V.Length; i++)
            { V[i] = toCopy.V[i]; }

            B = new double[toCopy.B.Length];
            for (int i = 0; i < toCopy.B.Length; i++)
            { B[i] = toCopy.B[i]; }

            volumeMatrix = new double[toCopy.volumeMatrix.Length];
            for (int i = 0; i < toCopy.volumeMatrix.Length; i++)
            { volumeMatrix[i] = toCopy.volumeMatrix[i]; }

            volumeMatrixOld = new double[toCopy.volumeMatrixOld.Length];
        for (int i = 0; i < toCopy.volumeMatrixOld.Length; i++)
        { volumeMatrixOld[i] = toCopy.volumeMatrixOld[i]; }

            matrixPrimaryDiagonal = new double[toCopy.matrixPrimaryDiagonal.Length];
        for (int i = 0; i < toCopy.matrixPrimaryDiagonal.Length; i++)
        { matrixPrimaryDiagonal[i] = toCopy.matrixPrimaryDiagonal[i]; }

            matrixSecondaryDiagonal = new double[toCopy.matrixSecondaryDiagonal.Length];
        for (int i = 0; i < toCopy.matrixSecondaryDiagonal.Length; i++)
        { matrixSecondaryDiagonal[i] = toCopy.matrixSecondaryDiagonal[i]; }

            heatConductivity = new double[toCopy.heatConductivity.Length];
        for (int i = 0; i < toCopy.heatConductivity.Length; i++)
        { heatConductivity[i] = toCopy.heatConductivity[i]; }

            heatConductivityMean = new double[toCopy.heatConductivityMean.Length];
        for (int i = 0; i < toCopy.heatConductivityMean.Length; i++)
        { heatConductivityMean[i] = toCopy.heatConductivityMean[i]; }

            heatCapacity = new double[toCopy.heatCapacity.Length];
        for (int i = 0; i < toCopy.heatCapacity.Length; i++)
        { heatCapacity[i] = toCopy.heatCapacity[i]; }

            solution = new double[toCopy.solution.Length];
        for (int i = 0; i < toCopy.solution.Length; i++)
        { solution[i] = toCopy.solution[i]; }

            matrixDiagonal = new double[toCopy.matrixDiagonal.Length];
        for (int i = 0; i < toCopy.matrixDiagonal.Length; i++)
        { matrixDiagonal[i] = toCopy.matrixDiagonal[i]; }

            matrixLowerTriangle = new double[toCopy.matrixLowerTriangle.Length];
        for (int i = 0; i < toCopy.matrixLowerTriangle.Length; i++)
        { matrixLowerTriangle[i] = toCopy.matrixLowerTriangle[i]; }

            heatFlow = new double[toCopy.heatFlow.Length];
        for (int i = 0; i < toCopy.heatFlow.Length; i++)
        { heatFlow[i] = toCopy.heatFlow[i]; }

            soilSurfaceTemperature = toCopy.soilSurfaceTemperature;
            soilTemperature = new double[22];
        for (int i = 0; i < 22; i++)
        { soilTemperature[i] = toCopy.soilTemperature[i]; }

            noSnowSoilSurfaceTemperature = toCopy.noSnowSoilSurfaceTemperature;
        }
    }

    /// <summary>
    /// Gets and sets the V
    /// </summary>
    [Description("V")]
    [Units("°C")]
    public double[] V
    {
        get { return this._V; }
        set { this._V= value; }
    }

    /// <summary>
    /// Gets and sets the B
    /// </summary>
    [Description("B")]
    [Units("°C")]
    public double[] B
    {
        get { return this._B; }
        set { this._B= value; }
    }

    /// <summary>
    /// Gets and sets the volumeMatrix
    /// </summary>
    [Description("volumeMatrix")]
    [Units("°C")]
    public double[] volumeMatrix
    {
        get { return this._volumeMatrix; }
        set { this._volumeMatrix= value; }
    }

    /// <summary>
    /// Gets and sets the volumeMatrixOld
    /// </summary>
    [Description("volumeMatrixOld")]
    [Units("°C")]
    public double[] volumeMatrixOld
    {
        get { return this._volumeMatrixOld; }
        set { this._volumeMatrixOld= value; }
    }

    /// <summary>
    /// Gets and sets the matrixPrimaryDiagonal
    /// </summary>
    [Description("matrixPrimaryDiagonal")]
    [Units("°C")]
    public double[] matrixPrimaryDiagonal
    {
        get { return this._matrixPrimaryDiagonal; }
        set { this._matrixPrimaryDiagonal= value; }
    }

    /// <summary>
    /// Gets and sets the matrixSecondaryDiagonal
    /// </summary>
    [Description("matrixSecondaryDiagonal")]
    [Units("°C")]
    public double[] matrixSecondaryDiagonal
    {
        get { return this._matrixSecondaryDiagonal; }
        set { this._matrixSecondaryDiagonal= value; }
    }

    /// <summary>
    /// Gets and sets the heatConductivity
    /// </summary>
    [Description("heatConductivity")]
    [Units("°C")]
    public double[] heatConductivity
    {
        get { return this._heatConductivity; }
        set { this._heatConductivity= value; }
    }

    /// <summary>
    /// Gets and sets the heatConductivityMean
    /// </summary>
    [Description("heatConductivityMean")]
    [Units("°C")]
    public double[] heatConductivityMean
    {
        get { return this._heatConductivityMean; }
        set { this._heatConductivityMean= value; }
    }

    /// <summary>
    /// Gets and sets the heatCapacity
    /// </summary>
    [Description("heatCapacity")]
    [Units("°C")]
    public double[] heatCapacity
    {
        get { return this._heatCapacity; }
        set { this._heatCapacity= value; }
    }

    /// <summary>
    /// Gets and sets the solution
    /// </summary>
    [Description("solution")]
    [Units("°C")]
    public double[] solution
    {
        get { return this._solution; }
        set { this._solution= value; }
    }

    /// <summary>
    /// Gets and sets the matrixDiagonal
    /// </summary>
    [Description("matrixDiagonal")]
    [Units("°C")]
    public double[] matrixDiagonal
    {
        get { return this._matrixDiagonal; }
        set { this._matrixDiagonal= value; }
    }

    /// <summary>
    /// Gets and sets the matrixLowerTriangle
    /// </summary>
    [Description("matrixLowerTriangle")]
    [Units("°C")]
    public double[] matrixLowerTriangle
    {
        get { return this._matrixLowerTriangle; }
        set { this._matrixLowerTriangle= value; }
    }

    /// <summary>
    /// Gets and sets the heatFlow
    /// </summary>
    [Description("heatFlow")]
    [Units("°C")]
    public double[] heatFlow
    {
        get { return this._heatFlow; }
        set { this._heatFlow= value; }
    }

    /// <summary>
    /// Gets and sets the soilSurfaceTemperature
    /// </summary>
    [Description("soilSurfaceTemperature")]
    [Units("°C")]
    public double soilSurfaceTemperature
    {
        get { return this._soilSurfaceTemperature; }
        set { this._soilSurfaceTemperature= value; }
    }

    /// <summary>
    /// Gets and sets the soilTemperature next day
    /// </summary>
    [Description("soilTemperature next day")]
    [Units("°C")]
    public double[] soilTemperature
    {
        get { return this._soilTemperature; }
        set { this._soilTemperature= value; }
    }

    /// <summary>
    /// Gets and sets the soilSurfaceTemperature without snow
    /// </summary>
    [Description("soilSurfaceTemperature without snow")]
    [Units("°C")]
    public double noSnowSoilSurfaceTemperature
    {
        get { return this._noSnowSoilSurfaceTemperature; }
        set { this._noSnowSoilSurfaceTemperature= value; }
    }

}