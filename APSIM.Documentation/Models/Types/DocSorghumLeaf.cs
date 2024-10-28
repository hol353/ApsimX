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
    public class DocSorghumLeaf : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocSorghumLeaf" /> class.
        /// </summary>
        public DocSorghumLeaf(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            List<ITag> subTags = new()
            {
                new Section(
                "LeafCulms",
                new Paragraph("The LeafCulms model manages the canopy resources produced by tillering. Two main tillering strategies are provided by default," +
                " and are managed via the TilleringMethod switch defined in SorghumLeaf, which can be manipulated via script methods.\n\n" +
                "FixedTillering will use the FTN property provided as part of the sowing method to determine the total number of fertile tillers.\n\n" +
                "Setting FTN to a negative value will calculate the number of fixed tillers using latitude and sowing density to provide a rule of thumb value.\n\n" +
                "These values have been derived using data from the Australian sorghum growing area, and may not be suitable for other locations.\n\n" +
                "DynamicTillering will calculate the potential number of tillers - usually determined by the time the 6th leaf has appeared. \n\n" +
                "The number of fertile tillers is then maintained by the addition or removal of active tillers. Further information provided below for each method.")),
                new Section(
                "Dry Matter Fixation",
                new Paragraph(
                    "Aboveground biomass accumulation is simulated as the minimum of light-limited or water-limited growth. In the absence of water limitation, biomass accumulation is the product of the amount of intercepted radiation (IR) and its conversion efficiency, the radiation use efficiency (RUE).\n\n" +
                    "Under water limitation, aboveground biomass accumulation is the product of realized transpiration and its conversion efficiency, biomass produced per unit of water transpired, or transpiration efficiency(TE)\n\n"
                )
            ),

                // TODO: not sure why this section throws out the whole formatting of the document, will investigate later on.
                // Section rueSection = new("Radiation Use Efficiency", new List<ITag>());
                // (rueSection.Children as List<ITag>).Add(new Section("Extinction Coefficient Function", AutoDocumentation.DocumentModel((model as SorghumLeaf).extinctionCoefficientFunction)));
                // (rueSection.Children as List<ITag>).Add(new Section("Photosynthesis", AutoDocumentation.DocumentModel((model as SorghumLeaf).photosynthesis)));
                // subTags.Add(rueSection);

                // TODO: This also causes formatting problems and needs correcting.
                // subTags.AddRange(AutoDocumentation.DocumentModel((model as SorghumLeaf).potentialBiomassTEFunction));

                new Section("Initial Dry Matter Mass", new Paragraph($"Initial DM mass = {(model as SorghumLeaf).InitialDMWeight} gm^-2^")),
                new Section("Nitrogen Demand", new Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool.")),
                new Section("DM Retranslocation Factor", new Paragraph($"{model.Name} does not retranslocate non-structural DM.")),
                new Section("Nitrogen Supply", new Paragraph($"{model.Name} does not reallocate N when senescence of the organ occurs.")),
                new Section("Nitrogen Retranslocation Factor", new Paragraph($"{model.Name} does not retranslocate non-structural N."))
                //TODO: LAI section
                //TODO: Senescence and detachment
            };

            section.Add(subTags);

            return new List<ITag>() {section};
        }
    }
}
