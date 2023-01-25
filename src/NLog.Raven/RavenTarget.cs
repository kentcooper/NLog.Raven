using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Json;

namespace NLog.Raven
{
    /// <summary>
    /// RavenDB target for NLog
    /// </summary>
    [Target("Raven")]
    public class RavenTarget : TargetWithLayout
    {
        private DocumentStore _documentStore;

        /// <summary>
        /// Urls for DocumentStore
        /// </summary>
        public Layout Urls { get; set; }

        /// <summary>
        /// Default database name for DocumentStore
        /// </summary>
        public Layout Database { get; set; }

        /// <summary>
        /// The id type to use for log entries. Either 'String' | 'Guid'
        /// </summary>
        public string IdType { get; set; } = "string";

        /// <summary>
        /// File Path to Client Certificate
        /// </summary>
        public Layout CertPath { get; set; }

        /// <summary>
        /// Certificate Store Location Type. See also <see cref="StoreLocation"/>
        /// </summary>
        public string CertStoreLocation { get; set; }

        /// <summary>
        /// Certificate Store Location Name.
        /// </summary>
        public Layout CertStoreName { get; set; }

        /// <summary>
        /// Lookup certificate from <see cref="X509FindType.FindByThumbprint"/>
        /// </summary>
        public Layout CertThumbprint { get; set; }

        /// <summary>
        /// Lookup certificate from <see cref="X509FindType.FindBySubjectName"/>
        /// </summary>
        public Layout CertCn { get; set; }

        /// <summary>
        /// Default document collection name
        /// </summary>
        public Layout CollectionName { get; set; } = "NLogEntries";

        /// <summary>
        /// 
        /// </summary>
        [ArrayParameter(typeof(RavenField), "field")]
        public IList<RavenField> Fields { get; set; } = new List<RavenField>();
        
        /// <summary>
        /// Expiration date of document
        /// </summary>
        public int ExpiryDays { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RavenTarget"/> class.
        /// </summary>
        public RavenTarget()
        {
            Name = "Raven";
        }

        /// <inheritdoc/>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            var defaultLogEvent = LogEventInfo.CreateNullEvent();

            var urls = RenderLogEvent(Urls, defaultLogEvent);
            if (string.IsNullOrWhiteSpace(urls))
            {
                throw new NLogConfigurationException(
                    "Cannot resolve RavenDB Url. Please make sure either the Url or ConnectionStringName property is set.");
            }

            _documentStore = new DocumentStore
            {
                Urls = urls.Split('\u002C')
            };

            var database = RenderLogEvent(Database, defaultLogEvent);
            if (!string.IsNullOrWhiteSpace(database))
            {
                _documentStore.Database = database;
            }

            var certificate = TryLoadCertificate();
            if (certificate != null)
            {
                _documentStore.Certificate = certificate;
            }

            var collectionName = RenderLogEvent(CollectionName, defaultLogEvent);
            _documentStore.Conventions.FindCollectionName = type => type == typeof(NLogEntry) ? collectionName : DocumentConventions.DefaultGetCollectionName(type);                        
            _documentStore.Initialize();
        }

        /// <inheritdoc/>
        protected override void Write(IList<AsyncLogEventInfo> logEvents)
        {
            try
            {
                using (var bulkInsert = _documentStore.BulkInsert())
                {
                    var expirationMetadata = ExpiryDays > 0 ? new MetadataAsDictionary
                    {
                        new KeyValuePair<string, object>(Constants.Documents.Metadata.Expires, DateTime.UtcNow.AddDays(ExpiryDays))
                    } : null;

                    foreach (var log in logEvents)
                    {
                        var logEvent = log.LogEvent;
                        var logEntry = CreateLogEntry(logEvent);
                        if (expirationMetadata != null)
                        {
                            bulkInsert.Store(logEntry, expirationMetadata);
                        }
                        else
                        {
                            bulkInsert.Store(logEntry);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InternalLogger.Error(ex, "Error while sending log messages to RavenDB");

                foreach (var ev in logEvents)
                {
                    ev.Continuation(ex);
                }
            }
        }

        /// <inheritdoc/>
        protected override void Write(LogEventInfo logEvent)
        {
            try
            {
                var expiry = DateTime.UtcNow.AddDays(ExpiryDays);
                using (var session = _documentStore.OpenSession())
                {
                    var entry = CreateLogEntry(logEvent);
                    session.Store(entry);
                    if(ExpiryDays > 0)
                        session.Advanced.GetMetadataFor(entry)[Constants.Documents.Metadata.Expires] = expiry;
                    
                    session.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                InternalLogger.Error(ex, "Error while sending log messages to RavenDB");
                throw;
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

            for (int i = 0; i < Fields.Count; ++i)
            {
                var field = Fields[i];

                var fieldValue = RenderLogEvent(field.Layout, logEvent);
                if (!string.IsNullOrWhiteSpace(fieldValue))
                {
                    entry[field.Name] = fieldValue;
                }
            }

            return entry;
        }

        private X509Certificate2 TryLoadCertificate()
        {
            var defaultLogEvent = LogEventInfo.CreateNullEvent();

            var certPath = RenderLogEvent(CertPath, defaultLogEvent);
            if (!string.IsNullOrWhiteSpace(certPath))
            {
                X509Certificate2 clientCertificate = new X509Certificate2(certPath);
                return clientCertificate;
            }

            var certStoreName = RenderLogEvent(CertStoreName, defaultLogEvent);
            if (string.IsNullOrWhiteSpace(certStoreName))
            {
                return null;
            }

            var certStoreLocation = RenderLogEvent(CertStoreLocation, defaultLogEvent);
            var certCn = RenderLogEvent(CertCn, defaultLogEvent);
            var certThumbprint = RenderLogEvent(CertThumbprint, defaultLogEvent);

            StoreLocation location;

            switch (certStoreLocation)
            {
                case "CurrentUser":
                    location = StoreLocation.CurrentUser;
                    break;
                default:
                    location = StoreLocation.LocalMachine;
                    break;
            }
            X509Store store = new X509Store(certStoreName, location);

            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            X509Certificate2Collection results;

            if (!string.IsNullOrEmpty(certCn))
            {
                results = store.Certificates.Find(X509FindType.FindBySubjectName, certCn, false);
            }
            else
            {
                results = store.Certificates.Find(X509FindType.FindByThumbprint, certThumbprint, false);
            }

            if (results.Count < 1)
            {
                throw new NLogConfigurationException("The raven database certificate could not be found on the local machine. Please check the certificate manager and verify that the certificate is available in the Personal store of the Local Machine");
            }

            return results[0];
        }
    }
}
