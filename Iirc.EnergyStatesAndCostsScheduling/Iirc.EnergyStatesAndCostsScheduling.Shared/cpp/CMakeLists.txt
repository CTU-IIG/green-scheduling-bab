# This file is released under MIT license.
# See file LICENSE.txt for more information.

cmake_minimum_required(VERSION 3.10)
project(EnergyStatesAndCostsScheduling)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(CPLEX_HOME "/home/novakan9/cplex")
set(CPLEX_INCLUDE_DIR ${CPLEX_HOME}/cplex/include)
set(CONCERT_INCLUDE_DIR ${CPLEX_HOME}/concert/include)
set(CP_OPTIMIZER_INCLUDE_DIR ${CPLEX_HOME}/cpoptimizer/include)

set(CPLEX_LIB_DIR ${CPLEX_HOME}/cplex/lib/x86-64_linux/static_pic)
set(CONCERT_LIB_DIR ${CPLEX_HOME}/concert/lib/x86-64_linux/static_pic)
set(CP_OPTIMIZER_LIB_DIR ${CPLEX_HOME}/cpoptimizer/lib/x86-64_linux/static_pic)

set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${CMAKE_SOURCE_DIR}/modules/")

set(GUROBI_LIBRARIES "${GUROBI_HOME}/lib/libgurobi.so" "${GUROBI_HOME}/lib/libgurobi_c++.so")
#target_link_libraries(my_target ${GUROBI_LIBRARIES})


find_package(GUROBI REQUIRED)
include_directories(SYSTEM ${GUROBI_INCLUDE_DIR})

find_package(CPLEX REQUIRED)
include_directories(SYSTEM ${CPLEX_CP_INCLUDE_DIRS})

find_package(OpenMP)
if (OPENMP_FOUND)
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${OpenMP_C_FLAGS}")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${OpenMP_CXX_FLAGS}")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} ${OpenMP_EXE_LINKER_FLAGS}")
endif()

set(CMAKE_POSITION_INDEPENDENT_CODE ON)

set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -march=native -pedantic -Wall -Wextra -Werror ${CPLEX_CONCERT_DEFINITIONS}")

IF(CMAKE_BUILD_TYPE MATCHES Release)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/bin)
ENDIF()

IF(CMAKE_BUILD_TYPE MATCHES Debug)
    add_definitions(-DDEBUG)
ENDIF()

set(LIB_SRC
        src/datastructs/FixedPermCostComputation.cpp src/datastructs/FixedPermCostComputation.h
        src/datastructs/GcdOfValues.cpp src/datastructs/GcdOfValues.h
        src/datastructs/Block.h
        src/algorithms/PackToBlocksByCp.cpp src/algorithms/PackToBlocksByCp.h
        src/algorithms/BlockFinding.cpp src/algorithms/BlockFinding.h
        src/input/Instance.cpp src/input/Instance.h
        src/input/Job.cpp src/input/Job.h
        src/input/Interval.cpp src/input/Interval.h
        src/input/readers/CppInputReader.cpp src/input/readers/CppInputReader.h
        src/output/Status.h
        src/output/Result.cpp src/output/Result.h
        src/solvers/SolverConfig.cpp src/solvers/SolverConfig.h
        src/utils/Stopwatch.cpp src/utils/Stopwatch.h
        src/openga/openGA.hpp
        )

add_executable(BranchAndBoundJob src/solvers/BranchAndBoundJob.cpp src/solvers/BranchAndBoundJob.h ${LIB_SRC})
add_executable(ConstructiveHeuristic src/solvers/ConstructiveHeuristic.cpp src/solvers/ConstructiveHeuristic.h ${LIB_SRC})
add_executable(GeneticAlgorithm src/solvers/GeneticAlgorithm.cpp src/solvers/GeneticAlgorithm.h ${LIB_SRC})

target_link_libraries(BranchAndBoundJob
        ${GUROBI_LIBRARIES}
        ${CPLEX_CP_LIBRARIES}
        ${CPLEX_LIBRARIES}
        ${CPLEX_CONCERT_LIBRARIES}
        ${CMAKE_DL_LIBS}
        )
target_link_libraries(ConstructiveHeuristic
        ${GUROBI_LIBRARIES}
        ${CPLEX_CP_LIBRARIES}
        ${CPLEX_LIBRARIES}
        ${CPLEX_CONCERT_LIBRARIES}
        ${CMAKE_DL_LIBS}
        )
target_link_libraries(GeneticAlgorithm
        ${GUROBI_LIBRARIES}
        ${CPLEX_CP_LIBRARIES}
        ${CPLEX_LIBRARIES}
        ${CPLEX_CONCERT_LIBRARIES}
        ${CMAKE_DL_LIBS}
        )
