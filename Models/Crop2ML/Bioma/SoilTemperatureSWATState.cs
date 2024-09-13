using Models.Core;
namespace Models.Crop2ML.Bioma;

/// <summary>State variables class for the SoilTemperatureSWAT component</summary>
public class SoilTemperatureSWATState
{
    /// <summary>Soil temperature of each layer</summary>
    [Units("degC")]
    public double[] SoilTemperatureByLayers { get; set; }
}