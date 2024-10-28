using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.PMF.Interfaces;
using Models.PMF.Phen;

namespace Models.PMF
{
    /// <summary>This is a composite biomass class, representing the sum of 1 or more biomass objects from one or more organs.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class CompositeBiomass : Model, IBiomass
    {
        private List<IOrganDamage> organs = null;

        [Link]
        private ISummary summary = null;

        /// <summary>List of organs to aggregate.</summary>
        [Description("List of organs to aggregate.")]
        public string[] OrganNames { get; set; }

        /// <summary>Include live material?</summary>
        [Description("Include live material?")]
        public bool IncludeLive { get; set; }

        /// <summary>Include dead material?</summary>
        [Description("Include dead material?")]
        public bool IncludeDead { get; set; }

        /// <summary>Clear ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            organs = new List<IOrganDamage>();
            var parentPlant = this.FindAncestor<Plant>();
            if (parentPlant == null)
                throw new Exception("CompositeBiomass can only be dropped on a plant.");
            foreach (var organName in OrganNames)
            {
                var organ = parentPlant.Children.FirstOrDefault(o => o.Name == organName);
                if (organ == null && !(organ is IOrganDamage))
                    throw new Exception($"In {Name}, cannot find a plant organ called {organName}");
                organs.Add(organ as IOrganDamage);
            }
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (Name == "AboveGround")
                summary.WriteMessage(this, $"{Name} = {Wt:f2} (g/m^2)", MessageType.Diagnostic);
        }

        /// <summary>Gets the mass.</summary>
        [Units("g/m^2")]
        public double Wt
        {
            get
            {
                double wt = 0;
                if (organs != null)
                    foreach (var organ in organs)
                    {
                        if (IncludeLive)
                            wt += organ.Live.Wt;
                        if (IncludeDead)
                            wt += organ.Dead.Wt;
                    }

                return wt;
            }
        }

        /// <summary>Gets the nitrogen content.</summary>
        [Units("g/m^2")]
        public double N
        {
            get
            {
                double n = 0;
                if (organs != null)
                    foreach (var organ in organs)
                    {
                        if (IncludeLive)
                            n += organ.Live.N;
                        if (IncludeDead)
                            n += organ.Dead.N;
                    }

                return n;
            }
        }

        /// <summary>Gets the nitrogen concentration.</summary>
        [Units("g/g")]
        public double NConc
        {
            get
            {
                if (Wt > 0)
                    return N / Wt;
                else
                    return 0.0;
            }
        }


        /// <summary>Gets the structural mass.</summary>
        [Units("g/m^2")]
        public double StructuralWt
        {
            get
            {
                double wt = 0;
                if (organs != null)
                    foreach (var organ in organs)
                    {
                        if (IncludeLive)
                            wt += organ.Live.StructuralWt;
                        if (IncludeDead)
                            wt += organ.Dead.StructuralWt;
                    }

                return wt;
            }
        }

        /// <summary>Gets the structural nitrogen content.</summary>
        [Units("g/m^2")]
        public double StructuralN
        {
            get
            {
                double n = 0;
                if (organs != null)
                    foreach (var organ in organs)
                    {
                        if (IncludeLive)
                            n += organ.Live.StructuralN;
                        if (IncludeDead)
                            n += organ.Dead.StructuralN;
                    }

                return n;
            }
        }


        /// <summary>Gets the storage mass.</summary>
        [Units("g/m^2")]
        public double StorageWt
        {
            get
            {
                double wt = 0;
                if (organs != null)
                    foreach (var organ in organs)
                    {
                        if (IncludeLive)
                            wt += organ.Live.StorageWt;
                        if (IncludeDead)
                            wt += organ.Dead.StorageWt;
                    }

                return wt;
            }
        }

        /// <summary>Gets the storage nitrogen content.</summary>
        [Units("g/m^2")]
        public double StorageN
        {
            get
            {
                double n = 0;
                if (organs != null)
                    foreach (var organ in organs)
                    {
                        if (IncludeLive)
                            n += organ.Live.StorageN;
                        if (IncludeDead)
                            n += organ.Dead.StorageN;
                    }

                return n;
            }
        }
    }
}