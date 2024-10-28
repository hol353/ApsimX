using System;
using Models.Core;
using Models.Functions;

namespace Models.PMF
{

    /// <summary>
    /// This class holds the functions for calculating the absolute demands and priorities for each biomass fraction. 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OrganNutrientDelta))]
    public class NutrientDemandFunctions : Model
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

        /// <summary>Factor for Structural biomass priority</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction QStructuralPriority = null;

        /// <summary>Factor for Metabolic biomass priority</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction QMetabolicPriority = null;

        /// <summary>Factor for Storage biomass priority</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        public IFunction QStoragePriority = null;

        /// <summary> The constructor</summary>
        public NutrientDemandFunctions() { }

    }


}
