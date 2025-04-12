using System;
using System.Collections.Generic;

namespace PlantSchedule.DTO {
    public interface ISimulationInstance
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public DateTime SimulationStart { get; set; }
        public DateTime SimulationEnd { get; set; }
        public Dictionary<string, double> Objectives { get; set; }
    }
    public class SimulationInstance : ISimulationInstance
    {
        public int ProcessID { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public DateTime SimulationStart { get; set; }
        public DateTime SimulationEnd { get; set; }
        public Dictionary<string, double> Objectives { get; set; }
    }
    public class InosimInstance : ISimulationInstance {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; } = "13";
        /// <summary>
        /// Toggles the window mode of INOSIM. "0" = Visible (default), "2" = Invisible
        /// </summary>
        public string Visibility { get; set; } = "0";

        /// <summary>
        /// The license type to use, e.g. "ExpertEdition" or "Runtime". If not specified, computer default will be used
        /// </summary>
        public string License { get; set; } = "ExpertEdition";

        /// <summary>
        /// Database file (.imdf) to use
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// The project to use in the selected database
        /// </summary>
        public string Project { get; set; }

        /// <summary>
        /// The experiment to use in the selected project
        /// </summary>
        public string Experiment { get; set; }

        /// <summary> 
        /// If specified INOSIM will write the log here, e.g. "C:\Temp\Protocol.txt"
        /// </summary>
        public string LogFile { get; set; }
        public int ProcessID { get; set; }  
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public DateTime SimulationStart { get; set; }
        public DateTime SimulationEnd { get; set; }
        public Dictionary<string,double> Objectives { get; set; }   
    }
}
