using System;
using Models.Core;
using Models.Functions;

namespace Models.PMF
{

    /// <summary>
    /// This class holds the functions for calculating the Nutrient supplies from the organ. 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OrganNutrientDelta))]
    public class NutrientSupplyFunctions : Model
    {
        /// <summary>The supply from reallocaiton from senesed material</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public NutrientPoolFunctions ReAllocation = null;

        /// <summary>The supply from uptake</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Uptake = null;

        /// <summary>The supply from fixation.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Fixation = null;

        /// <summary>The supply from retranslocation of storage</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public NutrientPoolFunctions ReTranslocation = null;

        /// <summary> The constructor</summary>
        public NutrientSupplyFunctions() { }

    }
}
