#!/usr/bin/env python3

# This file is released under MIT license.
# See file LICENSE.txt for more information.

# Class wrapping model

import json
import time
import re
from datetime import timedelta
from pathlib import Path

from docplex.cp.parameters import VALUE_OFF, VALUE_EXTENDED
from docplex.cp.model import CpoParameters, CpoSolveResult
from pysat.examples.rc2 import RC2

import cp_utils
import ilp_utils
import utils
from datastructs.instance import Instance, Job
from datastructs.result import Result, Status
from datastructs.config import PresolveLevel
from typing import Dict, List, Tuple, Union


class Model:
    def __init__(self, config_path: Path, specialized_solver_config_path: Path, instance_path: Path, result_path: Path):
        self.start_time = time.time()

        self.config_path = config_path
        self.specialized_solver_config_path = specialized_solver_config_path
        self.instance_path = instance_path
        self.result_path = result_path

        self.config = json.loads(config_path.read_text())
        self.instance = Instance.from_json(instance_path.read_text())

        self.config['TimeLimit'] = utils.parse_timedelta(self.config['TimeLimit'])
        self.specialized_solver_config = json.loads(specialized_solver_config_path.read_text())

        self.model = None

    def initialize(self):
        raise NotImplementedError("Method initialize is model-specific and should be implemented.")

    def solve(self):
        raise NotImplementedError("Method solve is model-specific and should be implemented.")

    def get_starts(self):
        raise NotImplementedError("Method get_starts is model-specific and should be implemented.")

    def save_result(self):
        raise NotImplementedError("Method save_result is model-specific and should be implemented.")

    def get_init_start_times(self, sort_starts=False) -> Dict[Job, int]:
        if self.config['InitStartTimes'] is None or not self.config['InitStartTimes']:
            return None

        start_times = {}
        for d in self.config['InitStartTimes']:
            job = self.instance.jobs[d['JobIndex']]
            start_time = d['StartTime']
            start_times[job] = start_time

        if sort_starts:  # swap the start times such that the jobs are ordered according to their position in inst.jobs
            jobs_by_len = self.get_job_by_lengths()
            for lst in jobs_by_len.values():
                starts_of_cur_len = [start_times[j] for j in lst]
                starts_of_cur_len.sort()

                for j, s in zip(lst, starts_of_cur_len):
                    start_times[j] = s

        return start_times

    def get_valid_start_time_ranges(self) -> Dict[Job, Tuple[int, int]]:
        # Begin: inclusive, end: exclusive
        ranges = dict()

        if self.specialized_solver_config['RelaxedJobsOrdering']:
            jobs_by_len = self.get_job_by_lengths()
            for processing_time, same_jobs in jobs_by_len.items():
                for position, job in enumerate(same_jobs):
                    job_range = (
                        self.instance.earliest_on_interval_idx + position * processing_time,
                        self.instance.latest_on_interval_idx - (len(same_jobs) - position) * processing_time + 1 + 1
                    )
                    ranges[job] = job_range
        else:
            for job in self.instance.jobs:
                job_range = (
                    self.instance.earliest_on_interval_idx,
                    self.instance.latest_on_interval_idx - job.processing_time + 1 + 1
                )
                ranges[job] = job_range

        return ranges

    def get_gaps_in_schedule(self, init_start_times: Dict[Job, int]) -> List[Tuple[int,int]]:
        s_e = [(init_start_times[job], init_start_times[job] + job.processing_time) for job in self.instance.jobs]
        if len(s_e) < 1:  # No job in the schedule
            return [(1, len(self.instance.intervals) - 1)]
        s_e.sort(key=lambda x: x[0])
        if s_e[0][0] > 1:
            gaps = [(1, s_e[0][0])]
        for cur, nxt in zip(s_e, s_e[1:]):
            e_cur = cur[1]
            s_nxt = nxt[0]
            if s_nxt > e_cur:
                gaps.append((e_cur, s_nxt))
        if s_e[-1][1] < len(self.instance.intervals) - 1:
            gaps.append((s_e[-1][1], len(self.instance.intervals) - 1))

        return gaps

    def get_gaps_starts_by_length(self, init_start_times: Dict[Job, int], sort_starts=False) -> Dict[int, List[int]]:
        all_gaps = self.get_gaps_in_schedule(init_start_times)
        gaps_starts_by_len = dict()
        # add gaps to the corresponding list
        for g_s, g_e in all_gaps:
            g_len = g_e - g_s
            if g_len not in gaps_starts_by_len:
                gaps_starts_by_len[g_len] = [g_s]
            else:
                gaps_starts_by_len[g_len].append(g_s)

        # sort the lists (forcing the ordering)
        if sort_starts:
            for i in gaps_starts_by_len.values():
                i.sort()

        return gaps_starts_by_len

    # TODO: do not use float("inf") as an upper bound, it pruned even infeasible transitions detected in lb array
    def get_upper_bound(self, init_start_times: Dict[Job, int]):
        return self.instance.total_energy_cost(init_start_times) if init_start_times is not None else float("inf")

    def get_job_by_lengths(self) -> Dict[int, List[Job]]:
        dct = dict()
        for job in self.instance.jobs:
            if job.processing_time not in dct:
                dct[job.processing_time] = [job]
            else:
                dct[job.processing_time].append(job)
        return dct

