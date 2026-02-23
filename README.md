# Bovine Ballistics Prefire
A physics simulator and multivariable polynomial generator for the 2026 FRC season. It is based primarily on the work of FRC 1690 in [this video](https://youtu.be/N6ogT5DjGOk?si=JThpThN7udolmcYm&t=2815).
##
### Project Goals:
- [x] Achieve a basic physics simulation
- [x] Achieve air resistance simulation
- [ ] Achieve Magnus force simulation
- [x] Have a visual version
- [x] Add argument support to both versions
- [x] Add JSON configuration
- [x] Add simulation speed adjustment
- [x] Add automatic polynomial generation
- [ ] Integrate with robot gradle
##
### Using the simulator:
Ballistics comes in two flavors. Both have the same physics simulation under the hood, but there are some subtle differences to be aware of. Below are the features of each.
#### GUI Version:
- [x] Windows support
- [x] Linux support
- [x] MacOS support
- [ ] Gradle support
- [x] Can manually start simulation
- [x] Can see simulation
- [ ] Can see trajectory details
- Slow, real-time
#### Command Line Version:
- [x] Windows support
- [ ] Linux support (planned)
- [ ] MacOS support
- [x] Gradle support (must be added as a custom task by the user)
- [ ] Can manually start simulation
- [ ] Can see simulation
- [x] Can see trajectory details
- Fast, parallelized
####
The simulator can be customized in a few ways, each potentially crucial to the effectiveness. The first point of customization is the required shooter.json file. This gives the simulator data unique to the robot's shooter. An example file can be found at [examples/shooter.json](examples/shooter.json). Furthermore, the application can also be configured with command line arguments passed when running the executable:
- **--timescale x** affects the speed at which the program runs. Data is not lost at faster speeds, just this argument isn't useful when visualizing.
- **--inputpath x** refers to the shooter.json file, should it be renamed or moved. Absolute and relative both work, but they need to be referencing a text file.
- **--outputdir x** refers to the folder/directory the user wants the polynomials to output to. The folder/directory must exist beforehand should this argument be used.
- **--autostart x** refers to whether or not the program should run when it starts up or wait for user input. It only affects the GUI version of the app, as the command line version does this already. Valid arguments for **x** are **true**, **yes**, or **y**, or anything else for false.
#### Data included in the shooter.json file (MAKE SURE THAT xRes * vxRes >= 10):
- **shooterHeight**: the distance from the flywheel center to the ground, in meters
####
- **rFly**: the radius of the flywheel, in meters
- **rRol**: the radius of the hood rollers, in meters (leave as 0 if no rollers)
- **rHood**: the radius of the hood, in meters
- **fVelo**: the ratio of hood roller surface speed to flywheel surface speed (leave as 0 if no rollers)
####
- **maxVFly**: the maximum surface speed of the flywheel, in meters/second
- **minVFly**: the minimum surface speed of the flywheel, in meters/second
- **vFlyMaxTries**: the amount of tries the simulator will take with various speeds before giving up on a combination of x, vx, and angle
####
- **minAngle**: the minimum hood angle, in degrees with 0 being parallel to the horizon
- **maxAngle**: the maximum hood angle, in degrees with 0 being parallel to the horizon
- **angleRes**: how many various angles the simulator will gather data from
####
- **minVX**: the minimum robot velocity while shooting, in meters/second
- **maxVX**: the maximum robot velocity while shooting, in meters/second
- **vxRes**: how many various robot speeds the simulator will gather data from
####
- **minX**: the minimum distance of the shooter from the center of the Hub while shooting, in meters
- **maxX**: the maximum distance of the shooter from the center of the Hub while shooting, in meters
- **xRes**: how many various hub-relative shooter positions the simulator will gather data from
####
- **angleDev**: Expected average angular error of the hood in degrees
- **vFlyDev**: Expected average surface speed error of the flywheel in meters/second
####
- **robustnessFactor**: Arbitrary scalar definining the weight on the cost of the robustness of a trajectory
- **heightFactor**: Arbitrary scalar defining the weight on the cost of the height of a trajectory
