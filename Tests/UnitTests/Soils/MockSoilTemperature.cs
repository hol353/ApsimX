using Models.Core;
using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Soils
{
    internal class MockSoilTemperature : Model, ISoilTemperature
    {
        public double[] Value { get; set; }
        public double[] AverageSoilTemperature { get; }
        public double AverageSoilSurfaceTemperature { get; }
        public double[] MinimumSoilTemperature { get; }
        public double MinimumSoilSurfaceTemperature { get; }
        public double[] MaximumSoilTemperature { get; }
        public double MaximumSoilSurfaceTemperature { get; }

        public event EventHandler SoilTemperatureChanged;
    }
}
