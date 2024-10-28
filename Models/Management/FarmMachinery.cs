using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using System.Data;
using APSIM.Shared.Documentation.Extensions;

// TODO
// replacement, lifetime, ageing
namespace Models.Management
{
    /// <summary>
    /// Track machinery availability, operating costs and emissions
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Factorial.CompositeFactor))]
    [ValidParent(ParentType = typeof(Factorial.Factor))]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    public class FarmMachinery : Model
    {
        private List<string> _tractorNames = new();

        /// <summary> </summary>
        [Description("Fuel Cost ($/l)" )]
        public double FuelCost {get; set;}

        /////////////  Arrays of combined machinery pair (tractor + implement) parameters
        /// <summary> </summary>
        [Display]
        public List<string> TractorNames 
         {
            get {
               CollectMachineryAndImplements();
               return _tractorNames;
            }
            set { 
               _tractorNames = value;
            }
         }
               
        /// <summary> </summary>
        [Display]
        public List<string> ImplementNames {get; set;} = new();

        /// <summary> Coverage - work rate (ha/hr) </summary>
        [Display]
        public List<double> WorkRates {get; set;} = new();
    
        /// <summary> Fuel consumption rate</summary>
        [Display]
        public List<double> FuelConsRates {get; set;} = new();

        /// <summary> The daily amount of fuel consumed (litres) </summary>
        public double FuelConsumption {get; set;}

        /// <summary> Machinery is available today (ie not in use) </summary>
        /// <param name="tractor" />
        /// <param name="implement" />
        public bool MachineryAvailable (string tractor, string implement)
        {
           bool inUse = Jobs.Select(x => x.Tractor == tractor || x.Implement == implement).Count() > 0 ;
           //Summary.WriteMessage(this, $"Querying {tractor} and {implement}, active= {string.Join(";", Jobs.Select(x => x.Tractor + "," + x.Implement))}, res = {! inUse}", MessageType.Information); 
           return ! inUse;
        }        

        /// <summary> Add a job to the queue </summary>
        [EventSubscribe("Operate")]
        public void OnOperate(object sender, FarmMachineryOperateArgs e)
        {
           var tractor = this.FindChild<FarmMachineryItem>(e.Tractor);
           var implement = this.FindChild<FarmMachineryItem>(e.Implement);
           int iRow = getComboIndex(e.Tractor, e.Implement);

           var workRate = WorkRates[iRow];

           Jobs.Add(new MachineryJob{Tractor = e.Tractor, Category = e.Category, Implement = e.Implement, Paddock = e.Paddock, Area = findArea(e.Paddock)});
           Summary.WriteMessage(this, $"Queueing {e.Tractor} and {e.Implement} in {e.Paddock}", MessageType.Information); 
        }

        /// <summary>Operate a tractor/implement combo  </summary>
        public class FarmMachineryOperateArgs : EventArgs
        {
           /// <summary> </summary>
           public string Tractor { get; set; }
           /// <summary> </summary>
           public string Implement { get; set; }
           /// <summary> </summary>
           public string Paddock { get; set; }
           /// <summary> </summary>
           public string Category { get; set; }
        }        

        /// <summary>Our queue </summary>
        [NonSerialized]
        private List<MachineryJob> Jobs = null;
    
        [Link] private Summary Summary = null;

        [NonSerialized]
        private Events myEvents = null;
        /// <summary>
        /// return a list of tractors we know about
        /// </summary>
        public List<string> getTractorNames () {
           var result  = this.FindAllChildren<FarmMachineryItem>().
                Where(i => i.MachineryType == MachineryType.Tractor).
                Select(i => i.Name).ToList();
           return result;
        }

        /// <summary>
        /// return a list of tractors we know about
        /// </summary>
        public List<string> getImplementNames () {
           var result  = this.FindAllChildren<FarmMachineryItem>().
                Where(i => i.MachineryType == MachineryType.Implement).
                Select(i => i.Name).ToList();
           return result;
        }

        /// <summary>
        /// Go through all child components and ensure they are in the tractor/implement arrays
        /// so that the user can modify them.
        /// </summary>
        public void CollectMachineryAndImplements()
        { 
            // Add in missing tractor / implements
            foreach (var t in getTractorNames()) 
               foreach(var i in getImplementNames()) 
               {
                  var alreadyExists = _tractorNames.Zip(ImplementNames)
                                                   .Any(zip => zip.First == t && zip.Second == i);
                  if (!alreadyExists)
                  {
                     _tractorNames.Add(t);
                     ImplementNames.Add(i);
                     WorkRates.Add(0);
                     FuelConsRates.Add(0);
                  }
               }

            var childTractorNames = getTractorNames();
            var childImplementNames = getImplementNames();

            // Remove tractor / implements that no longer exist.
            for (int i = _tractorNames.Count - 1; i >= 0; i--)
            {
               bool remove = !childTractorNames.Contains(_tractorNames[i]) || 
                             !childImplementNames.Contains(ImplementNames[i]);
               if (remove)
               {
                  _tractorNames.RemoveAt(i);
                  ImplementNames.RemoveAt(i);
                  WorkRates.RemoveAt(i);
                  FuelConsRates.RemoveAt(i);
               }
            }
        }

        /// <summary> </summary>
        [EventSubscribe("StartOfSimulation")]
        public void OnStartOfSimulation(object sender, EventArgs e)
        {
            Jobs = new List<MachineryJob>();
            myEvents = new Events(this);
            FuelConsumption = 0;
        }

        /// <summary> </summary>
        [EventSubscribe("EndOfSimulation")]
        public void OnEndOfSimulation(object sender, EventArgs e)
        {
        }

        /// <summary> </summary>
        [EventSubscribe("StartOfDay")]
        public void DoStartOfDay(object sender, EventArgs e) 
        {
            FuelConsumption = 0;
        }

      /// <summary> </summary>
      [EventSubscribe("EndOfDay")]
      public void DoEndOfDay(object sender, EventArgs e)
      {
         var tomorrowsJobs = new List<MachineryJob>();

         // Go through each job and see if it can be started. 
         // We can start the job if there is unused time available.
         // A job may continue for several days
         
         var hoursWorkedToday = new Dictionary<string, double>();
         foreach (var item in this.FindAllChildren<FarmMachineryItem>())
            hoursWorkedToday[item.Name] = 0;

         foreach (var job in Jobs)
         {
            var underLimit = true;

            // jobs ahead of this one may finish today, and we could start it.
            var priorJobIndex = Jobs.IndexOf(job) - 1;
            if (priorJobIndex >= 0)
            {
               foreach (var otherJob in Jobs.GetRange(0, priorJobIndex))
               {
                  // fixme - only valid for n=1
                  if (job.Tractor == otherJob.Tractor &&
                      hoursWorkedToday[otherJob.Tractor] < getMaxHours(otherJob.Tractor) &&
                      job.Implement == otherJob.Implement &&
                      hoursWorkedToday[otherJob.Implement] < getMaxHours(otherJob.Implement))
                  {
                     underLimit = false;
                  }
               }
            }
            if ( underLimit )
            {
               // The job can be running today. Work out how many hours, and then the costs
               var maxHours = Math.Min(getMaxHours(job.Tractor) - hoursWorkedToday[job.Tractor],
                                        getMaxHours(job.Implement) - hoursWorkedToday[job.Implement]);
               var rate = getRate(job.Tractor, job.Implement);
               double areaToday = 0.0;
               double hours = 0.0;
               if (maxHours * rate <= job.Area)
               {
                  hours = maxHours;
                  areaToday = maxHours * rate;
               }
               else
               {
                  hours = job.Area / rate;
                  areaToday = hours * rate;
               }

               double cost = hours *
                   getFuelCost(job.Tractor, job.Implement) *
                   (1 + getRunningCostsPcnt(job.Tractor) / 100);

               myEvents.Publish("DoPaddockExpenditure", new object[] { this,
                             new FarmEconomics.PaddockExpenditureArgs {
                                 Description = $"Fuel, Oil & Tyre costs of {job.Tractor} and {job.Implement}",
                                 Category = job.Category,
                                 Paddock = job.Paddock,
                                 Area =  areaToday,
                                 Rate = cost }});

               hoursWorkedToday[job.Tractor] += hours;
               hoursWorkedToday[job.Implement] += hours;
               job.Area -= areaToday;
               FuelConsumption += hours * getFuelConsumption(job.Tractor, job.Implement);


               // var t = this.FindChild<FarmMachineryItem>(job.Tractor);
               //    Where(i => i.MachineryType == MachineryType.Tractor)??
               // t.Age += hours; fixme
               // fixme implement too
               Summary.WriteMessage(this, $"Operating {job.Tractor} and {job.Implement} for {hours} hours ({areaToday} ha)", MessageType.Information);
            }
            if (job.Area > 0)
               tomorrowsJobs.Add(job);
            else
               Summary.WriteMessage(this, $"Finishing {job.Tractor} and {job.Implement} in {job.Paddock}", MessageType.Information);
         }
         Jobs = tomorrowsJobs;

         // fixme end of financial year calculations
      }

      /// <summary> </summary>
      [EventSubscribe("DoManagement")]
      public void DoManagement(object sender, EventArgs e)
      {
      }

      /// <summary> Index into the arrays for this tractor/implement combination </summary>
      private int getComboIndex(string tractor, string implement){
           int iRow;
           for(iRow = 0; iRow < TractorNames.Count; iRow++) {
               if (TractorNames[iRow] == tractor &&
                   ImplementNames[iRow] == implement)
                  break;
           }

           if (iRow >= TractorNames.Count)
               throw new Exception($"Cant find work rates for {tractor} and {implement}");
            return(iRow);
        }
        private double getRate(string tractor, string implement){
           return(WorkRates[getComboIndex(tractor, implement)]);
        }

        /// <summary>
        ///  Fuel cost
        /// </summary>
        /// <param name="tractor"></param>
        /// <param name="implement"></param>
        /// <returns>$/hr</returns>
        private double getFuelCost(string tractor, string implement){
           return(getFuelConsumption(tractor, implement) * FuelCost);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tractor"></param>
        /// <param name="implement"></param>
        /// <returns>lt/hr</returns>
        private double getFuelConsumption(string tractor, string implement){
           return(FuelConsRates[getComboIndex(tractor, implement)]);
        }

        private double getRunningCostsPcnt(string tractor){
           var t = this.FindChild<FarmMachineryItem>(tractor);
           return((double)t?.OilTyreCost);
        }
        private double getMaxHours(string item){
           var t = this.FindChild<FarmMachineryItem>(item);
           return((double)t?.MaxHours);
        }

        [Link]
        private Simulation simulation = null;
        private double findArea (string paddock) {
            Zone z = simulation.FindChild<Zone>(paddock);
            return z.Area;
        } 

    }

   /// <summary>A job in our queue </summary>
   public class MachineryJob
   {
      /// <summary> </summary>
      public string Tractor { get; set; }
      /// <summary> </summary>
      public string Implement { get; set; }
      /// <summary> </summary>
      public string Paddock { get; set; }
      /// <summary> </summary>
      public string Category { get; set; }
      /// <summary> </summary>
      public double Area { get; set; }
   }
}

