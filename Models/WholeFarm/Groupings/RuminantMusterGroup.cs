﻿using Models.Core;
using Models.WholeFarm.Activities;
using Models.WholeFarm.Groupings;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm.Groupings
{
	///<summary>
	/// Contains a group of filters to identify individual ruminants to muster
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(RuminantActivityMuster))]
	public class RuminantMusterGroup: WFActivityBase
	{
		[Link]
		Clock Clock = null;
		[Link]
		private ResourcesHolder Resources = null;
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// Name of managed pasture to muster to
		/// </summary>
		[Description("Name of managed pasture to muster to")]
		public string ManagedPastureName { get; set; }

		/// <summary>
		/// Determines whether this must be performed to setup herds at the start of the simulation
		/// </summary>
		[Description("Perform muster at start of simulation")]
		public bool PerformAtStartOfSimulation { get; set; }

		/// <summary>
		/// Month to muster in (set to 0 to not perform muster)
		/// </summary>
		[Description("Month to muster in")]
		public int Month { get; set; }

		/// <summary>
		/// Determines whether sucklings are automatically mustered with the mother or seperated
		/// </summary>
		[Description("Move sucklings with mother")]
		public bool MoveSucklings { get; set; }

		private GrazeFoodStoreType pasture { get; set; }
		private List<LabourFilterGroupSpecified> labour { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("StartOfSimulation")]
		private void OnStartOfSimulation(object sender, EventArgs e)
		{
			// This needs to happen after all manage pasture activities have been initialised on commencing
			// Therefore we use StartOfSimulation event

			// link to graze food store type pasture to muster to
			// blank is general yards.
			bool resavailable = true;

			if (ManagedPastureName != "")
			{
				pasture = Resources.GetResourceItem("GrazeFoodStore", ManagedPastureName, out resavailable) as GrazeFoodStoreType;
			}
			if (!resavailable)
			{
				Summary.WriteWarning(this, String.Format("Could not find manage pasture in graze food store named \"{0}\" for {1}", ManagedPastureName, this.Name));
				throw new Exception(String.Format("Invalid pasture name ({0}) provided for mustering activity {1}", ManagedPastureName, this.Name));
			}

			// get labour specifications
			labour = this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
			if (labour == null) labour = new List<LabourFilterGroupSpecified>();

			if (PerformAtStartOfSimulation)
			{
				Muster();
			}
		}

		private void Muster()
		{
			// get herd to muster
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd;

			if (herd == null && herd.Count == 0) return;

			// get list from filters
			foreach (Ruminant ind in herd.Filter(this))
			{
				// set new location ID
				ind.Location = pasture.Name;

				// check if sucklings are to be moved with mother
				if (MoveSucklings)
				{
					// if female
					if (ind.GetType() == typeof(RuminantFemale))
					{
						RuminantFemale female = ind as RuminantFemale;
						// check if mother with sucklings
						if (female.SucklingOffspring.Count > 0)
						{
							foreach (var suckling in female.SucklingOffspring)
							{
								suckling.Location = pasture.Name;
							}
						}
					}
				}

			}
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			ResourceRequestList = null;
			if (Clock.Today.Month == Month)
			{
				RuminantHerd ruminantHerd = Resources.RuminantHerd();
				List<Ruminant> herd = ruminantHerd.Herd.Filter(this);
				int head = herd.Count();
				double AE = herd.Sum(a => a.AdultEquivalent);

				if (head == 0) return null;

				// for each labour item specified
				foreach (var item in labour)
				{
					double daysNeeded = 0;
					switch (item.UnitType)
					{
						case LabourUnitType.Fixed:
							daysNeeded = item.LabourPerUnit;
							break;
						case LabourUnitType.perHead:
							daysNeeded = Math.Ceiling(head / item.UnitSize) * item.LabourPerUnit;
							break;
						case LabourUnitType.perAE:
							daysNeeded = Math.Ceiling(AE / item.UnitSize) * item.LabourPerUnit;
							break;
						default:
							throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", item.UnitType, item.Name, this.Name));
					}
					if(daysNeeded>0)
					{
						if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
						ResourceRequestList.Add(new ResourceRequest()
						{
							AllowTransmutation = false,
							Required = daysNeeded,
							ResourceName = "Labour",
							ResourceTypeName = "",
							ActivityName = this.Name,
							FilterDetails = new List<object>() { item }
						}
						);
					}
				}
			}
			return ResourceRequestList;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void PerformActivity()
		{
			// check if labour provided or PartialResources allowed

			if (Clock.Today.Month == Month)
			{
				// move individuals
				Muster();
			}
		}
	}
}
