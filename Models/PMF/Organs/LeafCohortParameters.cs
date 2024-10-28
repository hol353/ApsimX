using System;
using Models.Core;
using Models.Functions;

namespace Models.PMF.Organs
{
    /// <summary>
    /// Leaf cohort parameters.
    /// </summary>
    [Serializable]
    public class LeafCohortParameters : Model
    {
        /// <summary>The maximum area</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("mm2")]
        public IFunction MaxArea = null;
        /// <summary>The growth duration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("deg day")]
        public IFunction GrowthDuration = null;
        /// <summary>The lag duration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("deg day")]
        public IFunction LagDuration = null;
        /// <summary>The senescence duration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("deg day")]
        public IFunction SenescenceDuration = null;
        /// <summary>The detachment lag duration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("deg day")]
        public IFunction DetachmentLagDuration = null;
        /// <summary>The detachment duration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("deg day")]
        public IFunction DetachmentDuration = null;
        /// <summary>The specific leaf area maximum</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction SpecificLeafAreaMax = null;
        /// <summary>The specific leaf area minimum</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction SpecificLeafAreaMin = null;
        /// <summary>The structural fraction</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction StructuralFraction = null;
        /// <summary>The maximum n conc</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction MaximumNConc = null;
        /// <summary>The minimum n conc</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction MinimumNConc = null;
        /// <summary>The initial n conc</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction InitialNConc = null;
        /// <summary>The n reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction NReallocationFactor = null;
        /// <summary>The dm reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction DMReallocationFactor = null;
        /// <summary>The n retranslocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction NRetranslocationFactor = null;
        /// <summary>The expansion stress</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction ExpansionStress = null;

        /// <summary>The expansion stress</summary>
        public double ExpansionStressValue { get; set; }
        /// <summary>The CellDivisionStressValue</summary>
        public double CellDivisionStressValue { get; set; }
        /// <summary>The LagAccelerationValue</summary>
        public double LagAccelerationValue { get; set; }
        /// <summary>The SenescenceAccelerationValue</summary>
        public double SenescenceAccelerationValue { get; set; }
        /// <summary>The ShadeInducedSenescenceRateValue</summary>
        public double ShadeInducedSenescenceRateValue { get; set; }
        /// <summary>The SenessingLeafRelativeSizeValue</summary>
        public double SenessingLeafRelativeSizeValue { get; set; }


        /// <summary>The critical n conc</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction CriticalNConc = null;
        /// <summary>The dm retranslocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction DMRetranslocationFactor = null;
        /// <summary>The shade induced senescence rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction ShadeInducedSenescenceRate = null;
        /// <summary>The stress induced reduction of lag phase through acceleration of tt accumulation by the cohort during this phase</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction LagAcceleration = null;
        /// <summary>The stress induced reduction of senescence phase through acceleration of tt accumulation by the cohort during this phase</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction SenescenceAcceleration = null;
        /// <summary>The non structural fraction</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction StorageFraction = null;
        /// <summary>The cell division stress</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction CellDivisionStress = null;
        /// <summary>The Shape of the sigmoidal function of leaf area increase</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction LeafSizeShapeParameter = null;
        /// <summary>The size of leaves on senessing tillers relative to the dominant tillers in that cohort</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction SenessingLeafRelativeSize = null;
        /// <summary>The proportion of mass that is respired each day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction MaintenanceRespirationFunction = null;
        /// <summary>Modify leaf size by age</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction LeafSizeAgeMultiplier = null;
        /// <summary>Modify lag duration by age</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction LagDurationAgeMultiplier = null;
        /// <summary>Modify senescence duration by age</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction SenescenceDurationAgeMultiplier = null;
        /// <summary>The cost for remobilisation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction RemobilisationCost = null;

    }
}