﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MLCore.Algorithm;

namespace MLCore
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"{DateTime.Now}  Computing in progress... ");
            List<int> indexes = new List<int>() { 0, 100, 200, 300, 600, 700, 800, 900 };
            Action<int, int> workload = WriteProbDist;
            workload += WriteDerivedAlphas;
            Parallel.ForEach(indexes, i => workload(i, i + 99));
            Console.WriteLine($"{DateTime.Now}  Successfully finished all. ");
        }

        private static void WriteProbDist(int startIndex, int endIndex)
        {
            const string header = "feature0,feature1,label,alpha," +
                "knn-cv0-fold,knn-cv0-p0,knn-cv0-p1,knn-cv1-fold,knn-cv1-p0,knn-cv1-p1,knn-cv2-fold,knn-cv2-p0,knn-cv2-p1,knn-cv3-fold,knn-cv3-p0,knn-cv3-p1,knn-cv4-fold,knn-cv4-p0,knn-cv4-p1,knn-cv5-fold,knn-cv5-p0,knn-cv5-p1,knn-cv6-fold,knn-cv6-p0,knn-cv6-p1,knn-cv7-fold,knn-cv7-p0,knn-cv7-p1,knn-cv8-fold,knn-cv8-p0,knn-cv8-p1,knn-cv9-fold,knn-cv9-p0,knn-cv9-p1," +
                "nb-cv0-fold,nb-cv0-p0,nb-cv0-p1,nb-cv1-fold,nb-cv1-p0,nb-cv1-p1,nb-cv2-fold,nb-cv2-p0,nb-cv2-p1,nb-cv3-fold,nb-cv3-p0,nb-cv3-p1,nb-cv4-fold,nb-cv4-p0,nb-cv4-p1,nb-cv5-fold,nb-cv5-p0,nb-cv5-p1,nb-cv6-fold,nb-cv6-p0,nb-cv6-p1,nb-cv7-fold,nb-cv7-p0,nb-cv7-p1,nb-cv8-fold,nb-cv8-p0,nb-cv8-p1,nb-cv9-fold,nb-cv9-p0,nb-cv9-p1," +
                "dt-cv0-fold,dt-cv0-p0,dt-cv0-p1,dt-cv1-fold,dt-cv1-p0,dt-cv1-p1,dt-cv2-fold,dt-cv2-p0,dt-cv2-p1,dt-cv3-fold,dt-cv3-p0,dt-cv3-p1,dt-cv4-fold,dt-cv4-p0,dt-cv4-p1,dt-cv5-fold,dt-cv5-p0,dt-cv5-p1,dt-cv6-fold,dt-cv6-p0,dt-cv6-p1,dt-cv7-fold,dt-cv7-p0,dt-cv7-p1,dt-cv8-fold,dt-cv8-p0,dt-cv8-p1,dt-cv9-fold,dt-cv9-p0,dt-cv9-p1";

            List<Type> algorithms = new List<Type>() { typeof(KNNContext), typeof(NaiveBayesContext), typeof(DecisionTreeContext) };
            for (int datasetNumber = startIndex; datasetNumber <= endIndex; datasetNumber++)
            {
                List<Instance> instances = CSV.ReadFromCsv($".\\artificial\\{datasetNumber}.txt", null);
                Dictionary<Instance, List<string>> resultsSerialized = new Dictionary<Instance, List<string>>();
                instances.ForEach(i => resultsSerialized[i] = new List<string>());

                List<(Instance, double)> alphas = new KNNContext(instances).GetAllAlphaValues().ToList();
                foreach ((Instance, double) tuple in alphas)
                {
                    resultsSerialized[tuple.Item1].Add(tuple.Item2.ToString());
                }

                foreach (Type algorithm in algorithms)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        Instance[] instancesCopy = new Instance[instances.Count];
                        instances.CopyTo(instancesCopy);
                        Dictionary<Instance, (Dictionary<string, double>, int)> cvResults = CrossValidation.CVProbDist(instancesCopy.ToList(), algorithm);
                        foreach (KeyValuePair<Instance, (Dictionary<string, double>, int)> kvp in cvResults)
                        {
                            resultsSerialized[kvp.Key].Add(kvp.Value.Item2.ToString());
                            resultsSerialized[kvp.Key].Add(kvp.Value.Item1.ContainsKey("0.0") ? kvp.Value.Item1["0.0"].ToString() : "0");
                            resultsSerialized[kvp.Key].Add(kvp.Value.Item1.ContainsKey("1.0") ? kvp.Value.Item1["1.0"].ToString() : "0");
                        }
                    }
                }

                List<List<string>> dataToWrite = new List<List<string>>();
                foreach (KeyValuePair<Instance, List<string>> kvp in resultsSerialized)
                {
                    List<string> row = kvp.Key.Serialize().Split(',').ToList();
                    dataToWrite.Add(row.Concat(kvp.Value).ToList());
                }
                CSV.WriteToCsv($".\\artificial-results\\{datasetNumber}_results.csv", dataToWrite, header: header);
                Console.WriteLine($"{DateTime.Now}  Finished computing probabilities on dataset #{datasetNumber}. ");
            }
        }

        private static void WriteDerivedAlphas(int startIndex, int endIndex)
        {
            // init
            const string header136 = "feature0,feature1,label,alpha,knn-cv0-fold,knn-cv0-p0,knn-cv0-p1,knn-cv0-alpha,knn-cv1-fold,knn-cv1-p0,knn-cv1-p1,knn-cv1-alpha,knn-cv2-fold,knn-cv2-p0,knn-cv2-p1,knn-cv2-alpha,knn-cv3-fold,knn-cv3-p0,knn-cv3-p1,knn-cv3-alpha,knn-cv4-fold,knn-cv4-p0,knn-cv4-p1,knn-cv4-alpha,knn-cv5-fold,knn-cv5-p0,knn-cv5-p1,knn-cv5-alpha,knn-cv6-fold,knn-cv6-p0,knn-cv6-p1,knn-cv6-alpha,knn-cv7-fold,knn-cv7-p0,knn-cv7-p1,knn-cv7-alpha,knn-cv8-fold,knn-cv8-p0,knn-cv8-p1,knn-cv8-alpha,knn-cv9-fold,knn-cv9-p0,knn-cv9-p1,knn-cv9-alpha,knn-avg-p0,knn-avg-p1,knn-avg-alpha,knn-avg-alphashift,nb-cv0-fold,nb-cv0-p0,nb-cv0-p1,nb-cv0-alpha,nb-cv1-fold,nb-cv1-p0,nb-cv1-p1,nb-cv1-alpha,nb-cv2-fold,nb-cv2-p0,nb-cv2-p1,nb-cv2-alpha,nb-cv3-fold,nb-cv3-p0,nb-cv3-p1,nb-cv3-alpha,nb-cv4-fold,nb-cv4-p0,nb-cv4-p1,nb-cv4-alpha,nb-cv5-fold,nb-cv5-p0,nb-cv5-p1,nb-cv5-alpha,nb-cv6-fold,nb-cv6-p0,nb-cv6-p1,nb-cv6-alpha,nb-cv7-fold,nb-cv7-p0,nb-cv7-p1,nb-cv7-alpha,nb-cv8-fold,nb-cv8-p0,nb-cv8-p1,nb-cv8-alpha,nb-cv9-fold,nb-cv9-p0,nb-cv9-p1,nb-cv9-alpha,nb-avg-p0,nb-avg-p1,nb-avg-alpha,nb-avg-alphashift,dt-cv0-fold,dt-cv0-p0,dt-cv0-p1,dt-cv0-alpha,dt-cv1-fold,dt-cv1-p0,dt-cv1-p1,dt-cv1-alpha,dt-cv2-fold,dt-cv2-p0,dt-cv2-p1,dt-cv2-alpha,dt-cv3-fold,dt-cv3-p0,dt-cv3-p1,dt-cv3-alpha,dt-cv4-fold,dt-cv4-p0,dt-cv4-p1,dt-cv4-alpha,dt-cv5-fold,dt-cv5-p0,dt-cv5-p1,dt-cv5-alpha,dt-cv6-fold,dt-cv6-p0,dt-cv6-p1,dt-cv6-alpha,dt-cv7-fold,dt-cv7-p0,dt-cv7-p1,dt-cv7-alpha,dt-cv8-fold,dt-cv8-p0,dt-cv8-p1,dt-cv8-alpha,dt-cv9-fold,dt-cv9-p0,dt-cv9-p1,dt-cv9-alpha,dt-avg-p0,dt-avg-p1,dt-avg-alpha,dt-avg-alphashift";
            const int labelColumnIndex = 2;
            const int alphaColumnIndex = 3;
            Dictionary<string, List<int>> indexes = new Dictionary<string, List<int>>
            {
                // e.g. 5: row #5 and #6 are data
                { "knn", new List<int>() { 5, 8, 11, 14, 17, 20, 23, 26, 29, 32 } },
                { "nb", new List<int>() { 35, 38, 41, 44, 47, 50, 53, 56, 59, 62 } },
                { "dt", new List<int>() { 65, 68, 71, 74, 77, 80, 83, 86, 89, 92 } }
            };

            // for each dataset
            for (int datasetNumber = startIndex; datasetNumber <= endIndex; datasetNumber++)
            {
                List<List<string>> table = CSV.ReadFromCsv($".\\artificial-results\\{datasetNumber}_results.csv", true);

                // <string tag, List<string value> columnValues>
                Dictionary<string, List<string>> newColumns = new Dictionary<string, List<string>>();

                // for each algorithm
                foreach (KeyValuePair<string, List<int>> kvp in indexes)
                {
                    List<List<double>> tenAlphaColumns = new List<List<double>>();

                    // for each xfcv ProDist results
                    foreach (int index in kvp.Value)
                    {
                        List<string> p0Column = table.SelectColumn(index);
                        List<string> p1Column = table.SelectColumn(index + 1);
                        List<string> labelColumn = table.SelectColumn(labelColumnIndex);
                        List<List<string>> joinedTable = TabularOperation.JoinColumns(p0Column, p1Column, labelColumn);

                        // assume ordered
                        IEnumerable<Instance> derivedInstances = new List<Instance>();
                        foreach (List<string> row in joinedTable)
                        {
                            // deserialize and create Instance instance
                            Dictionary<string, Feature> derivedFeatures = new Dictionary<string, Feature>
                            {
                                { "p0Feature", new Feature(ValueType.Continuous, double.Parse(row[0])) },
                                { "p1Feature", new Feature(ValueType.Continuous, double.Parse(row[1])) }
                            };
                            derivedInstances = derivedInstances.Append(new Instance(derivedFeatures, row[^1], "label"));
                        }

                        KNNContext context = new KNNContext(derivedInstances.ToList());
                        // assume ordered as original instances
                        List<(Instance, double)> alphas = context.GetAllAlphaValues();
                        newColumns.Add($"{kvp.Key}-cv{kvp.Value.IndexOf(index)}-alpha", alphas.Select(t => t.Item2.ToString()).ToList());
                        tenAlphaColumns.Add(alphas.Select(t => t.Item2).ToList());
                    }

                    // calculate average for 10 xfcv results for this algorithm
                    List<List<string>>[] joinedPTables = new List<List<string>>[10];
                    for (int i = 0; i < 10; i++)
                    {
                        joinedPTables[i] = TabularOperation.JoinColumns(table.SelectColumn(kvp.Value[i]), table.SelectColumn(kvp.Value[i] + 1));
                    }
                    List<List<string>> pAverageColumns = TabularOperation.Average(joinedPTables).Transpose();
                    newColumns.Add($"{kvp.Key}-avg-p0", pAverageColumns[0]);
                    newColumns.Add($"{kvp.Key}-avg-p1", pAverageColumns[1]);

                    List<List<string>>[] joinedAlphaTables = new List<List<string>>[10];
                    for (int i = 0; i < 10; i++)
                    {
                        joinedAlphaTables[i] = TabularOperation.JoinColumns(tenAlphaColumns[i].ConvertAll(d => d.ToString()));
                    }
                    List<List<string>> alphaAverageColumns = TabularOperation.Average(joinedAlphaTables).Transpose();
                    newColumns.Add($"{kvp.Key}-avg-alpha", alphaAverageColumns[0]);

                    // calculate alpha shift
                    List<string> alphaShiftColumn = alphaAverageColumns[0].Minus(table.SelectColumn(alphaColumnIndex));
                    newColumns.Add($"{kvp.Key}-avg-alphashift", alphaShiftColumn);
                }

                // write newColumns into file
                List<int> insertPositions = new List<int>() { 7, 11, 15, 19, 23, 27, 31, 35, 39, 43, 44, 45, 46, 47,
                                                              51, 55, 59, 63, 67, 71, 75, 79, 83, 87, 88, 89, 90, 91,
                                                              95, 99, 103, 107, 111, 115, 119, 123, 127, 131, 132, 133, 134, 135};
                for (int i = 0; i < newColumns.Count; i++)
                {
                    table.InsertColumn(newColumns.ElementAt(i).Value, insertPositions[i]);
                }
                CSV.WriteToCsv($".\\artificial-results-n\\{datasetNumber}_results_n.csv", table, header136);
                Console.WriteLine($"{DateTime.Now}  Finished computing derived alphas on dataset #{datasetNumber}. ");
            }
        }
    }
}
