using System;
using System.Collections.Generic;

namespace PlantSchedule.DTO {
    public class Resource {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public List<Order> Priority { get; set; } = new List<Order>();
        public List<Operation> Operations { get; set; } = new List<Operation>();
        public List<Tuple<DateTime, DateTime>> Maintenance { get; set; } = new List<Tuple<DateTime, DateTime>>();
        public Dictionary<string, bool> Allocate { get; set; } = new Dictionary<string, bool>();
        public double IdleTime { get; set; } = Double.MaxValue;
    }
}