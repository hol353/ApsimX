using Models.Core;
using System;

namespace Models.Functions
{
    /// <summary>
    /// A model to give a general pattern of KL decline over depth.  Assumes values of 1 with depths less than 300 mm and then 
    /// decreases exponentlly to zero at the crops maximum root depth
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("A model to give a general pattern of KL decline over depth.  Assumes values of 1 with depths less than 300 mm and then decreases exponentlly to zero at the crops maximum root depth")]
    public class KLModiferVsDepthFunction : Model, IFunction
    {
        /// <summary>The x value to use for interpolation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction LayerDepth = null;

        /// <summary>The x value to use for interpolation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction MaximumRootDepth = null;

        /// <summary>Constructor</summary>
        public KLModiferVsDepthFunction() { }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Cannot find value for  + Name +  XProperty:  + XProperty</exception>
        public double Value(int arrayIndex = -1)
        {
            double depth = LayerDepth.Value(arrayIndex);
            double maxRootDepth = MaximumRootDepth.Value();
            if (depth <= 300)
                return 1.0;
            else if (depth < maxRootDepth)
                return Math.Exp(-0.0015*(1500/(maxRootDepth - 300)*(depth-300)));
            else
                return 0;
        }
    }
}
