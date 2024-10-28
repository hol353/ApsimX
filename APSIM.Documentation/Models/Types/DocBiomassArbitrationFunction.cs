using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocBiomassArbitrationFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocBiomassArbitrationFunction" /> class.
        /// </summary>
        public DocBiomassArbitrationFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);
            
            Organ parentOrgan = FindParentOrgan(model.Parent);

            // add a description of the equation for this function
            section.Add(new Paragraph("<i>" + model.Name + " = [" + parentOrgan.Name + "].maximumNconc × (["
                + parentOrgan.Name + "].Live.Wt + potentialAllocationWt) - [" + parentOrgan.Name + "].Live.N</i>"));
            section.Add(new Paragraph("The demand for storage N is further reduced by a factor specified by the ["
                + parentOrgan.Name + "].NitrogenDemandSwitch."));

            return new List<ITag>() {section};
        }

        private Organ FindParentOrgan(IModel model)
        {
            if (model is Organ) 
                return model as Organ;

            if (model is IPlant)
                throw new Exception(model.Name + "cannot find parent organ to get Structural and Storage DM status");

            return FindParentOrgan(model.Parent);
        }
    }
}
