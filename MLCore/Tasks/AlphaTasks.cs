﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLCore.Algorithm;

namespace MLCore.Tasks
{
    /// <summary>
    /// Archived from Program.cs
    /// </summary>
    public static class AlphaTasks
    {
        public static void WriteBaseAlphaBinFreq(string sourceFolder, string datasetWithAlphaOutputPath, string outputFilename, string logFilename)
        {
            int finishedCount = 0;
            bool hasFinished = false;
            StringBuilder logger = new StringBuilder();
            StringBuilder resultsBuilder = new StringBuilder($"filename,bin0,bin1,bin2,bin3,bin4,bin5,bin6,bin7,bin8,bin9\r\n");

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Parallel.ForEach(Directory.EnumerateFiles(sourceFolder), new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount }, filename => TryGetBinFreq(filename, true));
            logger.AppendLine($"Finished all {finishedCount}. ");
            Console.WriteLine($"Finished all {finishedCount}. ");
            Output();
            hasFinished = true;

            void CurrentDomain_ProcessExit(object? sender, EventArgs e)
            {
                if (!hasFinished)
                {
                    logger.AppendLine($"Program exited after finishing {finishedCount}. ");
                    Output();
                }
            }

            void Output()
            {
                using StreamWriter resultsWriter = new StreamWriter(outputFilename);
                resultsWriter.Write(resultsBuilder);
                using StreamWriter logWriter = new StreamWriter(logFilename);
                logWriter.Write(logger);
            }

            void TryGetBinFreq(string filename, bool writeDatasetWithAlpha)
            {
                List<Instance> instances = CSV.ReadFromCsv(filename, null);
                filename = Path.GetFileNameWithoutExtension(filename);
                try
                {
                    double[] binFreq = new double[10];
                    IEnumerable<(Instance instance, double alpha)> results = new KNNContext(instances).GetAllAlphaValues();
                    IEnumerable<double> alphas = results.Select(tuple => tuple.alpha);
                    for (int i = 0; i < 10; i++)
                    {
                        double binLowerRange = i * 0.1;
                        double binUpperRange = i == 9 ? 1.01 : binLowerRange + 0.1; // include alpha = 1.0 in bin9
                        binFreq[i] = alphas.Count(a => a >= binLowerRange && a < binUpperRange) / (double)instances.Count;
                    }
                    resultsBuilder.AppendLine($"{filename},{string.Join(',', binFreq)}");

                    if (writeDatasetWithAlpha)
                    {
                        StringBuilder sb = new StringBuilder($"{string.Join(',', instances.First().Features.Select(f => f.Name))},label,alpha\r\n");
                        foreach ((Instance instance, double alpha) in results)
                        {
                            sb.AppendLine($"{instance.Serialize()},{alpha}");
                        }
                        using StreamWriter sw = new StreamWriter($"{datasetWithAlphaOutputPath}\\{filename}.csv");
                        sw.Write(sb);
                    }

                    logger.AppendLine($"{DateTime.Now}\tSuccessfully finished {filename} (Total: {++finishedCount})");
                    Console.WriteLine($"{DateTime.Now}\tSuccessfully finished {filename} (Total: {finishedCount})");
                    Console.WriteLine($"{filename},{string.Join(',', binFreq)}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}\t{e.GetType()} encountered in processing {filename}, skipping this file");
                    resultsBuilder.AppendLine($"{filename},{string.Join(',', Enumerable.Repeat("NaN", 10))}");
                    logger.AppendLine(new string('>', 64));
                    logger.AppendLine($"{DateTime.Now}\t{e.GetType()} encountered in processing {filename}, skipping this file");
                    logger.AppendLine(e.ToString());
                    logger.AppendLine(new string('>', 64));
                }
            }
        }

