using System;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;

namespace Models.PMF
{
    /// <summary>
    /// This class holds the functions for calculating the absolute demands for each biomass fraction. 
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class BiomassDemand : Model
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

    }
}
