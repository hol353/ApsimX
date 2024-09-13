using Models.Core;
namespace Models.Crop2ML.Bioma;

/// <summary>
/// auxiliary variables class of the SurfacePartonSoilSWATC component
/// </summary>
public class SurfaceTemperaturePartonAuxiliary
{
    /// <summary>Minimum surface soil temperature</summary>
    [Units("degC")]
    public double SurfaceTemperatureMinimum { get; set; }

    /// <summary>The Maximum surface soil temperature</summary>
    [Units("degC")]
    public double SurfaceTemperatureMaximum { get; set; }

    /// <summary>Average surface soil temperature</summary>
    [Units("degC")]
    public double SurfaceSoilTemperature { get; set; }
}