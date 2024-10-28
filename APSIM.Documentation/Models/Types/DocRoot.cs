using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;
using System.Linq;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocRoot : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocRoot" /> class.
        /// </summary>
        public DocRoot(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);
            section.Title = $"The APSIM {model.Name} Model";

            List<ITag> growthTags = new List<ITag>();
            growthTags.Add(new Paragraph("Roots grow downwards through the soil profile, with initial depth determined by sowing depth and the growth rate determined by RootFrontVelocity. The RootFrontVelocity is modified by multiplying it by the soil's XF value, which represents any resistance posed by the soil to root extension."));
            growthTags.Add(new Paragraph("_Root Depth Increase = RootFrontVelocity x XF~i~ x RootDepthStressFactor_"));
            growthTags.Add(new Paragraph("where i is the index of the soil layer at the rooting front."));
            growthTags.Add(new Paragraph("Root depth is also constrained by a maximum root depth."));
            growthTags.Add(new Paragraph("Root length growth is calculated using the daily DM partitioned to roots and a specific root length.  Root proliferation in layers is calculated using an approach similar to the generalised equimarginal criterion used in economics.  The uptake of water and N per unit root length is used to partition new root material into layers of higher 'return on investment'. For example, the Root Activity for water is calculated as"));
            growthTags.Add(new Paragraph("_RAw~i~ = -WaterUptake~i~ / LiveRootWt~i~ x LayerThickness~i~ x ProportionThroughLayer_"));
            growthTags.Add(new Paragraph("The amount of root mass partitioned to a layer is then proportional to root activity"));
            growthTags.Add(new Paragraph("_DMAllocated~i~ = TotalDMAllocated x RAw~i~ / TotalRAw_"));
            section.Add(new Section("Growth", growthTags));

            section.Add(new Section("Dry Matter Demands", new Paragraph("A daily DM demand is provided to the organ arbitrator and a DM supply returned. By default, 100% of the dry matter (DM) demanded from the root is structural. The daily loss of roots is calculated using a SenescenceRate function.  All senesced material is automatically detached and added to the soil FOM.")));

            section.Add(new Section("Nitrogen Demands", new Paragraph("The daily structural N demand from root is the product of total DM demand and the minimum N concentration.  Any N above this is considered Storage and can be used for retranslocation and/or reallocation as the respective factors are set to values other then zero.")));

            section.Add(new Section("Nitrogen Uptake", new Paragraph(
                "Potential N uptake by the root system is calculated for each soil layer (i) that the roots have extended into. " +
                "In each layer potential uptake is calculated as the product of the mineral nitrogen in the layer, a factor controlling the rate of extraction " +
                "(kNO3 or kNH4), the concentration of N form (ppm), and a soil moisture factor (NUptakeSWFactor) which typically decreases as the soil dries. " +
                "    _NO3 uptake = NO3~i~ x kNO3 x NO3~ppm, i~ x NUptakeSWFactor_" +
                "    _NH4 uptake = NH4~i~ x kNH4 x NH4~ppm, i~ x NUptakeSWFactor_" +
                "As can be seen from the above equations, the values of kNO3 and kNH4 equate to the potential fraction of each mineral N pool which can be taken up per day for wet soil when that pool has a concentration of 1 ppm." +
                "Nitrogen uptake demand is limited to the maximum daily potential uptake (MaxDailyNUptake) and the plant's N demand. The former provides a means to constrain N uptake to a maximum value observed in the field for the crop as a whole." +
                "The demand for soil N is then passed to the soil arbitrator which determines how much of the N uptake demand" +
                "each plant instance will be allowed to take up.")));

            section.Add(new Section("Water Uptake", new Paragraph(
                "Potential water uptake by the root system is calculated for each soil layer that the roots have extended into. " +
                "In each layer potential uptake is calculated as the product of the available water in the layer (water above LL limit) " +
                "and a factor controlling the rate of extraction (KL).  The values of both LL and KL are set in the soil interface and " +
                "KL may be further modified by the crop via the KLModifier function. " +
                "_SW uptake = (SW~i~ - LL~i~) x KL~i~ x KLModifier_ ")));

            // Document Constants
            var constantTags = new List<ITag>();
            foreach (var constant in model.FindAllChildren<Constant>())
                constantTags = AutoDocumentation.DocumentModel(constant);

            section.Add(new Section("Constants", constantTags));

            return new List<ITag>() {section};
        }
    }
}
