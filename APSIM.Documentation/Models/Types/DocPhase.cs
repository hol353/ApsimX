using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
using Models.PMF.Phen;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for phase models.
    /// Reduces need for repeated functionality for documentation by phase models.
    /// </summary>
    public class DocPhase : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocPhase" /> class.
        /// </summary>
        public DocPhase(IModel model) : base(model) { }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            // Removes the summary tag in each is phase is different.
            List<ITag> sectionChildren = section.Children.ToList();
            sectionChildren.RemoveAt(0);
            Section newSection = new(section.Title, sectionChildren);
            section.Children.RemoveAt(0);
            section.Add(newSection);

            List<ITag> subTags = new List<ITag>();

            string text = GetPhaseText(model as IPhase);
            if (text.Length > 0)
                section.Add(new Paragraph(text));

            return new List<ITag>() {section};
        }

        /// <summary>
        /// Gets the text for a specific type of Phase model.
        /// </summary>
        public static string GetPhaseText(IPhase phase)
        {
            if (phase is StartPhase startPhase)
            {
                return $"This phase goes from {startPhase.Start.ToLower()} to {startPhase.End.ToLower()}." +
                    $"It has no length but sets plant status to emerged once progressed.";
            }
            else if (phase is PhotoperiodPhase photoperiodPhase)
            {
                return $"This phase goes from {photoperiodPhase.Start} to {photoperiodPhase.End}. " +
                    $"The phase ends when photoperiod has a reaches a critical photoperiod with a given direction (Increasing/Decreasing). " +
                    $"The base model uses a critical photoperiod of {photoperiodPhase.CricialPhotoperiod} hours ({photoperiodPhase.PPDirection}).";
            }
            else if (phase is VernalisationPhase vernalisationPhase)
            {
                return $"The {phase.Name} phase goes from the {vernalisationPhase.Start} stage to the {vernalisationPhase.End}" +
                $" stage and reaches {vernalisationPhase.End} when vernalisation saturation occurs.";
            }
            else if (phase is NodeNumberPhase nodeNumberPhase)
            {
                return $"This phase goes from {nodeNumberPhase.Start.ToLower()} to {nodeNumberPhase.End.ToLower()} and extends from the end of the previous phase until the *CompletionNodeNumber* is achieved." +
                    $"The duration of this phase is determined by leaf appearance rate and the *CompletionNodeNumber* target";
            }
            else if (phase is LeafDeathPhase leafDeafPhase)
            {
                return $"The *{leafDeafPhase.Name}* phase goes from the *{leafDeafPhase.Start}* stage to the *{leafDeafPhase.End}* stage, which occurs when all leaves have fully senesced.";
            }
            else if (phase is LeafAppearancePhase leafAppearancePhase)
            {
                return $"This phase goes from {leafAppearancePhase.Start.ToLower()} to {leafAppearancePhase.End.ToLower()} and it continues until the final main-stem leaf has finished expansion." +
                    $"The duration of this phase is determined by leaf appearance rate (Structure.Phyllochron) and the number of leaves produced on the mainstem (Structure.FinalLeafNumber)";
            }
            else if (phase is GrazeAndRewind grazeAndRewind)
            {
                return $"When the {grazeAndRewind.Start} phase is reached, phenology is rewound to the {grazeAndRewind.PhaseNameToGoto} phase.";
            }
            else if (phase is GotoPhase gotoPhase)
            {
                return $"When the {gotoPhase.Start} phase is reached, phenology is rewound to the {gotoPhase.PhaseNameToGoto} phase.";
            }
            else if (phase is GerminatingPhase germinatingPhase)
            {
                return $"The phase goes from {germinatingPhase.Start.ToLower()} to {germinatingPhase.End.ToLower()} and assumes {germinatingPhase.End.ToLower()} will be reached on the day after " +
                    $"sowing or the first day thereafter when the extractable soil water at sowing depth is greater than zero.";
            }
            else if (phase is EndPhase)
            {
                return "It is the end phase in phenology and the crop will sit, unchanging, in this phase until it is harvested or removed by other method";
            }
            else if (phase is EmergingPhase emergingPhase)
            {
                return $"This phase goes from {emergingPhase.Start.ToLower()} to {emergingPhase.End.ToLower()} and simulates time to {emergingPhase.End.ToLower()} as a function of sowing depth. The *ThermalTime Target* for ending this phase is given by:\n\n" +
                    "*Target* = *SowingDepth* x *ShootRate* + *ShootLag*\n\n" +
                    "Where:\n\n" +
                    $"*SowingDepth* (mm) is sent from the manager with the sowing event.\n\n" +
                    $"Progress toward emergence is driven by thermal time accumulation, where thermal time is calculated using model: {emergingPhase.FindChild<VariableReference>().Name}";
            }
            // TODO: Needs work. See if Function documentation can be used here instead.
            else if (phase is GenericPhase)
            {
                var targetChild = phase.FindChild("Target");
                var progressionChild = phase.FindChild<VariableReference>("Progression");
                if (targetChild != null && progressionChild != null)
                {
                    string units = "";
                    if (targetChild is AddFunction)
                    {
                        List<string> targetChildrenNames = new();
                        foreach (IFunction function in targetChild.FindAllChildren<IFunction>())
                            targetChildrenNames.Add(function.Name);
                        return $"This phase goes from {phase.Start.ToLower()} to {phase.End.ToLower()}.\n\n" +
                            $"The *Target* for completion is calculated as the :\n\n" +
                            $"{string.Join(" + ", targetChildrenNames)}\n\n" +
                            $"*Progression* through phase is calculated daily and accumulated until the *Target* is reached.\n\n" +
                            $"Variable Reference: {(progressionChild as VariableReference).VariableName}";
                    }
                    else
                    {
                        units = (targetChild as Constant).Units ?? "";
                        return $"This phase goes from {phase.Start.ToLower()} to {phase.End.ToLower()}.\n\n" +
                        $"The *Target* for completion is calculated as:\n\n" +
                        $"{(targetChild as Constant).Value()} {units}\n\n" +
                        $"*Progression* through phase is calculated daily and accumulated until the *Target* is reached.\n\n" +
                        $"Variable Reference: {(progressionChild as VariableReference).VariableName}";
                    }
                }
                else return "";
            }
            else 
            {
                return $"The {phase.Name} does not specify stages.";
            }
        }
    }
}
