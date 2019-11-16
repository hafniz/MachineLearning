﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Math;

namespace MLCore.Algorithm
{
    public class DecisionTreeContext : AlgorithmContextBase
    {
        public class Node
        {
            public string SplitFeatureName { get; set; }
            public double? SplitThreshold { get; set; }
            public bool IsLeafNode { get; set; }
            public List<Instance> InstancesIn { get; }
            public Dictionary<string, double> LeafProbDist { get; set; } = new Dictionary<string, double>();
            public Dictionary<string, Node> SubNodes { get; } = new Dictionary<string, Node>();

            public Node(List<Instance> instancesIn, string splitFeatureName, double? splitThreshold)
            {
                InstancesIn = instancesIn;
                SplitFeatureName = splitFeatureName;
                SplitThreshold = splitThreshold;
            }

            public Node NavigateDown(Instance instance)
            {
                if (SubNodes is null)
                {
                    throw new NullReferenceException("SubNodes is null. ");
                }
                if (instance.Features[SplitFeatureName].ValueType == ValueType.Discrete)
                {
                    return SubNodes[instance.Features[SplitFeatureName].Value];
                }
                if (instance.Features[SplitFeatureName].Value <= SplitThreshold)
                {
                    return SubNodes["less than or equal to threshold"];
                }
                return SubNodes["greater than threshold"];
            }

            public void CalcLeafProbDist()
            {
                foreach (string? label in InstancesIn.Select(i => i.LabelValue).Distinct())
                {
                    if (label is null)
                    {
                        throw new NullReferenceException("Unlabeled instances used in growing a tree. ");
                    }
                    LeafProbDist.Add(label, InstancesIn.Count(i => i.LabelValue == label) / (double)InstancesIn.Count);
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                AppendNodeContent(this, 0);
                return sb.ToString();

                void AppendNodeContent(Node currentNode, int currentDepth)
                {
                    if (currentNode.IsLeafNode)
                    {
                        foreach (KeyValuePair<string, double> kvp in currentNode.LeafProbDist)
                        {
                            sb.AppendLine($"{new string(' ', currentDepth * 4)}{kvp.Key}: {kvp.Value}");
                        }
                        return;
                    }
                    if (currentNode.SplitThreshold.HasValue) // Continuous
                    {
                        sb.AppendLine($"{new string(' ', currentDepth * 4)}{currentNode.SplitFeatureName} <= {currentNode.SplitThreshold.Value}: ");
                        AppendNodeContent(currentNode.SubNodes["less than or equal to threshold"], currentDepth + 1);
                        sb.AppendLine($"{new string(' ', currentDepth * 4)}{currentNode.SplitFeatureName} > {currentNode.SplitThreshold.Value}: ");
                        AppendNodeContent(currentNode.SubNodes["greater than threshold"], currentDepth + 1);
                    }
                    else // Discrete
                    {
                        foreach (KeyValuePair<string, Node> branch in currentNode.SubNodes)
                        {
                            sb.AppendLine($"{new string(' ', currentDepth * 4)}{currentNode.SplitFeatureName} = {branch.Key}: ");
                            AppendNodeContent(branch.Value, currentDepth + 1);
                        }
                    }
                }
            }
        }

        public DecisionTreeContext(List<Instance> trainingInstances) : base(trainingInstances) { }
        private Node? RootNode { get; set; }
        private static double Xlog2X(double x) => x == 0 ? 0 : x * Log2(x);

        private static double Entropy(IEnumerable<Instance> instances)
        {
            double sum = 0;
            instances.Select(i => i.LabelValue).Distinct().ToList().ForEach(l => sum -= Xlog2X(instances.Count(i => i.LabelValue == l) / (double)instances.Count()));
            return sum;
        }

