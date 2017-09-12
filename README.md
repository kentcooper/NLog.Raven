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
          connectionStringName="String"
          url="String"
          user="String"
          password="Song"
          domain="String"
          database="String"          
          idType="String"
          collectionName="String">
    
    <!-- repeated --> 
    <field name="String" layout="Layout" />    
  </target>
</targets>
```
## Parameters

### Options

_connectionStringName_ - The name of the connection string to get from the config file. 
_url_ - The url of the raven db server
_user_ - The windows user name to use when making the connection
_password_ - The password of the user to use when making the connection
_domain_ - The windows domain
_database_ - The name of the database to connect to
_idType_ - The id type to use for log entries. Either 'String' | 'Guid'
_collectionName_ - Set this to change the default document collection name

## Examples

```xml
<targets>
      <target name="raven" xsi:type="BufferingWrapper" flushTimeout="7000">
        <target  xsi:type="Raven" ConnectionStringName="RavenNLog" IdType="Guid" CollectionName="LogEntries">
          <field name="EventDate" layout="${longdate}"/>
          <field name="Logger" layout="${logger}"/>
          <field name="Message" layout="${message}"/>
          <field name="Host" layout="${machinename}"/>
          <field name="Exception" layout="${exception:format=toString,Data:maxInnerExceptionLevel=10}"/>
        </target>
      </target>
    </targets>
    <rules>
      <logger name="*" minlevel="Info" writeTo="raven"/>
    </rules>
```

