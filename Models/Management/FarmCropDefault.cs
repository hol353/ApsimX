using System;
using Models.Core;

namespace Models.Management
{
    /// <summary>
    /// A crop cost / price 
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Manager))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class FarmCropMgtInfo : Model
    {
        /// <summary>.</summary>
        [Description("The real (PMF) crop that this alias represents")]
        public string RealName { get; set; }
        /// <summary>.</summary>
        [Description("Rainfall required for sowing (mm)")]
        public double MinRain{ get; set; }
        /// <summary>.</summary>
        [Description("Duration of rainfall accumulation (d)")]
        public int RainDays { get; set; }
        /// <summary>.</summary>
        [Description("Start of sowing window (d-mmm)")]
        public string StartDate{ get; set; }
        /// <summary>.</summary>
        [Description("End of sowing window (d-mmm)")]
         public string EndDate{ get; set; }
        /// <summary>.</summary>
        [Description("Minimum extractable soil water (mm)")]
         public double MinESW{ get; set; }
        /// <summary>.</summary>
        [Description("Must sow at end of window")]
        public bool MustSow{ get; set; }
        /// <summary>.</summary>
        [Description("Established plant population (/m2)")]
        public double Population{ get; set; }
        /// <summary>.</summary>
        [Description("Cultivar to be sown")]
        public string CultivarName{ get; set; }
        /// <summary>.</summary>
        [Description("Depth of sowing (mm)")]
        public double SowingDepth{ get; set; }
        /// <summary>.</summary>
        [Description("Row spacing (mm)")]
        public double RowSpacing{ get; set; }
        /// <summary>.</summary>
        [Description("Maximum area of farm to plant to this crop (0-1, fraction)")]
        public double maxArea{ get; set; }
        /// <summary>.</summary>
        [Description("Fertiliser type to apply")]
        public string fertType{ get; set; }
        /// <summary>.</summary>
        [Description("Fertiliser amount to apply")]
        public double fertAmount{ get; set; }
    }
}