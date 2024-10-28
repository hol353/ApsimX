using System;
using Models.Core;

// TODO
// work rates: each tractor should display a grid of work rates & fuel consumption for each implement combo
// capital costs?

namespace Models.Management
{
    /// <summary>
    /// A piece of machinery managed by the machinery component
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(FarmMachinery))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class FarmMachineryItem : Model
    {
        /// <summary>Gets the type.</summary>
        [Description("The type of machine")]
        public MachineryType MachineryType { get; set; }
        /// <summary></summary>
        [Description("Maximum daily hours of operation (h)")]
        public double MaxHours {get; set;}

         /// <summary></summary>
        [Description("New Price ($)")]
        public double NewPrice { get; set; }

        /// <summary></summary>
        [Description("Trade In Value (% of new)")]
        public double TradeInValue { get; set; }

        /// <summary></summary>
        [Description("Life of Equipment (hrs)")]
        public double LifeOfEquipment { get; set; }

        /// <summary></summary>
        [Display(VisibleCallback = "isTractor")]
        [Description("Oil & Tyre costs (%age of fuel costs)")]
        public double OilTyreCost { get; set; }

        /// <summary></summary>
        [Description("Repairs & Maintenance (% of new value over lifetime)")]
        public double RepairsMaintenance { get; set; }

        /// <summary></summary>
        public bool isTractor() {return(this.MachineryType == MachineryType.Tractor);}
    }
    /// <summary>The type of machinery this is</summary>
    public enum MachineryType {
        /// <summary> A Tractor </summary>
        Tractor, 
        /// <summary> An implement </summary>
        Implement
    }
}