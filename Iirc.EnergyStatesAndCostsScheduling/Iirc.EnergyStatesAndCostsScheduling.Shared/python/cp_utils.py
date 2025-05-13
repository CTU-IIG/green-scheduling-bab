# This file is released under MIT license.
# See file LICENSE.txt for more information.

from docplex.cp.solution import CpoSolveResult
from docplex.cp.model import SOLVE_STATUS_FEASIBLE, SOLVE_STATUS_INFEASIBLE, SOLVE_STATUS_OPTIMAL, FAIL_STATUS_TIME_LIMIT, FAIL_STATUS_SEARCH_COMPLETED

from datastructs.result import Status


def get_result_status(solve_result: CpoSolveResult) -> Status:
    solve_status = solve_result.get_solve_status()
    if solve_status == SOLVE_STATUS_FEASIBLE:
        return Status.Heuristic
    elif solve_status == SOLVE_STATUS_OPTIMAL:
        return Status.Optimal
    elif solve_status == SOLVE_STATUS_INFEASIBLE:
        return Status.Infeasible
    else:
        return Status.NoSolution


def time_limit_reached(solve_result: CpoSolveResult) -> bool:
    # For some reason, CP returns FAIL_STATUS_SEARCH_COMPLETED when no solution is found within time limit,
    fail_status = solve_result.get_fail_status()
    result_status = get_result_status(solve_result)
    if fail_status == FAIL_STATUS_SEARCH_COMPLETED and result_status == Status.NoSolution:
        return True
    return solve_result.get_fail_status() == FAIL_STATUS_TIME_LIMIT
