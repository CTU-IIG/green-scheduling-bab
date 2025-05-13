// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredGraph.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2019 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.Graph
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Iirc.Utils.Collections;

    public class LayeredGraph
    {
        public LayeredGraph(int colsCount, int rowsCount)
        {
            this.ColsCount = colsCount;
            this.RowsCount = rowsCount;
            this.NodesCount = colsCount * rowsCount + 2;
            this.Source = 0;
            this.Sink = this.NodesCount - 1;
            this.IncomingEdgesCount = Enumerable.Repeat(0, this.NodesCount).ToArray();
            this.Edges = new List<Edge>();
            this.OutgoingEdges = Enumerable.Range(0, this.NodesCount).Select(_ => new List<Edge>()).ToList();
        }

        public void AddEdge(int row1, int col1, int row2, int col2, int weight)
        {
            int node1 = this.NodeIndex(row1, col1);
            int node2 = this.NodeIndex(row2, col2);
            this.AddEdge(node1, node2, weight);
        }
        
        public void AddEdgeFromSource(int row, int col, int weight)
        {
            int node = this.NodeIndex(row, col);
            this.AddEdge(this.Source, node, weight);
        }
        
        public void AddEdgeToSink(int row, int col, int weight)
        {
            int node = this.NodeIndex(row, col);
            this.AddEdge(node, this.Sink, weight);
        }

        public void AddEdge(int fromNode, int toNode, int weight)
        {
            var edge = new Edge { FromNode = fromNode, ToNode = toNode, Weight = weight };
            this.OutgoingEdges[fromNode].Add(edge);
            this.Edges.Add(edge);
            this.IncomingEdgesCount[toNode]++;
        }

        public int NodeIndex(int row, int col)
        {
            return this.ColsCount * row + col + 1;
        }

        public int NodeRow(int node)
        {
            return (node - 1) / this.ColsCount;
        }
        
        public int NodeCol(int node)
        {
            return node - this.NodeRow(node) * this.ColsCount - 1;
        }

        public bool HasIncomingEdge(int row, int col)
        {
            return this.IncomingEdgesCount[this.NodeIndex(row, col)] > 0;
        }
        
        public bool HasIncomingEdge(int node)
        {
            return this.IncomingEdgesCount[node] > 0;
        }

        public void RemoveEdges()
        {
            foreach (var nodeOutgoingEdges in this.OutgoingEdges)
            {
                nodeOutgoingEdges.Clear();
            }
            
            for (var node = 0; node < this.NodesCount; node++)
            {
                this.IncomingEdgesCount[node] = 0;
            }
        }

        public void RemoveEdgesFromSource()
        {
            foreach (var edge in this.OutgoingEdges[this.Source])
            {
                this.IncomingEdgesCount[edge.ToNode]--;
            }
            
            this.OutgoingEdges[this.Source].Clear();
            this.Edges.RemoveAll(edge => edge.FromNode == this.Source);
        }

        public string ToDotFormat()
        {
            var sb = new StringBuilder();

            sb.AppendLine("digraph layeredGraph {");

            foreach (var edge in this.Edges)
            {
                sb.AppendLine($"{edge.FromNode} -> {edge.ToNode} [label={edge.Weight}]");
            }

            sb.AppendLine($"{{rank=same; {this.Source}}}");
            sb.AppendLine($"{{rank=same; {this.Sink}}}");
            for (var row = 0; row < this.RowsCount; row++)
            {
                var firstNodeInRow = row * this.ColsCount + 1;
                var nodesInRow = EnumerableExtensions.RangeTo(firstNodeInRow, firstNodeInRow + this.ColsCount - 1);
                sb.AppendLine($"{{rank=same; {string.Join(' ', nodesInRow)}}}");
            }
            
            sb.AppendLine("}");

            return sb.ToString();
        }
        
        public int ColsCount { get; set; }
        public int RowsCount { get; set; }
        
        public int NodesCount { get; set; }
        public int Source { get; set; }
        public int Sink { get; set; }

        public List<Edge> Edges { get; private set; }

        public int[] IncomingEdgesCount { get; set; }
        
        public List<List<Edge>> OutgoingEdges { get; set; }
        
        public class Edge
        {
            public int FromNode { get; set; }
            public int ToNode { get; set; }
            public int Weight { get; set; }
        }
    }
}
