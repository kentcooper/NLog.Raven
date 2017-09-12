using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using Raven.Client.Document;
using Raven.Abstractions.Logging.LogProviders;

namespace NLog.Raven
{
    [Target("Raven")]
    public class RavenTarget : TargetWithLayout
    {
        private DocumentStore _documentStore;

        public string ConnectionStringName { get; set; }
        public string Url { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
        public string ApiKey { get; set; }
        public string Domain { get; set; }
        public string IdType { get; set; } = "string";

        public string CollectionName { get; set; } = "NLogEntries";

        [ArrayParameter(typeof(RavenField), "field")]
        public IList<RavenField> Fields { get; set; } = new List<RavenField>();

        public RavenTarget()
        {
            Name = "Raven";
        }

        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            if (!string.IsNullOrEmpty(ConnectionStringName))
            {
                _documentStore = new DocumentStore
                {
                    ConnectionStringName = ConnectionStringName
                };
            }
            else
            {
                if (string.IsNullOrEmpty(Url))
                {
                    throw new NLogConfigurationException(
                        "Cannot resolve RavenDB Url. Please make sure either the Url or ConnectionStringName property is set.");
                }

                _documentStore = new DocumentStore
                {
                    Url = Url
                };

                if (!string.IsNullOrWhiteSpace(Database))
                {
                    _documentStore.DefaultDatabase = Database;
                }

                if (!string.IsNullOrWhiteSpace(ApiKey))
                {
                    _documentStore.ApiKey = ApiKey;
                }
                else if (!string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password) && !string.IsNullOrWhiteSpace(Domain))
                {
                    _documentStore.Credentials = new NetworkCredential(User, Password, Domain);
                }
            }

            _documentStore.Conventions.FindTypeTagName = type => type == typeof(NLogEntry) ? CollectionName : DocumentConvention.DefaultTypeTagName(type);
            _documentStore.Conventions.DisableProfiling = true;
            NLogLogManager.ProviderIsAvailableOverride = false;
            _documentStore.Initialize();



        }

        protected override void Write(AsyncLogEventInfo logEvent)
        {
            Write(new[] { logEvent });
        }

        protected override void Write(AsyncLogEventInfo[] logEvents)
        {
            try
            {
                var events = logEvents.Select(e => e.LogEvent);

                var nLogEntries = new List<NLogEntry>();

                foreach (var logEvent in events)
                {
                    nLogEntries.Add(CreateLogEntry(logEvent));
                }

                using (var bulkInsert = _documentStore.BulkInsert())
                {
                    foreach (var nLogEntry in nLogEntries)
                    {
                        bulkInsert.Store(nLogEntry);
                    }
                }

            }
            catch (Exception ex)
            {
                InternalLogger.Error($"Error while sending log messages to RavenDB: message=\"{ex.Message}\"");

                foreach (var ev in logEvents)
                {
                    ev.Continuation(ex);

                }
            }
        }
        protected override void Write(LogEventInfo logEvent)
        {
            try
            {
                using (var session = _documentStore.OpenSession())
                {
                    session.Store(CreateLogEntry(logEvent));
                    session.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                InternalLogger.Error($"Error while sending log messages to RavenDB: message=\"{ex.Message}\"");

            }
        }

        private NLogEntry CreateLogEntry(LogEventInfo logEvent)
        {
            dynamic entry = new NLogEntry();

            if (IdType.ToLowerInvariant() == "string")
            {
                entry.Id = (string)null;
            }

            if (IdType.ToLowerInvariant() == "guid")
            {
                entry.Id = Guid.NewGuid();
            }

            foreach (var field in Fields)
            {
                var renderedField = field.Layout.Render(logEvent);

                if (!string.IsNullOrWhiteSpace(renderedField))
                {
                    entry[field.Name] = renderedField;
                }
            }

            return entry;
        }
    }
}
