# This file is released under MIT license.
# See file LICENSE.txt for more information.

from enum import IntEnum


class PresolveLevel(IntEnum):
    Auto = -1,
    Off = 0,
    Conservative = 1,
    Aggressive = 2