        private static double GainRatioDiscrete(List<Instance> instances, string featureName)
        {
            if (instances[0].Features[featureName].ValueType != ValueType.Discrete)
            {
                throw new ArgumentException($"Values of {featureName} is not of discrete type. ");
            }

            double sum = 0;
            instances.Select(i => i.Features[featureName].Value).Distinct().ToList().ForEach(v =>
            sum += instances.Count(i => i.Features[featureName].Value == v) / (double)instances.Count * Entropy(instances.Where(i => i.Features[featureName].Value == v)));
            double infoGain = Entropy(instances) - sum;
            double splitRatio = 0;
            instances.Select(i => i.Features[featureName].Value).Distinct().ToList().ForEach(v =>
            splitRatio -= Xlog2X(instances.Count(i => i.Features[featureName].Value == v) / (double)instances.Count));
            return infoGain / splitRatio;
        }

        private static double GainRatioContinuous(List<Instance> instances, string featureName, out double threshold)
        {
            if (instances[0].Features[featureName].ValueType != ValueType.Continuous)
            {
                throw new ArgumentException($"Values of {featureName} is not of continuous type. ");
            }

            List<double> distinctValues = instances.Select(i => i.Features[featureName].Value).Distinct().ToList().ConvertAll(v => (double)v);
            threshold = double.NaN;
            double maxGainRatio = 0;
            foreach (double tryThreshold in distinctValues)
            {
                List<Instance> dichotomized = new List<Instance>();
                foreach (Instance instance in instances)
                {
                    dichotomized.Add(new Instance(new Dictionary<string, Feature> { {
                        featureName, new Feature(ValueType.Discrete, instance.Features[featureName].Value <= tryThreshold ? $"less than or equal to {tryThreshold}" : $"greater than {tryThreshold}")
                    } }, instance.LabelValue, instance.LabelName));
                }
                double tryGainRatio = GainRatioDiscrete(dichotomized, featureName);
                if (tryGainRatio > maxGainRatio)
                {
                    maxGainRatio = tryGainRatio;
                    threshold = tryThreshold;
                }
            }
            return maxGainRatio;
        }

        private static string GetSplitFeature(List<Instance> instances, out double? threshold)
        {
            string featureName = "";
            double maxGainRatio = 0;
            threshold = null;
            foreach (string tryFeatureName in instances[0].Features.Select(kvp => kvp.Key))
            {
                if (instances[0].Features[tryFeatureName].ValueType == ValueType.Discrete)
                {
                    double tryGainRatio = GainRatioDiscrete(instances, tryFeatureName);
                    if (tryGainRatio > maxGainRatio)
                    {
                        maxGainRatio = tryGainRatio;
                        featureName = tryFeatureName;
                        threshold = null;
                    }
                }
                else
                {
                    double tryGainRatio = GainRatioContinuous(instances, tryFeatureName, out double tryThreshold);
                    if (tryGainRatio > maxGainRatio)
                    {
                        maxGainRatio = tryGainRatio;
                        featureName = tryFeatureName;
                        threshold = tryThreshold;
                    }
                }
            }
            return featureName;
        }

        private static Dictionary<string, List<Instance>> Split(List<Instance> instances, string featureName, double? threshold = null)
        {
            Dictionary<string, List<Instance>> splitResults = new Dictionary<string, List<Instance>>();
            if (instances[0].Features[featureName].ValueType == ValueType.Discrete)
            {
                foreach (string featureValue in instances.Select(i => i.Features[featureName].Value).Distinct())
                {
                    splitResults.Add(featureValue, new List<Instance>());
                }
                foreach (Instance instance in instances)
                {
                    splitResults[(string)instance.Features[featureName].Value].Add(instance);
                }
            }
            else
            {
                splitResults.Add("less than or equal to threshold", new List<Instance>());
                splitResults.Add("greater than threshold", new List<Instance>());
                foreach (Instance instance in instances)
                {
                    if (instance.Features[featureName].Value <= threshold)
                    {
                        splitResults["less than or equal to threshold"].Add(instance);
                    }
                    else
                    {
                        splitResults["greater than threshold"].Add(instance);
                    }
                }
            }
            return splitResults;
        }

