using System.Text.Json;
using PlantSchedule.DTO;
namespace PlantSchedule.RTS;

public interface IIndividual
{
    public List<IGene> Genes { get; set; }
    public int Id { get; set; } 
    public int Age { get; set; }
    public double Measure { get; set; }
    public void Mutate();
    public Dictionary<String, double> Fitness { get; set; }
}

public class Individual : IIndividual
{
    public List<IGene> Genes { get; set; } = new List<IGene>();
    public int Id { get; set; } = new int();
    public Worker Worker = new Worker();
    public Dictionary<string, double> Fitness { get; set; } = new Dictionary<string, double>();
    public string FitnessName { get; set; }
    public int Age { get; set; }
    public double Measure { get; set; }

    public String Json;

    public Individual()
    {
        Id = GetHashCode();
        Age = 0;
        Measure = -1.0;
        this.Worker = GetWorker();
    }
    public Individual(List<IGene> genes)
    {
        Genes = new List<IGene>();
        foreach (var gene in genes)
        {
            Genes.Add(gene.Copy());
        }
        Id = GetHashCode();
        Age = 0;
        Measure = -1.0;
        this.Worker = GetWorker();
    }
    public double GetFitness(string fitnessName)
    {
        return Fitness[fitnessName];
    }
    public Individual(string worker)
    {
        var ind = new Individual();
        if (!File.Exists(worker)) throw new FileNotFoundException($"The file {worker} was not found");
        try
        {
            var w = File.ReadAllText(worker);
            this.Worker = JsonSerializer.Deserialize<Worker>(w);
            // Define the genomes
            this.Genes = new List<IGene>() {
                new Gene<String>(
                    GeneName.Order,
                    GeneType.StringPermutation,
                    new List<String>(),
                    Mutations<String>.GetMutationMethod("Random"),
                    Mutations<String>.GetMutationMethod("Permutation"),
                    Crossovers<String>.GetCrossoverMethod("Cycle")
                )
            };
            foreach (var order in this.Worker.Orders)
            {
                ((Gene<String>)this.Genes[0]).Values.Add(order.Name);
            }
            // dereference and delete all 
            // this.Worker = GetWorker(this.Worker);
        }
        catch
        {
            throw new Exception($"Could not deserialize the content in {worker}.");
        }

        this.Worker.Orders = new List<Order>();
        this.Worker.Resources = new List<Resource>();

    }
    public Individual(Individual other)
    {
        Genes = new List<IGene>();
        foreach (var gene in other.Genes)
        {
            Genes.Add(gene.Copy());
        }
        Id = other.Id;
        Worker = GetWorker();
        foreach (var order in other.Worker.Orders)
        {
            Worker.Orders.Add(new Order
            {
                AltBOM = order.AltBOM,
                StartDate = order.StartDate,
                BaseProduct = order.BaseProduct,
                Product = order.Product,
                Operations = new List<Operation>(order.Operations.ToList()),
                Started = order.Started,
                BOM = order.BOM,
                Component = order.Component,
                DueDate = order.DueDate,
                EndDate = order.EndDate,
                Finished = order.Finished,
                Amount = order.Amount,
                Recipe = order.Recipe,
                Release = order.Release,
                Line = order.Line,
                Id = order.Id,
                Name = order.Name,
                UVP = order.UVP,
                Item = order.Item,
                MaterialDescription = order.MaterialDescription,
            });
        }
        foreach (var resource in other.Worker.Resources)
        {
           Worker.Resources.Add(new Resource
           {
               Id = resource.Id,
               Name = resource.Name,
               Priority = resource.Priority,
               Operations = new List<Operation>(resource.Operations.ToList()),
               Maintenance = new List<Tuple<DateTime, DateTime>>(resource.Maintenance.ToList()),
               Allocate = new Dictionary<string, bool>(resource.Allocate.ToDictionary()),
               IdleTime = resource.IdleTime
           }); 
        }
    }
    
    private PlantSchedule.DTO.Worker GetWorker(Worker other)
    {
        // TODO: Check if the objects are copied or referenced
        var orderArray = new Order[other.Orders.Count];
        other.Orders.CopyTo(orderArray);
        var resoureceArray = new Resource[other.Resources.Count];
        other.Resources.CopyTo(resoureceArray);
        
        var worker = new PlantSchedule.DTO.Worker()
        {
            Name = "",
            Idle = false,
            Id = GetHashCode().ToString(),
            Orders = orderArray.ToList(),
            Resources = resoureceArray.ToList(),
        };
        //Set inosim instance/
        worker.SimulationInstance = new PlantSchedule.DTO.SimulationInstance();
        return worker;
    }
    private PlantSchedule.DTO.Worker GetWorker()
    {
        var worker = new PlantSchedule.DTO.Worker()
        {
            Name = "",
            Idle = false,
            Id = GetHashCode().ToString(),
            Orders = new List<PlantSchedule.DTO.Order>(),
            Resources = new List<Resource>(),
        };
        //Set inosim instance/
        worker.SimulationInstance = new PlantSchedule.DTO.SimulationInstance();
        return worker;
    }
    public void SetWorker(Worker worker)
    {
        var w = JsonSerializer.Serialize(worker);
        Worker = JsonSerializer.Deserialize<Worker>(w);
    }

    public void Mutate()
    {
        Genes.ForEach(g => g.Mutation());
    }

    public void Init()
    {
        Genes.ForEach(g => g.Initialization());
    }

    public void ToConsole()
    {
        Console.Write($"Individual {Id}; ");
        this.Genes.ForEach(g =>
        {
            Console.Write($"{g.Name + "_" + g.Type} ");
            foreach (var v in g)
            {
                Console.Write(v + " ");
            }
        });
        Console.Write($"; Fitness: {this.Fitness}");
        Console.WriteLine();
    }
    
}