class CpModel(Model):
    def __init__(self, config_path: Path, specialized_solver_config_path: Path, instance_path: Path, result_path: Path):
        super().__init__(config_path, specialized_solver_config_path, instance_path, result_path)
        self.solution = None

    def solve(self):
        params = {}
        if self.config['StopOnFeasibleSolution']:
            params["SolutionLimit"] = 1
        if self.config['NumWorkers'] > 0:
            params["Workers"] = self.config['NumWorkers']
        if self.config["PresolveLevel"] == PresolveLevel.Off:
            self.set_parameter("Presolve", VALUE_OFF)
        params["TimeLimit"] = max(1, self.config['TimeLimit'].total_seconds() - (time.time() - self.start_time))
        params["SearchConfiguration"] = 262144  # FIX: suggested by IBM                
        if self.specialized_solver_config["FailureDirectedSearchEmphasis"]:
            params["FailureDirectedSearchEmphasis"] = float(self.specialized_solver_config["FailureDirectedSearchEmphasis"])
        self.solution = self.model.solve(params=CpoParameters(**params))        

    def set_parameter(self, param: str, value: Union[str, int, float]):
        cur_params = self.model.get_parameters()
        if cur_params is None:
            cur_params = CpoParameters()
        cur_params[param] = value
        self.model.set_parameters(cur_params)
        
    def get_result_additional_info(self, solution: CpoSolveResult):
        # Assumption: the additional info is at the end of the log, no need to search it whole.
        log = solution.get_solver_log()[-2000:]
        
        additional_info = dict()

        num_branches_searchresult = re.search(r'!\s*Number of branches\s*:\s*(\d+)\s*', log)
        if num_branches_searchresult is not None:
            additional_info['NumNodes'] = int(num_branches_searchresult.groups()[0])

        num_fails_searchresult = re.search(r'!\s*Number of fails\s*:\s*(\d+)\s*', log)
        if num_fails_searchresult is not None:
            additional_info['NumFails'] = int(num_fails_searchresult.groups()[0])

        search_speed_searchresult = re.search(r'!\s*Search speed \(br\. / s\)\s*:\s*(\d*\.\d+|\d+)\s*', log)
        if search_speed_searchresult is not None:
            additional_info['SearchSpeed'] = float(search_speed_searchresult.groups()[0])
            
        return additional_info
            

    def save_result(self):
        solution = self.solution
        objective = None
        if cp_utils.get_result_status(solution) in {Status.Heuristic, Status.Optimal}:
            objective = solution.get_objective_values()[0]
            
        additional_info = self.get_result_additional_info(solution)

        solver_result = Result(
            cp_utils.get_result_status(solution),
            cp_utils.time_limit_reached(solution),
            timedelta(seconds=time.time() - self.start_time),
            self.get_starts(),
            None if solution is None or solution.get_objective_bounds() is None else solution.get_objective_bounds()[0],
            objective,
            None,
            additional_info
        )

        self.result_path.write_text(solver_result.to_json())


class IlpModel(Model):
    def set_parameters(self):
        params = {}
        if self.config['StopOnFeasibleSolution']:
            params["SolutionLimit"] = 1
        params["Threads"] = self.config['NumWorkers']
        params["TimeLimit"] = max(1, self.config['TimeLimit'].total_seconds() - (time.time() - self.start_time))
        params["Presolve"] = self.config["PresolveLevel"]
        params["MIPGap"] = 0
        ilp_utils.set_params(self.model, params)

    def solve(self):
        self.set_parameters()
        self.model.optimize()

    def save_result(self):
        objective = None
        if ilp_utils.get_result_status(self.model) in {Status.Heuristic, Status.Optimal}:
            objective = int(self.model.ObjVal + 0.5)

        solver_result = Result(
            ilp_utils.get_result_status(self.model),
            ilp_utils.time_limit_reached(self.model),
            timedelta(seconds=time.time() - self.start_time),
            self.get_starts(),
            self.model.ObjBound,
            objective,
            None
        )

        self.result_path.write_text(solver_result.to_json())