        public static void AlphaAllOps(string sourceFolder, string datasetWithAlphaOutputPath, double parallelThreadMultiplier = 1.0)
        {
            StringBuilder logger = new StringBuilder();
            StringBuilder allBinFreqBuilder = new StringBuilder("filename,oalpha-bin0,oalpha-bin1,oalpha-bin2,oalpha-bin3,oalpha-bin4,oalpha-bin5,oalpha-bin6,oalpha-bin7,oalpha-bin8,oalpha-bin9,knnallrew-bin0,knnallrew-bin1,knnallrew-bin2,knnallrew-bin3,knnallrew-bin4,knnallrew-bin5,knnallrew-bin6,knnallrew-bin7,knnallrew-bin8,knnallrew-bin9,nbpkid-bin0,nbpkid-bin1,nbpkid-bin2,nbpkid-bin3,nbpkid-bin4,nbpkid-bin5,nbpkid-bin6,nbpkid-bin7,nbpkid-bin8,nbpkid-bin9,dtc44-bin0,dtc44-bin1,dtc44-bin2,dtc44-bin3,dtc44-bin4,dtc44-bin5,dtc44-bin6,dtc44-bin7,dtc44-bin8,dtc44-bin9,knnallrew-adiff-bin0,knnallrew-adiff-bin1,knnallrew-adiff-bin2,knnallrew-adiff-bin3,knnallrew-adiff-bin4,knnallrew-adiff-bin5,knnallrew-adiff-bin6,knnallrew-adiff-bin7,knnallrew-adiff-bin8,knnallrew-adiff-bin9,nbpkid-adiff-bin0,nbpkid-adiff-bin1,nbpkid-adiff-bin2,nbpkid-adiff-bin3,nbpkid-adiff-bin4,nbpkid-adiff-bin5,nbpkid-adiff-bin6,nbpkid-adiff-bin7,nbpkid-adiff-bin8,nbpkid-adiff-bin9,dtc44-adiff-bin0,dtc44-adiff-bin1,dtc44-adiff-bin2,dtc44-adiff-bin3,dtc44-adiff-bin4,dtc44-adiff-bin5,dtc44-adiff-bin6,dtc44-adiff-bin7,dtc44-adiff-bin8,dtc44-adiff-bin9\r\n");
            int finishedCount = 0;
            bool hasFinished = false;

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            int maxDegreeOfParallelism = (int)(Environment.ProcessorCount * parallelThreadMultiplier);
            Console.WriteLine($"Max degree of parallelism: {maxDegreeOfParallelism} ");
            Parallel.ForEach(Directory.EnumerateFiles(sourceFolder), new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, filename => TryAlphaAllOps(filename));
            logger.AppendLine($"Finished all {finishedCount}. ");
            Console.WriteLine($"Finished all {finishedCount}. ");
            Output();
            hasFinished = true;

            void TryAlphaAllOps(string filename)
            {
                try
                {
                    // 1. read raw datasets
                    Dictionary<Instance, Dictionary<string, double>> datasetInfo = new Dictionary<Instance, Dictionary<string, double>>();
                    List<Instance> instances = CSV.ReadFromCsv(filename, null);
                    instances.ForEach(i => datasetInfo.Add(i, new Dictionary<string, double>()));

                    foreach ((Instance instance, double alpha) in new KNNContext(instances).GetAllAlphaValues())
                    {
                        datasetInfo[instance].Add("alpha", alpha);
                    }

                    filename = Path.GetFileNameWithoutExtension(filename);
                    //StringBuilder fileBinFreqBuilder = new StringBuilder($"{filename},");

                    // 2. do work
                    foreach ((AlgorithmContextBase context, string symbol) in new List<(AlgorithmContextBase context, string symbol)>
                {
                    (new KNNContext(instances) { NeighboringMethod = KNNContext.NeighboringOption.AllNeighborsWithReweighting }, "knnallrew"),
                    (new NaiveBayesContext(instances), "nbpkid"),
                    (new DecisionTreeContext(instances) { UseLaplaceCorrection = true }, "dtc44")
                })
                    {
                        // 2.1 calc prob dist
                        context.Train();
                        foreach (Instance instance in instances)
                        {
                            Dictionary<string, double> result = context.GetProbDist(instance);
                            double p0 = result.ContainsKey("0") ? result["0"] : 0.0;
                            double p1 = result.ContainsKey("1") ? result["1"] : 0.0;
                            bool isCorrect = true;
                            if (p0 > p1)
                            {
                                isCorrect = instance.LabelValue == "0";
                            }
                            else if (p1 > p0)
                            {
                                isCorrect = instance.LabelValue == "1";
                            }
                            datasetInfo[instance].Add($"{symbol}-p0", p0);
                            datasetInfo[instance].Add($"{symbol}-p1", p1);
                            datasetInfo[instance].Add($"{symbol}-iscorrect", isCorrect ? 1 : 0);
                        }

                        // 2.2 calc alpha
                        List<Instance> derivedInstances = new List<Instance>();
                        foreach (Instance instance in instances)
                        {
                            derivedInstances.Add(new Instance(new List<Feature>
                        {
                            new Feature($"{symbol}-p0", ValueType.Continuous, datasetInfo[instance][$"{symbol}-p0"]),
                            new Feature($"{symbol}-p1", ValueType.Continuous, datasetInfo[instance][$"{symbol}-p1"]),
                        }, instance.LabelValue));
                        }

                        List<double> derivedAlphas = new KNNContext(derivedInstances).GetAllAlphaValues().Select(tuple => tuple.Item2).ToList();
                        int temp = 0;
                        foreach (KeyValuePair<Instance, Dictionary<string, double>> kvp in datasetInfo)
                        {
                            kvp.Value.Add($"{symbol}-alpha", derivedAlphas[temp]);
                            kvp.Value.Add($"{symbol}-adiff", derivedAlphas[temp++] - kvp.Value["alpha"]);
                        }

                        // 2.3 record bin freq
                        //for (int i = 0; i < 10; i++)
                        //{
                        //    double binLowerBound = i / 10.0;
                        //    double binUpperBound = i == 9 ? 1.01 : (i + 1) / 10.0;
                        //    fileBinFreqBuilder.Append(derivedAlphas.Count(a => a < binUpperBound && a >= binLowerBound) / (double)instances.Count);
                        //    fileBinFreqBuilder.Append(',');
                        //}
                    }
                    //allBinFreqBuilder.AppendLine(fileBinFreqBuilder.ToString()[..^1]);

                    // 3. write dataset with alphas 
                    List<List<string>> tableFields = new List<List<string>>();
                    foreach ((Instance instance, Dictionary<string, double> props) in datasetInfo)
                    {
                        List<string> rowFields = instance.Serialize().Split(',').ToList();
                        foreach (KeyValuePair<string, double> kvp in props)
                        {
                            rowFields.Add(kvp.Value.ToString());
                        }
                        tableFields.Add(rowFields);
                    }
                    CSV.WriteToCsv($"{datasetWithAlphaOutputPath}\\{filename}.csv", new Table<string>(tableFields), $"{string.Join(',', instances.First().Features.Select(f => f.Name))},label,{string.Join(',', datasetInfo.First().Value.Select(kvp => kvp.Key))}");

                    logger.AppendLine($"{DateTime.Now}\tSuccessfully finished {filename} (Total: {++finishedCount})");
                    Console.WriteLine($"{DateTime.Now}\tSuccessfully finished {filename} (Total: {finishedCount})");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}\t{e.GetType()} encountered in processing {filename}, skipping this file");
                    logger.AppendLine(new string('>', 64));
                    logger.AppendLine($"{DateTime.Now}\t{e.GetType()} encountered in processing {filename}, skipping this file");
                    logger.AppendLine(e.ToString());
                    logger.AppendLine(new string('>', 64));
                }
            }

