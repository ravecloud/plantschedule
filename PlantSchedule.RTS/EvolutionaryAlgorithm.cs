using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using GeneticSharp;
using PlantSchedule.DTO;

namespace PlantSchedule.RTS;

public enum LogOption{
    Init,
    EndOfGeneration,
    Start,
    End,
    Update,
    ParameterUpdate,
    Custom
}
public enum LogType
{
    INFO,
    WARNING,
    ERROR,
    CUSTOM
}
[Flags]
public enum UpdateType
{
    Time = 1,
    Order = 2,
    Maintenance = 4,
    Delay = 8
}

public class Maintenance
{
    public DateTime Start;
    public DateTime End;
    public String Unit;
}
public class UpdateEvent
{
    public int UpdateGeneration { get; set; }
    public DateTime UpdateTime { get; set; }
    public UpdateType UpdateType;
    public List<String> ?Orders;
    public List<Maintenance> ?Maintenances;
}

public enum EvalOpt
{
    Init,
    Population,
    Offspring
}

public class EvolutionaryAlgorithm
{
    public SortedDictionary<int, UpdateEvent> UpdateEvents;
    // Configs
    private IConfig Config;

    // Services
    public IService Service;
    public ISimulator Simulator;
    public Stopwatch Stopwatch = new Stopwatch();
    public StreamWriter ResultsWriter;
    public StreamWriter LogWriter;

    // Parameters
    public int PopulationSize;
    public int ParentsSize;
    public int OffspringSize;
    public int ElitistSize;
    public double MutationRate;
    public double CrossoverRate;
    public string Objective;
    public string Initialization;
    public bool Sync;
    public bool HasUpdates;
    public double TimeDiff;

    // Stats
    public int Generation = 0;
    public DateTime SimulationStart;
    public DateTime SimulationEnd;
    public DateTime PlantTime;
    public double Mean;
    public double Var;

    // Components
    public List<Individual> Population;
    public List<Individual> Offspring;

    // Functions
    public Func<List<Individual>, int, string, List<Individual>> ParentSelection;
    public Func<List<Individual>, int, string, List<Individual>> ElitistSelection;
    public Func<List<Individual>, int, string, List<Individual>> SurvivorSelection;
    public Func<bool> TerminationCriterion = () => false;
    public Func<bool> HasUpdate = () => false;
    public Action UpdateParameters = () => { };

    // Helper
    private List<(int, int)> selectedParents;
    private List<int> selectedSurvivors;

#if KOPANOS_SIMULATOR
    public EvolutionaryAlgorithm(KopanosConfig config)
    {
        Config = config;
        Service = config.Service switch
        {
            "KopanosService" => new KopanosService(),
            "PlantService" => new PlantService(),
            _ => throw new Exception("Service not found")
        };

        Simulator = Service.GetSimulator(config);
        SimulationStart = config.SimulationStart;
        SimulationEnd = config.SimulationEnd;
        PopulationSize = config.PopulationSize;
        ParentsSize = config.ParentsSize;
        OffspringSize = config.OffspringSize;
        MutationRate = config.MutationRate;
        CrossoverRate = config.CrossoverRate;
        Objective = config.Objective;
        Initialization = config.Initialization;
        ElitistSize = config.ElitistSize;

        // Set selection methods
        this.ParentSelection = Selection.GetSelectionMethod(((KopanosConfig)this.Config).ParentSelection, SelectionType.Parent);
        this.ElitistSelection = Selection.GetSelectionMethod(((KopanosConfig)this.Config).ElitistSelection, SelectionType.Elitist);
        this.SurvivorSelection = Selection.GetSelectionMethod(((KopanosConfig)this.Config).SurvivorSelection, SelectionType.Survivor);
        this.StartWriter();

        // Set termination
        this.TerminationCriterion = () => this.Generation == ((KopanosConfig)this.Config).Generations; // 24*4;

        this.InitEa();
    }
#endif

    public EvolutionaryAlgorithm(CSharpConfig config)
    {
        Config = config;
        Service = config.Service switch
        {
            // "KopanosService" => new KopanosService(),
            // "PlantService" => new PlantService(),
            "CSharpService" => new CSharpService(),
            _ => throw new Exception("Service not found")
        };

        Simulator = (CSharpSimulator)Service.GetSimulator(config);
        SimulationStart = config.SimulationStart;
        SimulationEnd = config.SimulationEnd;
        PopulationSize = config.PopulationSize;
        ParentsSize = config.ParentsSize;
        OffspringSize = config.OffspringSize;
        MutationRate = config.MutationRate;
        CrossoverRate = config.CrossoverRate;
        Objective = config.Objective;
        Initialization = config.Initialization;
        ElitistSize = config.ElitistSize;

        // Set selection methods
        this.ParentSelection = Selection.GetSelectionMethod(((CSharpConfig)this.Config).ParentSelection, SelectionType.Parent);
        this.ElitistSelection = Selection.GetSelectionMethod(((CSharpConfig)this.Config).ElitistSelection, SelectionType.Elitist);
        this.SurvivorSelection = Selection.GetSelectionMethod(((CSharpConfig)this.Config).SurvivorSelection, SelectionType.Survivor);
        this.StartWriter();

        if (config.RushOrders != null && config.RushOrders.Count > 0)
        {
            foreach (var rushOrder in config.RushOrders)
            {
                if (!((CSharpSimulator)this.Simulator).DueDates.ContainsKey(rushOrder.Key))
                {
                    var rushOrderStart = config.SimulationStart.AddSeconds(rushOrder.Value[0] * config.TimeIncrement);
                    ((CSharpSimulator)this.Simulator).DueDates.Add(rushOrder.Key, rushOrderStart.AddHours(rushOrder.Value[1]));
                }
            }
        }
        // Set termination
        this.TerminationCriterion = () => this.Generation == ((CSharpConfig)this.Config).Generations; // 24*4;

        this.InitEa();
    }

    public EvolutionaryAlgorithm(int populationSize,
        int parentsSize,
        int offspringSize,
        double mutationRate,
        double crossoverRate,
        string objective,
        string initialization,
        int elitistSize,
        IService service,
        IConfig config)
    {
        Config = config;
        Service = service;
        Simulator = Service.GetSimulator(config);
        SimulationStart = config.SimulationStart;
        SimulationEnd = config.SimulationEnd;
        PopulationSize = populationSize;
        ParentsSize = parentsSize;
        OffspringSize = offspringSize;
        MutationRate = mutationRate;
        CrossoverRate = crossoverRate;
        Objective = objective;
        Initialization = initialization;
        ElitistSize = elitistSize;

        // Set termination
        this.TerminationCriterion = () => this.Generation == ((KopanosConfig)this.Config).Generations; // 24*4;

        // Set writer
        this.StartWriter();

        this.InitEa();
    }

