# MIMSyncScheduler
 
Used to run MIM 2016 tasks on a schedule and supports a variety of tasks, such as:

- MIM Synchronisation run profiles
- Executables
- SQL Scripts
- PowerShell scripts

Can help reduce overall schedule run time by executing tasks intelligently via:

- Only performing syncronisation runs if there are pending imports
- Only performing export runs if there are pending exports
- Run tasks in parallel (all but syncronisation run profiles are supported)

Other features:

- Can be run from a Windows Scheduled Task
- Can be run from the command line with output logging
- Logs to rolling log files
- Has a What-If mode to enable validation and testing
- Uses XML configuration files to define the schedule

Originally written in 2013, recently uplifted a little to get it portable and into GitHub. Battle-tested over the years against some of the largest MIM solutions during my time as a MIM consultant. Use as you like, will accept pull requests.

## Usage:

Run from the command line with a single argument for the schedule file:

`.\Tetron.Mim.SynchronisationScheduler.exe c:\somewhere\full-schedule.config`

Enable What-If mode from the Tetron.Mim.SynchronisationScheduler.exe.config file by modifying this key in the appSettings section. Enabling this will cause the schedule to run and the logs to output what they would do if the vaue were not set, but won't actually execute the task.

`<add key="whatif" value="true"/>`
