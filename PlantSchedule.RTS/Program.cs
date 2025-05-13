#define UPDATEEVENTS
//#undef UPDATEEVENTS
#define UPDATEPARAMS
//#undef UPDATEPARAMS
#define RUSHORDERS
#undef RUSHORDERS
#define TIMEUPDATE
//#undef TIMEUPDATE
using PlantSchedule.RTS;
using System.Globalization;
using System.Reflection;

CultureInfo englishCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = englishCulture;

// Get config
var currPath = Directory.GetCurrentDirectory();
string? assembyDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
if (assembyDirectoryPath == null) throw new Exception("Assembly directoy path could not be found");
var configPath = Path.Combine(currPath, "config.json");
Console.WriteLine($"{currPath} \n{assembyDirectoryPath}");

if(args.Length > 0) configPath = args[0];
var config = new CSharpConfig().Deserialize(configPath);

// Init ea
var ea = new EvolutionaryAlgorithm(config);
//var cSharpSimulator = new CSharpSimulator(config);
//ea.Simulator = cSharpSimulator;

// Copy config
config.CopyConfigToResultsFolder(currPath);

//ea.RunClaireVoyant();
ea.Run();

#if false

// Define termination criterion
ea.TerminationCriterion = () => ea.Generation == config.Generations; // 24*4;
//ea.TerminationCriterion = () => (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape);

// 32 threads -> 32 parallel simulations. 3 seconds per 32 parallel simulations. -> 64 Individuals : 6 sec -> 128 Individuals : 12 sec


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
            UpdateTime = config.SimulationStart.AddSeconds(rushOrder.Value[0] * config.TimeIncrement), // We could also just set the config.TimeIncrement = 0
            Orders = new List<string>() { rushOrder.Key },
            UpdateType = UpdateType.Order | UpdateType.Time
        };

        if (ea.GenerationUpdateEventDictionary.ContainsKey(updateEvent.UpdateGeneration))
        {
            ea.GenerationUpdateEventDictionary[updateEvent.UpdateGeneration].Orders.AddRange(updateEvent.Orders);
            ea.GenerationUpdateEventDictionary[updateEvent.UpdateGeneration].UpdateType |= updateEvent.UpdateType;
        }
        else
        {
            ea.GenerationUpdateEventDictionary.Add(updateEvent.UpdateGeneration, updateEvent);
        }

        ea.UpdateEvents.Push(new UpdateEvent()
        {
            // In which generation the order is released
            UpdateGeneration = rushOrder.Value[0],
            UpdateTime = config.SimulationStart.AddSeconds(rushOrder.Value[0] * config.TimeIncrement), // We could also just set the config.TimeIncrement = 0
            Orders = new List<string>() { rushOrder.Key },
            UpdateType = UpdateType.Order | UpdateType.Time
        });
        orderCnt++;
    }
}

// If the changeover started before the simulation we want at least the next operation after the changeover to happen.
var updateGeneration = config.UpdateGeneration;
var updateInterval = config.TimeInterval; // 10, 25, 50, 100
var secondsPerGeneration = config.TimeIncrement; // 9, 18, 36, 72 every 50 generations you traverse 30 minutes 50 * 36 = 

// Set update events
// Change name to IsUpdateEvent
ea.UpdateCriterion = () =>
{
    if (ea.Generation == updateGeneration)
    {
        if (ea.GenerationUpdateEventDictionary.ContainsKey(updateGeneration))
        {
            ea.GenerationUpdateEventDictionary[updateGeneration].UpdateType |= UpdateType.Time;
        }
        else
        {
            ea.GenerationUpdateEventDictionary[updateGeneration] = new UpdateEvent()
            {
                UpdateGeneration = ea.Generation,
                UpdateTime = config.SimulationStart.AddSeconds(ea.Generation * config.TimeIncrement),
                UpdateType = UpdateType.Time
            };
        }
        return true;
    }
    else if (ea.GenerationUpdateEventDictionary.ContainsKey(updateGeneration))
    {
        return true;
    }
    else
    {
        return false;
    }
};

