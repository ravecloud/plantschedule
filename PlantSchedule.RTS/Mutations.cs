namespace PlantSchedule.RTS;

public static class Mutations<T> where T : IComparable<T>
{
    private static Random random = new Random();

    // Method to Get Mutation Function by Name
    public static Func<List<T>, List<T>> GetMutationMethod(string methodName)
    {
        var methods = typeof(Mutations<T>).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        foreach (var method in methods)
        {
            if (method.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase) && method.ReturnType == typeof(List<T>) && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(List<T>))
            {
                return (Func<List<T>, List<T>>)Delegate.CreateDelegate(typeof(Func<List<T>, List<T>>), method);
            }
        }

        var availableMethods = methods
            .Where(m => m.ReturnType == typeof(List<T>) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(List<T>))
            .Select(m => m.Name)
            .ToList();

        var availableMethodsList = string.Join(", ", availableMethods);
        throw new ArgumentException($"Method \"{methodName}\" not found. Available methods are: {availableMethodsList}. Please select an available method.");
    }

    // Randomly shuffles the entire list of values using Fisher-Yates shuffle
    private static List<T> RandomMutation(List<T> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
        return values;
    }


    private static List<T> EmptyMutation(List<T> values)
    {
        return values;
    }

    // Permutes a sublist of the values by shuffling the selected portion
    private static List<T> PermutationMutation(List<T> values)
    {
        var minPermutationLength = 2;
        minPermutationLength = minPermutationLength < values.Count ? minPermutationLength : values.Count;
        var maxPermutationLength = 0;
        var start = random.Next(0, values.Count - minPermutationLength);
        var end = random.Next(start + minPermutationLength - 1, values.Count);

        if (minPermutationLength == 0 && maxPermutationLength == 0)
        {
            if (end == start & start == values.Count - 1) start -= 1;
            if (end == start & start == 0) end += 1;
        }
        else
        {
            if (maxPermutationLength > 0)
            {
                end = end > start + maxPermutationLength ? start + maxPermutationLength : end;
            }
        }

        // Modify the list in place to reduce memory usage
        for (int i = start; i < end; i++)
        {
            int j = random.Next(start, end);
            (values[i], values[j]) = (values[j], values[i]);
        }

        return values;
    }

    // SwapMutation: Swaps two random elements in the list
    private static List<T> SwapMutation(List<T> values)
    {
        int index1 = random.Next(0, values.Count);
        int index2 = random.Next(0, values.Count - 1);
        // Ensure two distinct indices in a more efficient way
        if (index2 >= index1)
        {
            index2++;
        }

        // Swap values
        (values[index1], values[index2]) = (values[index2], values[index1]);

        return values;
    }

    // ScrambleMutation: Scrambles a randomly chosen subset of the list using Fisher-Yates shuffle
    private static List<T> ScrambleMutation(List<T> values)
    {
        int start = random.Next(0, values.Count);
        int end = random.Next(start, values.Count);
        var subList = values.GetRange(start, end - start);

        // Shuffle the sublist using Fisher-Yates shuffle
        for (int i = subList.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            (subList[i], subList[j]) = (subList[j], subList[i]);
        }

        for (int i = 0; i < subList.Count; i++)
        {
            values[start + i] = subList[i];
        }

        return values;
    }

    // ReverseSegmentMutation: Reverses a segment of the list
    private static List<T> ReverseSegmentMutation(List<T> values)
    {
        int start = random.Next(0, values.Count);
        int end = random.Next(start, values.Count);

        var subList = values.GetRange(start, end - start);
        subList.Reverse();

        for (int i = 0; i < subList.Count; i++)
        {
            values[start + i] = subList[i];
        }

        return values;
    }

    // BitFlipMutation: Flips a random bit for numeric types or flips boolean values at random positions
    private static List<T> BitFlipMutation(List<T> values)
    {
        if (typeof(T) == typeof(bool))
        {
            // Flip boolean values at random positions
            int index = random.Next(0, values.Count);
            values[index] = (T)(object)!((bool)(object)values[index]);
        }
        else if (typeof(T) == typeof(int) || typeof(T) == typeof(byte))
        {
            // Flip a random bit for integer or byte values
            int index = random.Next(0, values.Count);
            int bitPosition = random.Next(0, sizeof(int) * 8);
            int originalValue = (int)(object)values[index];
            int newValue = originalValue ^ (1 << bitPosition);
            values[index] = (T)(object)newValue;
        }
        else
        {
            throw new NotSupportedException("BitFlipMutation is only supported for boolean or integer types.");
        }

        return values;
    }
    // Swap mutation: Swap two positions
    // Insertion mutation: choose two positions and move one next to the other
}
