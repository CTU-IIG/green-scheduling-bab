# This file is released under MIT license.
# See file LICENSE.txt for more information.

from enum import IntEnum
from typing import List, Dict, Optional
import json

__all__ = [
    'Job',
    'Instance',
    'StateKind'
]


class StateKind(IntEnum):
    Off = 0,
    On = 1,
    Idle = 2


class Job:

    __slots__ = [
        'id',
        'index',
        'machine_idx',
        'processing_time'
    ]

    def __init__(self, job_id: int, index: int, machine_idx: int, processing_time: int):
        self.id = job_id
        self.index = index
        self.machine_idx = machine_idx
        self.processing_time = processing_time

    def __hash__(self):
        return hash(self.id)

    def __eq__(self, other):
        return self.id == other.id


class Interval:

    __slots__ = [
        'index',
        'start',
        'end',
        'energy_cost'
    ]

    def __init__(self, index: int, start: int, end: int, energy_cost: int):
        self.index = index
        self.start = start
        self.end = end
        self.energy_cost = energy_cost

    def __hash__(self):
        return hash(self.id)

    def __eq__(self, other):
        return self.id == other.id


class Instance:

    __slots__ = [
        'machines_count',
        'jobs',
        'intervals',
        'length_interval',
        'off_on_time',
        'on_off_time',
        'off_on_power_consumption',
        'on_off_power_consumption',
        'off_idle_time',
        'idle_off_time',
        'off_idle_power_consumption',
        'idle_off_power_consumption',
        'on_power_consumption',
        'idle_power_consumption',
        'off_power_consumption',
        'optimal_switching_costs',
        'gaps_lower_bounds',
        'idle_state_idx',
        'on_state_idx',
        'base_off_state_idx',
        'off_state_inds',
        'states',
        'state_diagram_power_consumption',
        'state_diagram_time',
        'state_power_consumption',
        'state_inds',
        'earliest_on_interval_idx',
        'latest_on_interval_idx',
        'cumulative_energy_cost',
        'metadata',
        'instance_filename',
        'machine_jobs'
    ]

    def __init__(
            self,
            machines_count: int,
            jobs: List[Job],
            intervals: List[Interval],
            length_interval: int,
            off_on_time: List[int],
            on_off_time: List[int],
            off_on_power_consumption: List[int],
            on_off_power_consumption: List[int],
            off_idle_time: List[Optional[int]],
            idle_off_time: List[Optional[int]],
            off_idle_power_consumption: List[Optional[int]],
            idle_off_power_consumption: List[Optional[int]],
            on_power_consumption: int,
            idle_power_consumption: int,
            off_power_consumption: List[int],
            optimal_switching_costs: List[List[Optional[int]]],
            gaps_lower_bounds: List[List[Optional[int]]],
            idle_state_idx: int,
            on_state_idx: int,
            base_off_state_idx: int,
            off_state_inds: List[int],
            states: List[StateKind],
            state_diagram_power_consumption: List[List[Optional[int]]],
            state_diagram_time: List[List[Optional[int]]],
            state_power_consumption: List[int],
            state_inds: List[int],
            earliest_on_interval_idx: int,
            latest_on_interval_idx: int,
            cumulative_energy_cost: List[List[int]],
            metadata: Optional[Dict[str, object]] = None,
            instance_filename: str = None):
        self.machines_count = machines_count
        self.jobs = jobs
        self.intervals = intervals
        self.length_interval = length_interval
        self.off_on_time = off_on_time
        self.on_off_time = on_off_time
        self.off_on_power_consumption = off_on_power_consumption
        self.on_off_power_consumption = on_off_power_consumption
        self.off_idle_time = off_idle_time
        self.idle_off_time = idle_off_time
        self.off_idle_power_consumption = off_idle_power_consumption
        self.idle_off_power_consumption = idle_off_power_consumption
        self.on_power_consumption = on_power_consumption
        self.idle_power_consumption = idle_power_consumption
        self.off_power_consumption = off_power_consumption
        self.optimal_switching_costs = optimal_switching_costs
        self.gaps_lower_bounds = gaps_lower_bounds
        self.idle_state_idx = idle_state_idx
        self.on_state_idx = on_state_idx
        self.base_off_state_idx = base_off_state_idx
        self.off_state_inds = off_state_inds
        self.states = states
        self.state_diagram_power_consumption = state_diagram_power_consumption
        self.state_diagram_time = state_diagram_time
        self.state_power_consumption = state_power_consumption
        self.state_inds = state_inds
        self.earliest_on_interval_idx = earliest_on_interval_idx
        self.latest_on_interval_idx = latest_on_interval_idx
        self.cumulative_energy_cost = cumulative_energy_cost
        self.metadata = metadata if metadata is not None else dict()
        self.instance_filename = instance_filename
        
        self.machine_jobs = [[] for _ in range(self.machines_count)]
        for job in self.jobs:
            self.machine_jobs[job.machine_idx].append(job)

    def __eq__(self, another):
        return hasattr(another, 'instance_filename') and self.instance_filename == another.instance_filename

    def __hash__(self):
        return hash(self.instance_filename)
    
    def has_switching_cost(self, beginIntervalIdx: int, endIntervalIdx: int) -> bool:
        return self.optimal_switching_costs[beginIntervalIdx][endIntervalIdx] is not None

    def total_energy_cost_on_interval(self, from_interval_index: int, to_interval_index: int, power_consumption: int) -> int:
        if to_interval_index < from_interval_index:
            return 0

        energy_consumption_per_interval = self.length_interval * power_consumption

        return sum(self.intervals[interval_index].energy_cost * energy_consumption_per_interval
                   for interval_index in range(from_interval_index, to_interval_index+1))

    def get_ordered_jobs_on_machines(self, start_times: Dict[Job, int]) -> List[List[Job]]:
        return [sorted([job for job in self.jobs if job.machine_idx==machine_index], key=lambda j: start_times[j])
                for machine_index in range(self.machines_count)]

    def total_energy_cost(self, start_times: Dict[Job, int]):
        tec = 0
        ordered_jobs_on_machines = self.get_ordered_jobs_on_machines(start_times)

        # TODO: assumes that machines are indexed 0, 1, ..., machines_count-1
        for machine_index in range(self.machines_count):
            ordered_jobs = ordered_jobs_on_machines[machine_index]

            if len(ordered_jobs) < 1:  # no job on machine, base-off -> base-off
                tec += self.optimal_switching_costs[0][-1]
                continue

            # switch to the first job
            start_interval = start_times[ordered_jobs[0]]
            tec += self.optimal_switching_costs[0][start_interval]

            # switching between jobs
            for job, next_job in zip(ordered_jobs, ordered_jobs[1:]):
                completion_interval = start_times[job] + job.processing_time - 1
                start_interval = start_times[next_job]
                tec += self.optimal_switching_costs[completion_interval+1][start_interval]

            # switching after the last job
            completion_interval = start_times[ordered_jobs[-1]] + ordered_jobs[-1].processing_time - 1
            tec += self.optimal_switching_costs[completion_interval+1][-1]

            # cost for processing jobs
            for job in self.jobs:
                start_interval = start_times[job]
                completion_interval = start_times[job] + job.processing_time - 1
                tec += self.total_energy_cost_on_interval(start_interval, completion_interval, self.on_power_consumption)

        return tec

    def get_gap_lower_bound(self, start_time: int, end_time: int) -> int:
        return self.gaps_lower_bounds[start_time][end_time]

    # TODO : do not use float("inf")
    def get_ub(self):
        """Get upper bound on the solution objective for the given instance"""
        p_total = sum(job.processing_time for job in self.jobs)
        if (self.latest_on_interval_idx - self.earliest_on_interval_idx) + 1 < p_total:
            return float("inf")
        if self.optimal_switching_costs[1][self.earliest_on_interval_idx] is None:
            return float("inf")
        if self.optimal_switching_costs[self.earliest_on_interval_idx + p_total][len(self.intervals) - 1] is None:
            return float("inf")
        ub = self.optimal_switching_costs[1][self.earliest_on_interval_idx]
        ub += self.cumulative_energy_cost[self.earliest_on_interval_idx][self.earliest_on_interval_idx + p_total - 1] * self.on_power_consumption
        ub += self.optimal_switching_costs[self.earliest_on_interval_idx + p_total][len(self.intervals) - 1]
        return ub

    @staticmethod
    def from_json(s: str, instance_filename: str = None):
        ins_raw = json.loads(s)

        jobs = []
        for job_index, job_raw in enumerate(ins_raw['Jobs']):
            jobs.append(Job(
                job_raw['Id'],
                job_raw['Index'],
                job_raw['MachineIdx'],
                job_raw['ProcessingTime']
            ))
            
        intervals = []
        for interval_index, interval_raw in enumerate(ins_raw['Intervals']):
            intervals.append(Interval(
                interval_raw['Index'],
                interval_raw['Start'],
                interval_raw['End'],
                interval_raw['EnergyCost']
            ))
            
        return Instance(
            ins_raw['MachinesCount'],
            jobs,
            intervals,
            ins_raw['LengthInterval'],
            ins_raw['OffOnTime'],
            ins_raw['OnOffTime'],
            ins_raw['OffOnPowerConsumption'],
            ins_raw['OnOffPowerConsumption'],
            ins_raw['OffIdleTime'],
            ins_raw['IdleOffTime'],
            ins_raw['OffIdlePowerConsumption'],
            ins_raw['IdleOffPowerConsumption'],
            ins_raw['OnPowerConsumption'],
            ins_raw['IdlePowerConsumption'],
            ins_raw['OffPowerConsumption'],
            ins_raw['OptimalSwitchingCosts'],
            ins_raw['GapsLowerBounds'],
            ins_raw['IdleStateIdx'],
            ins_raw['OnStateIdx'],
            ins_raw['BaseOffStateIdx'],
            ins_raw['OffStateInds'],
            ins_raw['States'],
            ins_raw['StateDiagramPowerConsumption'],
            ins_raw['StateDiagramTime'],
            ins_raw['StatePowerConsumption'],
            ins_raw['StateInds'],
            ins_raw['EarliestOnIntervalIdx'],
            ins_raw['LatestOnIntervalIdx'],
            ins_raw['CumulativeEnergyCost'],
            ins_raw['Metadata'],
            instance_filename
        )

