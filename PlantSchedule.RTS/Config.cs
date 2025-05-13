using System.Text.Json;

namespace PlantSchedule.RTS;
public interface IConfig
{
    public int NumberOfWorkers { get; set; }
    public String ResultDirectory { get; set; }
    public String TimeStamp { get; set; }
    public String BasePath { get; set; }
    public String ResultFolder { get; set; }
    // public Dictionary<String, double> Objectives { get; set; }
    public DateTime SimulationStart { get; set; }
    public DateTime SimulationEnd { get; set; }
    public int TimeIncrement { get; set; }
    // EA Options
    public String InitializationPath { get; set; }
}

public class CSharpConfig : IConfig
{
    // Interface of ICSharpConfig
    public int NumberOfWorkers { get; set; }
    public string ResultDirectory { get; set; }
    public string TimeStamp { get ; set ; }
    public string BasePath { get; set; }
    public string ResultFolder { get ; set ; }
    public string CsvDirectory { get; set; }
    public bool ParallelExecution { get; set; }
    public bool EvalAllOrders { get; set; }

    // public Dictionary<string, double> Objectives { get ; set ; }
    public Dictionary<string, List<int>> RushOrders { get ; set ; }
    public Dictionary<string, List<int>> Maintenances { get ; set ; }
    public Dictionary<string, DateTime> ReleaseDates { get; set; }
    public DateTime SimulationStart { get; set; }
    public DateTime SimulationEnd { get; set; }
    public int TimeIncrement { get; set; } // Could be a float
    public int TimeInterval {  get; set; }
    public int UpdateGeneration { get; set; }

    // EA Options
    public string InitializationPath { get; set; }
    public int PopulationSize { get; set; }
    public int OffspringSize { get; set; }
    public int ParentsSize { get; set; }
    public string MutationMethod {get;set;}
    public double MutationRate { get; set; }
    public string CrossoverMethod {get;set;}
    public double CrossoverRate {get;set;}
    public string InitializationMethod {get;set;}
    public string Initialization { get; set; }
    public string Objective { get; set; }
    public string Service { get; set; }
    public string ParentSelection { get; set; }
    public string ElitistSelection { get; set; }
    public string SurvivorSelection { get; set; }
    public int ElitistSize { get; set; }
    public int Generations { get; set; }
    public string PopulationContinuity { get; set; }
    public bool UpdateParameters { get; set; }

    public CSharpConfig(CSharpConfig other)
    {
        // Folders and directories
        BasePath = other.BasePath;
        CsvDirectory = other.CsvDirectory;
        TimeStamp = DateTime.Now.ToString("yyMMdd_HHmmss"); //timeStamp ;
        ResultDirectory = other.BasePath + "\\" + other.ResultDirectory;
        if (other.ResultFolder == "") ResultFolder = ResultDirectory + TimeStamp + "\\";
        else ResultFolder = ResultDirectory + other.ResultFolder + "\\";
        if (!Directory.Exists(ResultFolder)) Directory.CreateDirectory(ResultFolder);
        
        // Databases
        NumberOfWorkers = other.NumberOfWorkers;
        // Objectives = other.Objectives;
        RushOrders = other.RushOrders;
        Maintenances = other.Maintenances;
        ReleaseDates = other.ReleaseDates;
        SimulationStart = other.SimulationStart;
        SimulationEnd = other.SimulationEnd;
        TimeIncrement = other.TimeIncrement;
        TimeInterval = other.TimeInterval;
        UpdateGeneration = other.UpdateGeneration;
        PopulationContinuity = other.PopulationContinuity;
        UpdateParameters = other.UpdateParameters;
        ParallelExecution = other.ParallelExecution;
        EvalAllOrders = other.EvalAllOrders;
        
        // Optional
        InitializationPath = other.InitializationPath;

        // EA Options
        PopulationSize = other.PopulationSize;
        OffspringSize = other.OffspringSize;
        ParentsSize = other.ParentsSize;
        MutationRate = other.MutationRate;
        CrossoverRate = other.CrossoverRate;
        Initialization = other.Initialization;
        Objective = other.Objective;
        Service = other.Service;
        ParentSelection = other.ParentSelection;
        ElitistSelection = other.ElitistSelection;
        SurvivorSelection = other.SurvivorSelection;
        ElitistSize = other.ElitistSize;
        MutationMethod = other.MutationMethod;
        CrossoverMethod = other.CrossoverMethod;
        InitializationMethod = other.InitializationMethod;
        Generations = other.Generations;
    }
    public CSharpConfig(
        int numberOfWorkers,
        string timeStamp,
        string basePath,
        string csvDirectory,
        string resultDirectory,
        bool parallelExecution,
        bool evalAllOrders,
        Dictionary<string, double> objectives, 
        Dictionary<string, List<int>> rushOrders,
        Dictionary<string, List<int>> maintenances,
        Dictionary<string, DateTime> releaseDates,
        DateTime simulationStart, 
        DateTime simulationEnd, 
        int timeIncrement,
        int timeInterval,
        int updateGeneration,
        string initializationPath,
        string resultFolder,
        int populationSize,
        int offspringSize,
        int parentSize,
        double mutationRate,
        double crossoverRate,
        string initialization,
        string objective,
        string service,
        string mutationMethod,
        string crossoverMethod,
        string initializationMethod,
        int generations,
        string populationContinuity,
        bool updateParameters,
        string parentSelection,
        string elitistSelection)
    {
        // Folders and directories
        BasePath = basePath;
        CsvDirectory = csvDirectory;
        TimeStamp = DateTime.Now.ToString("yyMMdd_HHmmss"); //timeStamp ;
        ResultDirectory = basePath + "\\" + resultDirectory;
        if (resultFolder == "") ResultFolder = ResultDirectory + TimeStamp + "\\";
        else ResultFolder = ResultDirectory + resultFolder + "\\";
        if (!Directory.Exists(ResultFolder)) Directory.CreateDirectory(ResultFolder);
        ParallelExecution = parallelExecution;
        EvalAllOrders = evalAllOrders;
        
        // Databases
        NumberOfWorkers = numberOfWorkers;
        // Objectives = objectives;
        RushOrders = rushOrders;
        Maintenances = maintenances;
        ReleaseDates = releaseDates;
        SimulationStart = simulationStart;
        SimulationEnd = simulationEnd;
        TimeIncrement = timeIncrement;
        TimeInterval = timeInterval;
        UpdateGeneration = updateGeneration;
        PopulationContinuity = populationContinuity;
        UpdateParameters = updateParameters;
        
        // Optional
        InitializationPath = initializationPath;

        // EA Options
        PopulationSize = populationSize;
        OffspringSize = offspringSize;
        ParentsSize = parentSize;
        MutationRate = mutationRate;
        CrossoverRate = crossoverRate;
        Initialization = initialization;
        Objective = objective;
        Service = service;
        MutationMethod = mutationMethod;
        CrossoverMethod = crossoverMethod;
        InitializationMethod = initializationMethod;
        Generations = generations;
        ParentSelection = parentSelection;
        ElitistSelection = elitistSelection;
    }

