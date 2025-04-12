using System.Collections.Generic;

namespace PlantSchedule.DTO {
    public class Worker {
        public string Id { get; set; }
        public string Name { get; set; }
        public SimulationInstance SimulationInstance { get; set; }
        public List<Order> Orders { get; set; }
        public List<Resource> Resources { get; set; }
        public bool Idle { get; set; }
    }
}
