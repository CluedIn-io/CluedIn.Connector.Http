using System;
using System.Collections.Generic;
using CluedIn.Core.Net.Mail;
using CluedIn.Core.Providers;

namespace CluedIn.Connector.Http
{
    public class HttpConstants
    {
        public struct KeyName
        {
            public const string Url = "url";
            public const string Authorization = "authorization";
        }

        public const string ConnectorName = "HttpConnector";
        public const string ConnectorComponentName = "HttpConnector";
        public const string ConnectorDescription = "Supports publishing of data to external Http Post endpoints.";
        public const string Uri = "https://en.wikipedia.org/wiki/POST_(HTTP)";

        public static readonly Guid ProviderId = Guid.Parse("{486CC091-4C77-4190-8E2D-058BC4CA8A85}");
        public const string ProviderName = "Http Connector";
        public const bool SupportsConfiguration = false;
        public const bool SupportsWebHooks = false;
        public const bool SupportsAutomaticWebhookCreation = false;
        public const bool RequiresAppInstall = false;
        public const string AppInstallUrl = null;
        public const string ReAuthEndpoint = null;

        public static IList<string> ServiceType = new List<string> { "Connector" };
        public static IList<string> Aliases = new List<string> { "HttpConnector" };
        public const string IconResourceName = "Resources.HTTP_logo.svg";
        public const string Instructions = "Provide authentication instructions here, if applicable";
        public const IntegrationType Type = IntegrationType.Connector;
        public const string Category = "Connectivity";
        public const string Details = "Supports publishing of data to external Http Post endpoints.";

        public const string BatchingSupported = nameof(BatchingSupported);

        /// <summary>
        /// Environment key name for batch sync interval
        /// </summary>
        public static string BatchSyncIntervalKeyName => "Streams_HttpConnector_BatchSyncInterval";

        /// <summary>
        /// Environment key name for batch records threshold
        /// </summary>
        public static string BatchRecordsThresholdKeyName => "Streams_HttpConnector_BatchRecordsThreshold";

        public static AuthMethods AuthMethods = new AuthMethods
        {
            token = new Control[]
            {
                new Control
                {
                    name = KeyName.Url,
                    displayName = "Url",
                    type = "input",
                    isRequired = true
                },
                new Control
                {
                    name = KeyName.Authorization,
                    displayName = "Authorization",
                    type = "input"
                },
                new Control
                {
                    name = BatchingSupported,
                    displayName = BatchingSupported,
                    type = "checkbox"
                }
            }
        };

        public static IEnumerable<Control> Properties = new List<Control>();

        public static readonly ComponentEmailDetails ComponentEmailDetails = new ComponentEmailDetails {
            Features = new Dictionary<string, string>
            {
                                       { "Connectivity",        "Expenses and Invoices against customers" }
                                   },
            Icon = ProviderIconFactory.CreateConnectorUri(ProviderId),
            ProviderName = ProviderName,
            ProviderId = ProviderId,
            Webhooks = SupportsWebHooks
        };

        public static IProviderMetadata CreateProviderMetadata()
        {
            return new ProviderMetadata {
                Id = ProviderId,
                ComponentName = ConnectorName,
                Name = ProviderName,
                Type = "Connector",
                SupportsConfiguration = SupportsConfiguration,
                SupportsWebHooks = SupportsWebHooks,
                SupportsAutomaticWebhookCreation = SupportsAutomaticWebhookCreation,
                RequiresAppInstall = RequiresAppInstall,
                AppInstallUrl = AppInstallUrl,
                ReAuthEndpoint = ReAuthEndpoint,
                ComponentEmailDetails = ComponentEmailDetails
            };
        }
    }
}
