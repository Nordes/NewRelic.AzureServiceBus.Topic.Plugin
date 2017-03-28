[![Build status](https://ci.appveyor.com/api/projects/status/oe945ybj0o9uhgif/branch/master?svg=true)](https://ci.appveyor.com/project/Nordes/newrelic-azureservicebus-topic-plugin/branch/master)
# NewRelic Azure ServiceBus Topic Agent
NewRelic Azure ServiceBus Topic Agent is a NewRelic Plugin that monitors the azure service bus in one or more topic. 

* The plugin is NPI-compatible. *(On it's way)*
* (not true at the moment) When there's errors contacting NewRelic or Azure Storage Queue, an eventlog is created in Windows EventLogs journals

# Configuration
...
1. You need the proper connection strings.
2. You need an existing topic (we don't create if not exists)
3. We need a proper label on the message (If I am not mistaking, usually the class name is by default)

# Example
...todo...

# Metrics
The metrics sent to NewRelic are the following

| Metric format | Description |
| :------------ | :---------- |
| Component/Topics/`Agent name plugin.json`/all/`Topic Name`/`Property Name`[`Aggregation Type`] | Contains the value of the property depending on the type. If `count` is used then it will be used as a hit counter. |
| \* Component/Topics/`Agent name plugin.json`/all/`Topic Name`/`Property Name`/`Property value`[hits] | This will happens only if you specify that you want the value in the key (configuration : includeValueInKey: true). |
| Component/Topics/`Agent name plugin.json`/all/`Topic Name`/`Label value`[hits] | Specific to label which is a real property and not from the array of properties in a message. |


*Note:* The element with \* are not implemented

# Install as a Windows Service
To install the plug-in as a Windows Service, execute `plugin.exe install`. This will add it was a Windows Service named _NewRelic.AzureServiceBus.Topic.Plugin_ which will start automatically when windows start.

# Uninstall the Windows Service
Like the install `plugin.exe uninstall` and it's done.

# Todo's
...todo... ;)
