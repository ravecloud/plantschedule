using System.Collections;
using GeneticSharp;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace PlantSchedule.RTS;
public enum GeneName
{
    Order,
    Allocation,
    Batch,
    Time
}
public interface IGene : IEnumerable
{
    public GeneName Name { get;  }
    public GeneType Type { get; }
    public void Initialization();
    public void Mutation();
    public (IGene firstChild, IGene secondChild) Recombine(IGene other);
    public IGene Copy();
    public Type GetType();
}

public enum GeneType
{
    StringPermutation,
    IntegerPermutation,
    IntegerSelection
}
public class Gene<T>(GeneName name, GeneType geneType, List<T> values, Func<List<T>, List<T>> init, Func<List<T>, List<T>> mutate, Func<List<T>, List<T>, (List<T>, List<T>)> crossover)
    : IGene
    where T : IComparable<T>
{
    public GeneName Name { get; } = GeneName.Order;
    public GeneType Type { get; } = geneType;
    public Func<List<T>, List<T>> Init = init;
    public Func<List<T>, List<T>> Mutate = mutate;
    public Func<List<T>, List<T>, (List<T>, List<T>)> Crossover = crossover;
    public List<T> Values = values;

    public Gene(Gene<T> other) : this(other.Name, other.Type, new List<T>(other.Values), other.Init, other.Mutate, other.Crossover) {}

    public void Initialization()
    {
        if (Mutate != null)
        {
            this.Values = Init(this.Values);
        }
    }
    public void Mutation()
    {
        if (Mutate != null)
        {
            this.Values = Mutate(this.Values);
        }
    }

    public (IGene firstChild, IGene secondChild) Recombine(IGene other)
    {
        var res = Crossover(this.Values, ((Gene<T>)other).Values);
        var firstChild = new Gene<T>(this);
        firstChild.Values = new List<T>(res.Item1);
        var secondChild = new Gene<T>((Gene<T>)other);
        secondChild.Values = new List<T>(res.Item2);
        return (firstChild, secondChild);

    }
    public IGene Copy()
    {
        return new Gene<T>(this);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return this.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Type GetType()
    {
        return typeof(T);
    }
}