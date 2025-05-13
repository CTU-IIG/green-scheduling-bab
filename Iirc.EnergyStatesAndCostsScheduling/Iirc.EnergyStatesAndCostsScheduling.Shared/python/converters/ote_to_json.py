# This file is released under MIT license.
# See file LICENSE.txt for more information.

# Converter for OTE yearly report to json which can be used by the dataset generators.
#
# How to obtain the source xlsx file containing the OTE yearly report:
# 1. Go to https://www.ote-cr.cz/cs/statistika/rocni-zprava
# 2. Select year
# 3. Download the yearly report for electricity
# 4. Open sheet "DT ČR" representing the day-ahead market
# 5. Copy the left table to new spreadsheet and save it -> this is the source xlsx file.

import argparse
import pandas as pd
import json
import datetime
from pathlib import Path

import utils


parser = argparse.ArgumentParser()
parser.add_argument(
    'src_path',
    metavar='SRC_PATH',
    type=str,
    help='Path to the source xlsx file.')

args = parser.parse_args()

df = pd.read_excel(args.src_path, parse_dates=['Den'])

json_dict = {
    "costs": [],
    "dates": [],
}
for idx, row in df.iterrows():
    date = row["Den"].date()
    time = datetime.time(hour=row["Hodina"] - 1)
    full_date = datetime.datetime.combine(date, time)
    cost = int(row["Marginální cena ČR (Kč/MWh)"])
    json_dict["costs"].append(cost)
    json_dict["dates"].append(full_date.isoformat())
    
out_filename = utils.filename_without_ext(args.src_path) + ".json"
out_filepath = Path(args.src_path).resolve().parents[0] / out_filename

with open(out_filepath, "w+") as f:
    json.dump(json_dict, f)
