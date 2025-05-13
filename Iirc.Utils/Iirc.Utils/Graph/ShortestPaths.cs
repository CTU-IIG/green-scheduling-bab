// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="ShortestPaths.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2019 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.Graph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Iirc.Utils.SolverFoundations;
    using Iirc.Utils.Time;

    public class ShortestPaths : IAlgorithm
    {
        private Timer timer;
        private LayeredGraph graph;
        private bool topologicallyOrdered;

        private bool stopOnReachingSink;

        public ShortestPaths(int capacity)
        {
            this.PathWeights = new int[capacity];
            this.Prevs = new int[this.PathWeights.Length];
            this.ShortestPath = new List<int>();
        }

        public ShortestPaths() : this(1024)
        {
            
        }

        /// <summary>
        /// Sets the input graph.
        /// </summary>
        /// <param name="graph">The input graph.</param>
        /// <param name="topologicallyOrdered">
        /// Hint that the graph is topologically ordered along the node indices. If yes, a faster algorithm is used for
        /// computing the shortest path.
        /// <param name="stopOnReachingSink">Whether the algorithm should stop once the shortest path to sink is
        /// computed.</param>
        /// </param>
        /// <remarks>The graph is not copied.</remarks>
        public void SetInput(
            LayeredGraph graph,
            bool topologicallyOrdered = false,
            bool stopOnReachingSink = true,
            int? differentSource = null)
        {
            this.graph = graph;
            this.topologicallyOrdered = topologicallyOrdered;
            this.stopOnReachingSink = stopOnReachingSink;
            if (this.graph.NodesCount > this.PathWeights.Length)
            {
                this.PathWeights = new int[this.graph.NodesCount * 2];
                this.Prevs = new int[this.PathWeights.Length];
            }

            if (differentSource.HasValue)
            {
                this.Source = differentSource.Value;
            }
            else
            {
                this.Source = this.graph.Source;
            }
        }
        
        public Status Solve(TimeSpan? timeLimit = null)
        {
            this.timer = new Timer(timeLimit);
            this.timer.Start();
            
            this.Clear();
            
            var status = Status.NoSolution;
            if (this.topologicallyOrdered)
            {
                this.TopologicallyOrderedSolver();
            }
            else
            {
                this.Dijkstra();
            }

            if (this.Prevs[this.graph.Sink] != int.MaxValue)
            {
                status = Status.Optimal;
                
                this.ShortestPathWeight = this.PathWeights[this.graph.Sink];

                // Shortest path reconstruction.
                int nodeIdx = this.graph.Sink;
                while (nodeIdx != this.Source)
                {
                    this.ShortestPath.Add(nodeIdx);
                    nodeIdx = this.Prevs[nodeIdx];
                }
                this.ShortestPath.Add(this.Source);
                ShortestPath.Reverse();
            }
            
            this.timer.Stop();
            return this.TimeLimitReached ? Status.NoSolution : status;
        }

        private void Clear()
        {
            this.ShortestPathWeight = null;
            this.ShortestPath.Clear();
            for (int nodeIdx = 0; nodeIdx < this.graph.NodesCount; nodeIdx++)
            {
                this.PathWeights[nodeIdx] = int.MaxValue;
                this.Prevs[nodeIdx] = int.MaxValue;
            }

            this.PathWeights[this.Source] = 0;
        }

        private void Dijkstra()
        {
            var unvisitedNodes = new HashSet<int>();
            unvisitedNodes.Add(this.Source);
            
            while (unvisitedNodes.Any())
            {
                if (this.TimeLimitReached)
                {
                    break;
                }
                
                int selectedNode = -1;
                {
                    int pathWeightMin = int.MaxValue;
                    foreach (var node in unvisitedNodes)
                    {
                        if (pathWeightMin > this.PathWeights[node])
                        {
                            selectedNode = node;
                            pathWeightMin = this.PathWeights[node];
                        }
                    }
                }

                if (selectedNode == -1)
                {
                    // Disconnected graph.
                    return;
                }
                
                unvisitedNodes.Remove(selectedNode);
                
                foreach (var edge in this.graph.OutgoingEdges[selectedNode])
                {
                    if (this.PathWeights[edge.ToNode] == int.MaxValue)
                    {
                        unvisitedNodes.Add(edge.ToNode);
                    }
                    
                    var pathWeight = this.PathWeights[edge.FromNode] + edge.Weight;
                    if (this.PathWeights[edge.ToNode] > pathWeight)
                    {
                        this.PathWeights[edge.ToNode] = pathWeight;
                        this.Prevs[edge.ToNode] = edge.FromNode;
                    }
                }

                if (selectedNode == this.graph.Sink && this.stopOnReachingSink)
                {
                    // Optimization: sink reached, stop the algorithm.
                    return;
                }
            }
        }

        private void BellmanFord()
        {
            for (int i = 0; i < this.graph.NodesCount; i++)
            {
                if (this.TimeLimitReached)
                {
                    break;
                }

                foreach (var edge in this.graph.Edges)
                {
                    if (this.PathWeights[edge.FromNode] == int.MaxValue)
                    {
                        continue;
                    }

                    var pathWeight = this.PathWeights[edge.FromNode] + edge.Weight;
                    if (this.PathWeights[edge.ToNode] > pathWeight)
                    {
                        this.PathWeights[edge.ToNode] = pathWeight;
                        this.Prevs[edge.ToNode] = edge.FromNode;
                    }
                }
            }
        }

        private void TopologicallyOrderedSolver()
        {
            var unvisitedNodes = new SortedSet<int>();
            unvisitedNodes.Add(this.Source);
            while (unvisitedNodes.Any())
            {
                var fromNode = unvisitedNodes.Min;
                unvisitedNodes.Remove(fromNode);
                foreach (var edge in this.graph.OutgoingEdges[fromNode])
                {
                    unvisitedNodes.Add(edge.ToNode);
                    
                    var pathWeight = this.PathWeights[edge.FromNode] + edge.Weight;
                    if (this.PathWeights[edge.ToNode] > pathWeight)
                    {
                        this.PathWeights[edge.ToNode] = pathWeight;
                        this.Prevs[edge.ToNode] = edge.FromNode;
                    }
                }
            }
        }
        
        public bool TimeLimitReached
        {
            get
            {
                return this.timer.TimeLimitReached;
            }
        }
        
        public int[] PathWeights { get; private set; }
        public int[] Prevs { get; private set; }
        
        public int Source { get; private set; }
        
        public int? ShortestPathWeight { get; private set; }
        public List<int> ShortestPath { get; private set; }
    }
}
