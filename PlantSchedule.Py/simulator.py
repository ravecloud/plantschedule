import numpy as np
from enum import Enum
class Plant:
    pass

class Resource:
    name: str
    def __init__(self, name: str) -> None:
        self.name = name
        
class Utility:
    name: str
    def __init__(self, name: str) -> None:
        self.name = name

class Staff:
    name: str
    def __init__(self, name: str) -> None:
        self.name = name

class Material:
    name: str
    def __init__(self, name: str) -> None:
        self.name = name

class OperationType(Enum):
    WAIT = 0
    PROC = 1
    CLEAN = 2
    SETUP = 3
    END = 4

class Operation:
    OperationType: OperationType
    resources: list[str]
    utilities: list[str]
    staff: list[str]
    materials: list[str, float]
    duration: dict[str, float]
    clean: float
    setup: float
    amount: int
    def __init__(self, resources: list[str], utilities: list[str], staff: list[str], materials: list[str]) -> None:
        self.resources = resources
        self.utilities = utilities
        self.staff = staff
        self.materials = materials

class Wait(Operation):
    OperationType = OperationType.WAIT
    def __init__(self, duration: dict[str, float]) -> None:
        self.duration = duration

class Proc(Operation):
    OperationType = OperationType.PROC
    def __init__(self, duration: dict[str, float], amount: int) -> None:
        self.duration = duration
        self.amount = amount

class Clean(Operation):
    OperationType = OperationType.CLEAN
    def __init__(self, clean: float) -> None:
        self.clean = clean  

class Setup(Operation):
    OperationType = OperationType.SETUP
    def __init__(self, setup: float) -> None:
        self.setup = setup

class End(Operation):
    pass


class Recipe:
    states: list[Operation]
    transitions: dict[Operation, Operation]
    current_state: Operation
    

class Order:
    recipe: Recipe
    def __init__(self, recipe: Recipe) -> None:
        self.recipe = recipe