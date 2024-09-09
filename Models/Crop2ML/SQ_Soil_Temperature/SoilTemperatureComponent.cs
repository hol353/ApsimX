using Models.Core;
using Models.Utilities;
using System;
namespace Models.Crop2ML.SQ_Soil_Temperature;


/// <summary>
///  SoilTemperature component
/// </summary>
public class SoilTemperatureComponent
{

    /// <summary>
    ///  constructor of SoilTemperature component
    /// </summary>
    public SoilTemperatureComponent() {}

    //Declaration of the associated strategies
    CalculateSoilTemperature _CalculateSoilTemperature = new CalculateSoilTemperature();
    CalculateHourlySoilTemperature _CalculateHourlySoilTemperature = new CalculateHourlySoilTemperature();

    /// <summary>
    /// Gets and sets the Latente heat of water vaporization at 20Â°C
    /// </summary>
    [Description("Latente heat of water vaporization at 20Â°C")]
    [Units("MJ.kg-1")]
    public double lambda_
    {
        get
        {
             return _CalculateSoilTemperature.lambda_;
        }
        set
        {
            _CalculateSoilTemperature.lambda_ = value;
        }
    }

    /// <summary>
    /// Gets and sets the Delay between sunrise and time when minimum temperature is reached
    /// </summary>
    [Description("Delay between sunrise and time when minimum temperature is reached")]
    [Units("Hour")]
    public double b
    {
        get
        {
             return _CalculateHourlySoilTemperature.b;
        }
        set
        {
            _CalculateHourlySoilTemperature.b = value;
        }
    }

    /// <summary>
    /// Gets and sets the Nighttime temperature coefficient
    /// </summary>
    [Description("Nighttime temperature coefficient")]
    [Units("Dpmensionless")]
    public double c
    {
        get
        {
             return _CalculateHourlySoilTemperature.c;
        }
        set
        {
            _CalculateHourlySoilTemperature.c = value;
        }
    }

    /// <summary>
    /// Gets and sets the Delay between sunset and time when maximum temperature is reached
    /// </summary>
    [Description("Delay between sunset and time when maximum temperature is reached")]
    [Units("Hour")]
    public double a
    {
        get
        {
             return _CalculateHourlySoilTemperature.a;
        }
        set
        {
            _CalculateHourlySoilTemperature.a = value;
        }
    }

    /// <summary>
    /// Algorithm of SoilTemperature component
    /// </summary>
    public void CalculateModel(SoilTemperatureState s,SoilTemperatureState s1,SoilTemperatureRate r,SoilTemperatureAuxiliary a,SoilTemperatureExogenous ex)
    {
        _CalculateSoilTemperature.CalculateModel(s,s1, r, a, ex);
        _CalculateHourlySoilTemperature.CalculateModel(s,s1, r, a, ex);
    }

    /// <summary>
    /// Initialization of SoilTemperature component
    /// </summary>
    public void Init(SoilTemperatureState s, SoilTemperatureState s1, SoilTemperatureRate r, SoilTemperatureAuxiliary a, SoilTemperatureExogenous ex)
    {
        _CalculateSoilTemperature.Init(s, s1, r, a, ex);
    }

    /// <summary>
    /// constructor copy of SoilTemperature component
    /// </summary>
    /// <param name="toCopy"></param>
    public SoilTemperatureComponent(SoilTemperatureComponent toCopy): this() // copy constructor
    {
            lambda_ = toCopy.lambda_;
            b = toCopy.b;
            c = toCopy.c;
            a = toCopy.a;
    }
}