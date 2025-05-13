# Energy States and Costs Scheduling

This repository contains the source code for the scheduling problem with energy states and costs studied in [\[Benedikt2025\]].
## Dependencies

You need the following

- .NET Core (>= 6.0)
- Python (>= 3.7)
- IBM CP Optimizer (>= 12.9)
- Gurobi (= 10.0)
- docplex (>= 2.10)
- g++ (>= 8.3)
- CMake (>= 3.14)

The code is known to work on Fedora 29 and Debian Stretch operating systems.
For using Gurobi and IBM CP Optimizer, environment variables `GUROBI_HOME` and `CPLEX_INSTALL_DIR` must be set and point to the installation directories of the solvers.
For example, on GNU/Linux

```bash
>> echo $GUROBI_HOME
/home/username/opt/gurobi100/linux64
>> ls $GUROBI_HOME
bin  docs  EULA.pdf  examples  gurobi.log  include  lib  matlab  R  ReleaseNotes.html  setup.py  src
>> echo $CPLEX_INSTALL_DIR
/home/username/opt/ibm/ILOG
>> ls $CPLEX_INSTALL_DIR
CPLEX_Studio129
```

## Projects

The repository contains one C# solution with the following projects

- `Shared` - library with common code shared among the rest of the projects, e.g., it contains the source codes of the solvers.
- `DatasetGenerators` - generators of instance dataset from the given prescription.
- `Experiments` - runs the specified solvers on a dataset.
- `SolverCli` - command line interface for solving one instance with a specified solver.
- `Tests` - project containing tests of the code.

The following sections explains the solutions projects in detail.

The repository provides a few run configurations for [Jetbrains Rider IDE](https://www.jetbrains.com/rider/).
To use them with the data, either modify the paths in run configurations or create a symlink to the data repository in the root directory of the C# solution

```bash
ln -s DATA_REPOSITORY_PATH data
```

### Shared

This project contains the code of the solvers which is shared among all the remaining projects.
The solvers are written in different programming languages (C#, Python and C++).
For solvers written in C# and Python, nothing needs to be done as they are compiled when the projects are built.
However, the solvers written in C++ must be compiled beforehand

```bash
>> cd Iirc.EnergyStatesAndCostsScheduling.Shared/cpp
>> cmake -DCMAKE_BUILD_TYPE=Release .
>> make
```

Note that whenever Python or C++ solver is modified, the C# projects must be rebuilded so that the new solvers can be used (the building procedure copies the new version of the solvers to the output directory).

### DatasetGenerators

To compile and run the project from the command line (passing no arguments will print the help message)

```bash
>> dotnet run -c Release -p Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators -- DATA_ROOT_PATH PRESCRIPTION_FILE
```

where

- `DATA_ROOT_PATH` is the path to root directory with data (see the data repository for description of the filesystem structure).
- `PRESCRIPTION_FILE` is the filename (including suffix) of the prescription file that describes how the instances are to be generated.
  The prescription files are in JSON format, see class `Prescription` in `Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators/Prescription.cs` for the description of the files content (`Prescription` class is used for deserializing the JSON files).

The command will create a new dataset directory with a name of the prescription file (without the suffix) in `{DATA_ROOT_PATH}/datasets` and fills it with the generated instances.

The generated instance files are in JSON format, see class `JsonInstance` in `Iirc.EnergyStatesAndCostsScheduling.Shared/Input/Readers/InputReader.cs` for the description of the files content (`JsonInstance` class is used for deserializing the JSON files).

### Experiments

To compile and run the project from the command line (passing no arguments will print the help message)

```bash
>> dotnet run -c Release -p Iirc.EnergyStatesAndCostsScheduling.Experiments -- DATA_ROOT_PATH PRESCRIPTION_FILE
```

where

- `DATA_ROOT_PATH` is the path to root directory with data (see the data repository for description of the filesystem structure).
- `PRESCRIPTION_FILE` is the filename of the prescription file that describes the experimental setup, e.g., the solvers to test.
  The prescription files are in JSON format, see class `Prescription` in `Iirc.EnergyStatesAndCostsScheduling.Experiments/Prescription.cs` for the description of the files content (`Prescription` class is used for deserializing the JSON files).

The command will create a new result directory with a name of the prescription file (without the suffix) and fills it with the results.
The location of each result has the following format

```bash
{DATA_ROOT_PATH}/results/{prescriptionFilename}/{datasetName}/{solverId}/{instanceFilename}.json
```

where

- `{prescriptionFilename}` is the file name of the experimental setup prescription.
- `{datasetName}` is the dataset name as specified in the prescription.
- `{solverId}` is the solver id as specified in the prescription.
- `{instanceFilename}` is the file name of the instance for which a result is generated.

The generated result files are in JSON format, see class `Result` in `Iirc.EnergyStatesAndCostsScheduling.Shared/Output/Result.cs` for the description of the files content (`Result` class is used for deserializing the JSON files).

The experiment is generating the results on the fly.
By default, if the experiment is interrupted before it is completed, restarting the experiment will resume from the previous point, i.e., it will keep the previously generated results.
If flag `--from-scratch` is passed, the previously generated results are deleted and the experiment starts from scratch.

By default, only one instance is being solved at a time.
The number of instances to solve in parallel can be specified using option `--num-threads`.

### SolverCli

To compile and run the project from the command line (passing no arguments will print the help message)

```bash
>> dotnet run -c Release -p Iirc.EnergyStatesAndCostsScheduling.SolverCli -- CONFIG_PATH INSTANCE_PATH
```

where

- `CONFIG_PATH` is the path to configuration file.
  The configuration files are in JSON format, see class `Config` in `Iirc.EnergyStatesAndCostsScheduling.SolverCli/Config.cs` for the description of the files content (`Config` class is used for deserializing the JSON files).
- `INSTANCE_PATH` is the path to the instance file.

The command will run the given instance on the specified solver within the configuration and prints the solving information to the standard output.

### Tests

The tests can be run by
```bash
>> dotnet test -c Debug
```

Debug configuration is recommended for enabling asserts and code checks.

## License

[MIT license](LICENSE.txt)

## Authors

Please see file [AUTHORS.txt](AUTHORS.txt) for the list of authors.

## References

[\[Aghelinejad2017a\]](https://doi.org/10.1080/00207543.2017.1414969) MohammadMohsen Aghelinejad, Yassine Ouazene & Alice Yalaoui (2018) Production scheduling optimisation with machine state and time-dependent energy costs, International Journal of Production Research, 56:16, 5558-5575, DOI: 10.1080/00207543.2017.1414969

<a name="Benedikt2020a">[\[Benedikt2020a\]](https://www.researchgate.net/publication/337781297_Power_of_Pre-Processing_Production_Scheduling_with_Variable_Energy_Pricing_and_Power-Saving_States)</a> Ondřej Benedikt, István Módos & Zdeněk Hanzálek (2020) Power of Pre-Processing: Production Scheduling with Variable Energy Pricing and Power-Saving States, CPAIOR2020

[\[Benedikt2025\]](TODO) Ondřej Benedikt, István Módos, Antonin Novak & Zdeněk Hanzálek (under revision)

## <a name="citing"></a>Citing

TODO