    private void InitEa()
    {
        // TODO: Don't explicitly cast to CSharpConfig. Maybe not the best idea. Future you will handle!
        var config = (CSharpConfig)this.Config;
        var ea = this;
        ea.UpdateEvents = new SortedDictionary<int, UpdateEvent>();

        // Define termination criterion
        ea.TerminationCriterion = () => ea.Generation == config.Generations; // 24*4;
                                                                             //ea.TerminationCriterion = () => (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape);

        // 32 threads -> 32 parallel simulations. 3 seconds per 32 parallel simulations. -> 64 Individuals : 6 sec -> 128 Individuals : 12 sec

        if (config.Maintenances != null && config.Maintenances.Count > 0)
        {
            foreach (var maintenance in config.Maintenances)
            {
                var start = config.SimulationStart.AddSeconds(maintenance.Value[0] * config.TimeIncrement); //.AddHours(maintenance.Value[0]);
                var end = start.AddSeconds(maintenance.Value[1]);
                var maint = new Maintenance()
                {
                    Unit = maintenance.Key.Split("_")[0],
                    Start = start,
                    End = end
                };
                if (!ea.UpdateEvents.ContainsKey(maintenance.Value[0]))
                {
                    var updateEvent = new UpdateEvent
                    {
                        UpdateGeneration = maintenance.Value[0],
                        UpdateTime = start, //.AddSeconds(60), // TODO: REmove afterwards
                        Maintenances = new List<Maintenance>() { maint },
                        UpdateType = UpdateType.Maintenance | UpdateType.Time
                    };
                    ea.UpdateEvents[maintenance.Value[0]] = updateEvent;
                }
                else
                {
                    if (ea.UpdateEvents[maintenance.Value[0]].Maintenances != null)
                    {
                        ea.UpdateEvents[maintenance.Value[0]].Maintenances.Add(maint);
                    }
                    else
                    {
                        ea.UpdateEvents[maintenance.Value[0]].Maintenances = new List<Maintenance>();
                        ea.UpdateEvents[maintenance.Value[0]].Maintenances.Add(maint);
                    }
                    ea.UpdateEvents[maintenance.Value[0]].UpdateType |= UpdateType.Maintenance;
                }

            }
        }
        // Set rush orders
        // Sort according to due date and release date !!!
        if (config.RushOrders != null && config.RushOrders.Count > 0)
        {
            config.RushOrders = config.RushOrders.OrderByDescending(x => x.Value[0]).ToDictionary();
            var orderCnt = 0;
            foreach (var rushOrder in config.RushOrders)
            {
                var updateEvent = new UpdateEvent()
                {
                    // In which generation the order is released
                    UpdateGeneration = rushOrder.Value[0],
                    UpdateTime = config.SimulationStart.AddSeconds(rushOrder.Value[0] * config.TimeIncrement), // + 60), // TODO: Remove + 60 We could also just set the config.TimeIncrement = 0
                    Orders = new List<string>() { rushOrder.Key },
                    UpdateType = UpdateType.Order | UpdateType.Time
                };

                if (ea.UpdateEvents.ContainsKey(updateEvent.UpdateGeneration))
                {
                    ea.UpdateEvents[updateEvent.UpdateGeneration].Orders.AddRange(updateEvent.Orders);
                    ea.UpdateEvents[updateEvent.UpdateGeneration].UpdateType |= updateEvent.UpdateType;
                }
                else
                {
                    ea.UpdateEvents.Add(updateEvent.UpdateGeneration, updateEvent);
                }
            }
        }

        // If the changeover started before the simulation we want at least the next operation after the changeover to happen.
        var updateGeneration = config.UpdateGeneration;
        var updateInterval = config.TimeInterval; // 10, 25, 50, 100
        var secondsPerGeneration = config.TimeIncrement; // 9, 18, 36, 72 every 50 generations you traverse 30 minutes 50 * 36 = 

        // Set update events
        // Change name to IsUpdateEvent
        ea.HasUpdate = () =>
        {
            if (ea.Generation == updateGeneration)
            {
                if (ea.UpdateEvents.ContainsKey(updateGeneration))
                {
                    // ea.UpdateEvents[updateGeneration].UpdateType |= UpdateType.Time;
                    // ea.UpdateEvents[updateGeneration].UpdateTime = config.SimulationStart.AddSeconds((ea.Generation + updateInterval) * config.TimeIncrement);

                    /*
                    var nextUpdateGeneration = ea.Generation + (60 / secondsPerGeneration);
                    if (ea.UpdateEvents.ContainsKey(nextUpdateGeneration) == false &&
                        (ea.UpdateEvents[ea.Generation].UpdateType == (UpdateType.Time | UpdateType.Maintenance) ||
                        ea.UpdateEvents[ea.Generation].UpdateType == (UpdateType.Time | UpdateType.Order)))  
                    {
                        ea.UpdateEvents[nextUpdateGeneration] = new UpdateEvent()
                        {
                            UpdateGeneration = nextUpdateGeneration,
                            UpdateTime = config.SimulationStart.AddSeconds(nextUpdateGeneration * config.TimeIncrement),
                            UpdateType = UpdateType.Time
                        };
                    }
                    */
                }
                else
                {
                    ea.UpdateEvents[updateGeneration] = new UpdateEvent()
                    {
                        UpdateGeneration = ea.Generation,
                        //UpdateTime = config.SimulationStart.AddSeconds((ea.Generation + updateInterval) * config.TimeIncrement),
                        UpdateTime = config.SimulationStart.AddSeconds(ea.Generation * config.TimeIncrement),
                        UpdateType = UpdateType.Time
                    };
                }
                updateGeneration += updateInterval;
                return true;
            }
            else if (ea.UpdateEvents.ContainsKey(ea.Generation))
            {
                /*
                var nextUpdateGeneration = ea.Generation + (60 / secondsPerGeneration);
                if (ea.UpdateEvents.ContainsKey(nextUpdateGeneration) == false &&
                    (ea.UpdateEvents[ea.Generation].UpdateType == (UpdateType.Time | UpdateType.Maintenance) ||
                    ea.UpdateEvents[ea.Generation].UpdateType == (UpdateType.Time | UpdateType.Order)) &&
                    nextUpdateGeneration < updateGeneration) // if the reactive update would be sooner as the periodic update  
                {
                    ea.UpdateEvents[nextUpdateGeneration] = new UpdateEvent()
                    {
                        UpdateGeneration = nextUpdateGeneration,
                        UpdateTime = config.SimulationStart.AddSeconds(nextUpdateGeneration * config.TimeIncrement),
                        UpdateType = UpdateType.Time
                    };
                }
                */
                return true;
            }
            else
            {
                return false;
            }
        };

        ea.UpdateParameters = () =>
        {
            if (config.UpdateParameters)
            {

                var countDuplicates = 0;
                for (int i = 1; i < ea.Population.Count; i++)
                {
                    if (ea.Population[i].GetFitness(ea.Objective) == ea.Population[i - 1].GetFitness(ea.Objective))
                    {
                        // ea.Population[i].Mutate();
                        // ea.Population[i].Fitness[ea.Objective] += 10; // ea.Population[ea.Population.Count - 1].Fitness[ea.Objective] + 10; //add penalty to duplicates
                        countDuplicates += 1;
                    }
                }

                // Order population 
                ea.OrderPopulationByObjective();
                GetStatsOfPopulation(ea);
                MeasureDistance(ea);
                ChangeEAParameters(ea, countDuplicates);

                ea.Log(LogOption.Custom, message: $"Mean: {ea.Mean}, variance: {ea.Var}");
            }
            else
            {
                ea.OrderPopulationByObjective();
                GetStatsOfPopulation(ea);
                MeasureDistance(ea);
                ea.Log(LogOption.Custom, message: $"Mean: {ea.Mean}, variance: {ea.Var}");
            }
        };
    }
    private void ChangeEAParameters(EvolutionaryAlgorithm ea, int countDuplicates)
    {
        var avgMeasure = ea.Population[1..].Average(x => x.Measure); // < ea.Population.Count - 5;
        var avgAge = ea.Population[1..].Average(x => x.Age); // < ea.Population.Count - 5;
        var maxAge = ea.Population[1..].Max(x => x.Age);
        var avgMeasureCriteria = avgMeasure < 15;
        var maxAgeCriteria = maxAge > 10;
        var avgAgeCriteria = avgAge > 5;
        if (avgMeasureCriteria) // countDuplcates > 10 TODO: Change from hardcoded to non hard coded. take the list of orders as measure
        {
            //var newElitistSize = (int)(ea.PopulationSize * 0.01);
            //ea.ElitistSize =  newElitistSize > 0 ? newElitistSize : 1;
            ea.MutationRate = 1;
            foreach (var ind in ea.Population)
            {
                foreach (var gene in ind.Genes)
                {
                    if (gene.Name != GeneName.Order)
                    {
                        continue;
                    }
                    //SwapMutation
                    ((Gene<String>)gene).Mutate = Mutations<String>.GetMutationMethod("SwapMutation");
                    ((Gene<String>)gene).Crossover = Crossovers<String>.GetCrossoverMethod("EmptyCrossover");

                }
            }
            ea.ParentSelection = Selection.GetSelectionMethod("RandomSelection", SelectionType.Parent);
            //ea.ElitistSelection = Selection.GetSelectionMethod(((KopanosConfig)ea.Config).ElitistSelection, SelectionType.Elitist);
            //ea.SurvivorSelection = Selection.GetSelectionMethod(((KopanosConfig)ea.Config).SurvivorSelection, SelectionType.Survivor);
            ea.Log(LogOption.Custom, message: $"Elitist size: {ea.ElitistSize}, Mutation rate: {ea.MutationRate}, Mutation: {"SwapMutation"}");
        }
        else if (avgAgeCriteria) // countDuplcates > 10 TODO: Change from hardcoded to non hard coded. take the list of orders as measure
        {
            ea.ElitistSize = (int)(ea.PopulationSize * 0.05);
            if (ea.ElitistSize == 0) ea.ElitistSize = 1;
            //ea.ElitistSize =  1;
            ea.MutationRate = 1;
            foreach (var ind in ea.Population)
            {
                foreach (var gene in ind.Genes)
                {
                    if (gene.Name != GeneName.Order)
                    {
                        continue;
                    }
                    //SwapMutation
                    ((Gene<String>)gene).Mutate = Mutations<String>.GetMutationMethod("PermutationMutation");
                    ((Gene<String>)gene).Crossover = Crossovers<String>.GetCrossoverMethod("OrderCrossover");

                }
            }
            ea.ParentSelection = Selection.GetSelectionMethod("RandomSelection", SelectionType.Parent);
            //ea.ElitistSelection = Selection.GetSelectionMethod(((KopanosConfig)ea.Config).ElitistSelection, SelectionType.Elitist);
            //ea.SurvivorSelection = Selection.GetSelectionMethod(((KopanosConfig)ea.Config).SurvivorSelection, SelectionType.Survivor);
            ea.Log(LogOption.Custom, message: $"Elitist size: {ea.ElitistSize}, Mutation rate: {ea.MutationRate}");
        }
        else if (!avgAgeCriteria || !avgMeasureCriteria)
        {
            ea.SetParametersFromConfig();
            ea.Log(LogOption.Custom, message: $"Elitist size: {ea.ElitistSize}, Mutation rate: {ea.MutationRate}, Mutation: {ea.GetMutationMethodName()}");
        }
    }

