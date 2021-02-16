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

Originally written in 2013, recently uplifted a little to get it portable and into GitHub. Battle-tested over the years against some of the largest MIM solutions during my time as a MIM consultant. Use as you like, will accept (good) pull requests.

## Usage:

Run from the command line with a single argument for the schedule file:

`.\Tetron.Mim.SynchronisationScheduler.exe c:\somewhere\full-schedule.config`

## Configuration

### App.config

Program behaviour can be customised via some settings in the the App.config:

| Key | Description |
| --- | ----------- |
| LoggingLevel | Accepts values of `Verbose`, `Debug`, `Information`, `Warning`, `Error` and `Fatal`. The default is `Information` if not supplied. Controls how verbose the logs are. In your development environment you might want to use Debug and in production use Information or Warning. |
| whatif | Accepts values of `true` or `false`. When true, the schedule will be executed and logs will be created but the actual task work will not be executed. This is to allow you to test that everything is working as expected before letting it loose on your system. |

### Schedule file

The schedule file is an XML document that defines what tasks the scheduler is to run and in what order. See the Example Schedules folder for an example of all the different types of tasks and how they could be combined to get all MIM sync work complete.

The schedule files are specified by command line argument to enable you to easily run different schedules from a Windows Scheduled Task, i.e. You could have two schedule files, one for delta-sync operations that you run most days and then another, full-sync schedule that you run once a week to reconcile any inconsistencies in your MIM Sync system.

**Rules:**

There are a couple of rules you need to follow when crafting schedule files:

 * Block elements are used to run child tasks in parallel. Child nodes of the Block node can all be siblings, i.e. a flat structure, or they can nest if required
 * All other elements must nest and not be siblings. This tells the scheduler that tasks are run one after another, i.e. the parent task is run, then the child, then it's child, etc.

### Tasks

The scheduler supports the following task types that can be specified in the schedule file. Note that `Name` and `Enabled` are common to all tasks types and are required.

| Type | Arguments | Description |
| ---- | --------- | ----------- |
| ManagementAgent | `RunProfile` and `OnlyIfPendingExportsExist` | Causes a MIM Management Agent to run. `RunProfile` should be the name of the Run Profile as seen in MIM. `OnlyIfPendingExportsExist` is optional and cause be used to intelligently control whether more work should be done, i.e. if set to true then the management agent will only be run if there are pending exports for this management agent in MIM. |
| Executable | `Command`, `Arguments` and `ShowWindow` | Causes an executable program to run. `Command` is required and should be the path to the program. `Arguments` is optional and should contain any command-line arguments for the program. `ShowInWindow` is optional (default is false) and accepts `true` or `false` values and controls whether a window should be shown that runs the program. In development you might want to to see the window to confirm your executable is working and in production you should have it set to false as nobody will be watching. |
| PowerShell | `Path` | Causes a PowerShell script to be run. `Path` is required and should be the fully-qualified path to the script file. It does not support arguments so you may have to create a wrapper script for your actual script with the wrapper containing arguments in. Sorry. **Note:** do not use `Write-Host` in your scripts as this is not compatible with how the scheduler has to run PowerShell. Use `Write-Output` instead. |
| SqlServer | `Server` and `Command` | Causes a SQL Server command to be run. Specify host/port info in SQL Server format in `Server` and the SQL command in `Command`. Uses integrated security (Windows Authentication) so the identity running the Scheduler program needs the appropriate permissions on the SQL Server you are targetting. |
| Block | None | Causes all child tasks to be run in parallel. This helps massively in speeding up most MIM systems when you can perform all import and export MIM management agent  operations in parallel. **Note: you must not use this on synchronisation run profiles**. MIM is not designed to support this and SQL deadlocks are likely to happen. Parallel processing is massively helpful in reducing overall schedule run-time, especially when you have long-running management agents. |
| ContinuationCondition | `Type` | Controls whether more work should be undertaken if the continuation condition is met. Nest child nodes in this element you want to run only if the continuation condition is met. `Type` accepts `ManagementAgentsHadImports` as a value and causes the scheduler to only continue processing the schedule if preceding management agent runs detected changes on import runs. Very useful for optimising your system at the begining of schedules if you import from authoritative systems - you can then only continue to process tasks if there were any dected changes. Often there's no point doing any more work if nothing has changed up-stream. |

Note: It is rare, but possible that the Scheduler may receive SQL deadlock responses from MIM if it is busy performing another operation whilst trying to do something. This is a documented MIM error. The Scheduler will gracefully handle this by re-trying the operation.
