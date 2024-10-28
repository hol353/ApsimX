using System;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;

namespace Models.PMF
{

    /// <summary>
    /// This class holds the functions for calculating values for each Nutrient component. 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OrganNutrientDelta))]
    [ValidParent(ParentType = typeof(NutrientSupplyFunctions))]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class NutrientPoolFunctions : Model
    {
        /// <summary>The demand for the structural fraction.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Structural = null;

        /// <summary>The demand for the metabolic fraction.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Metabolic = null;

        /// <summary>The demand for the storage fraction.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction Storage = null;

        /// <summary> The constructor</summary>
        public NutrientPoolFunctions() { }

    }
}
