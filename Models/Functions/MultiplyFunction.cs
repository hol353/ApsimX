using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;

namespace Models.Functions
{
    /// <summary>A class that returns the product of its child functions.</summary>
    [Serializable]
    public class MultiplyFunction : Model, IFunction//
    {
        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            double returnValue = 1.0;

            foreach (IFunction F in ChildFunctions)
                returnValue = returnValue * F.Value(arrayIndex);
            return returnValue;
        }
    }
}