# This file is released under MIT license.
# See file LICENSE.txt for more information.

import gurobipy as grb
from datastructs.result import Status


def get_result_status(model: grb.Model) -> Status:
    solve_status = model.Status

    if solve_status == grb.GRB.OPTIMAL:
        return Status.Optimal
    elif model.SolCount >= 1:
        return Status.Heuristic
    elif solve_status == grb.GRB.INFEASIBLE:
        return Status.Infeasible
    else:
        return Status.NoSolution


def time_limit_reached(model: grb.Model) -> bool:
    return model.Status == grb.GRB.TIME_LIMIT


def set_params(model: grb.Model, params: dict):
    for param, val in params.items():
        model.setParam(param, val)