        private void SplitRecursive(Node node, int currentDepth)
        {
            currentDepth++;
            if (node.InstancesIn.Count <= 1 || node.InstancesIn.Select(i => i.LabelValue).Distinct().Count() <= 1 || string.IsNullOrEmpty(node.SplitFeatureName))
            {
                node.IsLeafNode = true;
                node.CalcLeafProbDist();
                return;
            }

            Dictionary<string, List<Instance>> branches = Split(node.InstancesIn, node.SplitFeatureName, node.SplitThreshold);
            foreach (KeyValuePair<string, List<Instance>> kvp in branches)
            {
                string subSplitFeatureName = GetSplitFeature(kvp.Value, out double? subSplitThreshold);
                node.SubNodes.Add(kvp.Key, new Node(kvp.Value, subSplitFeatureName, subSplitThreshold));
            }
            foreach (Node subNode in node.SubNodes.Select(kvp => kvp.Value))
            {
                SplitRecursive(subNode, currentDepth);
            }
        }

        public override void Train()
        {
            string splitFeatureName = GetSplitFeature(TrainingInstances, out double? threshold);
            RootNode = new Node(TrainingInstances, splitFeatureName, threshold);
            SplitRecursive(RootNode, -1);
        }

        public override Dictionary<string, double> GetProbDist(Instance testingInstance)
        {
            Node? currentNode = RootNode;
            if (currentNode is null)
            {
                throw new NullReferenceException("Root node is null. ");
            }
            while (!currentNode.IsLeafNode)
            {
                currentNode = currentNode.NavigateDown(testingInstance);
            }
            return OrderedNormalized(currentNode.LeafProbDist);
        }

        public override string ToString() => RootNode?.ToString() ?? "Root node is null. ";

