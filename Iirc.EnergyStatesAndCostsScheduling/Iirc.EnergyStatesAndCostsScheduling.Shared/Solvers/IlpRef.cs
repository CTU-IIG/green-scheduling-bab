// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System;
    using System.Collections.Generic;
    using Iirc.Utils.Gurobi;
    using Iirc.Utils.Collections;
    using System.Linq;
    using Gurobi;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    
    /// <summary>
    /// The ILP model introduced in [Aghelinejad2017a]. Denoted as ILP-REF in [Benedikt2020a].
    /// </summary>
    public class IlpRef : BaseMilpSolver<IlpRef.SpecializedSolverConfig, IlpRef.Variables>
    {
        protected override void CreateVariables()
        {
            this.vars = new Variables();

            this.vars.JobProcessed = new Dictionary<Job, GRBVar[]>();
            this.vars.InState = new GRBVar[this.Instance.MachinesCount][][];
            this.vars.Transiting = new GRBVar[this.Instance.MachinesCount][][][];

            foreach (var job in this.Instance.Jobs)
            {
                this.vars.JobProcessed[job] = new GRBVar[this.Instance.Intervals.Length];
                foreach (var interval in this.Instance.Intervals)
                {
                    this.vars.JobProcessed[job][interval.Index] = this.model.AddVar(
                        0, 1, 0, GRB.BINARY, $"jobProcessed[{job.Id},{interval.Index}]");
                }
            }

            foreach (var machineIdx in this.Instance.Machines)
            {
                this.vars.InState[machineIdx] = new GRBVar[this.Instance.States.Length][];
                this.vars.Transiting[machineIdx] = new GRBVar[this.Instance.States.Length][][];

                foreach (var stateIdx in this.Instance.StateInds)
                {
                    this.vars.InState[machineIdx][stateIdx] = new GRBVar[this.Instance.Intervals.Length];
                    foreach (var interval in this.Instance.Intervals)
                    {
                        this.vars.InState[machineIdx][stateIdx][interval.Index] = this.model.AddVar(
                        0, 1, 0, GRB.BINARY, $"inState[{machineIdx},{stateIdx},{interval.Index}]");
                    }
                }

                foreach (var fromStateIdx in this.Instance.StateInds)
                {
                    this.vars.Transiting[machineIdx][fromStateIdx] = new GRBVar[this.Instance.States.Length][];

                    foreach (var toStateIdx in this.Instance.StateInds)
                    {
                        this.vars.Transiting[machineIdx][fromStateIdx][toStateIdx] = new GRBVar[this.Instance.Intervals.Length];
                        foreach (var interval in this.Instance.Intervals)
                        {
                            this.vars.Transiting[machineIdx][fromStateIdx][toStateIdx][interval.Index] = this.model.AddVar(
                            0, 1, 0, GRB.BINARY, $"transiting[{machineIdx},{fromStateIdx},{toStateIdx},{interval.Index}]");
                        }
                    }
                }
            }
        }

        protected override void CreateConstraints()
        {
            foreach (var machineIdx in this.Instance.Machines)
            {
                foreach (var interval in this.Instance.Intervals)
                {
                    this.model.AddConstr(
                        this.Instance.MachineJobs[machineIdx]
                            .Quicksum(job => this.vars.JobProcessed[job][interval.Index])
                        ==
                        this.vars.InState[machineIdx][this.Instance.OnStateIdx][interval.Index],
                        $"procInOn[{machineIdx},{interval.Index}]");
                }
            }

            foreach (var machineIdx in this.Instance.Machines)
            {
                foreach (var interval in this.Instance.Intervals)
                {
                    var lhs = new GRBLinExpr();
                    lhs += this.Instance.StateInds
                        .Quicksum(stateIdx => this.vars.InState[machineIdx][stateIdx][interval.Index]);

                    lhs += this.Instance.StatePairsWithTransition()
                        .Quicksum(
                            states => this.vars.Transiting[machineIdx][states.Item1][states.Item2][interval.Index]);

                    this.model.AddConstr(lhs == 1, $"eitherInStateOrTransiting[{machineIdx},{interval.Index}]");
                }
            }

            foreach (var machineIdx in this.Instance.Machines)
            {
                foreach (var interval in this.Instance.Intervals.SkipLast())
                {
                    foreach (var fromStateIdx in this.Instance.StateInds)
                    {
                        var rhs = new GRBLinExpr();

                        rhs += this.Instance.StateInds
                            .Where(toStateIdx =>
                                this.Instance.StateDiagramTime[fromStateIdx][toStateIdx].HasValue
                                && this.Instance.StateDiagramTime[fromStateIdx][toStateIdx] == 0)
                            .Quicksum(toStateIdx => this.vars.InState[machineIdx][toStateIdx][interval.Index + 1]);

                        rhs += this.Instance.StateInds
                            .Where(toStateIdx =>
                                this.Instance.StateDiagramTime[fromStateIdx][toStateIdx].HasValue
                                && this.Instance.StateDiagramTime[fromStateIdx][toStateIdx] >= 1)
                            .Quicksum(toStateIdx =>
                                this.vars.Transiting[machineIdx][fromStateIdx][toStateIdx][interval.Index + 1]);

                        this.model.AddConstr(
                            this.vars.InState[machineIdx][fromStateIdx][interval.Index] <= rhs,
                            $"inStateRemainsOnTransitsInNext[{machineIdx},{interval.Index},{fromStateIdx}]");
                    }
                }
            }

            foreach (var machineIdx in this.Instance.Machines)
            {
                foreach (var interval in this.Instance.Intervals.SkipLast())
                {
                    foreach (var (fromStateIdx, toStateIdx) in this.Instance.StatePairsWithTransition())
                    {
                        if (this.Instance.StateDiagramTime[fromStateIdx][toStateIdx].Value == 0)
                        {
                            continue;
                        }

                        this.model.AddConstr(
                            this.vars.Transiting[machineIdx][fromStateIdx][toStateIdx][interval.Index]
                            <=
                            this.vars.Transiting[machineIdx][fromStateIdx][toStateIdx][interval.Index + 1] +
                            this.vars.InState[machineIdx][toStateIdx][interval.Index + 1],
                            $"inNextEitherTransitsOrInToState[{machineIdx},{interval.Index},{fromStateIdx},{toStateIdx}]");
                    }
                }
            }

            foreach (var machineIdx in this.Instance.Machines)
            {
                foreach (var interval in this.Instance.Intervals.SkipLast())
                {
                    foreach (var (fromStateIdx, toStateIdx) in this.Instance.StatePairsWithTransition())
                    {
                        if (this.Instance.StateDiagramTime[fromStateIdx][toStateIdx].Value == 0)
                        {
                            continue;
                        }

                        var transitionTime = this.Instance.StateDiagramTime[fromStateIdx][toStateIdx].Value;
                        var lhs = EnumerableExtensions
                            .RangeTo(
                                interval.Index + 1,
                                Math.Min(interval.Index + transitionTime, this.Instance.Intervals.Length - 1))
                            .Quicksum(otherIntervalIdx =>
                                this.vars.Transiting[machineIdx][fromStateIdx][toStateIdx][otherIntervalIdx]);

                        this.model.AddConstr(
                            lhs
                            >=
                            (this.vars.InState[machineIdx][fromStateIdx][interval.Index]
                             + this.vars.Transiting[machineIdx][fromStateIdx][toStateIdx][interval.Index + 1]
                             - 1) * transitionTime,
                            $"lbTransiting[{machineIdx},{interval.Index},{fromStateIdx},{toStateIdx}]");
                    }
                }
            }

            foreach (var machineIdx in this.Instance.Machines)
            {
                foreach (var interval in this.Instance.Intervals.SkipLast())
                {
                    foreach (var (fromStateIdx, toStateIdx) in this.Instance.StatePairsWithTransition())
                    {
                        if (this.Instance.StateDiagramTime[fromStateIdx][toStateIdx].Value == 0)
                        {
                            continue;
                        }

                        var transitionTime = this.Instance.StateDiagramTime[fromStateIdx][toStateIdx].Value;
                        if ((interval.Index + transitionTime) >= this.Instance.Intervals.Length)
                        {
                            continue;
                        }
                        
                        this.model.AddConstr(
                            this.vars.Transiting[machineIdx][fromStateIdx][toStateIdx][interval.Index]
                            + this.vars.Transiting[machineIdx][fromStateIdx][toStateIdx][interval.Index + transitionTime]
                            <=
                            1,
                            $"ubTransiting[{machineIdx},{interval.Index},{fromStateIdx},{toStateIdx}]");
                    }
                }
            }

            foreach (var machineIdx in this.Instance.Machines)
            {
                foreach (var interval in this.Instance.Intervals)
                {
                    this.model.AddConstr(
                        this.Instance.MachineJobs[machineIdx].Quicksum(job => this.vars.JobProcessed[job][interval.Index])
                        <=
                        1,
                        $"noOverlap[{machineIdx},{interval.Index}]");
                }
            }

            foreach (var job in this.Instance.Jobs)
            {
                foreach (var intervalIdx in EnumerableExtensions
                    .RangeTo(job.ProcessingTime, this.Instance.Intervals.Length - job.ProcessingTime - 1))
                {
                    var lhs = new GRBLinExpr();

                    lhs += EnumerableExtensions
                        .RangeTo(0, intervalIdx - job.ProcessingTime)
                        .Quicksum(otherIntervalIdx => this.vars.JobProcessed[job][otherIntervalIdx]);
                    
                    lhs += EnumerableExtensions
                        .RangeTo(intervalIdx + job.ProcessingTime, this.Instance.Intervals.Length - 1)
                        .Quicksum(otherIntervalIdx => this.vars.JobProcessed[job][otherIntervalIdx]);

                    this.model.AddConstr(
                        lhs <= job.ProcessingTime * (1 - this.vars.JobProcessed[job][intervalIdx]),
                        $"nonPreemption[{job.Id},{intervalIdx}]");
                }
            }

            foreach (var job in this.Instance.Jobs)
            {
                this.model.AddConstr(
                    this.Instance.Intervals.Quicksum(interval => this.vars.JobProcessed[job][interval.Index])
                    >=
                    job.ProcessingTime,
                    $"processingTime[{job.Id}]");
            }

            foreach (var machineIdx in this.Instance.Machines)
            {
                foreach (var interval in new[] {this.Instance.Intervals.First(), this.Instance.Intervals.Last()})
                {
                    this.model.AddConstr(
                        this.vars.InState[machineIdx][this.Instance.BaseOffStateIdx][interval.Index]
                        ==
                        1,
                        $"initBaseOff[{machineIdx},{interval.Index}]");
                }
            }
        }

        protected override void CreateObjective()
        {
            var obj = new GRBLinExpr();
            foreach (var machineIdx in this.Instance.Machines)
            {
                foreach (var interval in this.Instance.Intervals)
                {
                    obj +=
                        interval.EnergyCost
                        * this.Instance.StateInds
                            .Quicksum(stateIdx =>
                                this.Instance.StatePowerConsumption[stateIdx]
                                * this.vars.InState[machineIdx][stateIdx][interval.Index]);

                    foreach (var (fromStateIdx, toStateIdx) in this.Instance.StatePairsWithTransition())
                    {
                        obj += interval.EnergyCost
                               * (this.Instance.StateDiagramPowerConsumption[fromStateIdx][toStateIdx].Value
                                  * this.vars.Transiting[machineIdx][fromStateIdx][toStateIdx][interval.Index]);
                    }
                }
            }
            
            this.model.SetObjective(obj, GRB.MINIMIZE);
        }
        
        protected override StartTimes GetStartTimes()
        {
            var startTimes = new StartTimes();
            foreach (var job in this.Instance.Jobs)
            {
                startTimes[job] = this.vars.JobProcessed[job].WhereNonZero().First();
            }

            return startTimes;
        }
        
        protected override void SetInitStartTimes(StartTimes initStartTimes)
        {
            foreach (var job in this.Instance.Jobs)
            {
                var startIntervalIdx = initStartTimes.StartInterval(job, this.Instance).Index;
                var completionIntervalIdx = initStartTimes.CompletionInterval(job, this.Instance).Index;
                foreach (var intervalIdx in EnumerableExtensions.RangeTo(startIntervalIdx, completionIntervalIdx))
                {
                    this.vars.JobProcessed[job][intervalIdx].Start = 1;
                }
            }
        }
        
        protected override int? GetObjective()
        {
            return (int)Math.Round(this.model.ObjVal);
        }

        public class SpecializedSolverConfig
        {
        }
        
        public class Variables
        {
            public GRBVar[][][] InState { get; set; }
            public GRBVar[][][][] Transiting { get; set; }
            public Dictionary<Job, GRBVar[]> JobProcessed { get; set; }
        }
    }
}
