﻿using Models.Core;
using Models.WholeFarm.Groupings;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>Ruminant sales activity</summary>
	/// <summary>This activity undertakes the sale and transport of any individuals f;agged for sale.</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class RuminantActivityBuySell : WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// Name of breed to buy or sell
		/// </summary>
		[Description("Name of breed to buy or sell")]
		public string BreedName { get; set; }

		/// <summary>
		/// Price of breeding sire
		/// </summary>
		[Description("Price of breeding sire")]
		public double BreedingSirePrice { get; set; }

		/// <summary>
		/// Distance to market
		/// </summary>
		[Description("Distance to market (km)")]
		public double DistanceToMarket { get; set; }

		/// <summary>
		/// Cost of trucking ($/km/truck)
		/// </summary>
		[Description("Cost of trucking ($/km/truck)")]
		public double CostPerKmTrucking { get; set; }

		/// <summary>
		/// Number of 450kg animals per truck load
		/// </summary>
		[Description("Number of 450kg animals per truck load")]
		public double Number450kgPerTruck { get; set; }

		/// <summary>
		/// Yard fees when sold ($/head)
		/// </summary>
		[Description("Yard fees when sold ($/head)")]
		public double YardFees { get; set; }

		/// <summary>
		/// MLA fees when sold ($/head)
		/// </summary>
		[Description("MLA fees when sold ($/head)")]
		public double MLAFees { get; set; }

		/// <summary>
		/// Minimum number of truck loads before selling (0 continuous sales)
		/// </summary>
		[Description("Minimum number of truck loads before selling (0 continuous sales)")]
		public double MinimumTrucksBeforeSelling { get; set; }

		/// <summary>
		/// Minimum proportion of truck load before selling (0 continuous sales)
		/// </summary>
		[Description("Minimum proportion of truck load before selling (0 continuous sales)")]
		public double MinimumLoadBeforeSelling { get; set; }

		/// <summary>
		/// Sales commission to agent (%)
		/// </summary>
		[Description("Sales commission to agent (%)")]
		public double SalesCommission { get; set; }

		/// <summary>
		/// name of account to use
		/// </summary>
		[Description("Name of bank account to use")]
		public string BankAccountName { get; set; }

		private FinanceType bankAccount = null;

		/// <summary>
		/// Labour settings
		/// </summary>
		private List<LabourFilterGroupSpecified> labour { get; set; }

		/// <summary>An event handler to allow us to initialise herd pricing.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("StartOfSimulation")]
		private void OnStartOfSimulation(object sender, EventArgs e)
		{
			// get account
			bool tmp = true;
			bankAccount = Resources.GetResourceItem("Finances", BankAccountName, out tmp) as FinanceType;

			// check if pricing is present
			if(bankAccount != null)
			{
				RuminantHerd ruminantHerd = Resources.RuminantHerd();
				var breeds = ruminantHerd.Herd.Where(a => a.BreedParams.Breed == BreedName).GroupBy(a => a.HerdName);
				foreach (var breed in breeds)
				{
					if (!breed.FirstOrDefault().BreedParams.PricingAvailable())
					{
						Summary.WriteWarning(this, String.Format("No pricing schedule has been provided for herd ({0}). No transactions will be recorded for activity ({1}).", breed.Key, this.Name));
					}
				}
			}

			// get labour specifications
			labour = this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
			if (labour == null) labour = new List<LabourFilterGroupSpecified>();
		}

		/// <summary>An event handler to call for animal purchases</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalBuy")]
		private void OnWFAnimalBuy(object sender, EventArgs e)
		{
			// This activity will purchase animals based on available funds.

			RuminantHerd ruminantHerd = Resources.RuminantHerd();

			var newRequests = ruminantHerd.PurchaseIndividuals.Where(a => a.BreedParams.Breed == BreedName).ToList();
			foreach (var newgroup in newRequests.GroupBy(a => a.SaleFlag))
			{
				double fundsAvailable = 100000000;
				if (bankAccount != null)
				{
					fundsAvailable = bankAccount.FundsAvailable;
				}
				double cost = 0;
				double shortfall = 0;
				bool fundsexceeded = false;
				foreach (var newind in newgroup)
				{
					double value = 0;
					if (newgroup.Key == HerdChangeReason.SirePurchase)
					{
						value = BreedingSirePrice;
					}
					else
					{
						value = newind.BreedParams.ValueofIndividual(newind, true);
					}
					if (cost + value <= fundsAvailable & fundsexceeded==false)
					{
						ruminantHerd.AddRuminant(newind);
						cost += value;
					}
					else
					{
						fundsexceeded = true;
						shortfall += value;
					}
				}

				if (bankAccount != null)
				{
					ResourceRequest purchaseRequest = new ResourceRequest();
					purchaseRequest.ActivityName = this.Name;
					purchaseRequest.Required = cost;
					purchaseRequest.AllowTransmutation = false;
					purchaseRequest.Reason = newgroup.Key.ToString();
					bankAccount.Remove(purchaseRequest);

					// report any financial shortfall in purchases
					if (shortfall > 0)
					{
						purchaseRequest.Available = bankAccount.Amount;
						purchaseRequest.Required = cost + shortfall;
						purchaseRequest.Provided = cost;
						purchaseRequest.ResourceName = "Finances";
						purchaseRequest.ResourceTypeName = BankAccountName;
						ResourceRequestEventArgs rre = new ResourceRequestEventArgs() { Request = purchaseRequest };
						OnShortfallOccurred(rre);
					}
				}
			}
		}

		/// <summary>An event handler to call for animal sales</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalSell")]
		private void OnWFAnimalSell(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();

			Finance Accounts = Resources.FinanceResource() as Finance;
			FinanceType bankAccount = Accounts.GetFirst() as FinanceType;

			int trucks = 0;
			double saleValue = 0;
			double saleWeight = 0;
			int head = 0;

			// get current untrucked list of animals flagged for sale
			List<Ruminant> herd = ruminantHerd.Herd.Where(a => a.SaleFlag != HerdChangeReason.None & a.Breed == BreedName).OrderByDescending(a => a.Weight).ToList();

			// if sale herd > min loads before allowing sale
			if (herd.Select(a => a.Weight / 450.0).Sum() / Number450kgPerTruck >= MinimumTrucksBeforeSelling)
			{
				// while truck to fill
				while (herd.Select(a => a.Weight / 450.0).Sum() / Number450kgPerTruck > MinimumLoadBeforeSelling)
				{
					bool nonloaded = true;
					trucks++;
					double load450kgs = 0;
					// while truck below carrying capacity load individuals
					foreach (var ind in herd)
					{
						if(load450kgs + (ind.Weight / 450.0) <= Number450kgPerTruck)
						{
							nonloaded = false;
							head++;
							load450kgs += ind.Weight / 450.0;
							saleValue += ind.BreedParams.ValueofIndividual(ind, false);
							saleWeight += ind.Weight;
							ruminantHerd.RemoveRuminant(ind);
						}
					}
					if(nonloaded)
					{
						Summary.WriteWarning(this, String.Format("There was a problem loading the sale truck as sale individuals did not meet the loading criteria for breed {0}", BreedName));
						break;
					}
					herd = ruminantHerd.Herd.Where(a => a.SaleFlag != HerdChangeReason.None & a.Breed == BreedName).OrderByDescending(a => a.Weight).ToList();
				}

				if (trucks > 0 & bankAccount != null)
				{
					ResourceRequest expenseRequest = new ResourceRequest();
					expenseRequest.ActivityName = this.Name;
					expenseRequest.AllowTransmutation = false;

					// TODO: report shortfall before transactions

					// calculate transport costs
					expenseRequest.Required = trucks * DistanceToMarket * CostPerKmTrucking;
					expenseRequest.Reason = "Transport";
					bankAccount.Remove(expenseRequest);
					// calculate MLA fees
					expenseRequest.Required = head * MLAFees;
					expenseRequest.Reason = "R&D Fee";
					bankAccount.Remove(expenseRequest);
					// calculate yard fees
					expenseRequest.Required = head * YardFees;
					expenseRequest.Reason = "Yard costs";
					bankAccount.Remove(expenseRequest);
					// calculate commission
					expenseRequest.Required = saleValue * SalesCommission;
					expenseRequest.Reason = "Sales commission";
					bankAccount.Remove(expenseRequest);

					// add and remove from bank
					bankAccount.Add(saleValue, this.Name, "Sales");
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

			for (int i = 0; i < 2; i++)
			{
				string BuySellString = (i == 0) ? "Purchase" : "Sell";

				List<Ruminant> herd = Resources.RuminantHerd().Herd.Where(a => a.SaleFlag.ToString().Contains(BuySellString) & a.Breed == BreedName).ToList();
				int head = herd.Count();
				double AE = herd.Sum(a => a.AdultEquivalent);

				if (head > 0)
				{
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
						if (daysNeeded > 0)
						{
							if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
							ResourceRequestList.Add(new ResourceRequest()
							{
								AllowTransmutation = false,
								Required = daysNeeded,
								ResourceName = "Labour",
								ResourceTypeName = "",
								ActivityName = this.Name,
								Reason = BuySellString,
								FilterDetails = new List<object>() { item }
							}
							);
						}
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
			return; 
		}

		/// <summary>
		/// Resource shortfall event handler
		/// </summary>
		public override event EventHandler ResourceShortfallOccurred;

		/// <summary>
		/// Shortfall occurred 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnShortfallOccurred(EventArgs e)
		{
			if (ResourceShortfallOccurred != null)
				ResourceShortfallOccurred(this, e);
		}

	}
}