    private void GetStatsOfPopulation(EvolutionaryAlgorithm ea)
    {
        ea.Mean = ea.Population.Average(ind => ind.GetFitness(ea.Objective));
        // Step 2: Calculate the variance
        ea.Var = ea.Population
            .Select(ind => Math.Pow(ind.GetFitness(ea.Objective) - ea.Mean, 2))
            .Average();
    }

    private void MeasureDistance(EvolutionaryAlgorithm ea)
    {
        // we assume that the population is ordered
        var bestInd = ea.Population[0];
        foreach (var ind in ea.Population)
        {
            var gene1 = (Gene<String>)ind.Genes[0];
            var gene2 = (Gene<String>)bestInd.Genes[0];
            var hammingDistance = DistanceMeasure.HammingDistance(gene1.Values, gene2.Values);
            ind.Measure = hammingDistance;
        }
    }

    public void Run()
    {
        this.Log(LogOption.Start);
        this.Initialize();
        this.Evaluate(EvalOpt.Init);
        this.Log(LogOption.Init);
        while (!this.TerminationCriterion())
        {
            // all duplicates are randomly permutated such that we generate a more versatile population
            this.SelectParents();
            if (this.HasUpdate())
            {
                this.Update(this.UpdateEvents[this.Generation]);
                this.Generation += 1;
                this.Log(LogOption.Start);
                this.Evaluate(EvalOpt.Population);
                this.Log(LogOption.EndOfGeneration);
            }
            else
            {
                this.Generation += 1;
                this.Log(LogOption.Start);
                this.Recombine();
                this.Mutate();
                this.Evaluate(EvalOpt.Offspring);
                this.SelectSurvivors();
                // does it make sense to update the paramters before the survivor selection?
                this.UpdateParameters();
                this.Log(LogOption.EndOfGeneration);
            }
        }
        this.WriteBestResult();
        this.Log(LogOption.End, message: "Program Ends");
    }
    public void RunClaireVoyant()
    {
        this.Log(LogOption.Start);
        this.Initialize();
        this.AddEvents();
        this.Evaluate(EvalOpt.Init);
        this.Log(LogOption.Init);
        while (!this.TerminationCriterion())
        {
            // all duplicates are randomly permutated such that we generate a more versatile population
            this.SelectParents();
            this.Generation += 1;
            this.Log(LogOption.Start);
            this.Recombine();
            this.Mutate();
            this.Evaluate(EvalOpt.Offspring);
            this.SelectSurvivors();
            // does it make sense to update the paramters before the survivor selection?
            this.UpdateParameters();
            this.Log(LogOption.EndOfGeneration);
        }
        this.WriteBestResult();
        this.Log(LogOption.End, message: "Program Ends");
    }

    private void AddEvents()
    {
        foreach (var (time, updateEvent) in this.UpdateEvents)
        {
            if (updateEvent.UpdateType == UpdateType.Order)
            {
                foreach (var ind in this.Population)
                {
                    this.Simulator.Orders.Add("");
                    ((Gene<string>)ind.Genes[0]).Values.Add("");
                }
            }

        }
    }

    private void WriteBestResult()
    {
        var bestWorker = JsonSerializer.Serialize(this.Population[0].Worker);
        File.WriteAllText(this.Config.ResultFolder + "bestWorker.json", bestWorker);
    }

    public void OrderPopulationByObjective()
    {
        this.Population = this.Population.OrderBy(individual => individual.GetFitness(this.Objective)).ToList();
    }
    public string GetMutationMethodName()
    {
        return ((CSharpConfig)this.Config).MutationMethod;
    }

    public void SetParametersFromConfig()
    {

        this.ElitistSize = ((CSharpConfig)this.Config).ElitistSize;
        this.MutationRate = ((CSharpConfig)this.Config).MutationRate;
        this.ParentSelection = Selection.GetSelectionMethod(((CSharpConfig)this.Config).ParentSelection, SelectionType.Parent);
        this.ElitistSelection = Selection.GetSelectionMethod(((CSharpConfig)this.Config).ElitistSelection, SelectionType.Elitist);
        this.SurvivorSelection = Selection.GetSelectionMethod(((CSharpConfig)this.Config).SurvivorSelection, SelectionType.Survivor);
        foreach (var ind in this.Population)
        {
            foreach (var gene in ind.Genes)
            {
                if (gene.Name != GeneName.Order)
                {
                    continue;
                }
                if (((Gene<String>)gene).Mutate == Mutations<String>.GetMutationMethod(((CSharpConfig)this.Config).MutationMethod))
                {
                    return;
                }
                ((Gene<String>)gene).Mutate = Mutations<String>.GetMutationMethod(((CSharpConfig)this.Config).MutationMethod);
                ((Gene<String>)gene).Crossover = Crossovers<String>.GetCrossoverMethod(((CSharpConfig)this.Config).CrossoverMethod);
            }
        }
    }

    private void StartWriter()
    {
        var config = this.Config;
        Stopwatch.Start();
        ResultsWriter = new StreamWriter(config.ResultFolder + "results.csv");
        if (!File.Exists(config.ResultFolder + "results.csv"))
        {
            File.Create(config.ResultFolder + "results.csv");
        }
        ResultsWriter.WriteLine("Generation;Individual;Fitness;Age;Measure;Mean;Std;Sim Start; Sim End;Stopwatch;Worker");

        LogWriter = new StreamWriter(config.ResultFolder + "log.txt") { AutoFlush = true };
        if (!File.Exists(config.ResultFolder + "log.txt"))
        {
            File.Create(config.ResultFolder + "log.txt");
            LogWriter.WriteLine($"[INFO] Program start at {DateTime.Now.ToString()}");
            this.Log(message: $"Program starts at {DateTime.Now.ToString()}");
        }
        else
        {
            LogWriter.WriteLine($"[INFO] Program start at {DateTime.Now.ToString()}");
        }
        ResultsWriter.Flush();
    }
    
    private void RemoveOperationsAfterStartFromResources(List<Resource> resources)
    { //TODO: Add tabu list of orders
        var tabuOrdersList = new List<string>();
        for (int i = resources.Count -1 ; i >= 0; i--)
        {
            var operations = resources[i].Operations;
            // Create a list of operations? Iterate through it
            // From back to front!
            for (int j = operations.Count - 1; j >= 0; j--)
            {
                var op = operations[j];
                
                if (op.Order == "P02_00" && resources[i].Name == "M14") continue;

                if (op.Start >= SimulationStart)
                {
                    // Keep the operation if the last operation was a changeover
                    if (j != 0 && operations[j - 1].Start <= SimulationStart && operations[j - 1].Name.Contains("Changeover"))
                    {
                        tabuOrdersList.Add(operations[j-1].Order);
                        break;
                        //continue;
                    }
                    if (j > 1
                        && operations[j].Name.Contains("Maintenance")
                        && operations[j - 1].Name.Contains("Process")
                        && operations[j - 1].Start > SimulationStart
                        && operations[j - 2].Name.Contains("Changeover")
                        && operations[j - 2].Start < SimulationStart)
                    {
                        tabuOrdersList.Add(operations[j - 1].Order);
                        break;
                        //continue;
                    }
                    // Keep the operation if the last operation was a changeover
                    if (j != 0 && operations[j - 1].Start < SimulationStart && operations[j].Name.Contains("Maintenance"))
                    {
                        tabuOrdersList.Add(operations[j-1].Order);
                        break;
                        //continue;
                    }
                    /*
                    if (operations.Count - 1 > j + 1 && j > 0)
                    {
                        if (operations[j - 1].Start < SimulationStart && operations[j + 1].Name.Contains("Maintenance"))
                        {
                            tabuOrdersList.Add(operations[j-1].Order);
                            break;
                            //continue;
                        }

                    }
                    */
                    if (tabuOrdersList.Contains(op.Order))
                    {
                        break;
                    }
                    operations.RemoveAt(j);
                    // j -= 1;
                }
            }

            if (operations.Count == 0)
            {
                operations.Add(new Operation()
                {
                    Name = "Idle", // Idle, Changeover, Clean, Process, Maintenance, Break
                    Unit = "",
                    Order = "",
                    Duration = 0.0,
                    Start = this.SimulationStart,
                    End = this.SimulationStart
                });
            }
            if (operations.Count > 0 && operations[^1].Name.Contains("Maintenance"))
            {
                operations.Add(new Operation()
                {
                    Name = "Idle", // Idle, Changeover, Clean, Process, Maintenance, Break
                    Unit = "",
                    Order = "",
                    Duration = 0.0,
                    Start = operations[^1].End,
                    End = operations[^1].End
                });
                if (operations.Count > 3)
                {
                    var operationsCount = operations.Count;
                    var operationBeforeMaintenance = operations[operations.Count - 3];
                    if (operationBeforeMaintenance.Order != "")
                    {
                        operations[^1].Order = operationBeforeMaintenance.Order;
                    }


                }
            }

        }
    }

