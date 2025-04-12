using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantSchedule.RTS
{

    public static class DistanceMeasure
    {
        // Hamming Distance
        // This method calculates the Hamming distance between two sequences of equal length.
        // Hamming distance is defined as the number of positions at which the corresponding symbols are different.
        // It is commonly used for comparing strings or other discrete sequences.
        public static int HammingDistance<T>(List<T> seq1, List<T> seq2)
        {
            if (seq1.Count != seq2.Count)
            {
                throw new ArgumentException("Sequences must be of equal length.");
            }

            int distance = 0;
            for (int i = 0; i < seq1.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(seq1[i], seq2[i]))
                {
                    distance++;
                }
            }
            return distance;
        }

        // Levenshtein Distance
        // This method calculates the Levenshtein distance between two sequences.
        // Levenshtein distance is defined as the minimum number of single-character edits (insertions, deletions, or substitutions)
        // required to change one sequence into the other.
        // It is commonly used for applications like spell checking, DNA sequence comparison, and fuzzy string matching.
        public static int LevenshteinDistance<T>(List<T> seq1, List<T> seq2)
        {
            int len1 = seq1.Count;
            int len2 = seq2.Count;
            int[,] dp = new int[len1 + 1, len2 + 1];

            for (int i = 0; i <= len1; i++)
            {
                for (int j = 0; j <= len2; j++)
                {
                    if (i == 0)
                    {
                        dp[i, j] = j;
                    }
                    else if (j == 0)
                    {
                        dp[i, j] = i;
                    }
                    else if (EqualityComparer<T>.Default.Equals(seq1[i - 1], seq2[j - 1]))
                    {
                        dp[i, j] = dp[i - 1, j - 1];
                    }
                    else
                    {
                        dp[i, j] = 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i - 1, j], dp[i, j - 1]));
                    }
                }
            }

            return dp[len1, len2];
        }

        // Jaro-Winkler Similarity
        // This method calculates the Jaro-Winkler similarity between two sequences.
        // Jaro-Winkler similarity is a measure of similarity between two sequences that gives more favorable ratings
        // to sequences that match from the beginning. It is often used in record linkage and data matching.
        public static double JaroWinklerSimilarity<T>(List<T> seq1, List<T> seq2)
        {
            double jaroDistance = JaroSimilarity(seq1, seq2);
            int prefixLength = 0;
            int maxPrefixLength = 4;

            for (int i = 0; i < Math.Min(Math.Min(seq1.Count, seq2.Count), maxPrefixLength); i++)
            {
                if (EqualityComparer<T>.Default.Equals(seq1[i], seq2[i]))
                {
                    prefixLength++;
                }
                else
                {
                    break;
                }
            }

            return jaroDistance + (prefixLength * 0.1 * (1 - jaroDistance));
        }

        // Helper function to calculate Jaro similarity
        private static double JaroSimilarity<T>(List<T> seq1, List<T> seq2)
        {
            if (seq1.SequenceEqual(seq2))
            {
                return 1.0;
            }

            int len1 = seq1.Count;
            int len2 = seq2.Count;

            int matchDistance = Math.Max(len1, len2) / 2 - 1;
            bool[] seq1Matches = new bool[len1];
            bool[] seq2Matches = new bool[len2];

            int matches = 0;
            for (int i = 0; i < len1; i++)
            {
                int start = Math.Max(0, i - matchDistance);
                int end = Math.Min(i + matchDistance + 1, len2);

                for (int j = start; j < end; j++)
                {
                    if (seq2Matches[j]) continue;
                    if (!EqualityComparer<T>.Default.Equals(seq1[i], seq2[j])) continue;
                    seq1Matches[i] = true;
                    seq2Matches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0)
            {
                return 0.0;
            }

            double transpositions = 0;
            int k = 0;
            for (int i = 0; i < len1; i++)
            {
                if (!seq1Matches[i]) continue;
                while (!seq2Matches[k])
                {
                    k++;
                }
                if (!EqualityComparer<T>.Default.Equals(seq1[i], seq2[k]))
                {
                    transpositions++;
                }
                k++;
            }

            transpositions /= 2.0;

            return ((matches / (double)len1) + (matches / (double)len2) + ((matches - transpositions) / matches)) / 3.0;
        }
        // Cyclic Levenshtein Distance
        // This method calculates the minimum Levenshtein distance between seq1 and all possible cyclic rotations of seq2.
        // It finds the best match by rotating seq2 in all possible ways and then computing the Levenshtein distance.
        public static int CyclicLevenshteinDistance<T>(List<T> seq1, List<T> seq2)
        {
            int len2 = seq2.Count;
            int minDistance = int.MaxValue;

            // Iterate through all possible rotations of seq2
            for (int i = 0; i < len2; i++)
            {
                // Rotate seq2 by 'i' positions
                List<T> rotatedSeq2 = RotateList(seq2, i);
                // Calculate Levenshtein distance between seq1 and the rotated seq2
                int distance = LevenshteinDistance(seq1, rotatedSeq2);
                // Keep track of the minimum distance found
                minDistance = Math.Min(minDistance, distance);
            }

            return minDistance;
        }

        // Rotational Jaccard Similarity
        // This method calculates the Jaccard similarity index between seq1 and all possible cyclic rotations of seq2.
        // It finds the maximum Jaccard index by rotating seq2 in all possible ways and computing the index for each rotation.
        public static double RotationalJaccardSimilarity<T>(List<T> seq1, List<T> seq2)
        {
            int len1 = seq1.Count;
            int len2 = seq2.Count;
            if (len1 != len2)
            {
                throw new ArgumentException("Sequences must be of the same length for Jaccard similarity.");
            }

            double maxJaccardIndex = 0.0;

            // Iterate through all possible rotations of seq2
            for (int i = 0; i < len2; i++)
            {
                // Rotate seq2 by 'i' positions
                List<T> rotatedSeq2 = RotateList(seq2, i);
                // Calculate Jaccard index between seq1 and the rotated seq2
                double jaccardIndex = JaccardIndex(seq1, rotatedSeq2);
                // Keep track of the maximum Jaccard index found
                maxJaccardIndex = Math.Max(maxJaccardIndex, jaccardIndex);
            }

            return maxJaccardIndex;
        }

        // Circular (Cyclic) String Matching
        // This method checks if seq1 is a cyclic permutation of seq2.
        // It concatenates seq2 with itself and then checks if seq1 appears as a sublist in the concatenated list.
        public static bool IsCyclicMatch<T>(List<T> seq1, List<T> seq2)
        {
            if (seq1.Count != seq2.Count)
            {
                return false;
            }

            // Concatenate seq2 with itself
            List<T> concatenatedSeq2 = new List<T>(seq2);
            concatenatedSeq2.AddRange(seq2);

            // Check if seq1 is a sublist of concatenatedSeq2
            return IsSubList(concatenatedSeq2, seq1);
        }

        /*
        // Helper method to calculate Levenshtein Distance
        // This method computes the Levenshtein distance between two sequences by using dynamic programming.
        // It calculates the minimum number of single-element edits (insertions, deletions, substitutions) needed to change one sequence into the other.
        private static int LevenshteinDistance<T>(List<T> seq1, List<T> seq2)
        {
            int len1 = seq1.Count;
            int len2 = seq2.Count;
            int[,] dp = new int[len1 + 1, len2 + 1];

            for (int i = 0; i <= len1; i++)
            {
                for (int j = 0; j <= len2; j++)
                {
                    if (i == 0)
                    {
                        dp[i, j] = j;
                    }
                    else if (j == 0)
                    {
                        dp[i, j] = i;
                    }
                    else if (EqualityComparer<T>.Default.Equals(seq1[i - 1], seq2[j - 1]))
                    {
                        dp[i, j] = dp[i - 1, j - 1];
                    }
                    else
                    {
                        dp[i, j] = 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i - 1, j], dp[i, j - 1]));
                    }
                }
            }

            return dp[len1, len2];
        }
        */
        // Helper method to rotate a list
        // This method returns a new list that is a rotation of the input list by the specified number of positions.
        private static List<T> RotateList<T>(List<T> list, int positions)
        {
            int len = list.Count;
            List<T> rotated = new List<T>(len);
            for (int i = 0; i < len; i++)
            {
                rotated.Add(list[(i + positions) % len]);
            }
            return rotated;
        }

        // Helper method to calculate Jaccard Index
        // This method calculates the Jaccard similarity index between two sequences by computing the size of the intersection divided by the size of the union.
        private static double JaccardIndex<T>(List<T> seq1, List<T> seq2)
        {
            HashSet<T> set1 = new HashSet<T>(seq1);
            HashSet<T> set2 = new HashSet<T>(seq2);

            // Calculate intersection and union
            HashSet<T> intersection = new HashSet<T>(set1);
            intersection.IntersectWith(set2);

            HashSet<T> union = new HashSet<T>(set1);
            union.UnionWith(set2);

            // Return Jaccard index
            return (double)intersection.Count / union.Count;
        }

        // Helper method to check if a list is a sublist of another
        // This method checks if the sublist appears within the larger list as a contiguous subsequence.
        private static bool IsSubList<T>(List<T> list, List<T> sublist)
        {
            int len = list.Count;
            int subLen = sublist.Count;

            for (int i = 0; i <= len - subLen; i++)
            {
                bool match = true;
                for (int j = 0; j < subLen; j++)
                {
                    if (!EqualityComparer<T>.Default.Equals(list[i + j], sublist[j]))
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return true;
                }
            }

            return false;
        }


        public static void Test()
        {
            // Example usage of EAHelper functions
            var seq1 = new List<char> { 'G', 'A', 'T', 'T', 'A', 'C', 'A' };
            var seq2 = new List<char> { 'G', 'A', 'C', 'T', 'A', 'T', 'A' };
            Console.WriteLine($"Hamming Distance: {DistanceMeasure.HammingDistance(seq1, seq2)}");
            Console.WriteLine($"Levenshtein Distance: {DistanceMeasure.LevenshteinDistance(seq1, seq2)}");
            Console.WriteLine($"Jaro-Winkler Similarity: {DistanceMeasure.JaroWinklerSimilarity(seq1, seq2)}");
        }
    }

}
