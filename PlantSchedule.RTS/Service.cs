using PlantSchedule.DTO;

namespace PlantSchedule.RTS;
public interface IService
{
    public IIndividual GetIndividual(List<IGene> genome);
    public IIndividual GetIndividual(Individual ind);
    public ISimulator GetSimulator(IConfig config);
}
public class CSharpService : IService
{
    public IIndividual GetIndividual(List<IGene> genome)
    {
        return new Individual(genome);
    }

    public IIndividual GetIndividual(Individual ind)
    {
        var newInd = new Individual(ind.Genes);
        Order[] orderArray = new Order[ind.Worker.Orders.Count];
        ind.Worker.Orders.CopyTo(orderArray);
        newInd.Worker.Orders = orderArray.ToList();
        return newInd;
    }
    public ISimulator GetSimulator(IConfig config)
    {
        return new CSharpSimulator((CSharpConfig)config);
    }
}

#if KOPANOS_SIMUALTOR
public class KopanosService : IService
{
    public IIndividual GetIndividual(List<IGene> genome)
    {
        return new Individual(genome);
    }

    public IIndividual GetIndividual(Individual ind)
    {
        var newInd = new Individual(ind.Genes);
        Order[] orderArray = new Order[ind.Worker.Orders.Count];
        ind.Worker.Orders.CopyTo(orderArray);
        newInd.Worker.Orders = orderArray.ToList();
        return newInd;
    }
    public ISimulator GetSimulator(IConfig config)
    {
        if (config is not KopanosConfig) return new KopanosSimulator((CSharpConfig)config); // throw new Exception("Please provide a Kopanos Config!");
        else return new KopanosSimulator((KopanosConfig)config);
    }
}
#endif
#if PLANT_SIMULATOR
public class PlantService : IService
{
    public IIndividual GetIndividual(List<IGene> genome)
    {
        return new Individual(genome);
    }
    public IIndividual GetIndividual(Individual ind)
    {
        return new Individual(ind.Genes);
    }
    public ISimulator GetSimulator(IConfig config)
    {
        return new KopanosSimulator((KopanosConfig)config); //Change to PlantSimulator
    }
}
#endif