    private void RemoveOperationsAfterStart(List<Operation> operations)
    {
        // Create a list of operations? Iterate through it
        // From back to front!
        for (int j = operations.Count - 1; j >= 0; j--)
        {
            var op = operations[j];
            if (op.Start >= SimulationStart)
            {
                // Keep the operation if the last operation was a changeover
                if (j != 0 && operations[j - 1].Start < SimulationStart && operations[j - 1].Name.Contains("Changeover"))
                {
                    break;
                    //continue;
                }
                if (j > 1
                    && operations[j].Name.Contains("Maintenance")
                    && operations[j - 1].Name.Contains("Process")
                    && operations[j - 1].Start > SimulationStart
                    && operations[j - 2].Name.Contains("Changeover")
                    && operations[j - 2].Start < SimulationStart)
                {
                    break;
                    //continue;
                }
                // Keep the operation if the last operation was a changeover
                if (j != 0 && operations[j - 1].Start < SimulationStart && operations[j].Name.Contains("Maintenance"))
                {
                    break;
                    //continue;
                }
                if (operations.Count - 1 > j + 1 && j > 0)
                {
                    if (operations[j - 1].Start < SimulationStart && operations[j + 1].Name.Contains("Maintenance"))
                    {
                        break;
                        //continue;
                    }

                }
                operations.RemoveAt(j);
                // j -= 1;
            }
        }
        if (operations.Count == 0)
        {
            operations.Add(new Operation()
            {
                Name = "Idle", // Idle, Changeover, Clean, Process, Maintenance, Break
                Unit = "",
                Order = "",
                Duration = 0.0,
                Start = this.SimulationStart,
                End = this.SimulationStart
            });
        }
        if (operations.Count > 0 && operations[^1].Name.Contains("Maintenance"))
        {
            operations.Add(new Operation()
            {
                Name = "Idle", // Idle, Changeover, Clean, Process, Maintenance, Break
                Unit = "",
                Order = "",
                Duration = 0.0,
                Start = operations[^1].End,
                End = operations[^1].End
            });
            if (operations.Count > 3)
            {
                var operationsCount = operations.Count;
                var operationBeforeMaintenance = operations[operations.Count - 3];
                if (operationBeforeMaintenance.Order != "")
                {
                    operations[^1].Order = operationBeforeMaintenance.Order;
                }


            }
        }
    }

    private void CopyGenesToPopulation(Worker w, Individual p, List<Order> copyOrders, List<Resource> copyResources)
    {
        var gene = p.Genes[0];
        w.SimulationInstance.SimulationStart = this.SimulationStart;
        w.SimulationInstance.SimulationEnd = this.SimulationEnd;
        foreach (var individual in this.Population)
        {
            if (individual == Population[0])
            {
                individual.Worker.SimulationInstance.SimulationStart = this.SimulationStart;
                individual.Worker.SimulationInstance.SimulationEnd = this.SimulationEnd;
                individual.Worker.SimulationInstance.Objectives = new Dictionary<string, double>();
                individual.FitnessName = ((CSharpConfig)Config).Objective;
                individual.Fitness = new Dictionary<string, double>();
                continue;
            }
            individual.Genes[0] = gene.Copy();
            // individual.Mutate();
            // individual.Genes[0].Mutation();
            // change the other 
            individual.Worker.Orders = this.Simulator.CopyOrders(w.Orders);
            individual.Worker.Resources = this.Simulator.CopyResources(w.Resources);
            individual.Worker.SimulationInstance.SimulationStart = this.SimulationStart;
            individual.Worker.SimulationInstance.SimulationEnd = this.SimulationEnd;
            individual.Worker.SimulationInstance.Objectives = new Dictionary<string, double>();
            individual.FitnessName = ((CSharpConfig)Config).Objective;
            individual.Fitness = new Dictionary<string, double>();
        }
        w.Orders = copyOrders;
        w.Resources = copyResources;
    }

    private void ConcatGenes(Worker w, Individual p, List<Order> copyOrders, List<Resource> copyResources)
    {
        var gene = p.Genes[0];
        w.SimulationInstance.SimulationStart = this.SimulationStart;
        w.SimulationInstance.SimulationEnd = this.SimulationEnd;
        foreach (var individual in this.Population)
        {
            if (individual == Population[0])
            {
                individual.Worker.SimulationInstance.SimulationStart = this.SimulationStart;
                individual.Worker.SimulationInstance.SimulationEnd = this.SimulationEnd;
                individual.Worker.SimulationInstance.Objectives = new Dictionary<string, double>();
                individual.FitnessName = ((CSharpConfig)Config).Objective;
                individual.Fitness = new Dictionary<string, double>();
                continue;
            }

            var indGeneValues = ((Gene<String>)individual.Genes[0]).Values;
            // Gene.Copy
            // Not necessary since we use Best and Random in Modify
            for (var i = 0; i < indGeneValues.Count; i++)
            {
                if (!((Gene<String>)gene).Values.Contains(indGeneValues[i]))
                {
                    indGeneValues.RemoveAt(i);
                    i--;
                }
            }

            //individual.Genes[0] =  gene.Copy();
            //individual.Mutate();
            //individual.Genes[0].Mutation();
            // change the other 
            individual.Worker.Orders = this.Simulator.CopyOrders(w.Orders);
            individual.Worker.Resources = this.Simulator.CopyResources(w.Resources);
            individual.Worker.SimulationInstance.SimulationStart = this.SimulationStart;
            individual.Worker.SimulationInstance.SimulationEnd = this.SimulationEnd;
            individual.Worker.SimulationInstance.Objectives = new Dictionary<string, double>();
            individual.FitnessName = ((CSharpConfig)Config).Objective;
            individual.Fitness = new Dictionary<string, double>();
        }
        // all the other individuals need to have the operations copied to the orders that did already start.
        //w.Orders = copyOrders;
        //w.Resources = copyResources;
    }
    private void Initialize(bool init)
    {
        Stopwatch.Restart();
        var logMessage =
            $"[INFO] Start evolution at {DateTime.Now}\n" +
            $"[INFO] Population: {PopulationSize}, Offspring: {OffspringSize}, Objective: {Objective}, Mutation: {MutationRate}, Crossover: {CrossoverRate}\n" +
            $"[INFO] Initialize individuals...\n";

        List<IGene> genome = new List<IGene>() {
        new Gene<String>(
            GeneName.Order,
            GeneType.StringPermutation,
            new List<String>(),
            Mutations<String>.GetMutationMethod(((CSharpConfig)Config).InitializationMethod),
            Mutations<String>.GetMutationMethod(((CSharpConfig)Config).MutationMethod),
            Crossovers<String>.GetCrossoverMethod(((CSharpConfig)Config).CrossoverMethod)
        )
    };

        var simulator = this.Simulator;
        foreach (var orderName in simulator.Orders)
        {
            ((Gene<String>)genome[0]).Values.Add(orderName);
        }
        if (PopulationSize != 0) // there are no individualt, yet
        {
            Population = new List<Individual>();
            for (int i = 0; i < PopulationSize; i++)
            {
                Population.Add((Individual)Service.GetIndividual(genome));
                Population[^1].Fitness = new Dictionary<string, double>();
                Population[^1].FitnessName = ((CSharpConfig)this.Config).Objective;
                Population[^1].Worker.SimulationInstance.SimulationStart = ((CSharpConfig)this.Config).SimulationStart;
                Population[^1].Worker.SimulationInstance.SimulationEnd = ((CSharpConfig)this.Config).SimulationEnd;
                if (!init) Population[^1].Init(); // if not initialized then randomly change the orders
                /*
                if (i > 0)
                {
                    Population[^1].Init();
                }
                */
            }
        }
        if (OffspringSize != 0) // there are no individualt, yet
        {
            Offspring = new List<Individual>();
            for (int i = 0; i < OffspringSize; i++)
            {
                Offspring.Add((Individual)Service.GetIndividual(genome));
                Offspring[^1].Fitness = new Dictionary<string, double>();
                Offspring[^1].FitnessName = ((CSharpConfig)this.Config).Objective;
                Offspring[^1].Worker.SimulationInstance.SimulationStart = ((CSharpConfig)this.Config).SimulationStart;
                Offspring[^1].Worker.SimulationInstance.SimulationEnd = ((CSharpConfig)this.Config).SimulationEnd;
                if (!init) Offspring[^1].Init(); // if not initialized then randomly change the orders
                /*
                if (i > 0)
                {
                    Offspring[^1].Init();
                }
                */
            }
        }

        logMessage += $"[INFO] Elapsed time: {Stopwatch.Elapsed:g}";
        this.Log(LogOption.Custom, LogType.CUSTOM, logMessage);
    }

