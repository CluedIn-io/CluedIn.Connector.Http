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
                    type = "input",
                    isRequired = false
                },
                new Control
                {
                    Name = "mykeyname",
                    DisplayName = "My Key Name",
                    Type = "dynamic",
                    IsRequired = true,
                    SourceType = ControlSourceType.Dynamic,
                    Source = MyExtendedConfigurationProvider.SourceName,
                },
                new Control
                {
                    Name = "country",
                    DisplayName = "Country",
                    Type = "option",
                    IsRequired = true,
                    SourceType = ControlSourceType.Dynamic,
                    Source = MyExtendedConfigurationProvider.SourceName,
                },
                new Control
                {
                    Name = "state",
                    DisplayName = "State",
                    Type = "option",
                    SourceType = ControlSourceType.Dynamic,
                    Source = MyExtendedConfigurationProvider.SourceName,
                    IsRequired = true,
                    DataDependencies = new[]
                    {
                        new ControlDataDependency
                        {
                            Name = "country",
                        }
                    },
                    DisplayDependencies = new[]
                    {
                        new ControlDisplayDependency
                        {
                            Name = "country",
                            Operator = ControlDependencyOperator.Exists,
                            Value = null,
                            UnfulfilledAction = ControlDependencyUnfulfilledAction.Disabled,
                        },
                    },
                },
                new Control
                {
                    Name = "city",
                    DisplayName = "City",
                    Type = "option",
                    SourceType = ControlSourceType.Dynamic,
                    Source = MyExtendedConfigurationProvider.SourceName,
                    IsRequired = true,
                    DataDependencies = new[]
                    {
                        new ControlDataDependency
                        {
                            Name = "country",
                        },
                        new ControlDataDependency
                        {
                            Name = "state",
                        },
                    },
                    DisplayDependencies = new[]
                    {
                        new ControlDisplayDependency
                        {
                            Name = "country",
                            Operator = ControlDependencyOperator.Exists,
                            Value = null,
                            UnfulfilledAction = ControlDependencyUnfulfilledAction.Disabled,
                        },
                        new ControlDisplayDependency
                        {
                            Name = "state",
                            Operator = ControlDependencyOperator.Exists,
                            Value = null,
                            UnfulfilledAction = ControlDependencyUnfulfilledAction.Disabled,
                        },
                    },
                },
                new Control
                {
                    Name = "outputFormat2",
                    DisplayName = "Output Format2",
                    Type = "option",
                    SourceType = ControlSourceType.Dynamic,
                    Source = MyExtendedConfigurationProvider.SourceName,
                    IsRequired = true,
                },
                new Control
                {
                    Name = "delimiter2",
                    DisplayName = "Delimiter2",
                    Type = "input",
                    IsRequired = true,
                    DisplayDependencies = new[]
                    {
                        new ControlDisplayDependency
                        {
                            Name = "outputFormat2",
                            Operator = ControlDependencyOperator.Equals,
                            Value = "csv",
                            UnfulfilledAction = ControlDependencyUnfulfilledAction.Hidden,
                        },
                    },
                },
                new Control
                {
                    Name = "subscription",
                    DisplayName = "Subscription",
                    Type = "option",
                    IsRequired = true,
                    SourceType = ControlSourceType.Dynamic,
                    Source = MyAzureProvider.SourceName,
                },
                new Control
                {
                    Name = "resourceGroup",
                    DisplayName = "Resource Group",
                    Type = "option",
                    SourceType = ControlSourceType.Dynamic,
                    Source = MyAzureProvider.SourceName,
                    IsRequired = true,
                    DataDependencies = new[]
                    {
                        new ControlDataDependency
                        {
                            Name = "subscription",
                        }
                    },
                    DisplayDependencies = new[]
                    {
                        new ControlDisplayDependency
                        {
                            Name = "subscription",
                            Operator = ControlDependencyOperator.Exists,
                            Value = null,
                            UnfulfilledAction = ControlDependencyUnfulfilledAction.Disabled,
                        },
                    },
                },
                new Control
                {
                    Name = "resource",
                    DisplayName = "Resource",
                    Type = "option",
                    SourceType = ControlSourceType.Dynamic,
                    Source = MyAzureProvider.SourceName,
                    IsRequired = true,
                    DataDependencies = new[]
                    {
                        new ControlDataDependency
                        {
                            Name = "subscription",
                        },
                        new ControlDataDependency
                        {
                            Name = "resourceGroup",
                        },
                    },
                    DisplayDependencies = new[]
                    {
                        new ControlDisplayDependency
                        {
                            Name = "subscription",
                            Operator = ControlDependencyOperator.Exists,
                            Value = null,
                            UnfulfilledAction = ControlDependencyUnfulfilledAction.Disabled,
                        },
                        new ControlDisplayDependency
                        {
                            Name = "resourceGroup",
                            Operator = ControlDependencyOperator.Exists,
                            Value = null,
                            UnfulfilledAction = ControlDependencyUnfulfilledAction.Disabled,
                        },
                    },
                },
            }
        };

        public static IEnumerable<Control> Properties = new List<Control>
        {
            new Control
            {
                Name = "outputFormat",
                DisplayName = "Output Format",
                Type = "option",
                SourceType = ControlSourceType.Dynamic,
                Source = MyExtendedConfigurationProvider.SourceName,
                IsRequired = true,
            },
            new Control
            {
                Name = "delimiter",
                DisplayName = "Delimiter",
                Type = "input",
                IsRequired = true,
                DisplayDependencies = new[]
                {
                    new ControlDisplayDependency
                    {
                        Name = "outputFormat",
                        Operator = ControlDependencyOperator.Equals,
                        Value = "csv",
                            UnfulfilledAction = ControlDependencyUnfulfilledAction.Hidden,
                    },
                },
            }
        };

        public static readonly ComponentEmailDetails ComponentEmailDetails = new ComponentEmailDetails {
            Features = new Dictionary<string, string>
            {
                {
                    "Connectivity",
                    "Expenses and Invoices against customers"
                }
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
