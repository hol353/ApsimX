using System;
using Models.PMF;

namespace Models.Core
{
    /// <summary>
    /// An alias model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Cultivar))]
    public class Alias : Model {}

}
