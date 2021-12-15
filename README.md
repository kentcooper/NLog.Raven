# NLog.Raven
RavenDB target for NLog

[![NuGet Version](https://img.shields.io/nuget/v/NLog.Raven.svg?style=flat-square)](https://www.nuget.org/packages/NLog.Raven/) 

## Download

The NLog.Raven library is available on nuget.org via package name `NLog.Raven`.

To install NLog.Raven, run the following command in the Package Manager Console

    PM> Install-Package NLog.Raven
    
More information about NuGet package avaliable at
<https://nuget.org/packages/NLog.Raven>

## Configuration Syntax

```xml
<extensions>
  <add assembly="NLog.Raven"/>
</extensions>

<targets>
  <target xsi:type="Raven"
          name="String"
          urls="Layout"
          database="Layout"          
          collectionName="Layout"
          idType="String">
    <field name="String" layout="Layout" /> <!-- repeated --> 
  </target>
</targets>
```
## Parameters

_urls_ - The url of the raven db server

_database_ - The name of the database to connect to

_idType_ - The id type to use for log entries. Either 'String' | 'Guid'

_collectionName_ - Set this to change the default document collection name

## Examples

```xml
<targets>
      <target name="raven" xsi:type="BufferingWrapper" flushTimeout="7000">
        <target  xsi:type="Raven" Urls="http://ws-1:8080;Database=RavenNLog" IdType="Guid" CollectionName="LogEntries">
          <field name="EventDate" layout="${longdate}"/>
          <field name="Logger" layout="${logger}"/>
          <field name="Message" layout="${message}"/>
          <field name="Host" layout="${machinename}"/>
          <field name="Exception" layout="${exception:format=toString,Data}"/>
        </target>
      </target>
</targets>
<rules>
    <logger name="*" minlevel="Info" writeTo="raven"/>
</rules>
```

