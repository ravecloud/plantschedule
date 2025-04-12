
namespace PlantSchedule.DTO {
    public class Intermediate {
        public string UVP { get; set; }
        public string MaterialDescription { get; set; }
        public int BOM { get; set; }
        public int AltBOM { get; set; }
        public int Item { get; set; }
        public double BaseQuantity { get; set; }
        public int ComponentNumber { get; set; }
        //public Dictionary<String, Double> Components { get; set; } = new Dictionary<String, Double>();
        public Dictionary<String, Double> Components { get; set; } = new Dictionary<String, Double>();
        public string UVPIndex { get; set; }
    }

    public class FormOrderList {
        public List<Intermediate> FormOrders { get; set; } = new List<Intermediate>();
    }
}