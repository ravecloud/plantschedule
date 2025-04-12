using System;

namespace PlantSchedule.DTO
{
    public class Operation
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Order { get; set; }
        public double Duration { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}