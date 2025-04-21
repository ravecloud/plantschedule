#define PARALLELEXECUTION
#undef PARALLELEXECUTION
using DateTime = System.DateTime;
using System.Diagnostics;
using System.Text.Json;
using PlantSchedule.DTO;
using Timer = System.Timers.Timer;
using System.Data;
using System.Collections.Generic;


namespace PlantSchedule.RTS;

public interface ISimulator
{
    public DateTime SimulationStart { get; set; }
    public DateTime SimulationEnd { get; set; }
    public void Simulate(List<Individual> individuals, StreamWriter logWriter);
    public void Simulate(List<Individual> individuals, Individual reference, int skipId = -1);
    public List<String> Orders { get; set; }
    public List<PlantSchedule.DTO.Order> CopyOrders(List<PlantSchedule.DTO.Order> otherOrders);
    public void Simulate(string json);
    public List<PlantSchedule.DTO.Order> CreateOrders(Gene<String> orderGene);
    public List<Resource> CopyResources(List<Resource> resources);
    public void UpdateSimulator(Worker tempWorker);
    public void SetDueDate(string order);
    public void SetDueDate(Dictionary<string, int> rushOrderDueDate);
}
#if PLANT_SIMULATOR
public class PlantSimulator : ISimulator
{
    public DateTime SimulationStart { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DateTime SimulationEnd { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public List<string> Orders { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public List<Order> CopyOrders(List<Order> otherOrders)
    {
        throw new NotImplementedException();
    }

    public List<Resource> CopyResources(List<Resource> resources)
    {
        throw new NotImplementedException();
    }

    public List<Order> CreateOrders(Gene<string> orderGene)
    {
        throw new NotImplementedException();
    }

    public void SetDueDate(string order)
    {
        throw new NotImplementedException();
    }

    public void SetDueDate(Dictionary<string, int> rushOrderDueDate)
    {
        throw new NotImplementedException();
    }

    public void Simulate(List<Individual> individuals, StreamWriter logWriter)
    {
        throw new NotImplementedException();
    }

    public void Simulate(List<Individual> individuals, StreamWriter logWriter, DateTime simulationStart)
    {
        throw new NotImplementedException();
    }

    public void Simulate(string json)
    {
        throw new NotImplementedException();
    }

    public void UpdateSimulator(Worker tempWorker)
    {
        throw new NotImplementedException();
    }
}
#endif
public class CSharpSimulator : ISimulator
{
    #region PUBLIC MEMBERS
    public DateTime SimulationStart { get; set; }
    public DateTime SimulationEnd { get; set; }
    public Dictionary<(string,string), double> ProcessTimes { get; set; }   
    public Dictionary<(string,string), double> ChangeoverTimes_S1 { get; set; }   
    public Dictionary<(string,string), double> ChangeoverTimes_S2 { get; set; }   
    public Dictionary<(string,string), double> ChangeoverTimes_S3 { get; set; }   
    public Dictionary<(string,string), double> ChangeoverTimes_S4 { get; set; }   
    public Dictionary<(string,string), double> ChangeoverTimes_S5 { get; set; }   
    public Dictionary<(string,string), double> ChangeoverTimes_S6 { get; set; }
    public List<string> Orders { get; set; } = new List<string>();
    public Dictionary<String, DateTime> DueDates;
    public CSharpConfig Config { get; set; }
    public readonly Dictionary<String, List<String>> StageMachineMap = new Dictionary<string, List<string>>()
    {
        { "S1", new List<string>() { "M01", "M02" } },
        { "S2", new List<string>() { "M03", "M04", "M05" } },
        { "S3", new List<string>() { "M06", "M07", "M08" } },
        { "S4", new List<string>() { "M09", "M10", "M11" } },
        { "S5", new List<string>() { "M12", "M13", "M14" } },
        { "S6", new List<string>() { "M15", "M16", "M17" } }
    };
    #endregion
    #region PRIVATE MEMBERS
    private Dictionary<string, string> OrdersRecipeNoAlloc = new Dictionary<string, string>()
    {
        { "P01", "R01" },
        { "P02", "R02" },
        { "P03", "R03" },
        { "P04", "R02" },
        { "P05", "R03" },
        { "P06", "R04" },
        { "P07", "R02" },
        { "P08", "R05" },
        { "P09", "R02" },
        { "P10", "R02" },
        { "P11", "R02" },
        { "P12", "R06" },
        { "P13", "R06" },
        { "P14", "R04" },
        { "P15", "R04" },
        { "P16", "R01" },
        { "P17", "R07" },
        { "P18", "R02" },
        { "P19", "R02" },
        { "P20", "R05" },
        { "P21", "R08" },
        { "P22", "R02" },
        { "P23", "R05" },
        { "P24", "R09" },
        { "P25", "R10" },
        { "P26", "R05" },
        { "P27", "R09" },
        { "P28", "R02" },
        { "P29", "R03" },
        { "P30", "R10" },
        { "P31", "R01" },
        { "P32", "R02" },
        { "P33", "R03" },
        { "P34", "R02" },
        { "P35", "R03" },
        { "P36", "R04" },
        { "P37", "R02" },
        { "P38", "R05" },
        { "P39", "R02" },
        { "P40", "R02" }
    };
    #endregion
    #region CONSTRUCTOR
    public CSharpSimulator(IConfig config)
    {
        this.Config = (CSharpConfig)config;
        var csvDir = "CsvFiles";
        if (Directory.Exists(csvDir))
        {
            var sw = new Stopwatch();

            sw.Start();
            var files = Directory.GetFiles("CsvFiles");
            foreach (var file in files)
            {
                var propertyName = file.Split('.').First().Split('\\').Last();
                switch (propertyName)
                {
                    case "ProcessTimes":
                        ProcessTimes = ReadCsvFile(file);
                        break;
                    case "ChangeoverTimes_S1":
                        ChangeoverTimes_S1 = ReadCsvFile(file);
                        break;
                    case "ChangeoverTimes_S2":
                        ChangeoverTimes_S2 = ReadCsvFile(file);
                        break;
                    case "ChangeoverTimes_S3":
                        ChangeoverTimes_S3 = ReadCsvFile(file);
                        break;
                    case "ChangeoverTimes_S4":
                        ChangeoverTimes_S4 = ReadCsvFile(file);
                        break;
                    case "ChangeoverTimes_S5":
                        ChangeoverTimes_S5 = ReadCsvFile(file);
                        break;
                    case "ChangeoverTimes_S6":
                        ChangeoverTimes_S6 = ReadCsvFile(file);
                        break;

                    default:
                        break;
                }
            }
            sw.Stop();
            Console.WriteLine($"Elapsed milliseconds to read files: {sw.Elapsed}ms.");
        }
        // Initialize the genome
        this.DueDates = new();
        var rand = new Random();
        var numOrders = 30;
        var addToDueDate = 4;
        var addToDueDateInc = 0;
        var orderBatch = 10;
        var dueDateStart = 16;
        var dueDateEnd = 20;
        var randomList = GenerateUniqueRandomList(numOrders, 1, numOrders + 1);
        for (int i = 1; i <= numOrders; i++)
        {
            var j = randomList[i - 1];
            if (i - 1 >= orderBatch)
            {
                orderBatch += 10;
                addToDueDate += 5 + addToDueDateInc;
                addToDueDateInc += addToDueDateInc;
            }
            var rem = ((j-1)/30);
            var recipe = "P" + ((j - (rem * 30)).ToString("D2"));
            var orderName = "P" + ((j - (rem * 30)).ToString("D2")) + "_" + rem.ToString("D2");
            Orders.Add(orderName);
            DueDates.Add(orderName, Config.SimulationStart.AddHours(rand.Next((int)(dueDateStart + addToDueDate), (int)(dueDateEnd + addToDueDate))));
        }

        Config = (CSharpConfig)config;
        if(Config.RushOrders != null && Config.RushOrders.Count > 0)
        {
            foreach (var rushOrder in Config.RushOrders)
            {
                if (!this.DueDates.ContainsKey(rushOrder.Key)){
                    // Orders.Add(rushOrder.Key);
                    var rushOrderStart = Config.SimulationStart.AddSeconds(rushOrder.Value[0] * Config.TimeIncrement);
                    this.DueDates.Add(rushOrder.Key, rushOrderStart.AddHours(rushOrder.Value[1]));
                }
            }
        }
    }

    private static List<int> GenerateUniqueRandomList(int count, int min, int max)
    {
        Random rand = new Random();
        HashSet<int> uniqueNumbers = new HashSet<int>();
        while (uniqueNumbers.Count < count)
        {
            int number = rand.Next(min, max);
            uniqueNumbers.Add(number);
        }
        return new List<int>(uniqueNumbers);
    }

    private void StartSimulationSteps(Individual ind)
    {
        var orders = ind.Worker.Orders;
        var resources = ind.Worker.Resources;
        bool simulationFinished = false;
        while (!simulationFinished)
        {
            var allEnded = false;
            foreach (var order in orders)
            {
                var lastOperation = order.Operations.Last();
                var nameSplit = lastOperation.Name.Split("_");
                var state = nameSplit.First();
                var stage = nameSplit.Last();
                var nextStage = FindNextStage(stage, order.Name);
                var nextNextStage = FindNextStage(nextStage, order.Name);
                if (!StageMachineMap.ContainsKey(nextNextStage)) nextNextStage = nextStage;
                // Make the changeover a state of the order. Thereafter put the order in the resource again for which the changeover was done.
                switch (state)
                {
                    case "Changeover":
                        StepAfterChangeover(order, resources, nextStage);
                        continue;
                    case "End":
                        break;
                    default:
                        // Default is Process or Wait
                        Step(order, resources, nextStage);
                        break;
                }
            }
            simulationFinished = orders.FindIndex(x => !x.Operations.Last().Name.Contains("End")) > -1 ? false : true;
        }

        EvaluateObjectives(ind);
    }
     
    // TODO: The problem is the mutation and crossover!!!

    private void InitWorker(Individual ind, Gene<string> orderGene)
    {
        InitResources(ind);
        InitOrders(ind, orderGene);
    }
    
    private void InitResources(Individual ind)
    {
        if(ind.Worker.Resources.Count == 0)
        {
            foreach (var stageMachine in StageMachineMap)
            {
                foreach (var machine in stageMachine.Value)
                {
                    ind.Worker.Resources.Add(new Resource() { Name = machine });
                    ind.Worker.Resources.Last().Operations = new List<Operation>() { 
                        new Operation() {
                            Name = "Idle", // Idle, Changeover, Clean, Process, Maintenance, Break
                            Unit = machine,
                            Order = "",
                            Duration = 0.0,
                            Start = this.SimulationStart,
                            End = this.SimulationStart }
                    };
                }
            }
        } 
    }

    public void InitOrders(Individual ind, Gene<string> orderGene)
    {
        ind.Worker.Orders = new List<Order>();
        foreach (var order in orderGene.Values)
        {
            // Only add new order if it does not exist yet
            if (ind.Worker.Orders.Exists(x => x.Name == order))
            {
                continue;
            }

            var start = this.Config.SimulationStart;
            var end = this.Config.SimulationEnd;
            if(Config.ReleaseDates != null && Config.ReleaseDates.ContainsKey(order))
            {
                start = Config.ReleaseDates[order];
                end = Config.ReleaseDates[order];
            }

            var insertIndex = orderGene.Values.FindIndex(x => x == order);

            ind.Worker.Orders.Insert(insertIndex, new Order()
            {
                Name = order,
                Recipe = order.Split("_").First(),
                DueDate = DueDates[order],
                StartDate = start,
                EndDate = Config.SimulationEnd,
                Release = start
            });
            // TODO: Check with the simulation time if the operations are running, etc...
            // Set the release date?
            // End = this.Config.SimulationStart.AddHours(1)
            // works, but the initial order list is still active, which makes sense
            // we would have to sort according to release date?

            ind.Worker.Orders[insertIndex].Operations = new List<Operation>();
            ind.Worker.Orders[insertIndex].Operations.Add(new Operation()
            {
                Name = "Wait_S0", // Wait, Process, End, Transfer
                Unit = "",
                Order = order,
                Duration = 0.0,
                Start = start,
                End = start
            });
        }
        /*
        AddOperationsFromResource(ind);

        // add operations to orders
        var orders = ind.Worker.Orders;
        foreach (var resource in ind.Worker.Resources)
        {
            if(resource.Operations.Count > 1)
            {
                foreach (var operation in resource.Operations)
                {
                    if (operation.Name.Contains("Process") || operation.Name.Contains("Changeover"))
                    {
                        var orderIdx = orders.FindIndex(x => x.Name == operation.Order);
                        if (orderIdx > -1)
                        {
                            orders[orderIdx].Operations.Add(CopyOperation(operation));
                        }
                    }
                }
            }

        }
        */
    }
    public void AddOperationsFromResource(Individual ind)
    {
        // add operations to orders
        var orders = ind.Worker.Orders;
        foreach (var resource in ind.Worker.Resources)
        {
            if(resource.Operations.Count > 1)
            {
                foreach (var operation in resource.Operations)
                {
                    if (operation.Name.Contains("Process") || operation.Name.Contains("Changeover"))
                    {
                        var orderIdx = orders.FindIndex(x => x.Name == operation.Order);
                        if (orderIdx > -1)
                        {
                            orders[orderIdx].Operations.Add(CopyOperation(operation));
                        }
                    }
                }
            }

        }
    }
    public void AddOperationsFromReference(Individual ind, Individual reference)
    {
        // add operations to orders
        foreach (var order in reference.Worker.Orders)
        {
            var orderIndex = ind.Worker.Orders.FindIndex(o => o.Name == order.Name);
            if (orderIndex > -1) {
                ind.Worker.Orders[orderIndex].Operations = CopyOperations(order.Operations);
            }
        }

        foreach(var resource in reference.Worker.Resources)
        {
            var resourceIndex = ind.Worker.Resources.FindIndex(r => r.Name == resource.Name);
            if (resourceIndex > -1) {
                ind.Worker.Resources[resourceIndex].Operations = CopyOperations(resource.Operations);
            }
        }
    }

    private void PrepareSimulation(Individual ind, Individual reference, int skipId = -1)
    {
        skipId = -1;
        Gene<string>? orderGene = ind.Genes.Find(x => x.Name == GeneName.Order) as Gene<string>;
        if (orderGene == null) throw new Exception($"Could not find a gene with gene type {GeneName.Order.ToString()}");

        // Remove finished orders
        if (reference != null)
        {
            Gene<string>? referenceGene = reference.Genes.Find(x => x.Name == GeneName.Order) as Gene<string>;
            var vals = orderGene.Values;
            for (int i = vals.Count - 1; i >= 0; i--)
            {
                if (!referenceGene.Values.Contains(vals[i]))
                {
                    vals.RemoveAt(i);
                    i--;
                }
            }
        }

        InitResources(ind);
        InitOrders(ind, orderGene);
        if (reference != null && skipId != ind.Id) { 
            AddOperationsFromReference(ind, reference);
        }
    }
    #endregion
    #region PUBLIC METHODS
    public void Simulate(List<Individual> individuals, Individual reference, int skipId = -1)
    {
        var sw = new Stopwatch();
        sw.Start();

        if (Config.ParallelExecution)
        {
            Parallel.ForEach(individuals, new ParallelOptions() { MaxDegreeOfParallelism = Config.NumberOfWorkers }, ind =>
            {
                PrepareSimulation(ind, reference, skipId);
                StartSimulationSteps(ind);
            });
        }
        else
        {
            foreach (var ind in individuals)
            {
                PrepareSimulation(ind, reference, skipId);
                StartSimulationSteps(ind);
            }
        }

        sw.Stop();
    }

    public void Simulate(List<Individual> individuals, StreamWriter logWriter)
    {
        throw new NotImplementedException();
    }

    public Operation CopyOperation(Operation otherOperation)
    {
        return new PlantSchedule.DTO.Operation
        {
            Name = otherOperation.Name,
            Unit = otherOperation.Unit,
            Order = otherOperation.Order,
            Duration = otherOperation.Duration,
            Start = otherOperation.Start,
            End = otherOperation.End
        };
    }

    public void EvaluateObjectives(Individual ind)
    {
        // Calculate objectives
        var resources = ind.Worker.Resources;
        var orders = ind.Worker.Orders;

        var tardiness = 0.0;
        var makespan = 0.0;
        var earliness = 0.0;
        var tardinessList = new List<double>();
        var earlinessList = new List<double>();

        var firstResources = ind.Worker.Resources.GetRange(0, 2);
        var lastResources = ind.Worker.Resources.GetRange(14, 3);
        if (Config.EvalAllOrders)
        {
            var orderDict = new Dictionary<string, DateTime>();
            /*
            foreach (var res in firstResources)
            {
                foreach (var op in res.Operations)
                {
                    if (!orderDict.ContainsKey(op.Order))
                    {
                        orderDict[op.Order] = new(op.Start, new DateTime());
                    }
                }
            }
            */
            foreach (var res in lastResources)
            {
                foreach (var op in res.Operations)
                {
                    if (!orderDict.ContainsKey(op.Order) && op.Order.Contains("P") && op.Name.Contains("Process"))
                    {
                        orderDict[op.Order] = op.End;
                    }
                }
            }
            foreach (var order in orderDict)
            {
                var timeSpan = order.Value - DueDates[order.Key];
                if (timeSpan.TotalHours >= 0.0)
                {
                    tardiness += timeSpan.TotalHours;
                    tardinessList.Add(timeSpan.TotalHours);
                }
                if (timeSpan.TotalHours < 0.0)
                {
                    earliness += timeSpan.TotalHours;
                    earlinessList.Add(timeSpan.TotalHours);
                }
                var completionTime = order.Value - ind.Worker.SimulationInstance.SimulationStart; //order.Operations.First().Start;
                if (completionTime.TotalHours > makespan)
                {
                    makespan = completionTime.TotalHours;
                }
                // Console.WriteLine($"{order.Key}:{order.Value}");
            }
            // Console.WriteLine($"tardiness: {tardiness}, earliness: {earliness}, makespan: {makespan}");
        }
        else
        {
            tardiness = 0.0;
            makespan = 0.0;
            earliness = 0.0;
            tardinessList = new List<double>();
            earlinessList = new List<double>();
            foreach (var order in orders)
            {
                var timeSpan = order.Operations.Last().End - order.DueDate;
                if (timeSpan.TotalHours >= 0.0)
                {
                    tardiness += timeSpan.TotalHours;
                    tardinessList.Add(timeSpan.TotalHours);
                }
                if (timeSpan.TotalHours < 0.0)
                {
                    earliness += timeSpan.TotalHours;
                    earlinessList.Add(timeSpan.TotalHours);
                }
                var completionTime = order.Operations.Last().End - ind.Worker.SimulationInstance.SimulationStart; //order.Operations.First().Start;
                if (completionTime.TotalHours > makespan)
                {
                    makespan = completionTime.TotalHours;
                }
                // Console.WriteLine($"{order.Name}:{order.Operations.Last().End}");
            }

            // Console.WriteLine($"tardiness: {tardiness}, earliness: {earliness}, makespan: {makespan}");
        }

        var averageEarliness = earliness / orders.Count;
        var averageTardiness = tardiness / orders.Count;
        var averageLateness = averageTardiness + averageEarliness;
        var lateness = tardiness + earliness;
        var weightedLateness = tardiness + 0.1*earliness;
        var tardinessAvgEarliness = tardiness > 0.0 ? tardiness : averageEarliness;
        var avgTardinessAvgEarliness = averageTardiness > 0.0 ? averageTardiness : averageEarliness;
        var totalAvgTardiness = (tardinessList.Count > 0 ? tardinessList.Average() : 0.0);
        var totalAvgEarliness = (earlinessList.Count > 0 ? earlinessList.Average() : 0.0);
        var totalAverageLateness = earlinessList.Count > 0 ? (tardinessList.Count > 0 ? totalAvgEarliness/tardinessList.Count + earlinessList.Count*totalAvgTardiness : totalAvgEarliness + earlinessList.Count*totalAvgTardiness) : totalAvgTardiness;
        var totalAvgTardinesTotalAvgEarliness = totalAvgTardiness > 0.0 ? totalAvgTardiness : totalAvgEarliness;
        var tardinessEarliness = tardinessList.Count > 0 ? tardinessList.Max() : earlinessList.Max();
        
        // Set objectives
        //if (ind.Worker.InosimInstance.Objectives ) 
        // TODO: Check if reasonable?
        ind.Worker.SimulationInstance.Objectives = new Dictionary<string, double>();
        ind.Worker.SimulationInstance.Objectives.Add("Tardiness", tardiness);
        ind.Worker.SimulationInstance.Objectives.Add("Lateness", lateness);
        ind.Worker.SimulationInstance.Objectives.Add("TotalAverageLateness", totalAverageLateness);
        ind.Worker.SimulationInstance.Objectives.Add("AverageLateness", averageLateness);
        ind.Worker.SimulationInstance.Objectives.Add("WeightedLateness", weightedLateness);
        ind.Worker.SimulationInstance.Objectives.Add("AverageEarliness", averageEarliness);
        ind.Worker.SimulationInstance.Objectives.Add("AverageTardiness", averageTardiness);
        ind.Worker.SimulationInstance.Objectives.Add("TardinessAvgEarliness", tardinessAvgEarliness);
        ind.Worker.SimulationInstance.Objectives.Add("AverageTardinessAverageEarliness", avgTardinessAvgEarliness);
        ind.Worker.SimulationInstance.Objectives.Add("TotalAvgTardiness", totalAvgTardiness);
        ind.Worker.SimulationInstance.Objectives.Add("TotalAvgEarliness", totalAvgEarliness);
        ind.Worker.SimulationInstance.Objectives.Add("TotalAvgEarlinessTotalAvgTardiness", totalAvgTardinesTotalAvgEarliness);
        ind.Worker.SimulationInstance.Objectives.Add("TardinessEarliness", tardinessEarliness);
        ind.Worker.SimulationInstance.Objectives.Add("Makespan", makespan);

        // Set fitness
        ind.FitnessName = ((CSharpConfig)Config).Objective;
        // TODO: Check if reasonable?
        ind.Fitness = new Dictionary<string, double>();
        ind.Fitness.Add(ind.FitnessName, ind.Worker.SimulationInstance.Objectives[ind.FitnessName]);

        // Serialize worker
        // ind.Json = JsonSerializer.Serialize(ind.Worker);
    }

    public void Simulate(string json)
    {
        throw new NotImplementedException();
    }
     
    public List<PlantSchedule.DTO.Order> CopyOrders(List<PlantSchedule.DTO.Order> otherOrders)
    {
        List<PlantSchedule.DTO.Order> orders = new List<PlantSchedule.DTO.Order>();
        for (int i = 0; i < otherOrders.Count(); i++)
        {
            orders.Add(new PlantSchedule.DTO.Order
            {
                DueDate = otherOrders[i].DueDate,
                Finished = otherOrders[i].Finished,
                Id = GetHashCode().ToString(),
                Name = otherOrders[i].Name,
                Operations = CopyOperations(otherOrders[i].Operations),
                Recipe = otherOrders[i].Recipe,
                Started = otherOrders[i].Started
            });
        }
        return orders;
    }

    public List<PlantSchedule.DTO.Order> CreateOrders(Gene<String> orderGene)
    {
        List<PlantSchedule.DTO.Order> orders = new List<PlantSchedule.DTO.Order>();
        for (int i = 0; i < orderGene.Values.Count(); i++)
        {
            orders.Add(new PlantSchedule.DTO.Order
            {
                DueDate = DueDates[orderGene.Values[i]],
                Finished = false,
                Id = GetHashCode().ToString(),
                Name = orderGene.Values[i],
                Operations = new List<PlantSchedule.DTO.Operation>(),
                Recipe = OrdersRecipeNoAlloc[orderGene.Values[i].Split("_")[0]],
                Started = false
            });
        }
        return orders;
    }

    public List<Resource> CopyResources(List<Resource> otherResources)
    {
        List<PlantSchedule.DTO.Resource> resources = new List<PlantSchedule.DTO.Resource>();
        for (int i = 0; i < otherResources.Count(); i++)
        {
            resources.Add(new PlantSchedule.DTO.Resource
            {
                Id = GetHashCode().ToString(),
                Name = otherResources[i].Name,
                Priority = otherResources[i].Priority, //TODO: Copy priority as well
                Operations = CopyOperations(otherResources[i].Operations),
                Maintenance = otherResources[i].Maintenance.ToList(),
                Allocate = otherResources[i].Allocate.ToDictionary(),
                IdleTime = otherResources[i].IdleTime,
            });
        }
        return resources;
    }

    public void UpdateSimulator(Worker tempWorker)
    {
        // TODO: Add release dates 
        // Maybe initialize the first operation of the order with idle startdate = releasedate
        Orders = new();
        DueDates = new();
        foreach (var order in tempWorker.Orders)
        {
            Orders.Add(order.Name);
            DueDates.Add(order.Name, order.DueDate);
        }
        if(Config.RushOrders != null && Config.RushOrders.Count > 0)
        {
            foreach (var rushOrder in Config.RushOrders)
            {
                if(!DueDates.ContainsKey(rushOrder.Key))
                {
                    // /Orders.Add(rushOrder.Key);
                    DueDates[rushOrder.Key] = tempWorker.SimulationInstance.SimulationStart.AddHours(rushOrder.Value[1]);
                }
            }
        }
    }
    public void SetDueDate(string order)
    {
        var rand = new Random();
        DueDates[order] = Config.SimulationStart.AddHours(rand.Next((int)(20), (int)(40)));
    }

    public void SetDueDate(Dictionary<string,int> orderDueDates)
    {
        foreach (var item in orderDueDates)
        {
            DueDates[item.Key] = Config.SimulationStart.AddHours(item.Value);
        }
    }
#endregion
    #region PRIVATE METHODS
    private Dictionary<(string,string), double> ReadCsvFile(string file)
    {
        Dictionary<(string, string), double> csvData = new Dictionary<(string, string), double>();
        var objectName = file.Split('.')[0];
        try
        {
            using (var sr = new StreamReader(file))
            {
                string line;
                int count = 0;
                List<string> header = new List<string>();
                while ((line = sr.ReadLine()) != null)
                {
                    if (count == 0)
                    {
                        header = line.Split(',').ToList();
                        count++;
                        continue;
                    }

                    var vals = line.Split(',');
                    string index = "";
                    for (int i = 0; i < vals.Length; i++)
                    {
                        if (i == 0)
                        {
                            index = vals[i];
                            continue;
                        }

                        if (!string.IsNullOrEmpty(index) && !string.IsNullOrEmpty(header[i]))
                        {
                            var key = (index, header[i]);
                            var val = 0.0;
                            try
                            {
                                val = Double.Parse(vals[i]);
                            }
                            catch (Exception)
                            {
                                throw new Exception($"Could not parse {vals[i]} in line {i} in {file}.");
                            }

                            csvData.Add(key, val);

                        }
                    }
                }
            }
        }
        catch
        {
            throw new FileNotFoundException(file);
        }
        return csvData;
    }
    private List<Resource> FindAllocatableResource(List<Resource> resources, string order, string stage)
    {
        var allocatableResources = new List<Resource>();
        if (StageMachineMap.ContainsKey(stage))
        {

            foreach (var machine in StageMachineMap[stage])
            {
                var orderName = order.Split("_").First();
                if (this.ProcessTimes[(orderName, machine)] > 0.0)
                {
                    allocatableResources.Add(resources.Find(x => x.Name == machine));
                }
            }
        }
        return allocatableResources;
            
    }
    private (int, Resource) FindBestResourceWithOperationIndex(List<Resource> resources, Order order)
    {
        // TODO: Add feature that opeartions can be added in gaps
        // [ op1, gap1, ch2, op2, gap2, ch2, op2]
        var bestEnd = this.SimulationEnd.AddDays(10); // just a margin of safety
        var bestRes = resources[0];
        var resourceEnd = new DateTime();
        var orderEnd = order.Operations.Last().End;
        var bestOperation = -1;

        // TODO: Account for changeover times that are already there.
        // TODO: P18_00 is twice in the gantt -> because it was finished and reallocated
        foreach (var resource in resources)
        {
            var gapIndex = resource.Operations.Count - 1;
            var orderName = order.Name.Split("_").First();
            var changeoverTime = GetChangeover(resource, orderName);
            var processTime = ProcessTimes[(orderName, resource.Name)];

            // Search for all the gaps that are feasible 
            var gaps = new Dictionary<int,double>();
            for (int i = 0; i < resource.Operations.Count - 1; i++)
            {
                var op1 = resource.Operations[i];
                var op2 = resource.Operations[i + 1];
                var totalProcessTime = changeoverTime + processTime;
                if (op1.Order == op2.Order) // Make sure that it is no chanvover or cleaning
                {
                    continue;
                }
                var gap = op2.Start.Subtract(op1.End).TotalHours;
                if (gap == 0.0) continue;

                // Since it already is a changeover 
                if(op1.Order == order.Name && op1.Name.Contains("Changeover"))
                {
                    totalProcessTime -= changeoverTime;
                }
                
                if(this.SimulationStart > new DateTime(2020, 1, 1, 13, 20, 00) && order.Name == "P05_Rush" && resource.Name == "M03")
                {
                    Console.WriteLine();
                }

                if (op1.End >= order.Operations.Last().End && // check if last operation of order ends to the right time
                    op2.Start >= op1.End.AddHours(totalProcessTime)) // check if last operation of order fits into the gap
                {

                    gaps.Add(i, gap);
                }
            }

            if (gaps.Count > 0) gapIndex = gaps.OrderByDescending(kvp => kvp.Value).First().Key;
            resourceEnd = resource.Operations[gapIndex].End;
            
            // Here we can fill spots in the schedule
            if (false && resource.Operations[gapIndex].Name.Contains("Changeover") && resource.Operations.Last().Order != order.Name)
            {
                // since the changeover started before the simulation and carrys on
                // basically the changeover is preempted
                resourceEnd = SimulationStart;
            }

            if (resource.Operations[gapIndex].Name.Contains("Changeover") && resource.Operations[gapIndex].Order == order.Name)
            {
                changeoverTime = 0.0;
            } 

            var end = (resourceEnd.AddHours(changeoverTime) > orderEnd) switch
            {
                true => resourceEnd.AddHours(processTime + changeoverTime),
                false => orderEnd.AddHours(processTime)
            };

            if(end <= bestEnd)
            {
                bestEnd = end;
                bestRes = resource;
                bestOperation = gapIndex;
            }
        }

        return (bestOperation, bestRes);
    }
    private double GetChangeover(Resource resource, string order)
    {
        var changeoverTime = 0.0;
        if(resource.Operations.Last().Order != "")
        {
            var stage = "";
            try
            {
                stage = StageMachineMap.First(kvp => kvp.Value.Contains(resource.Name)).Key;
            }
            catch (Exception)
            {
                stage = resource.Operations.Last().Name.Split("_").Last();
            }
            //var stage = [resource.Name];
            var lastOrder = resource.Operations.Last().Order.Split("_").First();
            order = order.Split("_").First();
            switch (stage)
            {
                case "S1":
                    changeoverTime = ChangeoverTimes_S1[(order, lastOrder)];
                    break;
                case "S2":
                    changeoverTime = ChangeoverTimes_S2[(order, lastOrder)];
                    break;
                case "S3":
                    changeoverTime = ChangeoverTimes_S3[(order, lastOrder)];
                    break;
                case "S4":
                    changeoverTime = ChangeoverTimes_S4[(order, lastOrder)];
                    break;
                case "S5":
                    changeoverTime = ChangeoverTimes_S5[(order, lastOrder)];
                    break;
                case "S6":
                    changeoverTime = ChangeoverTimes_S6[(order, lastOrder)];
                    break;
                default:
                    break;
            }
        }
        return changeoverTime;
    }
    private void StepAfterChangeover(Order order, List<Resource> resources, string stage)
    {
        // get the current operation
        var operation = order.Operations.Last();

        // find the resource the last opeartion was on
        var resource = resources.Find(x => x.Name == order.Operations.Last().Unit);

        // find the earliest possible start and end. Should be of all operations because some operations could theoretically end later
        var orderEnd = order.Operations.Max(x => x.End);

        //var orderEnd = order.Operations.Last().End;
        var resourceEnd = resource.Operations.Last().End;
        var start = (resourceEnd > orderEnd) switch
        {
            true => resourceEnd,
            false => orderEnd
        };
        var duration = ProcessTimes[(order.Name.Split("_").First(), resource.Name)];

        // add operation to order and resource
        var nextOperation = new Operation
        {
            Name = "Process_" + operation.Name.Split("_")[1],
            Duration = duration,
            Start = start,
            End = start.AddHours(duration),
            Order = order.Name,
            Unit = resource.Name
        };

        resource.Operations.Add(nextOperation);
        order.Operations.Add(nextOperation);

    }

    private void Step(Order order, List<Resource> resources, string stage)
    {
        // Only go in the wait state of the stage if it needs to be processed on this stage. Otherwise skip stage
        var allocatableResources = FindAllocatableResource(resources, order.Name, stage);

        if (allocatableResources.Count > 0)
        {
            var (bestOperationIdx, bestResource) = FindBestResourceWithOperationIndex(allocatableResources, order);
            /*
            if (allocations.ContainsKey(order.Name))
            {
                var allocList = allocations[order.Name];
                foreach (var item in allocatableResources)
                {
                    if (allocList.Contains(item.Name))
                    {
                        bestResource = item;
                        break;
                    }
                }
            }
            */
            var changeoverTime = GetChangeover(bestResource, order.Name.Split("_").First());

            var resourceEnd = bestResource.Operations[bestOperationIdx].End;
            var orderEnd = order.Operations.Last().End;

            // Check if the last operation of the best resource is a changeover. Overwrite it then. Or start at 
            if(changeoverTime > 0.0)
            {
                if (bestResource.Operations.Last().Name.Contains("Changeover"))
                {
                    // First change the last opeartion time 
                    bestResource.Operations.Last().End = SimulationStart;
                    resourceEnd = SimulationStart;

                } 
                var changeoverOperation = new Operation
                {
                    Name = "Changeover_" + stage,
                    Duration = changeoverTime,
                    Start = resourceEnd,
                    End = resourceEnd.AddHours(changeoverTime),
                    Order = order.Name,
                    Unit = bestResource.Name
                };
                bestResource.Operations.Insert(++bestOperationIdx, changeoverOperation);
                // bestOperationIdx++;
                order.Operations.Add(changeoverOperation);
                // update end of resource
                resourceEnd = bestResource.Operations[bestOperationIdx].End;
                
            }

            var start = (resourceEnd > orderEnd) switch
            {
                true => resourceEnd,
                false => orderEnd
            };
            
            var duration = ProcessTimes[(order.Name.Split("_").First(), bestResource.Name)];
            var nextOperation = new Operation
            {
                Name = "Process_" + stage,
                Duration = duration,
                Start = start,
                End = start.AddHours(duration),
                Order = order.Name,
                Unit = bestResource.Name
            };

            // Shift the changeover to the last possible start time
            if (bestResource.Operations[bestOperationIdx].Name.Contains("Changeover"))
            {
                bestResource.Operations[bestOperationIdx].End = nextOperation.Start;
                bestResource.Operations[bestOperationIdx].Start = bestResource.Operations[bestOperationIdx].End.AddHours(-bestResource.Operations.Last().Duration);
            }

            bestResource.Operations.Insert(++bestOperationIdx, nextOperation);
            order.Operations.Add(nextOperation);
        }
        else
        {
            if(stage == "End")
            {
                var nextOperation = new Operation
                {
                    Name = "End_" + stage,
                    Duration = 0.0,
                    Start = order.Operations.Last().End,
                    End = order.Operations.Last().End,
                    Order = order.Name,
                    Unit = ""
                };
                order.Operations.Add(nextOperation);
            }
        }
    }
    private string FindNextStage(string stage, string order)
    {
        if (stage.Contains("End"))
        {
            return "End";
        }
        // if we order according to the realease dates it might happen that this order is not execute because we never reach the time 
        // maybe if we elongat the idle time of the resources to the min time relese date of the orders
        var stages = this.StageMachineMap.Keys.ToList();
        var nextStageIndex = stages.FindIndex(x => x == stage);

        var nextStage = "End";
        if(nextStageIndex < this.StageMachineMap.Count - 1)
        {
            nextStage = stages[nextStageIndex + 1];
        }
        else if (nextStageIndex == -1) 
        {
            nextStage = stages[0];
        }
        else
        {
            return nextStage;
        }

        bool isAllocatable = false;
        foreach (var machine in StageMachineMap[nextStage])
        {
            var orderName = order.Split("_").First();
            var processTime = ProcessTimes[(orderName, machine)];
            if (processTime > 0.0) isAllocatable = true;
        }

        if (!isAllocatable)
        {
            return FindNextStage(nextStage, order);
        }
        return nextStage;
    }
    public List<PlantSchedule.DTO.Operation> CopyOperations(List<PlantSchedule.DTO.Operation> otherOperations)
    {
        List<PlantSchedule.DTO.Operation> operations = new List<PlantSchedule.DTO.Operation>();
        for (int i = 0; i < otherOperations.Count(); i++)
        {
            operations.Add(CopyOperation(otherOperations[i]));
            /*
            new PlantSchedule.DTO.Operation
            {
                Name = otherOperations[i].Name,
                Unit = otherOperations[i].Unit,
                Order = otherOperations[i].Order,
                Duration = otherOperations[i].Duration,
                Start = otherOperations[i].Start,
                End = otherOperations[i].End

            };
            */
        }
        return operations;
    }
    #endregion
}
#if KOPANOS_SIMULATOR
public class KopanosSimulator : ISimulator
{
    public Worker[] Worker;
    public KopanosConfig Config;
    public InosimInstance insoimInstanceService;
    public DateTime SimulationStart { get; set; }
    public DateTime SimulationEnd { get; set; }
    private Dictionary<string, string> OrdersRecipeNoAlloc = new Dictionary<string, string>()
    {
        { "P01", "R01" },
        { "P02", "R02" },
        { "P03", "R03" },
        { "P04", "R02" },
        { "P05", "R03" },
        { "P06", "R04" },
        { "P07", "R02" },
        { "P08", "R05" },
        { "P09", "R02" },
        { "P10", "R02" },
        { "P11", "R02" },
        { "P12", "R06" },
        { "P13", "R06" },
        { "P14", "R04" },
        { "P15", "R04" },
        { "P16", "R01" },
        { "P17", "R07" },
        { "P18", "R02" },
        { "P19", "R02" },
        { "P20", "R05" },
        { "P21", "R08" },
        { "P22", "R02" },
        { "P23", "R05" },
        { "P24", "R09" },
        { "P25", "R10" },
        { "P26", "R05" },
        { "P27", "R09" },
        { "P28", "R02" },
        { "P29", "R03" },
        { "P30", "R10" },
        { "P31", "R01" },
        { "P32", "R02" },
        { "P33", "R03" },
        { "P34", "R02" },
        { "P35", "R03" },
        { "P36", "R04" },
        { "P37", "R02" },
        { "P38", "R05" },
        { "P39", "R02" },
        { "P40", "R02" }
    };
    public readonly Dictionary<String, List<String>> StageMachineMap = new Dictionary<string, List<string>>()
    {
        { "S1", new List<string>() { "M01", "M02" } },
        { "S2", new List<string>() { "M03", "M04", "M05" } },
        { "S3", new List<string>() { "M06", "M07", "M08" } },
        { "S4", new List<string>() { "M09", "M10", "M11" } },
        { "S5", new List<string>() { "M12", "M13", "M14" } },
        { "S6", new List<string>() { "M15", "M16", "M17" } },
    };
    public readonly Dictionary<String, Dictionary<String, List<String>>> RecipeMachineMap =
        new Dictionary<string, Dictionary<string, List<string>>>();
    public List<string> Orders { get; set; } = new List<string>();
    public Dictionary<String, DateTime> DueDates = new Dictionary<string, DateTime>();
    public KopanosSimulator(IConfig config)
    {
        Config = (KopanosConfig)config;
        Worker = InitializeWorker(new Worker[Config.NumberOfWorkers]);
        //var recipesMachineJson = File.ReadAllText(@"C:\Users\smenpasi\Projects\PlantSchedule\PlantSchedule.Models\recipes.txt");
        var recipesMachineJson = File.ReadAllText(@"JsonFiles\recipemachines.json");
        RecipeMachineMap = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string,List<string>>>>(recipesMachineJson);
        var dueDates = new Dictionary<string, DateTime>();
        var rand = new Random();
        // Initialize the genome
        for (int i = 1; i <= 30; i++)
        {
            var orderName = "P" + i.ToString("D2");
            Orders.Add(orderName);
            DueDates.Add(orderName, Config.SimulationStart.AddHours(rand.Next(20, 35)));
        }
    }
    public void UpdateSimulator(Worker tempWorker)
    {
        Orders = new();
        DueDates = new();
        foreach (var order in tempWorker.Orders)
        {
            Orders.Add(order.Name);
            DueDates.Add(order.Name, order.DueDate);
        }
    }
    private void SetInosimInstance(Individual individual, int i)
    {
        individual.Worker.InosimInstance =  new InosimInstance()
        {
            Name = Config.ResultFolder + "worker" + i.ToString("D2"),
            Database = Config.Databases[i],
            Experiment = Config.Experiment,
            Project = Config.Project,
            License = Config.Edition, //"ExpertEdition",
            Version = Config.Version,
            Visibility = Config.Visibility,
            Objectives = Config.Objectives
        };
        individual.Worker.Name = $"Worker{i.ToString("D2")}";
    }
    public InosimInstance GetInosimInstance()
    {
        return new InosimInstance()
        {
            Name = Config.ResultFolder + "worker" + 0.ToString("D2"),
            Database = Config.Databases[0],
            Experiment = Config.Experiment,
            Project = Config.Project,
            License = Config.Edition, //"ExpertEdition",
            Version = Config.Version,
            Visibility = Config.Visibility,
            Objectives = Config.Objectives
        };
    }
    private PlantSchedule.DTO.Worker[] InitializeWorker(Worker[] worker)
    {
        for (int i = 0; i < Config.NumberOfWorkers; i++)
        {
            //Allocate worker
            worker[i] = new PlantSchedule.DTO.Worker()
            {
                Name = $"Worker{i.ToString("D2")}",
                Idle = false,
                Id = GetHashCode().ToString(),
                Orders = new List<PlantSchedule.DTO.Order>()
            };
            //Set inosim instance/
            worker[i].InosimInstance = new PlantSchedule.DTO.InosimInstance()
            {
                Name = Config.ResultFolder + "worker" + i.ToString("D2"),
                Database = Config.Databases[i],
                Experiment = Config.Experiment,
                Project = Config.Project,
                License = Config.Edition, //"ExpertEdition",
                Version = Config.Version,
                Visibility = Config.Visibility,
                Objectives = Config.Objectives
            };
        }
        return worker;
    }
    public void Simulate(string json)
    {
        var inosimInstanceService = new PlantSchedule.API.Services.InosimInstancesService();
        dynamic inosimInstance =
            inosimInstanceService.CreateParameterizedSimulationInstance(GetInosimInstance(), json);
        var t = Task.Run(() => inosimInstance.RunSimulation());
        Task.WaitAll(t);
    }
    public void Simulate(List<IIndividual> individual, StreamWriter logWriter, dynamic simulationService, DateTime simulationStart)
    {
        throw new NotImplementedException();
        /*
        var inosimInstanceService = simulationService; // PlantSchedule.API.Services.InosimInstancesService();
        var tasks = new List<Task>();
        // Prepare workers
        var simulationEnd = simulationStart.AddHours(24);

        var t = individual.Count / Config.NumberOfWorkers;
        var r = individual.Count % Config.NumberOfWorkers;
        if (r > 0) t += 1;

        for (int i = 0; i < t; i++)
        {
            var numberOfWorker = 0;
            for (int j = 0; j < Config.NumberOfWorkers; j++)
            {
                var k = i * Config.NumberOfWorkers + j;
                if (k < individual.Count)
                {
                    numberOfWorker += 1;
                    SetInosimInstance((Individual)individual[k], j);
                    var worker = ((Individual)individual[k]).Worker;
                    // get orders
                    var orderGene = (Gene<string>)individual[k].Genes.Find(x => x.Name == GeneName.Order);
                    worker.Orders = new List<Order>(CreateOrders(orderGene));
                    worker.InosimInstance.SimulationStart = simulationStart;
                    worker.InosimInstance.SimulationEnd = simulationEnd;
                    var workerJson = SerializeWorkerAndWriteToFile(worker);
                    dynamic inosimInstance =
                        inosimInstanceService.CreateParameterizedSimulationInstance(worker.InosimInstance,
                            workerJson);
                    var logMessage = $"[INFO] PROCESS id {inosimInstance.ProcessID}\t{worker.Name} {worker
                        .InosimInstance.Database.Split("\\")[^1]}";
                    logWriter.WriteLine(logMessage);
                    Console.WriteLine(logMessage);

                    //Run simulation
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            return inosimInstance.RunSimulation();
                        }
                        catch (Exception e)
                        {
                            var logMessage =
                                $"[ERROR] Simulation \"{worker.InosimInstance.Database}\" failed\n" + e;
                            logWriter.WriteLine(logMessage);
                            Console.WriteLine(logMessage);
                            if (!worker.InosimInstance.Objectives.ContainsKey("FAILED"))
                                worker.InosimInstance.Objectives.Add("FAILED", -1);
                            return null;
                        }
                    }));
                }
                else
                {
                    // If all worker are full or if no more individuals are available ev
                    break;
                }
            }

            // Wait for results
            Timer timer = new Timer()
            {
                Interval = 1000 * 3600,
                AutoReset = false,
                Enabled = true
            };
            timer.Elapsed += (sender, args) =>
            {
                var logMessage = "[WARNING] Timer elapsed. Kill processes with name Inosim";
                logWriter.WriteLine(logMessage);
                Console.WriteLine(logMessage);
                new Process()
                    {
                        StartInfo = new ProcessStartInfo("powershell.exe", """Stop-Process -Name "Inosim" """)
                    }
                    .Start();
                new Process()
                {
                    StartInfo = new ProcessStartInfo("powershell.exe", """Stop-Process -Name "Inosim.ui" """)
                }.Start();
            };
            Task.WaitAll(tasks.ToArray());

            // Kill timer
            timer.Dispose();

            // Evaluate individual
            var individualStart = i * Config.NumberOfWorkers;
            var individualEnd = individualStart + numberOfWorker;
            EvaluateIndividuals(individual[individualStart..individualEnd], logWriter);
            //EvaluateIndividuals(Worker[0..numberOfWorker]);

            // Write results to individuals
        }
        */
    }
    public void Simulate(List<IIndividual> individual, StreamWriter logWriter, dynamic simulationService)
    {
        throw new NotImplementedException();
        /*
        var inosimInstanceService = simulationService; // PlantSchedule.API.Services.InosimInstancesService();
        var tasks = new List<Task>();
        // Prepare workers
        var simulationStart = DateTime.Now;
        var simulationEnd = simulationStart.AddHours(24);

        var t = individuals.Count / Config.NumberOfWorkers;
        var r = individuals.Count % Config.NumberOfWorkers;
        if (r > 0) t += 1;

        for (int i = 0; i < t; i++)
        {
            var numberOfWorker = 0;
            for (int j = 0; j < Config.NumberOfWorkers; j++)
            {
                var k = i * Config.NumberOfWorkers + j;
                if (k < individuals.Count)
                {
                    numberOfWorker += 1;
                    SetInosimInstance((Individual)individuals[k], j);
                    var worker = ((Individual)individuals[k]).Worker;
                    // get orders
                    var orderGene = (Gene<string>)individuals[k].Genes.Find(x => x.Name == GeneName.Order);
                    worker.Orders = new List<Order>(CreateOrders(orderGene));
                    worker.InosimInstance.SimulationStart = simulationStart;
                    worker.InosimInstance.SimulationEnd = simulationEnd;
                    var workerJson = SerializeWorkerAndWriteToFile(worker);
                    dynamic inosimInstance =
                        inosimInstanceService.CreateParameterizedSimulationInstance(worker.InosimInstance,
                            workerJson);
                    var logMessage = $"[INFO] PROCESS id {inosimInstance.ProcessID}\t{worker.Name} {worker
                        .InosimInstance.Database.Split("\\")[^1]}";
                    logWriter.WriteLine(logMessage);
                    Console.WriteLine(logMessage);

                    //Run simulation
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            return inosimInstance.RunSimulation();
                        }
                        catch (Exception e)
                        {
                            var logMessage =
                                $"[ERROR] Simulation \"{worker.InosimInstance.Database}\" failed\n" + e;
                            logWriter.WriteLine(logMessage);
                            Console.WriteLine(logMessage);
                            if (!worker.InosimInstance.Objectives.ContainsKey("FAILED"))
                                worker.InosimInstance.Objectives.Add("FAILED", -1);
                            return null;
                        }
                    }));
                }
                else
                {
                    // If all worker are full or if no more individuals are available ev
                    break;
                }
            }

            // Wait for results
            Timer timer = new Timer()
            {
                Interval = 1000 * 3600,
                AutoReset = false,
                Enabled = true
            };
            timer.Elapsed += (sender, args) =>
            {
                var logMessage = "[WARNING] Timer elapsed. Kill processes with name Inosim";
                logWriter.WriteLine(logMessage);
                Console.WriteLine(logMessage);
                new Process()
                    {
                        StartInfo = new ProcessStartInfo("powershell.exe", """Stop-Process -Name "Inosim" """)
                    }
                    .Start();
                new Process()
                {
                    StartInfo = new ProcessStartInfo("powershell.exe", """Stop-Process -Name "Inosim.ui" """)
                }.Start();
            };
            Task.WaitAll(tasks.ToArray());

            // Kill timer
            timer.Dispose();

            // Evaluate individual
            var individualStart = i * Config.NumberOfWorkers;
            var individualEnd = individualStart + numberOfWorker;
            EvaluateIndividuals(individuals[individualStart..individualEnd], logWriter);
            //EvaluateIndividuals(Worker[0..numberOfWorker]);

            // Write results to individuals
        }
        */
    }
    private void AddOrdersToWorker(Individual individual, DateTime simulationStart, DateTime simulationEnd)
    {
            // get orders
            var orderGene = (Gene<string>)individual.Genes.Find(x => x.Name == GeneName.Order);
            for (int i = 0; i < individual.Worker.Orders.Count; i++)
            {
                if (!individual.Worker.Orders[i].Started)
                {
                    individual.Worker.Orders.RemoveAt(i);
                    i -= 1;
                }
                
            }
            individual.Worker.Orders.AddRange(new List<Order>(CreateOrders(orderGene)));
            if (this.DueDates != null || this.DueDates.Count > 0)
            {
                foreach (var order in individual.Worker.Orders)
                {
                    if (this.DueDates.ContainsKey(order.Name))
                    {
                        order.DueDate = this.DueDates[order.Name];
                    }
                }
            }
            individual.Worker.InosimInstance.SimulationStart = simulationStart;
            individual.Worker.InosimInstance.SimulationEnd = simulationEnd;
    }
    public void Simulate(List<Individual> individuals, StreamWriter logWriter, DateTime simulationStart)
    {
        var inosimInstanceService = new PlantSchedule.API.Services.InosimInstancesService();
        var tasks = new List<Task>();
        // Prepare workers
        //var simulationStart = DateTime.Now;
        var simulationEnd = simulationStart.AddHours(24);

        var t = individuals.Count / Config.NumberOfWorkers; // Individuals per worker
        var r = individuals.Count % Config.NumberOfWorkers; // Individauals left
        if (r > 0) t += 1;

        for (int i = 0; i < t; i++)
        {
            var numberOfWorker = 0;
            for (int j = 0; j < Config.NumberOfWorkers; j++)
            {
                var k = i * Config.NumberOfWorkers + j;
                if (k < individuals.Count)
                {
                    numberOfWorker += 1;
                    var individual = (Individual)individuals[k];
                    SetInosimInstance(individual, j);
                    AddOrdersToWorker(individual, simulationStart, simulationEnd);
                    var workerJson = SerializeWorkerAndWriteToFile(individual.Worker);
                    dynamic inosimInstance =
                        inosimInstanceService.CreateParameterizedSimulationInstance(individual.Worker.InosimInstance,
                            workerJson);
                    var logMessage = $"[INFO] PROCESS id {inosimInstance.ProcessID}\t{individual.Worker.Name} {individual.Worker
                        .InosimInstance.Database.Split("\\")[^1]}";
                    logWriter.WriteLine(logMessage);
                    Console.WriteLine(logMessage);

                    //Run simulation
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            return inosimInstance.RunSimulation();
                        }
                        catch (Exception e)
                        {
                            var logMessage = 
                                $"[ERROR] Simulation \"{individual.Worker.InosimInstance.Database}\" failed\n" + e;
                            logWriter.WriteLine(logMessage);
                            Console.WriteLine(logMessage);
                            if (!individual.Worker.InosimInstance.Objectives.ContainsKey("FAILED"))
                                individual.Worker.InosimInstance.Objectives.Add("FAILED", -1);
                            return null;
                        }
                    }));
                }
                else
                {
                    // If all worker are full or if no more individualss are available ev
                    break;
                }
            }
            
