using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;

namespace Models.Functions
{
    /// <summary>Value returned is determined according to given criteria</summary>
    [Serializable]
    [Description("Tests if value of the first child is less than value of second child. Returns third child if true and forth if false")]
    public class LessThanFunction : Model, IFunction
    {
        /// <summary>The child functions</summary>
        private List<IFunction> ChildFunctions;
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            ChildFunctions = FindAllChildren<IFunction>().ToList();

            double TestVariable = 0.0;
            double LessThanCriteria = 0.0;
            double IfTrue = 0.0;
            double IfFalse = 0.0;

            IFunction F = null;

            for (int i = 0; i < ChildFunctions.Count; i++)
            {
                F = ChildFunctions[i] as IFunction;
                if (i == 0)
                    TestVariable = F.Value(arrayIndex);
                if (i == 1)
                    LessThanCriteria = F.Value(arrayIndex);
                if (i == 2)
                    IfTrue = F.Value(arrayIndex);
                if (i == 3)
                    IfFalse = F.Value(arrayIndex);
            }

            if (TestVariable < LessThanCriteria)
                return IfTrue;
            else
                return IfFalse;
        }
    }
}
