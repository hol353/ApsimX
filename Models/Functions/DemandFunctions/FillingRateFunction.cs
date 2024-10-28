using System;
using Models.Core;

namespace Models.Functions.DemandFunctions
{
    /// <summary>Filling rate is calculated from grain number, a maximum mass to be filled and the duration of the filling process.</summary>
    [Serializable]
    public class FillingRateFunction : Model, IFunction
    {
        /// <summary>The partition fraction</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction FillingDuration = null;

        /// <summary>The filling rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("grains/m2")]
        IFunction NumberFunction = null;

        /// <summary>Thermal time</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction ThermalTime = null;

        /// <summary>The maximum weight or maximum amount of N incremented for individual grains in a given phase</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/kernal")]
        IFunction PotentialSizeIncrement = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return (PotentialSizeIncrement.Value(arrayIndex) / FillingDuration.Value(arrayIndex)) * ThermalTime.Value(arrayIndex) * NumberFunction.Value(arrayIndex);
        }
    }
}