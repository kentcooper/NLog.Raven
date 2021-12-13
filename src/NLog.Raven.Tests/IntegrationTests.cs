using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog.Config;
using NLog.Layouts;
using Xunit;

namespace NLog.Raven.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public void SimpleLogTest()
        {
            var ravenTarget = new RavenTarget
            {
                Urls = "RavenNLog",
                Fields = new List<RavenField>()
                {
                    new RavenField("Host", new SimpleLayout("${machinename}")),
                    new RavenField("EventDate", new SimpleLayout("${longdate}"))
                }
            };

            var rule = new LoggingRule("*", ravenTarget);

            rule.EnableLoggingForLevel(LogLevel.Info);

            var config = new LoggingConfiguration();

            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;

            var logger = LogManager.GetLogger("RavenExample");

            logger.Info("Hello RavenDB Simple Log");

            LogManager.Flush();

        }

        [Fact]
        public void ReadFromConfigTest()
        {
            LogManager.Configuration = new XmlLoggingConfiguration("NLog.Raven.Tests.dll.config");

            var logger = LogManager.GetLogger("Example");

            for (var i = 0; i < 9000; i++)
            {
                logger.Info($"Hello Raven {i}");
            }

            LogManager.Flush();
        }

        [Fact]
        public void ReadFromConfigExceptionTest()
        {
            LogManager.Configuration = new XmlLoggingConfiguration("NLog.Raven.Tests.dll.config");

            var logger = LogManager.GetLogger("Example");

            try
            {
                throw new Exception("Random error exception");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Random Exception");
            }

            LogManager.Flush();
        }

    }
}
