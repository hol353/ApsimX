namespace Models.Interfaces
{
    /// <summary>
    /// This interface describes a soil temperature model.
    /// </summary>
    public interface ISoilTemperature
    {
        /// <summary>Returns soil temperature for each layer (oC)</summary>
        double[] Value { get; }

        /// <summary>Returns the daily average soil temperature for each layer (oC)</summary>
        double[] AverageSoilTemperature { get; }

        /// <summary>Returns the daily average temperature of soil surface (oC)</summary>
        double AverageSoilSurfaceTemperature { get; }

        /// <summary>Returns the daily minimum soil temperature for each layer (oC)</summary>
        double[] MinimumSoilTemperature { get; }

        /// <summary>Returns the daily minimum temperature of soil surface (oC)</summary>
        double MinimumSoilSurfaceTemperature { get; }

        /// <summary>Returns the daily maximum soil temperature for each layer (oC)</summary>
        double[] MaximumSoilTemperature { get; }

        /// <summary>Returns the daily maximum temperature of soil surface (oC)</summary>
        double MaximumSoilSurfaceTemperature { get; }

    }
}
