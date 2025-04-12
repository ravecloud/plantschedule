using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantSchedule.RTS
{
    public enum SelectionType
    {
        Parent,
        Elitist,
        Survivor
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class MethodTypeAttribute : Attribute
    {
        public SelectionType Type { get; }

        public MethodTypeAttribute(SelectionType type)
        {
            Type = type;
        }
    }
    public static class Selection
    {
        #region Parent Selection Methods
        // 1. Roulette Wheel Selection
        [MethodType(SelectionType.Parent)]
        private static List<Individual> RouletteWheelSelection(List<Individual> individuals, int numberOfParents, string objective = "")
        {
            var selectedParents = new List<Individual>();
            var random = new Random();
            for (int i = 0; i < numberOfParents; i++)
            {
                double totalFitness = individuals.Sum(individual => individual.GetFitness(objective));
                double randomValue = random.NextDouble() * totalFitness;
                double cumulativeSum = 0.0;

                foreach (var individual in individuals)
                {
                    cumulativeSum += individual.GetFitness(objective);
                    if (cumulativeSum >= randomValue)
                    {
                        selectedParents.Add(individual);
                        break;
                    }
                }
            }
            return selectedParents;
        }

        // 2. Tournament Selection
        [MethodType(SelectionType.Parent)]
        private static List<Individual> TournamentSelection(List<Individual> individuals, int numberOfParents, string objective = "")
        {
            var tournamentSize = numberOfParents / 4;
            var random = new Random();
            var selectedParents = new List<Individual>();

            for (int i = 0; i < numberOfParents; i++)
            {
                var tournamentParticipants = new List<Individual>();
                for (int j = 0; j < tournamentSize; j++)
                {
                    int randomIndex = random.Next(individuals.Count);
                    tournamentParticipants.Add(individuals[randomIndex]);
                }

                selectedParents.Add(tournamentParticipants.OrderByDescending(individual => individual.GetFitness(objective)).First());
            }
            return selectedParents;
        }

        // 3. Rank-Based Selection
        [MethodType(SelectionType.Parent)]
        private static List<Individual> RankBasedSelection(List<Individual> individuals, int numberOfParents, string objective = "")
        {
            var rankedIndividuals = individuals.OrderBy(individual => individual.GetFitness(objective)).ToList();
            var selectedParents = new List<Individual>();
            var random = new Random();

            for (int i = 0; i < numberOfParents; i++)
            {
                int totalRank = (rankedIndividuals.Count * (rankedIndividuals.Count + 1)) / 2;
                double randomValue = random.NextDouble() * totalRank;
                double cumulativeRank = 0;

                for (int j = 0; j < rankedIndividuals.Count; j++)
                {
                    cumulativeRank += (j + 1);
                    if (cumulativeRank >= randomValue)
                    {
                        selectedParents.Add(rankedIndividuals[j]);
                        break;
                    }
                }
            }
            return selectedParents;
        }

        // 4. Random Selection
        [MethodType(SelectionType.Parent)]
        private static List<Individual> RandomSelection(List<Individual> individuals, int numberOfParents, string objective = "")
        {
            var random = new Random();
            var selectedParents = new List<Individual>();
            for (int i = 0; i < numberOfParents; i++)
            {
                selectedParents.Add(individuals[random.Next(individuals.Count)]);
            }
            return selectedParents;
        }
        // 5. RankSelection Selection
        [MethodType(SelectionType.Parent)]
        private static List<Individual> RankSelection(List<Individual> currentPopulation, int numberOfSurvivors, string objective = "")
        {
#if DEBUG
            var sortedPop = currentPopulation.OrderBy(individual => individual.GetFitness(objective)).ToList();
            var pop = sortedPop.Take(numberOfSurvivors).ToList();
#endif
            return currentPopulation.OrderBy(individual => individual.GetFitness(objective)).Take(numberOfSurvivors).ToList();
        }
        #endregion

        #region Survivor Selection Methods
        // Survivor Selection Methods
        // 1. Elitism Selection

        [MethodType(SelectionType.Elitist)]
        private static List<Individual> ElitismSelection(List<Individual> currentPopulation, int numberOfSurvivors, string objective = "")
        {
#if DEBUG
            var sortedPop = currentPopulation.OrderBy(individual => individual.GetFitness(objective)).ToList();
            var pop = sortedPop.Take(numberOfSurvivors).ToList();
#endif
            return currentPopulation.OrderBy(individual => individual.GetFitness(objective)).Take(numberOfSurvivors).ToList();
        }

        // 2. NonElite Selection
        [MethodType(SelectionType.Survivor)]
        private static List<Individual> NonElititismSelection(List<Individual> currentPopulation, int numberOfSurvivors, string objective = "")
        {
            // take the worst of the population
            var sortedPopulation = currentPopulation.OrderByDescending(individual => individual.GetFitness(objective)).ToList();
            return sortedPopulation.Take(numberOfSurvivors).ToList();
        }

        // 3. Random Survivor Selection
        [MethodType(SelectionType.Survivor)]
        private static List<Individual> RandomSurvivor(List<Individual> currentPopulation, int numberOfSurvivors, string objective = "")
        {
            var random = new Random();
            return currentPopulation.OrderBy(_ => random.Next()).Take(numberOfSurvivors).ToList();
        }

        // 4. Tournament Survivor Selection
        [MethodType(SelectionType.Survivor)]
        private static List<Individual> TournamentSurvivor(List<Individual> currentPopulation, int numberOfSurvivors, string objective = "")
        {
            var tournamentSize = 4; // Hard coded. Can be changed later via arguments 
            var random = new Random();
            var survivors = new List<Individual>();

            for (int i = 0; i < numberOfSurvivors; i++)
            {
                var tournamentParticipants = new List<Individual>();
                for (int j = 0; j < tournamentSize; j++)
                {
                    int randomIndex = random.Next(currentPopulation.Count);
                    tournamentParticipants.Add(currentPopulation[randomIndex]);
                }

                survivors.Add(tournamentParticipants.OrderByDescending(individual => individual.GetFitness(objective)).First());
            }
            return survivors;
        }
        #endregion

        #region Helper methods
        // Generate Pairs of Parents
        public static List<(int, int)> GenerateParentPairs(List<Individual> selectedParents, int parentCount = 0)
        {
            if (parentCount == 0) parentCount = selectedParents.Count;
            var parentPairs = new List<(int, int)>();
            var random = new Random();

            // Ensure parentCount does not exceed selectedParents.Count by cycling through parents if needed
            while (selectedParents.Count < parentCount)
            {
                selectedParents.Add(selectedParents[random.Next(selectedParents.Count)]);
            }

            // If there are an odd number of parents, randomly add an extra one for pairing
            if (selectedParents.Count % 2 != 0)
            {
                selectedParents.Add(selectedParents[random.Next(selectedParents.Count)]);
            }

            // Create pairs of parents for offspring generation
            for (int i = 0; i < selectedParents.Count; i += 2)
            {
                var parent1Id = selectedParents[i].Id;
                var parent2Id = selectedParents[i + 1].Id;
                parentPairs.Add((parent1Id, parent2Id));
            }

            // If more offspring are required than the current number of pairs, add more pairs randomly
            while (parentPairs.Count < parentCount / 2)
            {
                var parent1 = selectedParents[random.Next(selectedParents.Count)].Id;
                var parent2 = selectedParents[random.Next(selectedParents.Count)].Id;
                parentPairs.Add((parent1, parent2));
            }

            return parentPairs;
        }

        // Method to Get Selection Function by Name and Type
        public static Func<List<Individual>, int, string, List<Individual>> GetSelectionMethod(string methodName, SelectionType methodType)
        {
            var methods = typeof(Selection).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(MethodTypeAttribute), false);
                if (attributes.Length > 0)
                {
                    var methodTypeAttribute = (MethodTypeAttribute)attributes[0];
                    if (methodTypeAttribute.Type == methodType && method.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
                    {
                        return (Func<List<Individual>, int, string, List<Individual>>)Delegate.CreateDelegate(typeof(Func<List<Individual>, int, string, List<Individual>>), method);
                    }
                }
            }
            var availableMethods = methods
                        .Where(m => m.GetCustomAttributes(typeof(MethodTypeAttribute), false)
                                    .Cast<MethodTypeAttribute>()
                                    .Any(attr => attr.Type == methodType))
                        .Select(m => m.Name)
                        .ToList();

            var availableMethodsList = string.Join(", ", availableMethods);
            throw new ArgumentException($"Method \"{methodName}\" for \"{methodType}Selection\" not found. " +
                $"Available methods for this type are: {availableMethodsList}. " +
                $"Select an available method and change it in the config.json under \"{methodType}Selection\".");
        }
        #endregion

    }
}