        /// <summary>
        /// For experimental use. Randomly generates rules of a 2-dimensional (values of both features are continuous), binary decision tree and a binary-labeled dataset classified by the tree. 
        /// </summary>
        /// <param name="maxDepth">Target depth of the tree. </param>
        /// <param name="outputConfig">The filenames, including extensions, of the file containing instances to be tested on and location where test results are to be saved respectively. If left null, testing and saving will not be performed. </param>
        /// <returns>The root node of the generated tree. </returns>
        public static Node GenerateTree(int maxDepth, (string testTemplate, string outputFilename)? outputConfig = null)
        {
            (Node node, List<(string splitFeatureName, double leftBound, double rightBound)> bounds) rootNodeInfo;
            rootNodeInfo.node = new Node(new List<Instance>(), "", null);
            rootNodeInfo.bounds = new List<(string splitFeatureName, double leftBound, double rightBound)> { ("feature0", 0, 1), ("feature1", 0, 1) };
            int startingFeatureIndex = new Random().Next(2);
            SplitFeatures(rootNodeInfo, maxDepth, -1, startingFeatureIndex);

            if (!(outputConfig is null))
            {
                List<Instance> testingInstances = CSV.ReadFromCsv(outputConfig.Value.testTemplate, null);
                List<Instance> predictResults = new List<Instance>();
                foreach (Instance testingInstance in testingInstances)
                {
                    Node currentNode = rootNodeInfo.node;
                    while (!currentNode.IsLeafNode)
                    {
                        currentNode = currentNode.NavigateDown(testingInstance);
                    }
                    predictResults.Add(new Instance(testingInstance.Features, currentNode.LeafProbDist.Single(kvp => kvp.Value == 1).Key));
                }
                CSV.WriteToCsv(outputConfig.Value.outputFilename, predictResults);
            }
            return rootNodeInfo.node;

            static void SplitFeatures((Node node, List<(string splitFeatureName, double leftBound, double rightBound)> bounds) nodeInfo, int maxDepth, int currentDepth, int splitFeatureIndex)
            {
                Random random = new Random();
                currentDepth++;

                nodeInfo.node.SplitFeatureName = nodeInfo.bounds[splitFeatureIndex].splitFeatureName;

                // This limits the splitThreshold within range of 40% - 60% between leftBound and rightBound. Can be changed or deleted according to demand. 
                double leftBound = nodeInfo.bounds[splitFeatureIndex].leftBound;
                double rightBound = nodeInfo.bounds[splitFeatureIndex].rightBound;
                double offset = random.NextDouble(); //0.4 + random.NextDouble() / 5;
                double splitThreshold = leftBound + offset * (rightBound - leftBound);
                nodeInfo.node.SplitThreshold = splitThreshold;

                nodeInfo.node.SubNodes.Add("less than or equal to threshold", new Node(new List<Instance>(), "", null));
                nodeInfo.node.SubNodes.Add("greater than threshold", new Node(new List<Instance>(), "", null));

                if (currentDepth + 1 >= maxDepth) // subnodes of current node are leaf nodes
                {
                    nodeInfo.node.SubNodes["less than or equal to threshold"].IsLeafNode = true;
                    nodeInfo.node.SubNodes["greater than threshold"].IsLeafNode = true;
                    if (new Random().Next() % 2 == 1) // first subnode is positive
                    {
                        nodeInfo.node.SubNodes["less than or equal to threshold"].LeafProbDist = new Dictionary<string, double>() { { "1.0", 1 }, { "0.0", 0 } };
                        nodeInfo.node.SubNodes["greater than threshold"].LeafProbDist = new Dictionary<string, double>() { { "0.0", 1 }, { "1.0", 0 } };
                    }
                    else
                    {
                        nodeInfo.node.SubNodes["less than or equal to threshold"].LeafProbDist = new Dictionary<string, double>() { { "0.0", 1 }, { "1.0", 0 } };
                        nodeInfo.node.SubNodes["greater than threshold"].LeafProbDist = new Dictionary<string, double>() { { "1.0", 1 }, { "0.0", 0 } };
                    }
                    return;
                }

                List<(string splitFeatureName, double leftBound, double rightBound)> lowBounds;
                List<(string splitFeatureName, double leftBound, double rightBound)> highBounds;

                if (splitFeatureIndex == 0)
                {
                    lowBounds = new List<(string splitFeatureName, double leftBound, double rightBound)>()
                    {
                        ("feature0", nodeInfo.bounds[0].leftBound, splitThreshold), ("feature1", nodeInfo.bounds[1].leftBound, nodeInfo.bounds[1].rightBound)
                    };
                    highBounds = new List<(string splitFeatureName, double leftBound, double rightBound)>()
                    {
                        ("feature0", splitThreshold, nodeInfo.bounds[0].rightBound), ("feature1", nodeInfo.bounds[1].leftBound, nodeInfo.bounds[1].rightBound)
                    };
                }
                else
                {
                    lowBounds = new List<(string splitFeatureName, double leftBound, double rightBound)>()
                    {
                        ("feature0", nodeInfo.bounds[0].leftBound, nodeInfo.bounds[0].rightBound), ("feature1", nodeInfo.bounds[1].leftBound, splitThreshold)
                    };
                    highBounds = new List<(string splitFeatureName, double leftBound, double rightBound)>()
                    {
                        ("feature0", nodeInfo.bounds[1].leftBound, nodeInfo.bounds[0].rightBound), ("feature1", splitThreshold, nodeInfo.bounds[1].rightBound)
                    };
                }

                SplitFeatures((nodeInfo.node.SubNodes["less than or equal to threshold"], lowBounds), maxDepth, currentDepth, splitFeatureIndex == 0 ? 1 : 0);
                SplitFeatures((nodeInfo.node.SubNodes["greater than threshold"], highBounds), maxDepth, currentDepth, splitFeatureIndex == 0 ? 1 : 0);
            }
        }
    }
}
