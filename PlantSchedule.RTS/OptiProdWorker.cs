namespace PlantSchedule.DTO {
    public class OptiProdWorker {
        public String Id { get; set; } = "";
        public String Name { get; set; } = "";
        public List<Product> Products;
        public List<Resource> Resources { get; set; } = new List<Resource>();
        public SimulationInstance InosimInstance { get; set; } = new SimulationInstance();
        public List<Order> Orders { get; set; } = new List<Order>();
        public bool Idle { get; set; } = true;
    }
}