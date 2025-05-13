# This file is released under MIT license.
# See file LICENSE.txt for more information.

from typing import Dict, Optional
from enum import IntEnum
from datetime import timedelta
import json
import utils

from datastructs.instance import Instance, Job


class Status(IntEnum):
    NoSolution = 0,
    Optimal = 1,
    Infeasible = 2,
    Heuristic = 3


class Result:

    __slots__ = [
        'status',
        'time_limit_reached',
        'running_time',
        'start_times',
        'lower_bound',
        'objective',
        'time_to_best',
        'additional_info'
    ]

    def __init__(
            self,
            status: Status,
            time_limit_reached: bool,
            running_time: timedelta,
            start_times: Optional[Dict[Job, int]],
            lower_bound: Optional[float],
            objective: Optional[int],
            time_to_best: Optional[timedelta],
            additional_info: object = None):
        self.status = status
        self.time_limit_reached = time_limit_reached
        self.running_time = running_time
        self.start_times = start_times
        self.lower_bound = lower_bound
        self.objective = objective
        self.time_to_best = time_to_best
        self.additional_info = additional_info

    def to_json(self) -> str:
        d = dict()
        d['Status'] = self.status
        d['TimeLimitReached'] = self.time_limit_reached
        d['RunningTime'] = utils.timedelta_to_str(self.running_time)
        d['LowerBound'] = self.lower_bound
        d['Objective'] = self.objective
        d['AdditionalInfo'] = self.additional_info
        
        if self.time_to_best is None:
            d['TimeToBest'] = None
        else:
            d['TimeToBest'] = utils.timedelta_to_str(self.time_to_best)

        if self.start_times is None:
            d['StartTimes'] = None
        else:
            d['StartTimes'] = [
                {'JobIndex': job.index, 'StartTime': start_time }
                for job, start_time in self.start_times.items()
            ]

        return json.dumps(d)

    @staticmethod
    def from_json(s: str, ins: Instance):
        result_raw = json.loads(s)

        start_times = None
        if result_raw['StartTimes'] is not None:
            start_times = {
                ins.jobs[d['JobIndex']]: d['StartTime']
                for d in result_raw['StartTimes']
            }

        return Result(
            Status(result_raw['Status']),
            result_raw['TimeLimitReached'],
            utils.parse_timedelta(result_raw['RunningTime']),
            start_times,
            result_raw['LowerBound'],
            result_raw['Objective'],
            None if 'TimeToBest' not in result_raw or result_raw['TimeToBest'] is None else utils.parse_timedelta(result_raw['TimeToBest']),
            None if 'AdditionalInfo' not in result_raw else result_raw['AdditionalInfo']
        )