    private void InitFromWorker()
    {
        var workerFile = ((CSharpConfig)Config).InitializationPath;
        if (!File.Exists(workerFile)) throw new FileNotFoundException($"The file {workerFile} was not found");
        try
        {
            var w = File.ReadAllText(workerFile);
            var tempWorker = JsonSerializer.Deserialize<Worker>(w);
            Simulator.UpdateSimulator(tempWorker);
            this.Config.SimulationStart = tempWorker.SimulationInstance.SimulationStart;
            this.Config.SimulationEnd = tempWorker.SimulationInstance.SimulationEnd;
            this.SimulationStart = this.Config.SimulationStart;
            this.SimulationEnd = this.Config.SimulationEnd;
        }
        catch
        {
            throw new Exception($"Could not deserialize the content in {workerFile}.");
        }
    }
    public void Initialize()
    {
        if (this.Initialization == "Worker")
        {
            /*
            var workerFile = ((CSharpConfig)Config).InitializationPath;
            if (!File.Exists(workerFile)) throw new FileNotFoundException($"The file {workerFile} was not found");
            try
            {
                var w = File.ReadAllText(workerFile);
                var tempWorker = JsonSerializer.Deserialize<Worker>(w);
                Simulator.UpdateSimulator(tempWorker);
                this.Config.SimulationStart = tempWorker.InosimInstance.SimulationStart;
                this.Config.SimulationEnd = tempWorker.InosimInstance.SimulationEnd;
                this.SimulationStart = this.Config.SimulationStart;
                this.SimulationEnd = this.Config.SimulationEnd;
            }
            catch
            {
                throw new Exception($"Could not deserialize the content in {workerFile}.");
            }
            */
            InitFromWorker();
            Initialize(true);
        }
        else if (this.Initialization == "WorkerRandom")
        {
            InitFromWorker();
            Initialize(false);
        }
        else
        {
            Initialize(false);
        }
    }
    private void Initialize(Individual individual)
    {
        Stopwatch.Restart();
        var logMessage =
            $"[INFO] Start evolution at {DateTime.Now}\n" +
            $"[INFO] Population: {PopulationSize}, Offspring: {OffspringSize}, Objective: {Objective}, Mutation: {MutationRate}, Crossover: {CrossoverRate}\n" +
            $"[INFO] Initialize individuals...\n";

        var dueDates = new Dictionary<string, DateTime>();
        var rand = new Random();
        // Initialize the genome
        if (PopulationSize != 0) // there are no individualt, yet
        {
            Population = new List<Individual>();
            for (int i = 0; i < PopulationSize; i++)
            {
                Population.Add((Individual)Service.GetIndividual(individual));
                Population[^1].Fitness = new Dictionary<string, double>();
            }
        }
        if (OffspringSize != 0) // there are no individualt, yet
        {
            Offspring = new List<Individual>();
            for (int i = 0; i < OffspringSize; i++)
            {
                Offspring.Add((Individual)Service.GetIndividual(individual));
                Offspring[^1].Fitness = new Dictionary<string, double>();
            }
        }

        logMessage += $"[INFO] Elapsed time: {Stopwatch.Elapsed:g}";
        this.Log(LogOption.Custom, LogType.CUSTOM, logMessage);
    }
    public void Terminate()
    {
        ;
    }

    public void SelectParents()
    {
        var rankedParents = ParentSelection(Population, ParentsSize, Objective);
        selectedParents = Selection.GenerateParentPairs(rankedParents);

        /*
        // First sort the population according to the fitness
        if (Population.Count > 0)
        {
            Population = Population.OrderBy(x => x.Fitness[Objective]).ToList();
            selectedParents = new List<Tuple<int, int>>();
            for (int i = 0; i < Population.Count; i+=2)
            {
                selectedParents.Add(new Tuple<int, int>(i, i + 1));
            }
        }
        */

    }

    public void Update(UpdateEvent updateEvent)
    {
        if (updateEvent.UpdateType.HasFlag(UpdateType.Time))
        {
            var frozenTime = updateEvent.UpdateTime;
            var timeDiff = updateEvent.UpdateTime - this.SimulationStart;
            this.SimulationStart = this.SimulationStart.Add(timeDiff);
            this.SimulationEnd = this.SimulationEnd.Add(timeDiff);
            this.HasUpdates = true;
        }
        if (updateEvent.UpdateType.HasFlag(UpdateType.Order))
        {
            if (this.Config is CSharpConfig && ((CSharpConfig)this.Config).PopulationContinuity == "Best")
            {
                var bestGene = (Gene<string>)((Gene<string>)this.Population[0].Genes[0]).Copy();
                for (int i = 0; i < this.Population.Count; i++)
                {
                    var ind = this.Population[i];
                    ind.Genes[0] = bestGene.Copy();
                    foreach (var order in updateEvent.Orders)
                    {
                        var rand = new Random();
                        var vals = ((Gene<String>)ind.Genes[0]).Values;
                        // vals.Insert(rand.Next(0, vals.Count - 1), order);
                        vals.Insert(0, order);
                    }
                }
            }
            else
            {
                foreach (var ind in this.Population)
                {
                    if (ind.Genes.Exists(x => x.Name == GeneName.Order))
                    {
                        var geneIdx = ind.Genes.FindIndex(x => x.Name == GeneName.Order);
                        foreach (var order in updateEvent.Orders)
                        {
                            var rand = new Random();
                            var vals = ((Gene<String>)ind.Genes[geneIdx]).Values;
                            // vals.Insert(rand.Next(0, vals.Count - 1), order);
                            // TODO: Check where the order should be added in the genome
                            // ATM at the beginning. Is this the best idea?
                            vals.Insert(0, order);
                        }
                        //((Gene<String>)ind.Genes[geneIdx]).Values.AddRange(updateEvent.Orders);
                    }
                    if (this.Config is CSharpConfig && ((CSharpConfig)this.Config).PopulationContinuity == "Random")
                    {
                        ((Gene<String>)ind.Genes[0]).Mutate = Mutations<String>.GetMutationMethod("RandomMutation");
                        ind.Mutate();
                        // TODO: ((Gene<string>)ind.Genes[0]);
                    }
                }
            }
            this.SetParametersFromConfig();
            this.HasUpdates = true;
        }
        if (updateEvent.UpdateType.HasFlag(UpdateType.Maintenance))
        {
            foreach (var ind in this.Population)
            {
                foreach (var maintenance in updateEvent.Maintenances)
                {
                    var res = ind.Worker.Resources.Find(r => r.Name == maintenance.Unit);
                    if (res == null) throw new Exception("The resource for the maintenance does not exist");
                    res.Maintenance.Add(new Tuple<DateTime, DateTime>(maintenance.Start, maintenance.End));
                }
            }
            this.HasUpdates = true;
        }
        if (updateEvent.UpdateType.HasFlag(UpdateType.Delay))
        {
            foreach (var ind in this.Population)
            {
                foreach (var maintenance in updateEvent.Maintenances)
                {
                    var res = ind.Worker.Resources.Find(r => r.Name == maintenance.Unit);
                    if (res == null) throw new Exception("The resource for the maintenance does not exist");
                    res.Maintenance.Add(new Tuple<DateTime, DateTime>(maintenance.Start, maintenance.End));
                }
            }
            this.HasUpdates = true;
        }

        // Modify only if we go one step further in time!!!
        if ((updateEvent.UpdateType & UpdateType.Time) == UpdateType.Time)
        {
            this.ModifyNew(updateEvent.UpdateType);
        }
        // Remove the element from the dictionary!!!
        this.UpdateEvents.Remove(this.Generation);
    }

    public void ModifyNew(UpdateType updateType)
    {
        // First sort the population according to the fitness
        Population = Population.OrderBy(x => x.Fitness[Objective]).ToList();
        /// Best individual
        var p = Population[0];
        var w = Population[0].Worker;
        // copy original orders and resources to use as initialization
        var copyOrders = Simulator.CopyOrders(p.Worker.Orders);
        var copyResources = Simulator.CopyResources(p.Worker.Resources);

        RemoveFinishedOrders(w, p);
        RemoveFinishedOperationsFromOrders(w.Orders, w.Resources);
        RemoveFinishedOperations(w.Resources);
        // RepairSchedules!!
        ConcatGenes(w, p, copyOrders, copyResources);
        // w.Resources = copyResources;
    }
    private void RemoveFinishedOrders(Worker w, Individual p)
    {
        // remove order if the last operation ends before simualtion statr
        for (int i = 0; i < w.Orders.Count; i++)
        {
            var lastOperation = w.Orders[i].Operations.Last();
            if (lastOperation.End < SimulationStart)
            {
                // remove order from gene
                var gene = ((Gene<String>)p.Genes[0]).Values;
                var j = gene.FindIndex(x => x == lastOperation.Order);
                gene.RemoveAt(j);
                // remove prder from worker
                w.Orders.RemoveAt(i);
                i--;
            }
            /*
            else
            {
                var order = w.Orders[i];
                order.Operations = new List<Operation>();
                order.Operations.Add(new Operation()
                {
                    Name = "Wait_S0", // Wait, Process, End, Transfer
                    Unit = "",
                    Order = order.Name,
                    Duration = 0.0,
                    Start = this.Config.SimulationStart,
                    End = this.Config.SimulationStart
                });
            }
            */
        }

        if (w.Orders.Count == 0)
        {
            this.WriteBestResult();
            this.Log(LogOption.End, message: "All orders finished!");
            Environment.Exit(0);
        }
    }
    private void RemoveFinishedOperationsFromOrders(List<Order> order, List<Resource> resources)
    {
        var operationsToRemove = new List<Operation>();
        foreach (var o in order)
        {
            var rangeIndex = o.Operations.Count;
            for (int i = 0; i < o.Operations.Count - 1; i++)
            {
                var op = o.Operations[i];
                if(op.Start >= this.SimulationStart)
                {
                    rangeIndex = i;
                    break;
                }
            }
            // remove all orders after the first order that is not in the range anymore.
            operationsToRemove.AddRange(o.Operations.GetRange(rangeIndex, o.Operations.Count - rangeIndex));
            o.Operations.RemoveRange(rangeIndex, o.Operations.Count - rangeIndex);
        }
        
        // remove all opeations from the resources list
        foreach(var o in operationsToRemove)
        {
            var r = resources.Find(r => r.Name == o.Unit);
            if(r != null)
            {
                var i = r.Operations.FindIndex(op  => op == o);
                r.Operations.RemoveAt(i);
            }
        }

    }
    private void RemoveFinishedOperations(List<Resource> resources)
    {
        foreach (var resource in resources)
        {
            // Only remove operations that are not in the time slot
            var operations = resource.Operations;
            var rem = 0;
            for (int i = operations.Count -1 ; i >= 0; i--)
            {
                var op = operations[i];
                if(op.Start >= this.SimulationStart)
                {
                    if (op.Name.Contains("Maintenance"))
                    {
                        resource.Maintenance.Add(new(op.Start, op.End));
                        resource.Operations.RemoveAt(i);
                    }
                    else
                    {
                        // resource.Operations.RemoveAt(i);
                    }

                }
            }
            if (operations.Count == 0)
            {
                operations.Add(new Operation()
                { 
                    Name = "Idle", // Idle, Changeover, Clean, Process, Maintenance, Break
                    Unit = "",
                    Order = "",
                    Duration = 0.0,
                    Start = this.SimulationStart,
                    End = this.SimulationStart
                });
            }
        }
    } 

