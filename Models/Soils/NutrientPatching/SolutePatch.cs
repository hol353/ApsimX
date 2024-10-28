using System;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Soils.NutrientPatching
{

    /// <summary>
    /// This class used for this nutrient encapsulates the nitrogen within a mineral
    /// N pool. Child functions provide information on flows of N from it to other
    /// mineral N pools, or losses from the system.
    /// </summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class SolutePatch : Solute
    {
        private Soil soil;
        private NutrientPatchManager patchManager;

        /// <summary>Solute amount (kg/ha)</summary>
        public override double[] kgha
        {
            get
            {
                return patchManager.GetSoluteKgha(Name);
            }
            set
            {
                patchManager?.SetSoluteKgha(SoluteSetterType.Other, Name, value);
            }
        }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Reset();
            AmountLostInRunoff = new double[Thickness.Length];
        }

        /// <summary>
        /// Set solute to initialisation state
        /// </summary>
        public override void Reset()
        {
            var solute = Soil.FindChild<Solute>(Name);
            if (solute == null)
                throw new Exception($"Cannot find solute {Name}");
            double[] initialkgha = solute.InitialValues;
            if (initialkgha == null)
                SetKgHa(SoluteSetterType.Other, new double[Physical.Thickness.Length]);  // Urea will fall to here.
            else
                SetKgHa(SoluteSetterType.Other, ReflectionUtilities.Clone(initialkgha) as double[]);
        }
        /// <summary>Setter for kgha.</summary>
        /// <param name="callingModelType">Type of calling model.</param>
        /// <param name="value">New values.</param>
        public override void SetKgHa(SoluteSetterType callingModelType, double[] value)
        {
            PatchManager.SetSoluteKgha(callingModelType, Name, value);
        }

        /// <summary>Setter for kgha delta.</summary>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="delta">New delta values</param>
        public override void AddKgHaDelta(SoluteSetterType callingModelType, double[] delta)
        {
            var values = kgha;
            for (int i = 0; i < delta.Length; i++)
                kgha[i] += delta[i];
            SetKgHa(callingModelType, values);
        }

        /// <summary>The soil physical node.</summary>
        private Soil Soil
        {
            get
            {
                if (soil == null)
                    soil = FindInScope<Soil>();
                return soil;
            }
        }

        /// <summary>The PatchManager node.</summary>
        private NutrientPatchManager PatchManager
        {
            get
            {
                if (patchManager == null)
                    patchManager = FindInScope<NutrientPatchManager>();
                return patchManager;
            }
        }

    }
}
