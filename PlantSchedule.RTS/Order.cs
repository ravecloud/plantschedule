
namespace PlantSchedule.DTO
{
    public class Order {
        public string Id { get; set; } = "";
        public string Name { get; set; }= "";
        public string UVP { get; set; }= "";
        public int BOM { get; set; } = 0;
        public int AltBOM { get; set; } = 0;
        public DateTime StartDate { get; set; } = new DateTime();
        public DateTime EndDate { get; set; } = new DateTime();
        public DateTime DueDate { get; set; } = new DateTime();
        public double Amount { get; set; } = 0.0;
        public string Recipe { get; set; } = "";
        public DateTime Release { get; set; }
        public string Line { get; set; }= "";
        public int Item { get; set; } = 0;
        public string MaterialDescription { get; set; }= "";
        public string Component { get; set; }= "";
        public string BaseProduct { get; set; }= "";
        public string Product { get; set; }= "";
        public List<Operation> Operations { get; set; } = new List<Operation>();
        public bool Started { get; set; } = false;
        public bool Finished { get; set; } = false;
        
    }
}