#if false
#if UPDATEEVENTS
{
#if TIMEUPDATE
    if(ea.Generation == updateGeneration)
    {
        var updateEvent = new UpdateEvent()
        {
            UpdateGeneration = ea.Generation,
            UpdateTime = config.SimulationStart.AddSeconds(ea.Generation*config.TimeIncrement),
            UpdateType = UpdateType.Time
        };

        // Peek updateEvent 
        if (ea.GenerationUpdateEventDictionary.ContainsKey(updateGeneration))
        {
            ea.GenerationUpdateEventDictionary[updateGeneration].UpdateType |= UpdateType.Time;
        }

        var tempUpdateEvents = new Stack<UpdateEvent>();
        // Check if two updates happen at the same time!!
        while(ea.UpdateEvents.Count() > 0 && ea.UpdateEvents.Peek().UpdateGeneration <= updateEvent.UpdateGeneration)
        {
            tempUpdateEvents.Push(ea.UpdateEvents.Pop());
        }

        ea.UpdateEvents.Push(updateEvent);

        while(tempUpdateEvents.Count > 0)
        {
            ea.UpdateEvents.Push(tempUpdateEvents.Pop());
        }

        //ea.UpdateEvents.Push();
        updateGeneration = ea.UpdateEvents.Peek().UpdateGeneration + updateInterval;

    }
#endif
    /*
    var updates = ea.UpdateEvents;
    ea.GenerationUpdateEventDictionary.Count();
    bool hasUpdates = updates.Count > 0;
    if (!hasUpdates)
    {
        return false;
    }
    else
    {
    */
        bool isUpdateGeneration = ea.GenerationUpdateEventDictionary.ContainsKey(ea.Generation); // (updates.Peek().UpdateGeneration == ea.Generation); // || updates.Peek().UpdateTime <= ea.PlantTime);
        //return isUpdateGeneration;
    /*
    }
    */
};
#else
            { 
    return false; 
};
#endif
#endif

ea.UpdateParameters = () =>
#if UPDATEPARAMS
{
    var countDuplicates = 0;
    for (int i = 1; i < ea.Population.Count; i++)
    {
        if (ea.Population[i].GetFitness(ea.Objective) == ea.Population[i - 1].GetFitness(ea.Objective))
        {
            ea.Population[i].Mutate();
            ea.Population[i].Fitness[ea.Objective] += 10; // ea.Population[ea.Population.Count - 1].Fitness[ea.Objective] + 10; //add penalty to duplicates
            countDuplicates += 1;
        }
    }

    // Order population 
    ea.OrderPopulationByObjective();
    //ea.Population = ea.Population.OrderBy(individual => individual.GetFitness(ea.Objective)).ToList();
    GetStatsOfPopulation(ea);
    MeasureDistance(ea);
    ChangeEAParameters(ea, countDuplicates);

    ea.Log(LogOption.Custom, message: $"Mean: {ea.Mean}, variance: {ea.Var}");
};
#else
{
    ea.OrderPopulationByObjective();
    GetStatsOfPopulation(ea);
    MeasureDistance(ea);
    ea.Log(LogOption.Custom, message: $"Mean: {ea.Mean}, variance: {ea.Var}");
};
#endif

ea.Run();

void ChangeEAParameters(EvolutionaryAlgorithm ea, int countDuplicates)
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
    else if(!avgAgeCriteria || !avgMeasureCriteria) 
    {
        ea.SetParametersFromConfig();
        ea.Log(LogOption.Custom, message: $"Elitist size: {ea.ElitistSize}, Mutation rate: {ea.MutationRate}, Mutation: {ea.GetMutationMethodName()}");
    }
}

void GetStatsOfPopulation(EvolutionaryAlgorithm ea)
{
    ea.Mean = ea.Population.Average(ind => ind.GetFitness(ea.Objective));
    // Step 2: Calculate the variance
    ea.Var = ea.Population
        .Select(ind => Math.Pow(ind.GetFitness(ea.Objective) - ea.Mean, 2))
        .Average();
}

void MeasureDistance(EvolutionaryAlgorithm ea)
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

// TODO: ESCAPE
// 1. Clairevoyant schedule
// - Read past schedules
// - Initialize with past schedules
// 2. Influence of reaction time
// - Constrained by: Simulation time, degree of parallelisation

// TODO: 
// 1. Fix genomes after updates
// 2. check for the right update times!!!
// 3. Fix Offspring Size
// 4. Add different selection types
// 5. Remove null-operations from resources after the simulation writes the results. Must be an issue of the simulaiton
// 6. Add Age

// TODO:
// ADD ALLOCATION FOR OPERATIONS OF ORDERS THAT ARE ALREADY FINISHED 
// DUE TO SETTING THE OPERATIONS TO ZERO THE ORDER ORDER GETS MESSED UP!!!


// levensthien distance matrix
// P1 P2 P3 P4
// P1 0  1  2  1
// P2 1  0  3  4
// P3 2  3  0  1
// P4 1  4  0  1
// 
// N-gram: find the longest consecutive sequence?
// 2, 3, 4, 5, 6, if 2x2 then 4?
// 1 2 3 4 5
// 3 4 1 2 5 -> 2-Gram: (3, 4), (1, 2) -> 2*2 = 4
// 5 1 2 3 4 -> 4-Gram: (1, 2, 3, 4) -> 4*1 = 4

#endif
