using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;
using Models.PMF.Organs;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocLeafCohortParameters : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocLeafCohortParameters" /> class.
        /// </summary>
        public DocLeafCohortParameters(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
           
            Section section = GetSummaryAndRemarksSection(model);

            var leafCohortParameters = model as LeafCohortParameters;

            var laiTags = new List<ITag>();
            laiTags.Add(new Paragraph("Leaf area index is calculated as the sum of the area of each cohort of leaves. " +
                                        "The appearance of a new cohort of leaves occurs each time Structure.LeafTipsAppeared increases by one. " +
                                        "From tip appearance the area of each cohort will increase for a certian number of degree days defined by the *GrowthDuration*"));
            laiTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.GrowthDuration));

            laiTags.Add(new Paragraph("If no stress occurs the leaves will reach a Maximum area (*MaxArea*) at the end of the *GrowthDuration*. " +
                                        "The *MaxArea* is defined by: "));
            laiTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.MaxArea));
            laiTags.Add(new Paragraph("In the absence of stress the leaf will remain at *MaxArea* for a number of degree days " +
                                        "set by the *LagDuration* and then area will senesce to zero at the end of the *SenescenceDuration*"));
            laiTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.LagDuration));
            laiTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.SenescenceDuration));
            laiTags.Add(new Paragraph("Mutual shading can cause premature senescence of cohorts if the leaf area above them becomes too great. Each cohort models the proportion of its area that is lost to shade induced senescence each day as:"));
            laiTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.ShadeInducedSenescenceRate));
            section.Add(new Section("Potential Leaf Area index", laiTags));

            var stressTags = new List<ITag>();
            stressTags.Add(new Paragraph("Stress reduces leaf area in a number of ways. Firstly, stress occuring prior to the appearance of the cohort can reduce cell division, so reducing the maximum leaf size. Leaf captures this by multiplying the *MaxSize* of each cohort by a *CellDivisionStress* factor which is calculated as:"));
            stressTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.CellDivisionStress));
            stressTags.Add(new Paragraph("Leaf.FN quantifys the N stress status of the plant and represents the concentration of metabolic N relative the maximum potentil metabolic N content of the leaf calculated as (*Leaf.NConc - MinimumNConc*)/(*CriticalNConc - MinimumNConc*)."));
            stressTags.Add(new Paragraph("Leaf.FW quantifies water stress and is calculated as *Leaf.Transpiration*/*Leaf.WaterDemand*, where *Leaf.Transpiration* is the minimum of *Leaf.WaterDemand* and *Root.WaterUptake*"));
            stressTags.Add(new Paragraph("Stress during the <i>GrowthDuration* of the cohort reduces the size increase of the cohort by multiplying the potential increase by a *ExpansionStress* factor:"));
            stressTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.ExpansionStress));
            stressTags.Add(new Paragraph("Stresses can also acellerate the onset and rate of senescence in a number of ways. Nitrogen shortage will cause N to be retranslocated out of lower order leaves to support the expansion of higher order leaves and other organs When this happens the lower order cohorts will have their area reduced in proportion to the amount of N that is remobilised out of them."));
            stressTags.Add(new Paragraph("Water stress hastens senescence by increasing the rate of thermal time accumulation in the lag and senescence phases. This is done by multiplying thermal time accumulation by *DroughtInducedLagAcceleration* and *DroughtInducedSenescenceAcceleration* factors, respectively"));
            section.Add(new Section("Stress effects on Leaf Area Index", stressTags));

            var dmDemandTags = new List<ITag>();
            dmDemandTags.Add(new Paragraph("Leaf calculates the DM demand from each cohort as a function of the potential size increment (DeltaPotentialArea) an specific leaf area bounds. " +
                                            "Under non stressed conditions the demand for non-storage DM is calculated as *DeltaPotentialArea* divided by the mean of *SpecificLeafAreaMax* and *SpecificLeafAreaMin*. " +
                                            "Under stressed conditions it is calculated as *DeltaWaterConstrainedArea* divided by *SpecificLeafAreaMin*."));
            dmDemandTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.SpecificLeafAreaMax));
            dmDemandTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.SpecificLeafAreaMin));
            dmDemandTags.Add(new Paragraph("Non-storage DM Demand is then seperated into structural and metabolic DM demands using the *StructuralFraction*:"));
            dmDemandTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.StructuralFraction));
            dmDemandTags.Add(new Paragraph("The storage DM demand is calculated from the sum of metabolic and structural DM (including todays demands) multiplied by a *NonStructuralFraction*"));
            section.Add(new Section("Dry matter Demand", dmDemandTags));

            var nDemandTags = new List<ITag>();
            nDemandTags.Add(new Paragraph("Leaf calculates the N demand from each cohort as a function of the potential DM increment and N concentration bounds."));
            nDemandTags.Add(new Paragraph("Structural N demand = *PotentialStructuralDMAllocation* * *MinimumNConc* where:"));
            nDemandTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.MinimumNConc));
            nDemandTags.Add(new Paragraph("Metabolic N demand is calculated as *PotentialMetabolicDMAllocation* * (*CriticalNConc* - *MinimumNConc*) where:"));
            nDemandTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.CriticalNConc));
            nDemandTags.Add(new Paragraph("Storage N demand is calculated as the sum of metabolic and structural wt (including todays demands) multiplied by *LuxaryNconc* (*MaximumNConc* - *CriticalNConc*) less the amount of storage N already present.  *MaximumNConc* is given by:"));
            nDemandTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.MaximumNConc));
            section.Add(new Section("Nitrogen Demand", nDemandTags));

            var dmSupplyTags = new List<ITag>();
            dmSupplyTags.Add(new Paragraph("In additon to photosynthesis, the leaf can also supply DM by reallocation of senescing DM and retranslocation of storgage DM:" +
                                            "Reallocation supply is a proportion of the metabolic and non-structural DM that would be senesced each day where the proportion is set by:"));
            dmSupplyTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.DMReallocationFactor));
            dmSupplyTags.Add(new Paragraph("Retranslocation supply is calculated as a proportion of the amount of storage DM in each cohort where the proportion is set by :"));
            dmSupplyTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.DMRetranslocationFactor));
            section.Add(new Section("Drymatter supply", dmSupplyTags));

            var nSupplyTags = new List<ITag>();
            nSupplyTags.Add(new Paragraph("Nitrogen supply from the leaf comes from the reallocation of metabolic and storage N in senescing material " +
                                            "and the retranslocation of metabolic and storage N.  Reallocation supply is a proportion of the Metabolic and Storage DM that would be senesced each day where the proportion is set by:"));
            nSupplyTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.NReallocationFactor));
            nSupplyTags.Add(new Paragraph("Retranslocation supply is calculated as a proportion of the amount of storage and metabolic N in each cohort where the proportion is set by :"));
            nSupplyTags.AddRange(AutoDocumentation.DocumentModel(leafCohortParameters.NRetranslocationFactor));
            section.Add(new Section("Nitrogen supply", nSupplyTags));

            // Document Constants
            var constantTags = new List<ITag>();
            foreach (var constant in leafCohortParameters.FindAllChildren<Constant>())
                constantTags.AddRange(AutoDocumentation.DocumentModel(constant));

            section.Add(new Section("Constants", constantTags));

            return new List<ITag>() {section};
        }
    }
}