    private void RemoveOperationsFromResources(List<Resource> resources)
    {
        foreach (var resource in resources)
        {
            var operations = resource.Operations;

            for (int i = 0; i < operations.Count; i++)
            {
                var rem = 0;
                var op = operations[i];
                if(op.Start >= this.SimulationStart)
                {
                    if (op.Name.Contains("Maintenance"))
                    {
                        resource.Maintenance.Add(new(op.Start, op.End));
                    }
                    resource.Operations.RemoveAt(i + rem);
                    rem--;
                    continue;
                }
                if(op.Start < this.SimulationStart && op.End >= this.SimulationStart)
                {
                    if (op.Name.Contains("Changeover"))
                    {
                        resource.Operations.RemoveAt(i + rem);
                        rem--;
                        continue;
                    }
                }
            }
            if (operations.Count == 0)
            {
                operations.Add(new Operation()
                { 
                    Name = "Idle", // Idle, Changeover, Clean, Process, Maintenance, Break
                    Unit = "",
                    Order = "",
                    Duration = 0.0,
                    Start = this.SimulationStart,
                    End = this.SimulationStart
                });
            }
            if (operations.Count > 0 && operations[^1].Name.Contains("Maintenance"))
            {
                operations.Add(new Operation()
                {
                    Name = "Idle", // Idle, Changeover, Clean, Process, Maintenance, Break
                    Unit = "",
                    Order = "",
                    Duration = 0.0,
                    Start = operations[^1].End,
                    End = operations[^1].End
                });
                if (operations.Count > 3)
                {
                    var operationsCount = operations.Count;
                    var operationBeforeMaintenance = operations[operations.Count - 3];
                    if (operationBeforeMaintenance.Order != "")
                    {
                        operations[^1].Order = operationBeforeMaintenance.Order;
                    }


                }
            }
            if (false)
            {

                for (int i = resource.Operations.Count - 1; i >= 0; i--)
                {
                    var op = resource.Operations[i];
                    if (op.Start > this.SimulationStart)
                    {
                        if (op.Name.Contains("Maintenance"))
                        {
                            resource.Maintenance.Add(new(op.Start, op.End));
                        }
                        resource.Operations.RemoveAt(i);
                    }
                    else if (op.Start <= this.SimulationStart && op.End >= this.SimulationStart)
                    {
                        if (op.Name.Contains("Changeover"))
                        {
                            //op.End = this.SimulationStart;
                            resource.Operations.RemoveAt(i);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
    public void Modify(UpdateType updateType)
    {

        // First sort the population according to the fitness
        Population = Population.OrderBy(x => x.Fitness[Objective]).ToList();
        var w = Population[0].Worker;
        var p = Population[0];
        var copyOrders = Simulator.CopyOrders(p.Worker.Orders);
        var copyResources = Simulator.CopyResources(p.Worker.Resources);

        if (Simulator is CSharpSimulator)
        {
            // Remove operations from resources
            /*
            List<Operation> operationsList = new();
            for (int i = 0; i < w.Orders.Count; i++)
            {
                var ord = w.Orders[i];
                if (ord.Operations.Count == 0) continue;
                RemoveOperationsAfterStart(ord.Operations);
            }
            operationsList = w.Orders.SelectMany(x => x.Operations).ToList();
            */
            // remove order if the last operation ends before simualtion statr
            for (int i = 0; i < w.Orders.Count; i++)
            {
                var lastOperation = w.Orders[i].Operations.Last();
                if (lastOperation.End < SimulationStart)
                {
                    var gene = ((Gene<String>)p.Genes[0]).Values;
                    var j = gene.FindIndex(x => x == lastOperation.Order);
                    gene.RemoveAt(j);
                    w.Orders.RemoveAt(i);
                    i--;
                }
                if (w.Orders.Count == 0)
                {
                    this.WriteBestResult();
                    this.Log(LogOption.End, message: "No more orders to allocate");
                    Environment.Exit(0);
                }
                // TODO: If w.Orders.Count == 0 -> this.Stop
            }
            // Copy Orders does not work somehow. Need to fix it!!

            // Remove operations from orders
            for (int i = 0; i < w.Orders.Count; i++)
            {
                var order = w.Orders[i];
                order.Operations = new List<Operation>();
                order.Operations.Add(new Operation()
                {
                    Name = "Wait_S0", // Wait, Process, End, Transfer
                    Unit = "",
                    Order = order.Name,
                    Duration = 0.0,
                    Start = this.Config.SimulationStart,
                    End = this.Config.SimulationStart
                });
                //RemoveOperationsAfterStart(order.Operations);
            }
            /*
            foreach (var res in w.Resources)
            {
                for(int i = res.Operations.Count - 1; i >= 0; i--)
                {
                    var op = res.Operations[i];
                    if(operationsList.Contains(op) || op.Name.Contains("Idle"))
                    {
                        break;
                    }
                    else
                    {
                        res.Operations.RemoveAt(i);
                    }
                    if (op.Name.Contains("Maintenance") || op.Name.Contains("Idle")) continue;
                    else
                    {
                    }
                }

                if (res.Maintenance != null && res.Maintenance.Count > 0)
                {
                    var maintenance = res.Maintenance[^1];
                    var duration = (maintenance.Item2 - maintenance.Item1).TotalHours;
                    var lastOperation = res.Operations[^1];
                    var start = new DateTime();
                    if (lastOperation.End > this.SimulationStart)
                    {
                        start = lastOperation.End;
                    }
                    else
                    {
                        start = maintenance.Item1;
                    }
                    var lastOrder = "";
                    if (res.Operations.Count > 0)
                    {
                        if (res.Operations[^1].Name != "")
                        {
                            lastOrder = res.Operations[^1].Order;
                        }
                    }
                    res.Operations.Add(new Operation()
                    {
                        Name = "Maintenance",
                        Order = "",
                        Start = start,
                        End = start.AddHours(duration),
                        Duration = duration,
                        Unit = res.Name
                    });

                    res.Operations.Add(new Operation()
                    {
                        Name = "Idle",
                        Order = lastOrder,
                        Start = start.AddHours(duration),
                        End = start.AddHours(duration),
                        Duration = 0.0,
                        Unit = res.Name
                    });
                    res.Maintenance.RemoveAt(0);
                }
            }
            */
            // Remove operations from resources
            /*
            for (int i = 0; i < w.Resources.Count; i++)
            {
                var res = w.Resources[i];
                if (res.Operations.Count == 0) continue;
                RemoveOperationsAfterStart(res.Operations);
            }
            */
            //RemoveOperationsAfterStartFromResources(w.Resources);
            RemoveOperationsFromResources(w.Resources);

            foreach (var res in w.Resources)
            {
                if (res.Maintenance != null && res.Maintenance.Count > 0)
                {
                    var maintenance = res.Maintenance[^1];
                    var duration = (maintenance.Item2 - maintenance.Item1).TotalHours;
                    var lastOperation = res.Operations[^1];
                    var start = new DateTime();
                    if (lastOperation.End > this.SimulationStart)
                    {
                        start = lastOperation.End;
                    }
                    else
                    {
                        start = maintenance.Item1;
                    }
                    var lastOrder = "";
                    if (res.Operations.Count > 0)
                    {
                        if (res.Operations[^1].Name != "")
                        {
                            lastOrder = res.Operations[^1].Order;
                        }
                    }
                    res.Operations.Add(new Operation()
                    {
                        Name = "Maintenance",
                        Order = "",
                        Start = start,
                        End = start.AddHours(duration),
                        Duration = duration,
                        Unit = res.Name
                    });

                    res.Operations.Add(new Operation()
                    {
                        Name = "Idle",
                        Order = lastOrder,
                        Start = start.AddHours(duration),
                        End = start.AddHours(duration),
                        Duration = 0.0,
                        Unit = res.Name
                    });
                    res.Maintenance.RemoveAt(0);
                }

            }
        }
        else
        {
            // Remove Orders that started
            for (int i = 0; i < w.Orders.Count; i++)
            {
                var order = w.Orders[i];
                order.Operations = order.Operations.FindAll(x => x.Start < SimulationStart && !x.Name.Contains("Wait"));
                if (order.Operations.Count > 0)
                {
                    order.Started = true;
                    ((Gene<String>)p.Genes[0]).Values.Remove(order.Name);
                }
                else
                {
                    w.Orders.RemoveAt(i);
                    i -= 1;
                }

            }

            // Set resources
            for (int i = 0; i < w.Resources.Count; i++)
            {
                var res = w.Resources[i];
                RemoveOperationsAfterStart(res.Operations);
                /*
                for(int j = 0; j < res.Operations.Count; j++)
                {
                    var op = res.Operations[j]; 
                    if(op.Start > SimulationStart)
                    {
                        res.Operations.RemoveAt(j);
                        j -= 1;
                    }
                }
                */

            }

        }
#if KOPANOS_SIMULATOR
        // Set durations TODO: TEST!!!
        if (Simulator is KopanosSimulator)
        {
            w.Orders.ForEach(o =>
            {
                o.Operations.ForEach(op =>
                {
                    if (op.Start < SimulationStart && op.End < SimulationStart && !op.Unit.Contains("Buffer"))
                    {
                        op.Duration = (op.End - op.Start).TotalSeconds / 10_000;
                    }
                });
            });

            // Find allocations! 
            var unitOrderAlloc = new Dictionary<string, Dictionary<string, bool>>();
            for (int i = 0; i < w.Resources.Count; i++)
            {
                var resource = w.Resources[i];
                resource.Allocate = new Dictionary<string, bool>();

                if (resource.Operations == null) continue;
                var selectedOperations = new List<Operation>();
                var tempCollection = resource.Operations.Where(operation => operation != null &&
                    operation.Start < this.SimulationStart && operation.Name == "Process").Select(o => o);
                var cnt = tempCollection.Count();
                if (!tempCollection.IsNullOrEmpty()) selectedOperations.AddRange(tempCollection.ToList());

                if (selectedOperations.Count() == 0) continue;
                for (int j = 0; j < selectedOperations.Count; j++)
                {
                    var operation = resource.Operations[j];
                    if (!unitOrderAlloc.ContainsKey(resource.Name))
                    {
                        unitOrderAlloc.Add(resource.Name,
                            new Dictionary<string, bool>() { { operation.Order, true } });
                    }
                    else
                    {
                        unitOrderAlloc[resource.Name].Add(operation.Order, true);
                    }

                    // Find the associated recipe and stage to the order 
                    var recipe = w.Orders.Find(o => o.Name == operation.Order).Recipe;
                    var stage = "";
                    foreach (var stageMachines in ((KopanosSimulator)Simulator).StageMachineMap)
                    {
                        if (stageMachines.Value.Contains(resource.Name))
                        {
                            stage = stageMachines.Key;
                            break;
                        }
                    }

                    // Find all other units that are part of the unit pool to deallocate them
                    var unitNotToAlloc = ((KopanosSimulator)Simulator).RecipeMachineMap[recipe][stage]
                        .Where(x => x != operation.Unit).ToList();
                    foreach (var unit in unitNotToAlloc)
                    {
                        if (!unitOrderAlloc.ContainsKey(unit))
                        {
                            unitOrderAlloc.Add(unit,
                                new Dictionary<string, bool>() { { operation.Order, false } });
                        }
                        else
                        {
                            unitOrderAlloc[unit].Add(operation.Order, false);
                        }
                    }

                }
            }

            // Set allocations
            if (unitOrderAlloc.Count() > 0)
            {
                foreach (var unitOrder in unitOrderAlloc)
                {
                    var r = w.Resources.Find(r => r.Name == unitOrder.Key);
                    foreach (var orderAlloc in unitOrderAlloc[unitOrder.Key])
                    {
                        r.Allocate.Add(orderAlloc.Key, orderAlloc.Value);
                    }
                }
            }
        }
        if (typeof(Simulator) != typeof(CSharpSimulator))
        {
            ConcatGenes(w, p, copyOrders, copyResources);
        }
#endif
        if (updateType == UpdateType.Time)
        {
            w.Resources = copyResources;
        }
        //CopyGenesToPopulation(w, p, copyOrders, copyResources);



        // Copy results to each individuals gene and worker
        /*
        var gene = p.Genes[0];
        w.InosimInstance.SimulationStart = this.SimulationStart;
        w.InosimInstance.SimulationEnd = this.SimulationEnd;
        foreach (var individual in this.Population)
        {
            if (individual == Population[0])
            {
                individual.Worker.InosimInstance.SimulationStart = this.SimulationStart;
                individual.Worker.InosimInstance.SimulationEnd = this.SimulationEnd;
                individual.Worker.InosimInstance.Objectives = new Dictionary<string, double>();
                individual.FitnessName = ((CSharpConfig)Config).Objective;
                individual.Fitness = new Dictionary<string, double>();
                continue;
            }
            individual.Genes[0] =  gene.Copy();
            individual.Mutate();
            //individual.Genes[0].Mutation();
            // change the other 
            individual.Worker.Orders = this.Simulator.CopyOrders(w.Orders);
            individual.Worker.Resources = this.Simulator.CopyResources(w.Resources);
            individual.Worker.InosimInstance.SimulationStart = this.SimulationStart;
            individual.Worker.InosimInstance.SimulationEnd = this.SimulationEnd;
            individual.Worker.InosimInstance.Objectives = new Dictionary<string, double>();
            individual.FitnessName = ((CSharpConfig)Config).Objective;
            individual.Fitness = new Dictionary<string, double>();
        }
        w.Orders = copyOrders;
        w.Resources = copyResources;
        //w.Orders.AddRange(this.Simulator.CreateOrders((Gene<string>)gene));
        */
    }
    public void Recombine()
    {
        // TODO: Check if the fitness function name is set
        Offspring = new List<Individual>();
        // take orders from the best parent?
        // List<PlantSchedule.DTO.Order> openOrders = ((Individual)this.Population[0]).Worker.Orders.Where(x => x.Started == false).ToList();
        // List<PlantSchedule.DTO.Order> fixedOrders = ((Individual)this.Population[0]).Worker.Orders.Where(x => x.Started == true).ToList();

        // Iterate over the selected parents and recombine them
        // Generates two offsprings per parent pair
        foreach (var parents in selectedParents)
        {
            var parentIdx1 = this.Population.FindIndex(p => p.Id == parents.Item1);
            if (parentIdx1 == -1) parentIdx1 = new Random().Next(0, selectedParents.Count);
            var parent1 = this.Population[parentIdx1];
            var geneValues1 = ((Gene<string>)parent1.Genes[0]).Values;
            /*
            foreach (var fixedOrder in fixedOrders)
            {
                if(geneValues1.Contains(fixedOrder.Name)) geneValues1.Remove(fixedOrder.Name); 
            }
            */


            var parentIdx2 = this.Population.FindIndex(p => p.Id == parents.Item2);
            if (parentIdx2 == -1) parentIdx2 = new Random().Next(0, selectedParents.Count);
            var parent2 = this.Population[parentIdx2];
            var geneValues2 = ((Gene<string>)parent1.Genes[0]).Values;
            /*
            foreach (var fixedOrder in fixedOrders)
            {
                if(geneValues2.Contains(fixedOrder.Name)) geneValues1.Remove(fixedOrder.Name); 
            }
            */
            (IGene gene1, IGene gene2) = parent1.Genes[0].Recombine(parent2.Genes[0]);
            AddOffspring(gene1, this.Population[0]);
            AddOffspring(gene2, this.Population[0]);

            //AddOffspring(gene1, fixedOrders);
            //AddOffspring(gene2, fixedOrders);
            /*
            Offspring.Add(new Individual(new List<IGene>() { gene1 }));
            var worker1 = ((Individual)Offspring[^1]).Worker;
            worker1.InosimInstance.SimulationStart = this.SimulationStart;
            worker1.InosimInstance.SimulationEnd = this.SimulationEnd;
            ((Individual)Offspring[^1]).Worker.Orders.AddRange(Simulator.CopyOrders(fixedOrders));
            if(fixedOrders.Count > 0) ((Individual)Offspring[^1]).Worker.Orders.AddRange(Simulator.CopyOrders(fixedOrders));
            foreach (var order in ((Individual)Offspring[^1]).Worker.Orders)
            {
                RemoveOperationsAfterStart(order.Operations);
            }

            Offspring.Add(new Individual(new List<IGene>() { gene2 }));
            ((Individual)Offspring[^1]).Worker.Orders.AddRange(Simulator.CopyOrders(fixedOrders));
            if(fixedOrders.Count > 0) ((Individual)Offspring[^1]).Worker.Orders.AddRange(Simulator.CopyOrders(fixedOrders));
            foreach (var order in ((Individual)Offspring[^1]).Worker.Orders)
            {
                RemoveOperationsAfterStart(order.Operations);
            }
            */

        }
    }

    private void AddOffspring(IGene gene, Individual ind)
    {
        Offspring.Add(new Individual(new List<IGene>() { gene }));
        var worker = ((Individual)Offspring[^1]).Worker;
        worker.SimulationInstance.SimulationStart = this.SimulationStart;
        worker.SimulationInstance.SimulationEnd = this.SimulationEnd;
        worker.Orders = new List<Order>();
        worker.Resources = this.Simulator.CopyResources(ind.Worker.Resources);
        foreach (var res in worker.Resources)
        {
            RemoveOperationsAfterStart(res.Operations);
        }
    }

    private void AddOffspring(IGene gene, List<Order> fixedOrders)
    {
        Offspring.Add(new Individual(new List<IGene>() { gene }));
        var worker1 = ((Individual)Offspring[^1]).Worker;
        worker1.SimulationInstance.SimulationStart = this.SimulationStart;
        worker1.SimulationInstance.SimulationEnd = this.SimulationEnd;
        ((Individual)Offspring[^1]).Worker.Orders.AddRange(Simulator.CopyOrders(fixedOrders));
        if (fixedOrders.Count > 0) ((Individual)Offspring[^1]).Worker.Orders.AddRange(Simulator.CopyOrders(fixedOrders));
        foreach (var order in ((Individual)Offspring[^1]).Worker.Orders)
        {
            RemoveOperationsAfterStart(order.Operations);
        }
    }

    public void Mutate()
    {
        foreach (var offspring in Offspring)
        {
            var randomDouble = new Random().NextDouble();
            if (this.MutationRate > randomDouble)
            {
                offspring.Mutate();
            }
        }
    }

    private void Init()
    {
        Offspring.ForEach(x => x.Init());
    }

    public void SelectSurvivors()
    {
        // Set temp container
        var allIndividuals = new List<Individual>();
        allIndividuals.AddRange(Offspring);
        allIndividuals.AddRange(Population);

        // Check if the objective is set
        allIndividuals.ForEach(x => { if (!x.Fitness.ContainsKey(Objective)) x.Fitness[Objective] = Double.MaxValue; });

        // Sort individuals according to their fitness
        allIndividuals = allIndividuals.OrderBy(x => x.Fitness[Objective]).ToList();

        // Select the survivors for the elitist fraction
        var remainingPopulationSize = PopulationSize - ElitistSize;
        //var elitistPop = allIndividuals.Take(remainingPopulationSize);
        Population = ElitistSelection(allIndividuals, ElitistSize, Objective);

        // Select the remaining population. Selection should be a function pointer
        var randPop = allIndividuals.Skip(ElitistSize).ToList();
        Population.AddRange(SurvivorSelection(allIndividuals.Skip(ElitistSize).ToList(), remainingPopulationSize, Objective));

        // Order current population
        Population = Population.OrderBy(x => x.Fitness[Objective]).ToList();

        // Increase age
        Population.ForEach(x => x.Age += 1);

        // Clear container
        allIndividuals.Clear();
        Offspring.Clear();
    }

    public void Evaluate(EvalOpt option = EvalOpt.Init)
    {
        this.PlantTime = DateTime.Now;
        Simulator.SimulationStart = SimulationStart;
        Simulator.SimulationEnd = SimulationEnd;
        switch (option)
        {
            case EvalOpt.Init:
                Simulator.Simulate(this.Offspring, LogWriter, SimulationStart);
                Simulator.Simulate(this.Population, LogWriter, SimulationStart);
                Population.ForEach(x => { if (!x.Fitness.ContainsKey(Objective)) x.Fitness[Objective] = Double.MaxValue; });
                Population = Population.OrderBy(x => x.Fitness[Objective]).ToList();
                break;
            case EvalOpt.Population:
                Simulator.Simulate(this.Population, LogWriter, SimulationStart);
                Population.ForEach(x => { if (!x.Fitness.ContainsKey(Objective)) x.Fitness[Objective] = Double.MaxValue; });
                Population = Population.OrderBy(x => x.Fitness[Objective]).ToList();
                break;
            case EvalOpt.Offspring:
                Simulator.Simulate(this.Offspring, LogWriter, SimulationStart);
                break;
            default:
                break;
        }
    }

    public void Log(LogOption option = LogOption.Custom, LogType logType = LogType.INFO, string message = "")
    {
        var logMessage = "";
        var logTypeName = "[" + LogType.INFO.ToString() + "] ";
        if (logTypeName.Contains("CUSTOM")) logTypeName = "";

        switch (option)
        {
            case LogOption.Start:
                logMessage = $"[INFO] -------------------------------------------------------------\n" +
                             $"[INFO] Start generation {this.Generation} at {this.Stopwatch.Elapsed}";
                LogWriter.WriteLine(logMessage);
                Console.WriteLine(logMessage);
                break;
            case LogOption.EndOfGeneration:
                GetStatsOfPopulation(this);
                logMessage = "";
                Console.WriteLine("[INFO] Generation;Id;Fitness;Age;Distance;Mean;Std;Simulation Start;Simulation End;Time elapsed;");
                var count = 0;
                var consoleMessage = new StringBuilder();
                foreach (var individual in this.Population)
                {
                    logMessage =
                        $"{this.Generation.ToString("D4")};" +
                        $"{individual.Id.ToString("D10")};" +
                        $"{((Individual)individual).Fitness[Objective].ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{((Individual)individual).Age.ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{((Individual)individual).Measure.ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{this.Mean.ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{this.Var.ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{((Individual)individual).Worker.SimulationInstance.SimulationStart};" +
                        $"{((Individual)individual).Worker.SimulationInstance.SimulationEnd};" +
                        $"{Stopwatch.Elapsed}";
                    if (count < 10)
                    {
                        consoleMessage.AppendLine("[INFO] " + logMessage);
                        count++;
                    }
                    LogWriter.WriteLine("[INFO] " + logMessage);
                    // log only json worker of best individual
                    if (this.Population.IndexOf(individual) == 0)
                    {
                        individual.Json = JsonSerializer.Serialize(individual.Worker);
                        logMessage += $";{((Individual)individual).Json}";
                        ResultsWriter.WriteLine(logMessage);
                        break;
                    }
                    else logMessage += ";";
                    ResultsWriter.WriteLine(logMessage);
                }
                Console.WriteLine(consoleMessage);
                ResultsWriter.Flush();
                break;
            case LogOption.Init:
                GetStatsOfPopulation(this);
                logMessage = "";
                Console.WriteLine("[INFO] Generation;Id;Fitness;Age;Distance;Mean;Std;Simulation Start;Simulation End;Time elapsed;");
                count = 0;
                foreach (var individual in this.Population)
                {
                    logMessage =
                        $"{this.Generation.ToString("D4")};" +
                        $"{individual.Id.ToString("D10")};" +
                        $"{((Individual)individual).Fitness[Objective].ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{((Individual)individual).Age.ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{((Individual)individual).Measure.ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{this.Mean.ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{this.Var.ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{((Individual)individual).Worker.SimulationInstance.SimulationStart};" +
                        $"{((Individual)individual).Worker.SimulationInstance.SimulationEnd};" +
                        $"{Stopwatch.Elapsed}";
                    Console.WriteLine("[INFO] " + logMessage);
                    count++;
                    LogWriter.WriteLine("[INFO] " + logMessage);
                    if (this.Population.IndexOf(individual) == 0)
                    {
                        individual.Json = JsonSerializer.Serialize(individual.Worker);
                        logMessage += $";{((Individual)individual).Json}";
                        ResultsWriter.WriteLine(logMessage);
                        break;
                    }
                    ResultsWriter.WriteLine(logMessage);
                }

                /*
                foreach (var individual in this.Offspring)
                {
                    logMessage =
                        $"{this.Generation.ToString("D4")};" +
                        $"{individual.Id.ToString("D10")};" +
                        $"{((Individual)individual).Fitness[Objective].ToString("F2",CultureInfo.InvariantCulture)};" +
                        $"{((Individual)individual).Age.ToString("F2",CultureInfo.InvariantCulture)};" +
                        $"{((Individual)individual).Measure.ToString("F2",CultureInfo.InvariantCulture)};" +
                        $"{this.Mean.ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{this.Var.ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{((Individual)individual).Worker.InosimInstance.SimulationStart};" +
                        $"{((Individual)individual).Worker.InosimInstance.SimulationEnd};" +
                        $"{Stopwatch.Elapsed}";
                    if(count < 10)
                    {
                        Console.WriteLine("[INFO] " + logMessage);
                        count++;
                    }
                    LogWriter.WriteLine("[INFO] " + logMessage);
                    ResultsWriter.WriteLine(logMessage);
                }
                */

                ResultsWriter.Flush();
                break;
            case LogOption.End:
                logMessage = $"[INFO] -------------------------------------------------------------\n" +
                             $"[INFO] End Programm in Generation {this.Generation} after {this.Stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.ff")} elapsed time.\n" +
                             $"[INFO] -------------------------------------------------------------\n";
                LogWriter.WriteLine(logMessage);
                Console.WriteLine(logMessage);
                break;
            case LogOption.Update:
                break;
            case LogOption.Custom:
                logMessage = "";
                if (String.IsNullOrEmpty(message))
                {
                    break;
                }
                else
                {
                    logMessage = message;
                }
                Console.WriteLine(logTypeName + logMessage);
                LogWriter.WriteLine(logTypeName + logMessage);
                break;
        }
    }
}