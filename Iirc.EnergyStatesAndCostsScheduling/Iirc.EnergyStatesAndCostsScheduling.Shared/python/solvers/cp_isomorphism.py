#!/usr/bin/env python3

# This file is released under MIT license.
# See file LICENSE.txt for more information.

# Implementation of general CP model including different strategies parametrized by specialized_solver_config
# - jobs are modeled as non-optional intervals, which are not fixed
#   objective is modeled by element expressions
# - for each possible gap, there is one optional interval variable
#   also fixed in the place; its presence is penalized in the objective

import sys
from pathlib import Path

import docplex.cp.modeler as cpmod
from docplex.cp.function import CpoSegmentedFunction
from docplex.cp.model import CpoModel
from docplex.cp.model import INT_MAX

import cp_utils
from datastructs.model import CpModel
from datastructs.result import Status


class CpIsomorphism(CpModel):
    def __init__(self, config_path: Path, specialized_solver_config_path: Path, instance_path: Path, result_path: Path):
        super().__init__(config_path, specialized_solver_config_path, instance_path, result_path)
        self.job_vars = None

    def initialize(self):
        model = CpoModel()
        self.model = model
        instance = self.instance
        T = len(instance.intervals)
        first_on = instance.earliest_on_interval_idx
        last_on = instance.latest_on_interval_idx
        init_start_times = self.get_init_start_times(sort_starts=True)        

        # -------------------------------------- Construct the model.
        job_vars = dict()  # Dict[Job, CpoIntervalVar]
        self.job_vars = job_vars
        var_jobs = []
        var_gaps = []
        
        
        obj = 0  # objective value
        stp = model.create_empty_solution()  # starting point of the solving procedure                

        # variables for jobs -------------------------------------------------------------------------------------------
        for job in instance.jobs:
            var = model.interval_var(length=job.processing_time, optional=False, name="j[{:d}]".format(job.id))
            model.add(cpmod.start_of(var) >= first_on)  # earliest possible start time is first_on
            model.add(cpmod.end_of(var) <= last_on + 1)  # latest possible end time is last_on
            job_vars[job] = var
            
            # Add to objective
            energy = [instance.cumulative_energy_cost[t][t + job.processing_time - 1] * instance.on_power_consumption
                      if (first_on <= t <= last_on - (job.processing_time - 1))
                      else 0
                      for t in range(T)]
            obj += cpmod.element(energy, cpmod.start_of(var))            

        for i in range(len(instance.jobs)):
            var_gap = model.interval_var(optional=False, name="v_g[{:d}]".format(i))
            var_job = model.interval_var(optional=False, name="v_j[{:d}]".format(i))
            
            model.add(cpmod.end_of(var_gap) == cpmod.start_of(var_job))
            
            if len(var_gaps) == 0:
                model.add(cpmod.start_of(var_gap) == 1)
            else:
                model.add(cpmod.start_of(var_gap) == cpmod.end_of(var_jobs[-1]))
            
            var_jobs.append(var_job)
            var_gaps.append(var_gap)
        
        var_last_gap = model.interval_var(optional=False, name="v_g_last")
        model.add(cpmod.start_of(var_last_gap) == cpmod.end_of(var_jobs[-1]))
        model.add(cpmod.end_of(var_last_gap) == T-1)
        var_gaps.append(var_last_gap)

     
        gap_costs = ([0 for _ in range(T)] +  # gap_len == 0
                     [instance.optimal_switching_costs[gap_s][gap_s + gap_len]
                      if gap_s + gap_len <= T - 1 and instance.optimal_switching_costs[gap_s][gap_s + gap_len] is not None
                      else INT_MAX
                      for gap_len in range(1, T) for gap_s in range(T)])
        
        #for gap in var_gaps[1:-1]:
        for gap in var_gaps:
            obj += cpmod.element(gap_costs, (cpmod.end_of(gap) - cpmod.start_of(gap)) * T + cpmod.start_of(gap))                    

        model.add(cpmod.isomorphism(list(job_vars.values()),var_jobs))

        # set objective
        model.minimize(obj)

        # force ordering of the jobs
        jobs_by_lengths = self.get_job_by_lengths()
        for lst in jobs_by_lengths.values():
            for j_cur, j_nxt in zip(lst, lst[1:]):
                model.add(cpmod.end_before_start(job_vars[j_cur], job_vars[j_nxt]))

    def get_starts(self):
        start_times = dict()  # Dict[Job, int]
        if cp_utils.get_result_status(self.solution) in {Status.Heuristic, Status.Optimal}:
            for job in self.instance.jobs:
                job_sol = self.solution.get_var_solution(self.job_vars[job])
                start_times[job] = job_sol.get_start()
        print(start_times)
        return start_times

# -----------------------------------------------------------------------

model_class = CpIsomorphism(Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve(), Path(sys.argv[3]).resolve(), Path(sys.argv[4]).resolve())
model_class.initialize()
model_class.solve()
model_class.save_result()

