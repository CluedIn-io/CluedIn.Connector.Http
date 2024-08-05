using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

using CluedIn.Core;
using CluedIn.Core.Providers.ExtendedConfiguration;

namespace CluedIn.Connector.Http;

internal class MyAzureProvider : IExtendedConfigurationProvider
{
    public const string SourceName = "MyAzureProvider";
    public async Task<CanHandleResponse> CanHandle(ExecutionContext context, ExtendedConfigurationRequest request)
    {
        if (request.Source.StartsWith(SourceName))
        {
            return new CanHandleResponse { CanHandle = true };
        }

        await Task.CompletedTask;
        return new CanHandleResponse { CanHandle = false };
    }

    public async Task<ResolveOptionByValueResponse> ResolveOptionByValue(ExecutionContext context, ResolveOptionByValueRequest request)
    {
        var client = new ArmClient(new DefaultAzureCredential());
        if (request.Key == "subscription")
        {
            var subscriptions = client.GetSubscriptions();
            var subscriptionResourceId = new ResourceIdentifier(request.Value);
            var found = subscriptions.SingleOrDefault(x => x.Id == subscriptionResourceId);
            if (found == null)
            {
                return null;
            }

            return new ResolveOptionByValueResponse
            {
                Option = new Option(found.Id, found.Data.DisplayName)
            };
        }

        if (request.Key == "resourceGroup")
        {
            if (!request.CurrentValues.ContainsKey("subscription"))
                return null;
            var subscriptionResourceId = new ResourceIdentifier(request.CurrentValues["subscription"]);
            var subscription = client.GetSubscriptionResource(subscriptionResourceId);
            var groups = subscription.GetResourceGroups();
            var resourceGroupId = new ResourceIdentifier(request.Value);
            var found = groups.SingleOrDefault(x => x.Id == resourceGroupId);
            if (found == null)
            {
                return null;
            }
            return new ResolveOptionByValueResponse
            {
                Option = new Option(found.Id, found.Data.Name)
            };
        }

        if (request.Key == "resource")
        {
            if (!request.CurrentValues.ContainsKey("subscription"))
                return null;

            if (!request.CurrentValues.ContainsKey("resourceGroup"))
                return null;

            var subscriptionResourceId = new ResourceIdentifier(request.CurrentValues["subscription"]);
            var resourceGroupid = new ResourceIdentifier(request.CurrentValues["resourceGroup"]);
            var subscription = client.GetSubscriptionResource(subscriptionResourceId);
            var group = (await subscription.GetResourceGroupAsync(resourceGroupid.Name)).Value;
            var resources = group.GetGenericResourcesAsync();

            var list = new List<GenericResource>();

            await foreach (var resource in resources)
            {
                list.Add(resource);
            }
            var resourceId = new ResourceIdentifier(request.Value);
            var found = list.SingleOrDefault(x => x.Id == resourceId);
            if (found == null)
            {
                return null;
            }
            return new ResolveOptionByValueResponse
            {
                Option = new Option(found.Id, found.Data.Name)
            };
        }
        return null;
    }

    public async Task<ResolveOptionsResponse> ResolveOptions(ExecutionContext context, ResolveOptionsRequest request)
    {
        var client = new ArmClient(new DefaultAzureCredential());

        if (request.Key == "subscription")
        {
            var subscriptions = client.GetSubscriptions();
            return new ResolveOptionsResponse
            {
                Data = subscriptions.Select(sub => new Option(sub.Id, sub.Data.DisplayName)).ToList(),
                Total = subscriptions.Count(),
                Page = 1,
                Take = 20,
            };
        }

        if (request.Key == "resourceGroup")
        {
            if (!request.CurrentValues.ContainsKey("subscription"))
                return ResolveOptionsResponse.Empty;
            var subscriptionResourceId = new ResourceIdentifier(request.CurrentValues["subscription"]);
            var subscription = client.GetSubscriptionResource(subscriptionResourceId);
            var groups = subscription.GetResourceGroups();
            return new ResolveOptionsResponse
            {
                Data = groups.Select(group => new Option(group.Id, group.Data.Name)).ToList(),
                Total = groups.Count(),
                Page = 1,
                Take = 20,
            };
        }

        if (request.Key == "resource")
        {
            if (!request.CurrentValues.ContainsKey("subscription"))
                return ResolveOptionsResponse.Empty;

            if (!request.CurrentValues.ContainsKey("resourceGroup"))
                return ResolveOptionsResponse.Empty;

            var subscriptionResourceId = new ResourceIdentifier(request.CurrentValues["subscription"]);
            var resourceGroupId = new ResourceIdentifier(request.CurrentValues["resourceGroup"]);
            var subscription = client.GetSubscriptionResource(subscriptionResourceId);
            var group = (await subscription.GetResourceGroupAsync(resourceGroupId.Name)).Value;
            var resources = group.GetGenericResourcesAsync();

            var list = new List<GenericResource>();

            await foreach(var resource in resources)
            {
                list.Add(resource);
            }
            return new ResolveOptionsResponse
            {
                Data = list.Select(item => new Option(item.Id, item.Data.Name)).ToList(),
                Total = list.Count(),
                Page = 1,
                Take = 20,
            };
        }

        return ResolveOptionsResponse.Empty;
    }
}
