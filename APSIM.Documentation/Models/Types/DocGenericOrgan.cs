using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;
using Models.PMF.Library;
using Models.PMF.Organs;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocGenericOrgan : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocGenericOrgan" /> class.
        /// </summary>
        public DocGenericOrgan(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            // List the parameters, properties, and processes from this organ that need to be documented:

            // Document DM demands.
            section.Add(new Section("Dry Matter Demand", DocumentDMDemand(model.FindChild("dmDemands"))));

            // Document N demands.
            section.Add(new Section("Nitrogen Demand", DocumentNDemand(model.FindChild("nDemands"))));

            // Document N concentration thresholds.
            section.Add(new Section("N Concentration Thresholds", DocumentNConcentrationThresholds(model as GenericOrgan)));

            IModel nDemandSwitch = model.FindChild("NitrogenDemandSwitch");
            List<ITag> demandTags = new List<ITag>();
            if (nDemandSwitch is Constant nDemandConst)
            {
                if (nDemandConst.Value() != 1)
                    demandTags.Add(new Paragraph($"The demand for N is reduced by a factor of {nDemandConst.Value()} as specified by the NitrogenDemandSwitch"));
            }
            else
            {
                demandTags.Add(new Paragraph($"The demand for N is reduced by a factor specified by the NitrogenDemandSwitch."));
                demandTags.AddRange(AutoDocumentation.DocumentModel(nDemandSwitch));
            }
            section.Add(new Section("Nitrogen Demand Switch", demandTags));

            // Document DM supplies.
            section.Add(new Section("Dry Matter Supply", DocumentDMSupply(model as GenericOrgan)));

            // Document N supplies.
            section.Add(new Section("Nitrogen Supply", DocumentNSupply(model as GenericOrgan)));

            // Document senescence and detachment.
            section.Add(new Section("Senescence and Detachment", DocumentSenescence(model as GenericOrgan)));

            IModel biomassRemovalModel = model.FindChild<BiomassRemoval>();
            if (biomassRemovalModel != null)
                section.Add(AutoDocumentation.DocumentModel(biomassRemovalModel));

            return new List<ITag>() {section};
        }

        private static IEnumerable<ITag> DocumentDMDemand(IModel dmDemands)
        {
            List<ITag> subTags = new List<ITag>();
            subTags.Add(new Paragraph("The dry matter demand for the organ is calculated as defined in DMDemands, based on the DMDemandFunction and partition fractions for each biomass pool."));
            subTags.AddRange(AutoDocumentation.DocumentModel(dmDemands));
            return subTags;
        }

        private static IEnumerable<ITag> DocumentNDemand(IModel nDemands)
        {
            List<ITag> subTags = new List<ITag>();
            subTags.Add(new Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool."));
            subTags.AddRange(AutoDocumentation.DocumentModel(nDemands));
            return subTags;
        }

        private static List<ITag> DocumentNConcentrationThresholds(GenericOrgan organ)
        {
            List<ITag> subTags = new List<ITag>();
            IModel minNConc = organ.FindChild("MinimumNConc");
            if (minNConc != null)
                subTags.AddRange(AutoDocumentation.DocumentModel(minNConc));
            else
                subTags.Add(new Paragraph("MinimumNConc not found."));

            IModel critNConc = organ.FindChild("CriticalNConc");
            if (critNConc != null)
                subTags.AddRange(AutoDocumentation.DocumentModel(critNConc));
            else
                subTags.Add(new Paragraph("CritNConc not found."));

            IModel maxNConc = organ.FindChild("MaximumNConc");
            if (maxNConc != null)
                subTags.AddRange(AutoDocumentation.DocumentModel(maxNConc));
            else
                subTags.Add(new Paragraph("MaxNConc not found."));

            return subTags;
        }

        private static List<ITag> DocumentDMSupply(GenericOrgan organ)
        {
            List<ITag> subTags = new List<ITag>();
            IModel dmReallocFactor = organ.FindChild("DMReallocationFactor");
            if (dmReallocFactor is Constant dmReallocConst)
            {
                if (dmReallocConst.Value() == 0)
                    subTags.Add(new Paragraph($"{organ.Name} does not reallocate DM when senescence of the organ occurs."));
                else
                    subTags.Add(new Paragraph($"{organ.Name} will reallocate {dmReallocConst.Value() * 100}% of DM that senesces each day."));
            }
            else
            {
                subTags.Add(new Paragraph("The proportion of senescing DM that is allocated each day is quantified by the DMReallocationFactor."));
                subTags.AddRange(AutoDocumentation.DocumentModel(dmReallocFactor));
            }
            IModel dmRetransFactor = organ.FindChild("DMRetranslocationFactor");
            if (dmRetransFactor is Constant dmRetransConst)
            {
                if (dmRetransConst.Value() == 0)
                    subTags.Add(new Paragraph($"{organ.Name} does not retranslocate non-structural DM."));
                else
                    subTags.Add(new Paragraph($"{organ.Name} will retranslocate {dmRetransConst.Value() * 100}% of non-structural DM each day."));
            }
            else
            {
                subTags.Add(new Paragraph("The proportion of non-structural DM that is allocated each day is quantified by the DMReallocationFactor."));
                subTags.AddRange(AutoDocumentation.DocumentModel(dmReallocFactor));
            }
            return subTags;
        }

        private static List<ITag> DocumentNSupply(GenericOrgan organ)
        {
            List<ITag> subTags = new List<ITag>();
            IModel nReallocFactor = organ.FindChild("NReallocationFactor");
            if (nReallocFactor is Constant nReallocConst)
            {
                if (nReallocConst.Value() == 0)
                    subTags.Add(new Paragraph($"{organ.Name} does not reallocate N when senescence of the organ occurs."));
                else
                    subTags.Add(new Paragraph($"{organ.Name} can reallocate up to {nReallocConst.Value() * 100}% of N that senesces each day if required by the plant arbitrator to meet N demands."));
            }
            else
            {
                subTags.Add(new Paragraph("The proportion of senescing N that is allocated each day is quantified by the NReallocationFactor."));
                subTags.AddRange(AutoDocumentation.DocumentModel(nReallocFactor));
            }
            IModel nRetransFactor = organ.FindChild("NRetranslocationFactor");
            if (nRetransFactor is Constant nRetransConst)
            {
                if (nRetransConst.Value() == 0)
                    subTags.Add(new Paragraph($"{organ.Name} does not retranslocate non-structural N."));
                else
                    subTags.Add(new Paragraph($"{organ.Name} can retranslocate up to {nRetransConst.Value() * 100}% of non-structural N each day if required by the plant arbitrator to meet N demands."));
            }
            else
            {
                subTags.Add(new Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor."));
                subTags.AddRange(AutoDocumentation.DocumentModel(nRetransFactor));
            }
            return subTags;
        }

        private static List<ITag> DocumentSenescence(GenericOrgan organ)
        {
            List<ITag> subTags = new List<ITag>();
            IModel senescenceRate = organ.FindChild("SenescenceRate");
            if (senescenceRate is Constant senescenceConst)
            {
                if (senescenceConst.Value() == 0)
                    subTags.Add(new Paragraph($"{organ.Name} has senescence parameterised to zero so all biomass in this organ will remain alive."));
                else
                    subTags.Add(new Paragraph($"{organ.Name} senesces {senescenceConst.Value() * 100}% of its live biomass each day, moving the corresponding amount of biomass from the live to the dead biomass pool."));
            }
            else
            {
                subTags.Add(new Paragraph("The proportion of live biomass that senesces and moves into the dead pool each day is quantified by the SenescenceRate."));
                subTags.AddRange(AutoDocumentation.DocumentModel(senescenceRate));
            }

            IModel detachmentRate = organ.FindChild("DetachmentRateFunction");
            if (detachmentRate is Constant detachmentConst)
            {
                if (detachmentConst.Value() == 0)
                    subTags.Add(new Paragraph($"{organ.Name} has detachment parameterised to zero so all biomass in this organ will remain with the plant until a defoliation or harvest event occurs."));
                else
                    subTags.Add(new Paragraph($"{organ.Name} detaches {detachmentConst.Value() * 100}% of its live biomass each day, passing it to the surface organic matter model for decomposition."));
            }
            else
            {
                subTags.Add(new Paragraph("The proportion of Biomass that detaches and is passed to the surface organic matter model for decomposition is quantified by the DetachmentRateFunction."));
                subTags.AddRange(AutoDocumentation.DocumentModel(detachmentRate));
            }
            return subTags;
        }
    }
}
