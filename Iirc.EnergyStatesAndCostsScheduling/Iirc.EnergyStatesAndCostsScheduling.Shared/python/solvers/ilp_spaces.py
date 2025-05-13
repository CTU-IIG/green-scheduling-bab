#!/usr/bin/env python3

# This file is released under MIT license.
# See file LICENSE.txt for more information.

import sys
from pathlib import Path

import gurobipy as grb

import ilp_utils
from datastructs.model import IlpModel
from datastructs.result import Status


class IlpGapsOptional(IlpModel):
    def __init__(self, config_path: Path, specialized_solver_config_path: Path, instance_path: Path, result_path: Path):
        super().__init__(config_path, specialized_solver_config_path, instance_path, result_path)
        self.s_jt = None  # job j starts in period t

    def initialize(self):
        model = grb.Model()
        self.model = model
        instance = self.instance
        T = len(instance.intervals)
        first_on = instance.earliest_on_interval_idx
        last_on = instance.latest_on_interval_idx
        init_start_times = self.get_init_start_times(sort_starts=self.specialized_solver_config["ForceJobsOrdering"] or self.specialized_solver_config["RelaxedJobsOrdering"])
        ub = min(instance.get_ub(), self.get_upper_bound(init_start_times))
        valid_start_times_ranges = self.get_valid_start_time_ranges()

        # print("optimal_sw_cost")
        # for t_s in range(T):
        #     for t_e in range(t_s+1,T):
        #         print(t_s, t_e, instance.optimal_switching_costs[t_s][t_e])

        # print("init start times")
        # for job, start in init_start_times.items():
        #     print(job.id, start)
        # print("UB", ub)
        # print("Lower bounds on gaps")
        # for gaps in instance.gaps_lower_bounds:
        #     print(gaps)

        # cnt = 0
        # for row in instance.gaps_lower_bounds:
        #     cnt += sum([1 for i in row if i is not None and i > ub])
        # print("pruned:", cnt)

        # -------------------------------------- Construct the model.
        overlapping = [[] for t in range(T)]  # lists of gaps and jobs overlapping with period t

        job_starts = [
            (j, t)
            for j in instance.jobs
            for t in range(*valid_start_times_ranges[j])
        ]
        job_obj = [instance.cumulative_energy_cost[t][t + j.processing_time - 1] * instance.on_power_consumption
                   for j, t in job_starts]
        s_jt = model.addVars(job_starts, vtype=grb.GRB.BINARY, obj=job_obj, name="js")
        self.s_jt = s_jt

        if self.specialized_solver_config["PruneByUpperBound"]:
            gap_pairs = [(t_s, t_e) for t_s in range(1, T) for t_e in range(t_s + 1, T) if
                         instance.has_switching_cost(t_s, t_e) and instance.get_gap_lower_bound(t_s, t_e) <= ub]
        else:
            gap_pairs = [(t_s, t_e) for t_s in range(1, T) for t_e in range(t_s+1, T) if instance.has_switching_cost(t_s, t_e)]

        gap_obj = [instance.optimal_switching_costs[t_s][t_e] for t_s, t_e in gap_pairs]
        gap_names = ["g_tt[{0},{1}]".format(t_s, t_e) for t_s, t_e in gap_pairs]
        g_tt = model.addVars(gap_pairs, vtype=grb.GRB.BINARY, obj=gap_obj, name=gap_names)

        # - add variables
        for j, t in job_starts:
            for i in range(j.processing_time):  # add to overlapping
                overlapping[t+i].append(s_jt[j, t])  # the task j starting at t overlaps period t+i

        for t_s, t_e in gap_pairs:
            for t_between in range(t_s, t_e):
                overlapping[t_between].append(g_tt[t_s, t_e])  # gap from t_s ending at t_e overlaps t_between

        # - add constraints
        for j in instance.jobs:  # every job is scheduled
            model.addConstr(grb.quicksum(s_jt[j, t] for t in range(1, T) if (j, t) in s_jt) == 1)

        # each period needs to be overlapped by exactly one job or gap
        if self.specialized_solver_config["SingleConstrForBorderGaps"]:
            for t in range(first_on-1, last_on+2):
                model.addConstr(grb.quicksum(overlapping[t]) == 1)
        else:
            for t in range(1, T-1):  # basically no-overlap of jobs and gaps + force horizon to be completely filled
                model.addConstr(grb.quicksum(overlapping[t]) == 1)

        # force gaps to be followed by task
        if self.specialized_solver_config["ForbidConsecutiveGaps"]:
            for t in range(1, T):
                model.addConstr(grb.quicksum(g_tt[t1, t] for t1 in range(1, t) if (t1, t) in g_tt)
                                + grb.quicksum(g_tt[t, t2] for t2 in range(t+1, T) if (t, t2) in g_tt)
                                <= 1)

        # - init start times.
        if init_start_times is not None:
            for job in instance.jobs:
                if (job, init_start_times[job]) in s_jt:
                    s_jt[job, init_start_times[job]].start = 1
                else:
                    raise Exception("Trying to initialize variable s_jt[{:d},{:d}], but the variable was pruned.".format(job.id, init_start_times[job]))

        # TODO : this might make the starting point infeasible
        # force ordering of the jobs
        if self.specialized_solver_config["ForceJobsOrdering"]:
            jobs_by_lengths = self.get_job_by_lengths()
            for lst in jobs_by_lengths.values():
                for j_cur, j_nxt in zip(lst, lst[1:]):
                    s_cur = grb.quicksum(s_jt[j_cur, t]*t for t in range(T) if (j_cur, t) in s_jt)
                    s_nxt = grb.quicksum(s_jt[j_nxt, t]*t for t in range(T) if (j_nxt, t) in s_jt)
                    model.addConstr(s_cur + j_cur.processing_time <= s_nxt)

        if self.specialized_solver_config["ForceJobsOrdering2"]:
            jobs_by_lengths = self.get_job_by_lengths()
            for lst in jobs_by_lengths.values():
                for j_cur, j_nxt in zip(lst, lst[1:]):
                    for t in range(first_on, last_on + 1):
                        sum_cur = grb.quicksum(s_jt[j_cur, tp] for tp in range(first_on, t) if (j_cur, tp) in s_jt)
                        model.addConstr(s_jt[j_nxt, t] <= sum_cur)

        if self.specialized_solver_config["SearchJobsFirst"]:
            for j, t in job_starts:
                s_jt[j, t].BranchPriority = 1

        if self.specialized_solver_config["PruneByLinearRelaxation"]:
            print()
            # Relaxation
            # model.Params.NodeLimit = 1
            # model.optimize()

            self.set_parameters()
            model.update()
            model_relaxed = model.relax()
            model_relaxed.optimize()
            print("Relaxation:", model_relaxed.ObjVal)

            to_prune = 0
            for g_s, g_e in gap_pairs:
                #var = g_tt[g_s, g_e]
                var = model_relaxed.getVarByName("g_tt[{0},{1}]".format(g_s, g_e))
                if instance.get_gap_lower_bound(g_s, g_e) + var.RC > ub:
                #if instance.get_gap_lower_bound(g_s, g_e) > ub:
                    model.addConstr(g_tt[g_s, g_e] == 0)
                    #model.remove(g_tt[g_s, g_e])
                    to_prune += 1

            # model.Params.NodeLimit = grb.GRB.INFINITY

            print("POSSIBLE TO PRUNE", to_prune)
            print()

    def get_starts(self):
        start_times = dict()    # Dict[Job, int]
        if ilp_utils.get_result_status(self.model) in {Status.Heuristic, Status.Optimal}:
            for j in self.instance.jobs:
                for t in range(1, len(self.instance.intervals)):
                    if (j, t) in self.s_jt and self.s_jt[j, t].X > 0.5:
                        start_times[j] = t
                        break
        return start_times

    def solve(self):
        self.set_parameters()

        # sparsify matrix
        if self.specialized_solver_config["SparsifyMatrix"]:
            self.model.setParam("PreSparsify", 1)

        # parameters found by model.tune() after 600 seconds;
        # self.model.setParam("BranchDir", 1)
        # self.model.setParam("Cuts", 0)
        # self.model.setParam("PreDual", 0)
        # self.model.setParam("NumericFocus", 2)
        # self.model.setParam("Presolve", 1)
        # self.model.setParam("Threads", 1)

        # self.model.setParam("TuneTimeLimit", 43200)
        # self.model.tune()

        self.model.optimize()

# -----------------------------------------------------------------------

model_class = IlpGapsOptional(Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve(), Path(sys.argv[3]).resolve(), Path(sys.argv[4]).resolve())
model_class.initialize()
model_class.solve()
model_class.save_result()

