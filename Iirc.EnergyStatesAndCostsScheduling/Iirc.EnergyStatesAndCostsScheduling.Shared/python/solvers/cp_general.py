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


class CpGeneral(CpModel):
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
        ub = min(instance.get_ub(), self.get_upper_bound(init_start_times))
        total_proc = sum(j.processing_time for j in instance.jobs)

        # -------------------------------------- Construct the model.
        job_vars = dict()  # Dict[Job, CpoIntervalVar]
        self.job_vars = job_vars
        gap_vars = dict()  # Dict[Tuple[int, int], CpoIntervalVar]
        obj = 0  # objective value
        seq_vars = []  # variables for no-overlap constraint
        stp = model.create_empty_solution()  # starting point of the solving procedure
        step_fns = CpGeneral.get_step_functions(instance, first_on, last_on)  # Generate seqment function for each processing time.

        first_job_var = model.interval_var(length=0, optional=False, name="first_job_var", start=1)
        seq_vars.append(first_job_var)  # dummy job with 0 length, first in the sequence
        last_job_var = model.interval_var(length=0, optional=False, name="last_job_var", end=T-1)
        seq_vars.append(last_job_var)  # dummy job with 0 length, last in the sequence

        # variables for jobs -------------------------------------------------------------------------------------------
        for job in instance.jobs:
            var = model.interval_var(length=job.processing_time, optional=False, name="j[{:d}]".format(job.id))
            model.add(cpmod.start_of(var) >= first_on)  # earliest possible start time is first_on
            model.add(cpmod.end_of(var) <= last_on + 1)  # latest possible end time is last_on
            job_vars[job] = var
            seq_vars.append(var)

            if self.specialized_solver_config["JobInObjectiveModelling"] == 0:  # Optional :
                alternatives = []
                for t in range(first_on, T):
                    if t <= last_on - (job.processing_time - 1):  # (-1) because unit job can be processed in last_on
                        var = model.interval_var(start=t,
                                                 length=job.processing_time,
                                                 optional=True,
                                                 name="j[{:d},{:d}]".format(t, job.id))
                        alternatives.append(var)
                        obj += cpmod.presence_of(var) * instance.cumulative_energy_cost[t][t + job.processing_time - 1] * instance.on_power_consumption

                # add a present variable for the job
                model.add(cpmod.alternative(job_vars[job], alternatives))
            elif self.specialized_solver_config["JobInObjectiveModelling"] == 1:  # Logical :
                for t in range(T):
                    if first_on <= t <= last_on - (job.processing_time - 1):
                        obj += (cpmod.start_of(var, absentValue=-1) == t) * instance.cumulative_energy_cost[t][t + job.processing_time - 1] * instance.on_power_consumption
            elif self.specialized_solver_config["JobInObjectiveModelling"] == 2:  # Element :
                energy = [instance.cumulative_energy_cost[t][t + job.processing_time - 1] * instance.on_power_consumption
                          if (first_on <= t <= last_on - (job.processing_time - 1))
                          else 0
                          for t in range(T)]
                obj += cpmod.element(energy, cpmod.start_of(var))
            elif self.specialized_solver_config["JobInObjectiveModelling"] == 3:  # Overlap :
                for t in range(T):  # add overlaps to objective
                    if first_on <= t <= last_on:
                        obj += cpmod.overlap_length(var, (t, t + 1)) * instance.intervals[t].energy_cost * instance.on_power_consumption
            elif self.specialized_solver_config["JobInObjectiveModelling"] == 4:  # Step function :
                obj += cpmod.start_eval(var, step_fns[job.processing_time])
            else:
                raise Exception("Given JobInObjectiveModelling method {0} is not supported.".format(self.specialized_solver_config["JobInObjectiveModelling"]))

        # add variables for gaps ---------------------------------------------------------------------------------------
        if self.specialized_solver_config["GapsInObjectiveModelling"] == 0:  # Fixed :
            for t_s in range(1, T):
                for t_e in range(t_s+1, T):
                    if instance.has_switching_cost(t_s, t_e):
                        if instance.get_gap_lower_bound(t_s, t_e) > ub:  # skip gaps that are too costly
                            continue

                        sw_cost = instance.optimal_switching_costs[t_s][t_e]

                        var = model.interval_var(start=t_s,
                                                 end=t_e,
                                                 optional=True,
                                                 name="gap[{:d},{:d}]".format(t_s, t_e))
                        gap_vars[t_s, t_e] = var

                        seq_vars.append(var)
                        obj += cpmod.presence_of(var) * sw_cost  # if the gap is present, add cost to objective
        elif self.specialized_solver_config["GapsInObjectiveModelling"] == 1:  # Free :
            gaps_by_lengths = {i: [] for i in range(1, T - 1)}
            for gap_len in range(1, T - total_proc - 1):
                costs = [instance.optimal_switching_costs[t][t + gap_len]
                         if instance.optimal_switching_costs[t][t + gap_len] is not None
                         else len(instance.intervals) * instance.on_power_consumption + 1  # TODO : max value
                         for t in range(T) if t + gap_len < T]
                costs[0] = 0  # TODO: for absent value

                for i in range(int(T / gap_len)):
                    var = model.interval_var(optional=True, length=gap_len, name="gap[{:d},{:d}]".format(gap_len, i))
                    model.add(cpmod.start_of(var, absentValue=1) >= 1)
                    model.add(cpmod.end_of(var) <= T - 1)
                    obj += cpmod.element(costs, cpmod.start_of(var))
                    gaps_by_lengths[gap_len].append(var)

                    seq_vars.append(var)

                # force order on the gaps
                for cur, nxt in zip(gaps_by_lengths[gap_len], gaps_by_lengths[gap_len][1:]):
                    model.add(cpmod.presence_of(cur) >= cpmod.presence_of(nxt))
                    model.add(cpmod.end_before_start(cur, nxt))
        elif self.specialized_solver_config["GapsInObjectiveModelling"] == 2:  # No :
            # Gaps will be added to the objective after introducing the sequence variable
            pass
        else:
            raise Exception("Given GapsInObjectiveModelling method {0} is not supported.".format(
                self.specialized_solver_config["GapsInObjectiveModelling"]))

        # add no overlap constraint ------------------------------------------------------------------------------------
        seq = model.sequence_var(seq_vars, name="seq")
        model.add(cpmod.no_overlap(seq))

        model.add(cpmod.first(seq, first_job_var))
        model.add(cpmod.last(seq, last_job_var))

        if self.specialized_solver_config["GapsInObjectiveModelling"] == 2:  # No :
            gap_costs = ([0 for _ in range(T)] +  # gap_len == 0
                         [instance.optimal_switching_costs[gap_s][gap_s + gap_len]
                          if gap_s + gap_len <= T - 1 and instance.optimal_switching_costs[gap_s][gap_s + gap_len] is not None
                          else INT_MAX
                          for gap_len in range(1, T) for gap_s in range(T)])

            for job in instance.jobs:
                job_var = self.job_vars[job]
                obj += cpmod.element(gap_costs, (cpmod.start_of_next(seq, job_var, T - 1) - cpmod.end_of(job_var)) * T + cpmod.end_of(job_var))
            obj += cpmod.element(gap_costs, cpmod.start_of_next(seq, first_job_var) * T)

        # constrain lengths to fill the whole horizon ------------------------------------------------------------------
        if self.specialized_solver_config["GapsInObjectiveModelling"] != 2:  # gaps are modelled
            if self.specialized_solver_config["FillAllModelling"] == 0:  # SumLengths :
                model.add(sum([cpmod.length_of(var) for var in seq_vars]) == T - 2)
            elif self.specialized_solver_config["FillAllModelling"] == 1:  # Pulse :
                cumul_func = 0
                for var in seq_vars:
                        if var is not first_job_var and var is not last_job_var:
                            cumul_func += cpmod.pulse(var, 1)
                model.add(cpmod.always_in(cumul_func,(1,T-1), 1, 1))
            elif self.specialized_solver_config["FillAllModelling"] == 2:  # StartOfNext :
                for var in seq_vars:
                    if var is not last_job_var:
                        model.add(cpmod.start_of_next(seq, var, lastValue=T, absentValue=0) == cpmod.end_of(var, absentValue=0))
            else:
                raise Exception("Given FillAllModelling method {0} is not supported.".format(
                    self.specialized_solver_config["FillAllModelling"]))

        # set objective
        model.minimize(obj)

        # - init start times.
        # if init_start_times is not None:
        #     for job in instance.jobs:
        #         stp.add_interval_var_solution(job_vars[job], presence=True, start=init_start_times[job])
        #
        #     # gaps in the schedule
        #     gaps = self.get_gaps_in_schedule(init_start_times)
        #
        #     for g_s, g_e in gaps:
        #         stp.add_interval_var_solution(gap_vars[g_s, g_e], presence=True, start=g_s)
        #
        #     # set starting point
        #     model.set_starting_point(stp)

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
        return start_times

    @staticmethod
    def get_step_functions(instance, first_on, last_on):
        step_fns = dict()
        for job in instance.jobs:
            if job.processing_time in step_fns:
                continue
            steps = [(t, instance.cumulative_energy_cost[t][t + job.processing_time - 1] * instance.on_power_consumption, 0)
                     for t in range(first_on, last_on - job.processing_time + 2)]
            step_fns[job.processing_time] = CpoSegmentedFunction(segments=steps)
        return step_fns
# -----------------------------------------------------------------------

model_class = CpGeneral(Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve(), Path(sys.argv[3]).resolve(), Path(sys.argv[4]).resolve())
model_class.initialize()
model_class.solve()
model_class.save_result()

