# This file is released under MIT license.
# See file LICENSE.txt for more information.

import argparse
import pandas as pd
import webbrowser
import json
import utils
from pathlib import Path

from datastructs.result import Status
from analysis.analysis_utils import DataPaths, GroupedInstances


parser = argparse.ArgumentParser()
parser.add_argument(
    'data_path',
    metavar='DATA_PATH',
    type=str,
    help='Path to the root data directory.')
parser.add_argument(
    'prescription_filename',
    metavar='PRESCRIPTION_FILENAME',
    type=str,
    help='File name of the prescription file.')
parser.add_argument(
    'dataset_name',
    metavar='DATASET_NAME',
    type=str,
    help='The name of the dataset for which to perform analysis.')

args = parser.parse_args()
paths = DataPaths(args.data_path)

instances = paths.load_instances(args.dataset_name)
solver_ids = paths.get_solver_ids(args.prescription_filename, args.dataset_name)

df = pd.DataFrame()

instance_to_optobj = dict()
for instance in instances:
    for solver_id in solver_ids:
        instance_id = instance['InstanceFilename']
        result_path = paths.result(args.prescription_filename, args.dataset_name, solver_id, instance_id)
        if result_path.exists():
            result = json.loads(result_path.read_text())
            if Status(result['Status']) == Status.Optimal and result['Objective'] is not None:
                obj = int(result['Objective'])
                if instance_id in instance_to_optobj and instance_to_optobj[instance_id][1] != obj:
                    print('Instance {0} has two different optimal objectives!'.format(instance_id))
                    print('Solver: {0}, obj: {1}'.format(solver_id, obj))
                    print('Solver: {0}, obj: {1}'.format(*instance_to_optobj[instance_id]))
                    exit(1)
                instance_to_optobj[instance_id] = [solver_id, obj]

for instance in instances:
    for solver_id in solver_ids:
        instance_id = instance['InstanceFilename']
        result_path = paths.result(args.prescription_filename, args.dataset_name, solver_id, instance_id)
        if result_path.exists():
            result = json.loads(result_path.read_text())
            if Status(result['Status']) == Status.Heuristic and result['Objective'] is not None:
                obj = int(result['Objective'])
                if instance_id in instance_to_optobj and instance_to_optobj[instance_id][1] > obj:
                    print('Instance {0} has heuristic value smaller than optimal one!'.format(instance_id))
                    print('Solver: {0}, obj: {1}'.format(solver_id, obj))
                    print('Solver: {0}, obj: {1}'.format(*instance_to_optobj[instance_id]))
                    exit(1)


for instance in instances:
    row = {'Instance': instance['InstanceFilename']}
    for solver_id in solver_ids:
        result_path = paths.result(args.prescription_filename, args.dataset_name, solver_id, instance['InstanceFilename'])
        if result_path.exists():
            result = json.loads(result_path.read_text())
            # row['Status/' + solver_id] = Status(result['Status']).name
            # row['Obj/' + solver_id] = int(result['Objective']) if result['Objective'] is not None else None
            row['Time/' + solver_id] = utils.parse_timedelta(result['RunningTime']).total_seconds()
        else:
            # row['Status/' + solver_id] = None
            # row['Obj/' + solver_id] = None
            row['Time/' + solver_id] = None
    df = df.append(row, ignore_index=True)
    
html_df = Path('index.html').resolve()
html_df.write_text(df.to_html())
webbrowser.open(str(html_df), new=2)
