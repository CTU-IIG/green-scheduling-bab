# This file is released under MIT license.
# See file LICENSE.txt for more information.

from typing import Dict, List
from pathlib import Path
import json

import utils
from datastructs.instance import Instance


class DataPaths:

    __slots__ = [
        'data_path'
    ]

    def __init__(self, data_path: str):
        self.data_path = Path(data_path).resolve()

    def datasets(self) -> Path:
        return self.data_path / "datasets"

    def dataset(self, dataset_name: str) -> Path:
        return self.datasets() / dataset_name

    def experiments_prescriptions(self) -> Path:
        return self.data_path / "experiments-prescriptions"

    def experiment_prescription(self, prescription_filename: str) -> Path:
        return self.experiments_prescriptions() / prescription_filename

    def results(self) -> Path:
        return self.data_path / "results"

    def results_experiment(self, prescription_filename: str) -> Path:
        return self.results() / utils.filename_without_ext(prescription_filename)

    def results_experiment_dataset(self, prescription_filename: str, dataset_name: str) -> Path:
        return self.results_experiment(prescription_filename) / dataset_name

    def results_experiment_dataset_solver(self, prescription_filename: str, dataset_name: str, solver_id: str) -> Path:
        return self.results_experiment_dataset(prescription_filename, dataset_name) / solver_id

    def result(self, prescription_filename: str, dataset_name: str, solver_id: str, result_filename: str) -> Path:
        return self.results_experiment_dataset_solver(prescription_filename, dataset_name, solver_id) / result_filename

    def load_instances(self, dataset_name: str) -> List[Dict]:
        # TODO: support normal instance
        paths = [
            child_path
            for child_path in self.dataset(dataset_name).iterdir() if child_path.is_file()
        ]

        instances = [json.loads(path.read_text()) for path in paths]

        for i in range(len(instances)):
            instances[i]["InstanceFilename"] = paths[i].name

        return instances

    def get_solver_ids(self, prescription_filename: str, dataset_name: str) -> List[str]:
        return sorted([
            child_path.name
            for child_path in self.results_experiment_dataset(prescription_filename, dataset_name).iterdir()
            if child_path.is_dir()
        ])


class GroupedInstances:
    """Instances grouped by parameters."""

    __slots__ = [
        'params',
        'instances',
    ]

    def __init__(self, params: Dict[str, object], instances: List[Instance]):
        self.params = params
        self.instances = instances

    @staticmethod
    def group_instances(group_params: List[str], instances: List[Instance]) -> List['GroupedInstances']:
        d: Dict[frozenset, List[Instance]] = dict()
        for instance in instances:
            params = {param: value for param, value in instance.metadata.items() if param in group_params}
            key = frozenset(params.items())
            if key not in d:
                d[key] = []
            d[key].append(instance)

        return [GroupedInstances(dict(key), instances) for key, instances in d.items()]

