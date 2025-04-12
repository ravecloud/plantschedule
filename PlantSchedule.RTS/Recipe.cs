using System.Collections.Generic;

namespace PlantSchedule.DTO
{
    public class Recipe
    {
        public Dictionary<string, List<double>> ProcessingTimes { get; set; }
        public Dictionary<string, List<double>> ChangeoverTime { get; set; }
    }
}