            void CurrentDomain_ProcessExit(object? sender, EventArgs e)
            {
                if (!hasFinished)
                {
                    logger.AppendLine($"Program exited after finishing {finishedCount}. ");
                    Output();
                }
            }

            void Output()
            {
                using StreamWriter allBinFreqWriter = new StreamWriter("..\\B739-allBinFreqs.csv");
                allBinFreqWriter.Write(allBinFreqBuilder);
                using StreamWriter logWriter = new StreamWriter("..\\log.txt");
                logWriter.Write(logger);
            }
        }

        public static void A270Ops(string sourceFolder, string datasetWithAlphaOutputPath, string logFilename)
        {
            StringBuilder logger = new StringBuilder();
            int finishedCount = 0;
            bool hasFinished = false;

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Parallel.ForEach(Directory.EnumerateFiles(sourceFolder), filename => TryGetBaseAlphas(filename));
            logger.AppendLine($"Finished all {finishedCount}. ");
            Console.WriteLine($"Finished all {finishedCount}. ");
            Output();
            hasFinished = true;


            void TryGetBaseAlphas(string filename)
            {
                try
                {
                    List<Instance> instances = CSV.ReadFromCsv(filename, null);
                    StringBuilder sb = new StringBuilder("feature0,feature1,label,alpha\r\n");
                    foreach ((Instance instance, double alpha) in new KNNContext(instances).GetAllAlphaValues())
                    {
                        sb.AppendLine($"{instance.Serialize()},{alpha}");
                    }
                    using StreamWriter sw = new StreamWriter($"{datasetWithAlphaOutputPath}\\{Path.GetFileNameWithoutExtension(filename)}.csv");
                    sw.Write(sb);
                    logger.AppendLine($"{DateTime.Now}\tSuccessfully finished {filename} (Total: {++finishedCount})");
                    Console.WriteLine($"{DateTime.Now}\tSuccessfully finished {filename} (Total: {finishedCount})");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}\t{e.GetType()} encountered in processing {filename}, skipping this file");
                    logger.AppendLine(new string('>', 64));
                    logger.AppendLine($"{DateTime.Now}\t{e.GetType()} encountered in processing {filename}, skipping this file");
                    logger.AppendLine(e.ToString());
                    logger.AppendLine(new string('>', 64));
                }
            }

            void CurrentDomain_ProcessExit(object? sender, EventArgs e)
            {
                if (!hasFinished)
                {
                    logger.AppendLine($"Program exited after finishing {finishedCount}. ");
                    Output();
                }
            }

            void Output()
            {
                using StreamWriter logWriter = new StreamWriter(logFilename);
                logWriter.Write(logger);
            }

        }

        public static void AlphaToBinFreq(string sourceFolder, string outputFilename)
        {
            StringBuilder resultsBuilder = new StringBuilder("filename,oalpha-bin0,oalpha-bin1,oalpha-bin2,oalpha-bin3,oalpha-bin4,oalpha-bin5,oalpha-bin6,oalpha-bin7,oalpha-bin8,oalpha-bin9,knnallrew-bin0,knnallrew-bin1,knnallrew-bin2,knnallrew-bin3,knnallrew-bin4,knnallrew-bin5,knnallrew-bin6,knnallrew-bin7,knnallrew-bin8,knnallrew-bin9,nbpkid-bin0,nbpkid-bin1,nbpkid-bin2,nbpkid-bin3,nbpkid-bin4,nbpkid-bin5,nbpkid-bin6,nbpkid-bin7,nbpkid-bin8,nbpkid-bin9,dtc44-bin0,dtc44-bin1,dtc44-bin2,dtc44-bin3,dtc44-bin4,dtc44-bin5,dtc44-bin6,dtc44-bin7,dtc44-bin8,dtc44-bin9,knnallrew-adiff-bin0,knnallrew-adiff-bin1,knnallrew-adiff-bin2,knnallrew-adiff-bin3,knnallrew-adiff-bin4,knnallrew-adiff-bin5,knnallrew-adiff-bin6,knnallrew-adiff-bin7,knnallrew-adiff-bin8,knnallrew-adiff-bin9,nbpkid-adiff-bin0,nbpkid-adiff-bin1,nbpkid-adiff-bin2,nbpkid-adiff-bin3,nbpkid-adiff-bin4,nbpkid-adiff-bin5,nbpkid-adiff-bin6,nbpkid-adiff-bin7,nbpkid-adiff-bin8,nbpkid-adiff-bin9,dtc44-adiff-bin0,dtc44-adiff-bin1,dtc44-adiff-bin2,dtc44-adiff-bin3,dtc44-adiff-bin4,dtc44-adiff-bin5,dtc44-adiff-bin6,dtc44-adiff-bin7,dtc44-adiff-bin8,dtc44-adiff-bin9\r\n");
            int finishedCount = 0;

            foreach (string filename in Directory.EnumerateFiles(sourceFolder))
            {
                CalcBinFreq(filename);
            }
            using StreamWriter sw = new StreamWriter(outputFilename);
            sw.Write(resultsBuilder);

            void CalcBinFreq(string filename)
            {
                StringBuilder sb = new StringBuilder(Path.GetFileNameWithoutExtension(filename));
                Table<string> table = CSV.ReadFromCsv(filename, true);

                foreach (Index index in new Index[] { 3, 7, 12, 17 })
                {
                    List<double> valueColumn = table.SelectColumn(index).ConvertAll(s => double.Parse(s));
                    double[] binFreq = new double[10];
                    for (int i = 0; i < 10; i++)
                    {
                        double binLowerBound = i / 10.0;
                        double binUpperBound = i == 9 ? 1.01 : (i + 1) / 10.0;
                        binFreq[i] = valueColumn.Count(d => d < binUpperBound && d >= binLowerBound) / (double)valueColumn.Count;
                    }
                    sb.Append("," + string.Join(',', binFreq));
                }

                foreach (Index index in new Index[] { 8, 13, 18 })
                {
                    List<double> valueColumn = table.SelectColumn(index).ConvertAll(s => double.Parse(s));
                    double[] binFreq = new double[10];
                    for (int i = -5; i < 5; i++)
                    {
                        double binLowerBound = i / 5.0;
                        double binUpperBound = i == 4 ? 1.01 : (i + 1) / 5.0;
                        binFreq[i + 5] = valueColumn.Count(d => d < binUpperBound && d >= binLowerBound) / (double)valueColumn.Count;
                    }
                    sb.Append("," + string.Join(',', binFreq));
                }

                resultsBuilder.AppendLine(sb.ToString());
                Console.WriteLine(++finishedCount);
            }
        }
    }
}