    public CSharpConfig()
    {
    }

    public CSharpConfig Deserialize(string currPath)
    {
        var config = JsonSerializer
            .Deserialize<CSharpConfig>(File.ReadAllText(currPath));
        return new CSharpConfig(config);
    }
    public CSharpConfig CopyConfigToResultsFolder(string currPath)
    {
        if (!Directory.Exists(ResultFolder)) Directory.CreateDirectory(ResultFolder);
        File.Copy(Path.Combine(currPath, "config.json"), ResultFolder + "config.json", true);
        return this;
    }
    public void Serialize()
    {
        var jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true});
        File.WriteAllText(Path.Combine(this.BasePath,"PlantSchedule.RTS\\config.json"), jsonString);
    }
}

public class PlantConfig : IConfig
{
    public int NumberOfWorkers { get ; set ; }
    public string ResultDirectory { get ; set ; }
    public string TimeStamp { get ; set ; }
    public string ResultFolder { get ; set ; }
    public string BasePath { get ; set ; }
    public Dictionary<string, double> Objectives { get ; set ; }
    public DateTime SimulationStart { get; set; }
    public DateTime SimulationEnd { get; set; }
    public int TimeIncrement { get; set; }
    // EA Options
    public String InitializationPath { get; set; }
    public PlantConfig(int numberOfWorkers, string resultDirectory, string timeStamp, System.Collections.Generic.Dictionary<string, double> objectives, DateTime simulationStart, DateTime simulationEnd, int timeIncrement)
    {
        NumberOfWorkers = numberOfWorkers;
        ResultDirectory = resultDirectory;
        TimeStamp = timeStamp;
        Objectives = objectives;
        SimulationStart = simulationStart;
        SimulationEnd = simulationEnd;
        TimeIncrement = timeIncrement;
        ResultFolder = ResultDirectory + TimeStamp + "\\";
        if (!Directory.Exists(ResultFolder)) Directory.CreateDirectory(ResultFolder);
    }
}

public class KopanosConfig : IConfig
{
    // Interface of IKopanosConfig
    public int NumberOfWorkers { get; set; }
    public string ResultDirectory { get; set; }
    public string TimeStamp { get ; set ; }
    public string BasePath { get; set; }
    public string ResultFolder { get ; set ; }
    public Dictionary<string, double> Objectives { get ; set ; }
    public DateTime SimulationStart { get; set; }
    public DateTime SimulationEnd { get; set; }
    public int TimeIncrement { get; set; }

    // Databse config
    public List<string> Databases { get; set; }
    public String Experiment {get;set;}
    public String Project {get;set;}
    public String Edition {get;set;}
    public String Version {get;set;}
    public String Visibility {get;set;}
    
    // EA Options
    public string InitializationPath { get; set; }
    