            // Wait for results
            Timer timer = new Timer()
            {
                Interval = 1000 * 3600,
                AutoReset = false,
                Enabled = true
            };
            timer.Elapsed += (sender, args) =>
            {
                var logMessage = "[WARNING] Timer elapsed. Kill processes with name Inosim";
                logWriter.WriteLine(logMessage);
                Console.WriteLine(logMessage);
                new Process()
                    {
                        StartInfo = new ProcessStartInfo("powershell.exe", """Stop-Process -Name "Inosim" """)
                    }
                    .Start();
                new Process()
                {
                    StartInfo = new ProcessStartInfo("powershell.exe", """Stop-Process -Name "Inosim.ui" """)
                }.Start();
            };
            Task.WaitAll(tasks.ToArray());

            // Kill timer
            timer.Dispose();
    
            // Evaluate individuals
            var individualsStart = i * Config.NumberOfWorkers;
            var individualsEnd = individualsStart + numberOfWorker;
            EvaluateIndividuals(individuals[individualsStart..individualsEnd], logWriter);
            //EvaluateIndividuals(Worker[0..numberOfWorker]);
            
            // Write results to individualss
        }
    }
    public void Simulate(List<Individual> individuals, StreamWriter logWriter)
    {
        var inosimInstanceService = new PlantSchedule.API.Services.InosimInstancesService();
        var tasks = new List<Task>();
        // Prepare workers
        var simulationStart = this.SimulationStart; // DateTime.Now;
        var simulationEnd = this.SimulationEnd; // simulationStart.AddHours(24);

        var t = individuals.Count / Config.NumberOfWorkers; // Individuals per worker
        var r = individuals.Count % Config.NumberOfWorkers; // Individauals left
        if (r > 0) t += 1;

        UpdateIndividuals((List<Individual>)individuals);
        // Find the orders and operations that are fixed
        if (individuals[0].Fitness.Count > 0)
        {
            var fixedOrders = individuals[0].Worker.Orders.Where(x => x.Started == true).Select(x => x.Name)
                .ToList();
            var openOrders = individuals[0].Worker.Orders.Where(x => x.Started == false).Select(x => x.Name)
                .ToList();
        }
        /*
        if (individuals[0].Fitness.Count > 0)
        {

            // copy fixed orders of best individual to other individuals!!!
            foreach(var order in ((Individual)individuals[0]).Worker.Orders)
            {
                
            }
            var fixedOrders = new Dictionary<string, int>();
            var finishedOrders = new Dictionary<string, int>();
            foreach (var order in ((Individual)individuals[0]).Worker.Orders)
            {
                var idx = order.Operations.FindLastIndex(x => x.Start <= simulationStart);
                if (idx > -1)
                {
                    fixedOrders.Add(order.Name, idx);
                    Console.WriteLine($"[INFO] Order {order.Name} has operation {order.Operations[idx].Name} that is fixed");
                    Console.WriteLine($"[INFO] Start: {order.Operations[idx].Start}, End: {order.Operations[idx].End}");
                }
                // orders to remove
                if (idx == order.Operations.Count - 1 && order.Operations[idx - 1].End <= simulationStart)
                {
                    finishedOrders.Add(order.Name, idx);
                    Console.WriteLine($"[INFO] Order {order.Name} has finished");
                }
            }
            foreach(var ind in individuals)
            {
                ((Gene<string>)ind.Genes[0]).Values.RemoveAll(x => fixedOrders.ContainsKey(x) || finishedOrders.ContainsKey(x));
                ((Gene<string>)ind.Genes[0]).Values.InsertRange(0, fixedOrders.Keys);
            //    ind.Genes[0].
            //    ind.Genes.OrderBy(x => fixedOrders.Keys.ToList().IndexOf(x));
            }
        }
        */
        // TODO: remove orders from orderGene if fixed or finished !!!
        for (int i = 0; i < t; i++)
        {
            var numberOfWorker = 0;
            for (int j = 0; j < Config.NumberOfWorkers; j++)
            {
                var k = i * Config.NumberOfWorkers + j;
                if (k < individuals.Count)
                {
                    numberOfWorker += 1;
                    SetInosimInstance((Individual)individuals[k], j);
                    var worker = ((Individual)individuals[k]).Worker;
                    // get orders
                    var orderGene = (Gene<string>)individuals[k].Genes.Find(x => x.Name == GeneName.Order);
                    // overwrites the whole list!!! needs to be changed!!!
                    DecodeGenome(worker, orderGene);    
                    //worker.Orders = new List<Order>(CreateOrders(orderGene));
                    worker.InosimInstance.SimulationStart = simulationStart;
                    worker.InosimInstance.SimulationEnd = simulationEnd;
                    var workerJson = SerializeWorkerAndWriteToFile(worker);
                    dynamic inosimInstance =
                        inosimInstanceService.CreateParameterizedSimulationInstance(worker.InosimInstance,
                            workerJson);
                    var logMessage = $"[INFO] PROCESS id {inosimInstance.ProcessID}\t{worker.Name} {worker
                        .InosimInstance.Database.Split("\\")[^1]}";
                    logWriter.WriteLine(logMessage);
                    Console.WriteLine(logMessage);

                    //Run simulation
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            return inosimInstance.RunSimulation();
                        }
                        catch (Exception e)
                        {
                            var logMessage = 
                                $"[ERROR] Simulation \"{worker.InosimInstance.Database}\" failed\n" + e;
                            logWriter.WriteLine(logMessage);
                            Console.WriteLine(logMessage);
                            if (!worker.InosimInstance.Objectives.ContainsKey("FAILED"))
                                worker.InosimInstance.Objectives.Add("FAILED", -1);
                            return null;
                        }
                    }));
                }
                else
                {
                    // If all worker are full or if no more individuals are available ev
                    break;
                }
            }
            
            // Wait for results
            Timer timer = new Timer()
            {
                Interval = 1000 * 3600,
                AutoReset = false,
                Enabled = true
            };
            timer.Elapsed += (sender, args) =>
            {
                var logMessage = "[WARNING] Timer elapsed. Kill processes with name Inosim";
                logWriter.WriteLine(logMessage);
                Console.WriteLine(logMessage);
                new Process()
                    {
                        StartInfo = new ProcessStartInfo("powershell.exe", """Stop-Process -Name "Inosim" """)
                    }
                    .Start();
                new Process()
                {
                    StartInfo = new ProcessStartInfo("powershell.exe", """Stop-Process -Name "Inosim.ui" """)
                }.Start();
            };
            Task.WaitAll(tasks.ToArray());

            // Kill timer
            timer.Dispose();
    
            // Evaluate individual
            var individualStart = i * Config.NumberOfWorkers;
            var individualEnd = individualStart + numberOfWorker;
            EvaluateIndividuals(individuals[individualStart..individualEnd], logWriter);
            //EvaluateIndividuals(Worker[0..numberOfWorker]);
            // Write results to individuals
        }
    }
    /// <summary>
    /// Updates the worker of the individuals
    /// checks which orders are fixed and which are finished
    /// by iterating the operations and comparing the start and
    /// end times of the simualtion start time.
    /// </summary>
    /// <param name="individuals" type="List<Individual>"></param>
    private void UpdateIndividuals(List<Individual> individuals)
    {
        foreach (var ind in individuals)
        {
            var worker = ind.Worker;
            // check whether the worker has already been updated
            if (worker.InosimInstance.Objectives != null) // null means that no simulation was run, yet
            {
                //Sort orders accordingly -> Important for the simulation and the pending procedure lists
                if (!worker.InosimInstance.Objectives.ContainsKey("FAILED"))
                {
                    var finishedOrders = worker.Orders
                        .Where(w => w.Operations[^1].End < this.SimulationStart)
                        .OrderByDescending(x => x.Operations[^1].End)
                        .ToList();
                    var startedOrders = worker.Orders
                        .Where(w => w.Operations[0].Start < this.SimulationStart &&
                                    w.Operations[^1].End >= this.SimulationStart)
                        //.OrderByDescending(w => w.Operations[0].Start)
                        .ToList();
                    var openOrders = worker.Orders.Where(w => w.Operations[0].Start > this.SimulationStart).ToList();
                    worker.Orders = new List<Order>();
                    worker.Orders.AddRange(finishedOrders);
                    worker.Orders.AddRange(startedOrders);
                    worker.Orders.AddRange(openOrders);
                    // there needs to be a better way. to complicated IMHO
                    var ordIdx = ind.Genes.FindIndex(x=>x.Type == GeneType.StringPermutation && x.Name == GeneName.Order);
                    ((Gene<String>)ind.Genes[ordIdx]).Values = openOrders.Select(x=>x.Name).ToList();
                }
                //Update worker
                List<int> deleteOrderIndices = new List<int>();
                worker.InosimInstance.SimulationStart = this.SimulationStart;
                worker.InosimInstance.SimulationEnd = this.SimulationEnd;
                for (int o = 0; o < worker.Orders.Count; o++)
                {
                    var order = worker.Orders[o];
                    for (int j = 0; j < order.Operations.Count; j++)
                    {
                        if (order.Operations[j].Start >= this.SimulationStart)
                        {
                            order.Operations.RemoveAt(j);
                            j--;
                        }
                    }
                    if (order.Operations.Count > 0)
                    {
                        order.Started = true;
                        if (order.Operations[^1].End < this.SimulationStart)
                        {
                            order.Finished = true;
                        }
                    }
                    else order.Started = false;
                }
            }
            else
            {
                // get out of the foreach loop
                break;
            }
        }
        
    }
    private void DecodeGenome(Worker worker, Gene<string>? orderGene)
    {
        if(worker.Orders != null)
        {
            worker.Orders.AddRange(new List<Order>(CreateOrders(orderGene!)));
        }
        else
        {
            worker.Orders = new List<Order>(CreateOrders(orderGene));
        }
    }
    // TODO: Copy worker json to results for each individual; Maybe each individual should contains its worker
    private void EvaluateIndividuals(List<Individual> individuals, StreamWriter logWriter)
    {
        Parallel.For(0, individuals.Count, j =>
        {
            if (!((Individual)individuals[j]).Worker.InosimInstance.Objectives.ContainsKey("FAILED") 
                && File.Exists(((Individual)individuals[j]).Worker.InosimInstance.Name + ".json"))
            {
                try
                {
                    var workerJson = File.ReadAllText(((Individual)individuals[j]).Worker.InosimInstance.Name + ".json")!;
                    if(workerJson == null) ((Individual)individuals[j]).Worker.InosimInstance.Objectives.Add("FAILED", -1.0);
                    else ((Individual)individuals[j]).Worker = JsonSerializer.Deserialize<PlantSchedule.DTO.Worker>(workerJson);

                    // Remove all operations that are null
                    for (var i = 0; i < ((Individual)individuals[j]).Worker.Resources.Count; i++)
                    {
                        var ops = ((Individual)individuals[j]).Worker.Resources[i].Operations.FindAll(o => o == null);
                        foreach (var op in ops)
                        {
                            var idx = ((Individual)individuals[j]).Worker.Resources[i].Operations.FindIndex(o => o == op);
                            ((Individual)individuals[j]).Worker.Resources[i].Operations.RemoveAt(idx);
                        }
                    }
                    individuals[j].Fitness = ((Individual)individuals[j]).Worker.InosimInstance.Objectives;
                    ((Individual)individuals[j]).Json = workerJson;
                }
                catch (Exception e)
                {
                    var logMessage = "[ERROR] " + e;
                    logWriter.WriteLine(logMessage);
                    Console.WriteLine(logMessage);
                    if (!((Individual)individuals[j]).Worker.InosimInstance.Objectives.ContainsKey("FAILED"))
                        ((Individual)individuals[j]).Worker.InosimInstance.Objectives.Add("FAILED", -1);
                }
            }
            else
            {
                if (!((Individual)individuals[j]).Worker.InosimInstance.Objectives.ContainsKey("FAILED"))
                    ((Individual)individuals[j]).Worker.InosimInstance.Objectives.Add("FAILED", -1.0);
            }
        });
    }
    private string SerializeWorkerAndWriteToFile(Worker worker, bool write = true)
    {
        //Serialize worker
        var workerJson = JsonSerializer.Serialize(worker);
        var workerJsonPath = worker.InosimInstance.Database.Substring(0,worker.InosimInstance.Database.Length - 4) + $"json"; //Add iteration info
        if (write)
        {
            File.WriteAllText(workerJsonPath, workerJson);
        }
        return workerJson;
    }
    public List<PlantSchedule.DTO.Order> CopyOrders(List<PlantSchedule.DTO.Order> otherOrders)
    {
        List<PlantSchedule.DTO.Order> orders = new List<PlantSchedule.DTO.Order>();
        for (int i = 0; i < otherOrders.Count(); i++)
        {
            orders.Add(new PlantSchedule.DTO.Order
            {
                DueDate = otherOrders[i].DueDate,
                Finished = otherOrders[i].Finished,
                Id = GetHashCode().ToString(),
                Name = otherOrders[i].Name,
                Operations = CopyOperations(otherOrders[i].Operations),
                Recipe = otherOrders[i].Recipe,
                Started = otherOrders[i].Started
            });
        }
        return orders;
    }
    private List<PlantSchedule.DTO.Operation> CopyOperations(List<PlantSchedule.DTO.Operation> otherOperations)
    {
        
        List<PlantSchedule.DTO.Operation> operations = new List<PlantSchedule.DTO.Operation>();
        for (int i = 0; i < otherOperations.Count(); i++)
        {
            operations.Add(new PlantSchedule.DTO.Operation
            {
                Name = otherOperations[i].Name,
                Unit = otherOperations[i].Unit,
                Order = otherOperations[i].Order,
                Duration = otherOperations[i].Duration,
                Start = otherOperations[i].Start,
                End = otherOperations[i].End
                            
            });
        }
        return operations;
    }
    public List<PlantSchedule.DTO.Order> CreateOrders(Gene<String> orderGene)
    {
        List<PlantSchedule.DTO.Order> orders = new List<PlantSchedule.DTO.Order>();
        for (int i = 0; i < orderGene.Values.Count(); i++)
        {
            orders.Add(new PlantSchedule.DTO.Order
            {
                DueDate = DateTime.Now,
                Finished = false,
                Id = GetHashCode().ToString(),
                Name = orderGene.Values[i],
                Operations = new List<PlantSchedule.DTO.Operation>(),
                Recipe = OrdersRecipeNoAlloc[orderGene.Values[i].Split("_")[0]],
                Started = false
            });
        }
        return orders;
    }
    public List<Resource> CopyResources(List<Resource> resources)
    {
        throw new NotImplementedException();
    }

    public void SetDueDate(string order)
    {
        throw new NotImplementedException();
    }

    public void SetDueDate(Dictionary<string, int> rushOrderDueDate)
    {
        throw new NotImplementedException();
    }
}
#endif
#if SIMULATOR
public class Simulator : ISimulator
{
    public List<int> Stages = new List<int>() { 1, 2, 3 };
    public List<int> Machines = new List<int>() { 1, 2, 3, 4, 5, 6, 7 };
    public DateTime SimulationStart { get; set; }
    public DateTime SimulationEnd { get; set; }
    List<string> Orders { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    List<string> ISimulator.Orders { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Dictionary<int, List<int>> StageMachines = new Dictionary<int, List<int>>()
    {
        { 1, new List<int>() { 1, 2 } },
        { 2, new List<int>() { 3, 4, 5 } },
        { 3, new List<int>() { 6, 7 } }
    };

    public double[][] RecipeParameters;
        

    public Simulator()
    {
        var recipeParameter = ";" + String.Join(";", Machines.Select(x=>"M" + x.ToString())) + "\n";
        RecipeParameters = new double[10][];
        for (int i = 0; i < RecipeParameters.Length; i++)
        {
            recipeParameter += $"R{i};";
            RecipeParameters[i] = new double[Machines.Count];
            for (int j = 0; j < RecipeParameters[i].Length; j++)
            {
                RecipeParameters[i][j] = ((Double)Random.Shared.Next(3600,3600*3))/3600.0;
            }
            recipeParameter += String.Join(";", RecipeParameters[i].Select(x => x.ToString("F2")));
            recipeParameter += "\n";
        }
        Console.WriteLine(recipeParameter);
    }


    public void Simulate(List<Individual> individuals, StreamWriter logWriter)
    {
        foreach (var individual in individuals)
        {
            Gene<String> orderGene = null;
            foreach (var gene in individual.Genes)
            {
                if (gene.Name == GeneName.Order)
                {
                    orderGene = new Gene<String>((Gene<String>)gene);
                    break;
                }
            }
            if (orderGene == null)
                throw new NotImplementedException("There needs to be at least one gene that is of type order");
            for (int i = 0; i < orderGene.Values.Count; i++)
            {
                for (int j = 0; j < Stages.Count; j++)
                {

                }
            }
        }
    }

    public void Simulate(List<Individual> individuals, StreamWriter logWriter, DateTime simulationStart)
    {
        throw new NotImplementedException();
    }

    public void Simulate(List<IIndividual> individuals, StreamWriter logWriter, DateTime simulationStart)
    {
        throw new NotImplementedException();
    }

    public void Simulate(string json)
    {
        throw new NotImplementedException();
    }

    public List<Order> CopyOrders(List<Order> otherOrders)
    {
        throw new NotImplementedException();
    }

    public List<Order> CreateOrders(Gene<string> orderGene)
    {
        throw new NotImplementedException();
    }

    public List<Resource> CopyResources(List<Resource> resources)
    {
        throw new NotImplementedException();
    }

    public void UpdateSimulator(Worker tempWorker)
    {
        throw new NotImplementedException();
    }

    public void SetDueDate(string order)
    {
        throw new NotImplementedException();
    }

    public void SetDueDate(Dictionary<string, int> rushOrderDueDate)
    {
        throw new NotImplementedException();
    }
}
#endif
