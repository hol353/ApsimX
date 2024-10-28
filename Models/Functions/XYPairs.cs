using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Utilities;
using Newtonsoft.Json;

namespace Models.Functions
{
    /// <summary>
    /// This function is calculated from an XY matrix which returns a value for Y 
    /// interpolated from the Xvalue provided.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.XYPairsView")]
    [PresenterName("UserInterface.Presenters.XYPairsPresenter")]
    [Description("Returns the corresponding Y value for a given X value, based on the line shape defined by the specified XY matrix.")]
    public class XYPairs : Model, IFunction, IIndexedFunction
    {
        /// <summary>Gets or sets the x.</summary>
        [Description("X")]
        [Display]
        public double[] X { get; set; }

        /// <summary>Gets or sets the y.</summary>
        [Description("Y")]
        [Display]
        public double[] Y { get; set; }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Cannot call Value on XYPairs function. Must be indexed.</exception>
        public double Value(int arrayIndex = -1)
        {
            throw new Exception("Cannot call Value on XYPairs function. Must be indexed.");
        }

        /// <summary>Values the indexed.</summary>
        /// <param name="dX">The d x.</param>
        /// <returns></returns>
        public double ValueIndexed(double dX)
        {
            bool DidInterpolate = false;
            return MathUtilities.LinearInterpReal(dX, X, Y, out DidInterpolate);
        }
    }
}
