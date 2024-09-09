namespace Models.Soils
{
    /// <summary>An interface for soil physical properties.</summary>
    public interface IPhysical
    {
        /// <summary>Air dry (mm/mm).</summary>
        double[] AirDry { get; set; }

        /// <summary>Bulk density (g/cc).</summary>
        double[] BD { get; set; }

        /// <summary>Drained upper limit (mm/mm).</summary>
        double[] DUL { get; set; }

        /// <summary>Drained upper limit (mm).</summary>
        double[] DULmm { get; }

        /// <summary>KS (mm/day).</summary>
        double[] KS { get; set; }

        /// <summary>Lower limit 15 bar (mm/mm).</summary>
        double[] LL15 { get; set; }

        /// <summary>Lower limit 15 bar (mm).</summary>
        double[] LL15mm { get; }

        /// <summary>Particle size clay.</summary>
        double[] ParticleSizeClay { get; set; }

        /// <summary>Particle size sand.</summary>
        double[] ParticleSizeSand { get; set; }

        /// <summary>Particle size silt.</summary>
        double[] ParticleSizeSilt { get; set; }

        /// <summary>Rocks.</summary>
        double[] Rocks { get; set; }

        /// <summary>Saturation (mm/mm).</summary>
        double[] SAT { get; set; }

        /// <summary>Saturation (mm).</summary>
        double[] SATmm { get; }

        /// <summary>Texture.</summary>
        string[] Texture { get; }

        /// <summary>Soil layer thickness (mm).</summary>
        double[] Thickness { get; set; }

        /// <summary>Soil layer cumulative thicknesses (mm)</summary>
        double[] ThicknessCumulative { get; }

        /// <summary>Gets the depth mid points (mm).</summary>
        double[] DepthMidPoints { get; }

        /// <summary>Plant available water CAPACITY (DUL-LL15).</summary>
        double[] PAWC { get; }

        /// <summary>Plant available water CAPACITY (DUL-LL15).</summary>
        double[] PAWCmm { get; }
    }
}