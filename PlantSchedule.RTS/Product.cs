
namespace PlantSchedule.DTO {
    public class Product {
        //Needs to be empty
        public String Id { get; set; }
        public String Name { get; set; }
        public String Material { get; set; }
        public String UVP { get; set; }
        public int FillingType { get; set; }
        public List<String> FormLines { get; set; } = new List<String>();
        public List<String> FillLines { get; set; } = new List<String>();
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public bool Started { get; set; } = false;
        public bool Finished { get; set; } = false;
        public double Amount { get; set; }
        public List<Operation> Operations { get; set; } = new List<Operation>();
        public string BaseProduct { get; set; } = "";
        public List<Order> Orders { get; set; } = new List<Order>();
    }

    public class ProductList {
        public List<Product> Products { get; set; } = new List<Product>();
    }
}