    public int PopulationSize { get; set; }
    public int OffspringSize { get; set; }
    public int ParentsSize { get; set; }
    public string MutationMethod {get;set;}
    public double MutationRate { get; set; }
    public string CrossoverMethod {get;set;}
    public double CrossoverRate {get;set;}
    public string InitializationMethod {get;set;}
    public string Initialization { get; set; }
    public string Objective { get; set; }
    public string Service { get; set; }
    public string ParentSelection { get; set; }
    public string ElitistSelection { get; set; }
    public string SurvivorSelection { get; set; }
    public int ElitistSize { get; set; }
    public int Generations { get; set; }

    public KopanosConfig() { }

    public KopanosConfig(KopanosConfig other)
    {
        // Folders and directories
        BasePath = other.BasePath;
        TimeStamp = DateTime.Now.ToString("yyMMdd_HHmmss"); //timeStamp ;
        ResultDirectory = other.BasePath + "\\" + other.ResultDirectory;
        if (other.ResultFolder == "") ResultFolder = ResultDirectory + TimeStamp + "\\";
        else ResultFolder = ResultDirectory + other.ResultFolder + "\\";
        if (!Directory.Exists(ResultFolder)) Directory.CreateDirectory(ResultFolder);
        
        // Databases
        Databases = other.Databases.Select(x => other.BasePath + "\\" + x).ToList();
        Experiment = other.Experiment;
        Edition = other.Edition;
        NumberOfWorkers = other.NumberOfWorkers;
        Project = other.Project;
        Version = other.Version;
        Visibility = other.Visibility;
        Objectives = other.Objectives;
        SimulationStart = other.SimulationStart;
        SimulationEnd = other.SimulationEnd;
        TimeIncrement = other.TimeIncrement;
        
        // Optional
        InitializationPath = other.InitializationPath;

        // EA Options
        PopulationSize = other.PopulationSize;
        OffspringSize = other.OffspringSize;
        ParentsSize = other.ParentsSize;
        MutationRate = other.MutationRate;
        CrossoverRate = other.CrossoverRate;
        Initialization = other.Initialization;
        Objective = other.Objective;
        Service = other.Service;
        ParentSelection = other.ParentSelection;
        ElitistSelection = other.ElitistSelection;
        SurvivorSelection = other.SurvivorSelection;
        ElitistSize = other.ElitistSize;
        MutationMethod = other.MutationMethod;
        CrossoverMethod = other.CrossoverMethod;
        InitializationMethod = other.InitializationMethod;
        Generations = other.Generations;
    }
    public KopanosConfig(
        string experiment, 
        List<string> databases, 
        string edition, 
        int numberOfWorkers, 
        string project, 
        string version, 
        string visibility, 
        string timeStamp, 
        string basePath, 
        string resultDirectory, 
        Dictionary<string, double> objectives, 
        DateTime simulationStart, 
        DateTime simulationEnd, 
        int timeIncrement, 
        string initializationPath,
        string resultFolder,
        int populationSize,
        int offspringSize,
        int parentSize,
        double mutationRate,
        double crossoverRate,
        string initialization,
        string objective,
        string service,
        string mutationMethod,
        string crossoverMethod,
        string initializationMethod,
        int generations)
    {
        // Folders and directories
        BasePath = basePath;
        TimeStamp = DateTime.Now.ToString("yyMMdd_HHmmss"); //timeStamp ;
        ResultDirectory = basePath + "\\" + resultDirectory;
        if (resultFolder == "") ResultFolder = ResultDirectory + TimeStamp + "\\";
        else ResultFolder = ResultDirectory + resultFolder + "\\";
        if (!Directory.Exists(ResultFolder)) Directory.CreateDirectory(ResultFolder);
        
        // Databases
        Databases = databases.Select(x => basePath + "\\" + x).ToList();
        Experiment = experiment;
        Edition = edition;
        NumberOfWorkers = numberOfWorkers;
        Project = project;
        Version = version;
        Visibility = visibility;
        Objectives = objectives;
        SimulationStart = simulationStart;
        SimulationEnd = simulationEnd;
        TimeIncrement = timeIncrement;
        
        // Optional
        InitializationPath = initializationPath;

        // EA Options
        PopulationSize = populationSize;
        OffspringSize = offspringSize;
        ParentsSize = parentSize;
        MutationRate = mutationRate;
        CrossoverRate = crossoverRate;
        Initialization = initialization;
        Objective = objective;
        Service = service;
        MutationMethod = mutationMethod;
        CrossoverMethod = crossoverMethod;
        InitializationMethod = initializationMethod;
        Generations = generations;
    }
    public KopanosConfig Deserialize(string currPath)
    {
        var config = JsonSerializer
            .Deserialize<KopanosConfig>(File.ReadAllText(Path.Combine(currPath, "config.json")));
        return new KopanosConfig(config);
    }
    public KopanosConfig CopyConfigToResultsFolder(string currPath)
    {
        if (!Directory.Exists(ResultFolder)) Directory.CreateDirectory(ResultFolder);
        File.Copy(Path.Combine(currPath, "config.json"), ResultFolder + "config.json", true);
        return this;
    }
    public void Serialize()
    {
        var jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true});
        File.WriteAllText(Path.Combine(this.BasePath,"PlantSchedule.RTS\\config.json"), jsonString);
    }
}
