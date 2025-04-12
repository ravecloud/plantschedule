namespace PlantSchedule.RTS;

public static class Crossovers<T> where T : IComparable<T>
{
    private static Random random = new Random();

    // Retrieves a crossover method based on its name
    public static Func<List<T>, List<T>, (List<T>, List<T>)> GetCrossoverMethod(string methodName)
    {
        var methods = typeof(Crossovers<T>).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        foreach (var method in methods)
        {
            if (method.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase) && method.ReturnType == typeof((List<T>, List<T>)) && method.GetParameters().Length == 2)
            {
                return (Func<List<T>, List<T>, (List<T>, List<T>)>)Delegate.CreateDelegate(typeof(Func<List<T>, List<T>, (List<T>, List<T>)>), method);
            }
        }

        var availableMethods = methods
            .Where(m => m.ReturnType == typeof((List<T>, List<T>)) && m.GetParameters().Length == 2)
            .Select(m => m.Name)
            .ToList();

        var availableMethodsList = string.Join(", ", availableMethods);
        throw new ArgumentException($"Method \"{methodName}\" not found. Available methods are: {availableMethodsList}. Please select an available method.");
    }

    // Cycle Crossover method
    private static (List<T>, List<T>) CycleCrossover(List<T> values1, List<T> values2)
    {
        if (values1.Count != values2.Count) throw new Exception("Both genomes have different length");
        var temp1 = values1.ToArray();
        var temp2 = values2.ToArray();
        int cycleElements = 0;
        int[] cycleIdx = new int[values1.Count];
        for (int i = 0; i < cycleIdx.Length; i++) cycleIdx[i] = -1;
        Stack<Stack<int>> cycles = new Stack<Stack<int>>();
        var n = 0;
        for (int i = n; i < values1.Count; i++)
        {
            cycles.Push(new Stack<int>());
            int whileIdx = i;
            if (values1[whileIdx].Equals(values2[whileIdx]))
            {
                cycles.Peek().Push(whileIdx);
                cycleIdx[cycleElements] = whileIdx;
                cycleElements++;
                continue;
            }

            while (true)
            {
                if (Array.IndexOf(cycleIdx, whileIdx) != -1) break;
                cycles.Peek().Push(whileIdx);
                cycleIdx[cycleElements] = whileIdx;
                cycleElements++;
                var tempValue2 = values2[whileIdx];
                var tempIdx1 = values1.IndexOf(tempValue2);
                whileIdx = tempIdx1;
                if (cycleElements == values1.Count) break;
            }

            if (cycleElements == values1.Count) break;
        }

        bool change = false;
        foreach (var cycle in cycles)
        {
            foreach (var idx in cycle)
            {
                if (change == false)
                {
                    temp1[idx] = values1[idx];
                    temp2[idx] = values2[idx];
                }
                else
                {
                    temp1[idx] = values2[idx];
                    temp2[idx] = values1[idx];
                }
            }

            change = change ? false : true;
        }

        return (temp2.ToList(), temp1.ToList());
    }

    // New Crossover Methods:

    // One-Point Crossover method
    private static (List<T>, List<T>) OnePointCrossover(List<T> values1, List<T> values2)
    {
        if (values1.Count != values2.Count) throw new Exception("Both genomes have different length");
        int crossoverPoint = random.Next(1, values1.Count);

        List<T> offspring1 = new List<T>(values1.Take(crossoverPoint).Concat(values2.Skip(crossoverPoint)));
        List<T> offspring2 = new List<T>(values2.Take(crossoverPoint).Concat(values1.Skip(crossoverPoint)));

        return (offspring1, offspring2);
    }

    // Two-Point Crossover method
    private static (List<T>, List<T>) TwoPointCrossover(List<T> values1, List<T> values2)
    {
        if (values1.Count != values2.Count) throw new Exception("Both genomes have different length");
        int point1 = random.Next(1, values1.Count - 1);
        int point2 = random.Next(point1, values1.Count);

        List<T> offspring1 = new List<T>(values1.Take(point1)
            .Concat(values2.Skip(point1).Take(point2 - point1))
            .Concat(values1.Skip(point2)));

        List<T> offspring2 = new List<T>(values2.Take(point1)
            .Concat(values1.Skip(point1).Take(point2 - point1))
            .Concat(values2.Skip(point2)));

        return (offspring1, offspring2);
    }

    // Uniform Crossover method
    private static (List<T>, List<T>) UniformCrossover(List<T> values1, List<T> values2)
    {
        if (values1.Count != values2.Count) throw new Exception("Both genomes have different length");
        List<T> offspring1 = new List<T>(values1.Count);
        List<T> offspring2 = new List<T>(values2.Count);

        for (int i = 0; i < values1.Count; i++)
        {
            if (random.NextDouble() < 0.5)
            {
                offspring1.Add(values1[i]);
                offspring2.Add(values2[i]);
            }
            else
            {
                offspring1.Add(values2[i]);
                offspring2.Add(values1[i]);
            }
        }

        return (offspring1, offspring2);
    }

    // Partially Mapped Crossover (PMX) method
    private static (List<T>, List<T>) PartiallyMappedCrossover(List<T> values1, List<T> values2)
    {
        if (values1.Count != values2.Count) throw new Exception("Both genomes have different length");
        int point1 = random.Next(1, values1.Count - 1);
        int point2 = random.Next(point1, values1.Count);

        List<T> offspring1 = new List<T>(values1);
        List<T> offspring2 = new List<T>(values2);

        // Copy segment from Parent 1 to Offspring 1, and Parent 2 to Offspring 2
        for (int i = point1; i < point2; i++)
        {
            offspring1[i] = values2[i];
            offspring2[i] = values1[i];
        }

        // Mapping for Offspring 1
        for (int i = point1; i < point2; i++)
        {
            T item1 = values1[i];
            T item2 = values2[i];

            if (!offspring1.Contains(item1))
            {
                int position = offspring1.IndexOf(item2);
                while (position >= point1 && position < point2)
                {
                    item2 = values2[position];
                    position = offspring1.IndexOf(item2);
                }
                offspring1[position] = item1;
            }
        }

        // Mapping for Offspring 2
        for (int i = point1; i < point2; i++)
        {
            T item1 = values2[i];
            T item2 = values1[i];

            if (!offspring2.Contains(item1))
            {
                int position = offspring2.IndexOf(item2);
                while (position >= point1 && position < point2)
                {
                    item2 = values1[position];
                    position = offspring2.IndexOf(item2);
                }
                offspring2[position] = item1;
            }
        }

        return (offspring1, offspring2);
    }

    // Order Crossover (OX) method
    private static (List<T>, List<T>) OrderCrossover(List<T> values1, List<T> values2)
    {
        if (values1.Count != values2.Count) throw new Exception("Both genomes have different length");
        int point1 = random.Next(1, values1.Count - 1);
        int point2 = random.Next(point1, values1.Count);

        List<T> offspring1 = new List<T>(new T[values1.Count]);
        List<T> offspring2 = new List<T>(new T[values2.Count]);

        // Copy segment from Parent 1 to Offspring 1, and Parent 2 to Offspring 2
        for (int i = point1; i < point2; i++)
        {
            offspring1[i] = values1[i];
            offspring2[i] = values2[i];
        }

        // Fill the remaining positions in offspring1 from values2, preserving order
        int currentIndex = point2 % values1.Count;
        for (int i = 0; i < values1.Count; i++)
        {
            int idx = (point2 + i) % values1.Count;
            if (!offspring1.Contains(values2[idx]))
            {
                offspring1[currentIndex] = values2[idx];
                currentIndex = (currentIndex + 1) % values1.Count;
            }
        }

        // Fill the remaining positions in offspring2 from values1, preserving order
        currentIndex = point2 % values2.Count;
        for (int i = 0; i < values2.Count; i++)
        {
            int idx = (point2 + i) % values2.Count;
            if (!offspring2.Contains(values1[idx]))
            {
                offspring2[currentIndex] = values1[idx];
                currentIndex = (currentIndex + 1) % values2.Count;
            }
        }

        return (offspring1, offspring2);
    }

    private static (List<T>, List<T>) EmptyCrossover(List<T> values1, List<T> values2)
    {
        return (values1, values2);
    }

}
