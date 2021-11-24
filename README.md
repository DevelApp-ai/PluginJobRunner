# PluginJobRunner

Sample project for [RuntimePluggableClassFactory](https://github.com/DevelApp-dk/RuntimePluggableClassFactory). The short description og RuntimePluggableClassFactory is to make it easier to work on plugin executable frameworks. It is a part of my overall aim that developers mainly work on what gives the business value mainly user experience, business rules, integration, orchistration and data avoiding as much plumming code as possible. Plumming code in my experience accounts for 25-90 % of a developers work and typically gives almost 0 % value for the business.

## Project setup
- PluginInterface: Carries the general IJobExecutor and IJobExecutionContext used to make the individual JobExecutors
- JobExecutorModel: Includes the domain model (and because dual database access also the ef database context and migrations)
- JobExecutor: Project for containing the individual JobExecutor implementations (here only the MergeSortArray)
- JobExecutorTest: Project for containing the unit tests for the JobExecutor project
- JobRunner: OpenApi server responsible for scheduling and running the jobs

## Design
To make it easier to test the functionality of the individual job function it has been made as a plugin. To facilitate easier use of plugins I have chosen to use a library I have developed for this called [RuntimePluggableClassFactory](https://github.com/DevelApp-dk/RuntimePluggableClassFactory).

The JobRunner is responsible for checking regularly if there has come new jobs in the database, load the JobExecutor and execute the job based on the data. It is not possible to determine if a nano-service system better made on a actor or virtual actor system or a microservice system and as it is not included in the requirements. I have decided to use a WebApi as a microservice for the JobRunner. I will use the Quartz.Net as a hosted service [Quartz.Net](https://www.quartz-scheduler.net/) serving as the timer service. In a production environment this would be inside the secured zone accessable from the DMZ where the Frontend runs. 

The Quartz setup and use is modified from [Creating a Quartz.NET hosted service with ASP.NET Core by Andrew Lock](https://andrewlock.net/creating-a-quartz-net-hosted-service-with-asp-net-core/) as his version allows for constructor dependency injection for jobs. His version is explains having the jobs created in the startup which is ok for some jobs but again have the problem that changing a job recycles the application pool. For a configurable job runner that is not acceptable. The problem is mainly a WebApi problem as the service is configured to wait for the jobs finish the run. Use of the [DisallowConcurrentExecution] might cause a problem but also solves a problem on a jobrunner.

The JobRunner Startup on development is set to create the database if missing run migrations on the database

There should be a frontend WebApi server for relaying the Api from the JobRunner service. The Frontend server in a production environment be a frontend server in a DMZ zone. As it would have somewhat the same as is done in the JobRunner in terms of the Api and just asa proxy it is seen as trivial and not included.

## Implementation notes
Job processing will be handed off to a job scheduler, in this case a timed version [Quartz.Net](https://www.quartz-scheduler.net/) as it can be configured to run in memory and persisted in database and be used for much more of scheduling.

Logging will be done using Serilog as it is rather configurable and easy to work with. It logs to file in the JobRunner project in the log folder. File logging will not occur if security is set to not allow serilog to create the log folder. Logging levels are different on production and development and controlled in the appsettings.json file and appsettings.Development.json file. Operationswise a dynamic setting of the loglevels would be nice to have as a normal change in the configurations will make a .net server recycle the application pool desturbing execution for normal users.

Unit testing needs to cover business rules or key components. This rule only has one key component which is the sorting of the array. It is possible to make an integration test of putting data into the scheduler but that will not be included.

Unit testing is done using XUnit (base testing), FluentAssertions (failing test readability), AutoFixture (easier unit testing setup), Moq (Mocking framework without shims support)

## How to use
### Startup in Visual Studio
Press F5 in Visual Studio when solution has started up.
Visual studio will startup with Swagger. Under the startup it shows the precise ports used as they might be different depending on the system running it. Default SSL setting on the WebApi needs a selfsigned SSL certificate that Visual Studio can generate automatically.

To use the Api from the OpenApi UI you need to authorize using the green button in the upper right corner with the ApiKey (defined in appsettings which is "Secret"). Click Close when you have entered the key. At this time it is possible to use all the functions of the Api

## Migrations
Migrations are created with ```dotnet ef migrations add InitialCreate -s ../JobRunner -o data -v``` in the JobExecutorModel project
Migrations are done with ```dotnet ef database update -s ../JobRunner``` in the JobExecutorModel project

## Defects and todos
* Use of JsonschemaBuilder for validating input data for job and providing typed support for both input and output. JsonSchemaBuilder might need an upgrade
* ApiKey is read from configuration file
* WebApi and JobRunner writes out unneeded headers "server: Microsoft-IIS/10.0", "x-firefox-spdy: h2", "x-powered-by: ASP.NET" which should be limited to development or removed all together
* jobData and returnedJobData in IJobExecutor is a string instead of inforced as Json. Is JsonValue from Manatee.Json useful ?
* IJobExecutionContext only exists because of mocking framework used for testing does not support shims so mocking framework should be changed
* RuntimePluggableClassFactory has two important implementation limitations (for my use it is defects): 
  - All dlls in the plugin library and their dependencies are loaded into memory and not filtered on which are approved
  - Plugin library is only loaded at startup so the application needs to be restarted to add or remove functionality
* Failing JobExecutor should not be killing the whole application. Stability should be investigated more
* More consistent naming
* IJobExecutionContext is not implemented and is only a stub
* ApiKey security in own Nuget as used often eventhough it is fairly simple code 
* Model should have the Job split into two so that definition JobExecutor and JobData is not in the same place as the execution data JobExecutionDuration but as Quartz.Net data model will take over when jobs gets persisted it is not in focus.
* There is no support for calling a specific version of the JobExecutor and RuntimePluggableClassFactory will always serve the newest version

## Things to think about
* Consider if the domain model and the database model should be separated ? Especially when Quartz.Net data model takes over a simplified version seems to be nice for the domain
* Can the webapi be made into executable framwork using plugins for controllers and model ? A form of dynamic controller serving for plugins microfrontend style ?
* JobSchedule.JobType with its Type type is clashing with the naming used in RuntimePluggableClassFactory as it can be different that the class name. Should the RuntimePluggableClassFactory be redone to use the Type type?
* Could (MediatR)[https://www.kiltandcode.com/2021/02/15/using-mediatr-request-handlers-in-aspnet-core-to-decouple-code/] be used to generalize dependency handling in the individual jobs as the context
