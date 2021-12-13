using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;

namespace NLog.Raven
{
    [Target("Raven")]
    public class RavenTarget : TargetWithLayout
    {
        private DocumentStore _documentStore;
        public string Urls { get; set; }
        public string Database { get; set; }
        public string IdType { get; set; } = "string";
        public string CertPath { get; set; }
        public string CertStoreLocation { get; set; }
        public string CertThumbprint { get; set; }
        public string CertCn { get; set; }
        public string CertStoreName { get; set; }

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

            if (string.IsNullOrWhiteSpace(Urls))
            {
                throw new NLogConfigurationException(
                    "Cannot resolve RavenDB Url. Please make sure either the Url or ConnectionStringName property is set.");
            }

            _documentStore = new DocumentStore
            {
                Urls = Urls.Split('\u002C')
            };

            if (!string.IsNullOrWhiteSpace(Database))
            {
                _documentStore.Database = Database;
            }

            if (!string.IsNullOrWhiteSpace(CertPath))
            {
                X509Certificate2 clientCertificate = new X509Certificate2(CertPath);
                _documentStore.Certificate = clientCertificate;
            }
            else if (!string.IsNullOrWhiteSpace(CertStoreName) && (!string.IsNullOrWhiteSpace(CertCn) || !string.IsNullOrWhiteSpace(CertThumbprint)))
            {
                _documentStore.Certificate = LoadCertificate();
            }


            _documentStore.Conventions.FindCollectionName = type => type == typeof(NLogEntry) ? CollectionName : DocumentConventions.DefaultGetCollectionName(type);                        
            _documentStore.Initialize();



        }

        [Obsolete]
        protected override void Write(AsyncLogEventInfo logEvent)
        {
            Write(new[] { logEvent });
        }

        [Obsolete]
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


        private X509Certificate2 LoadCertificate()
        {
            StoreLocation location;

            switch (CertStoreLocation)
            {
                case "CurrentUser":
                    location = StoreLocation.CurrentUser;
                    break;
                default:
                    location = StoreLocation.LocalMachine;
                    break;
            }
            X509Store store = new X509Store(CertStoreName, location);

            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            X509Certificate2Collection results;

            if (!string.IsNullOrEmpty(CertCn))
            {
                results = store.Certificates.Find(X509FindType.FindBySubjectName, CertCn, false);
            }
            else
            {
                results = store.Certificates.Find(X509FindType.FindByThumbprint, CertThumbprint, false);
            }

            if (results.Count < 1)
            {
                throw new NLogConfigurationException("The raven database certificate could not be found on the local machine. Please check the certificate manager and verify that the certificate is available in the Personal store of the Local Machine");
            }

            return results[0];
        }

    }
}
