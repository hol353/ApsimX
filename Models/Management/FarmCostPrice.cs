using System;
using Models.Core;

namespace Models.Management
{
    /// <summary>
    /// A crop cost / price 
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(FarmEconomics))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class CostPriceInfo : Model
    {
        /// <summary>Gets the value.</summary>
        [Description("The variable costs of growing this crop ($/ha)")]
        public double VariableCost { get; set; }

        /// <summary>Gets the optional units</summary>
        [Description("The price when selling this crop (wet, $/t)")]
        public double Price { get; set; }
    }

    /// <summary>
    /// A cost - eg Urea/NO3
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(FarmEconomics))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class CostInfo : Model
    {
        /// <summary>Gets the value.</summary>
        [Description("The cost of this item ($/kg)")]
        public double Cost { get; set; }
    }

}