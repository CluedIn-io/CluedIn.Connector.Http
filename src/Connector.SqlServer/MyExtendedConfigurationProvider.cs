using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CluedIn.Core;
using CluedIn.Core.Providers.ExtendedConfiguration;

using CsvHelper;

namespace CluedIn.Connector.Http;

internal class MyExtendedConfigurationProvider : IExtendedConfigurationProvider
{
    public const string SourceName = "MyExtendedConfigurationProvider";

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
        if (request.Key == "country")
        {
            var records = GetCountries();

            await Task.CompletedTask;
            var found = records.SingleOrDefault(country => country.Alpha2.ToLowerInvariant() == request.Value);
            return new ResolveOptionByValueResponse
            {
                Option = found == null ? null : new Option(found.Alpha2.ToLowerInvariant(), found.Name)
            };
        }

        if (request.Key == "state")
        {
            var found = HandleState(request.CurrentValues).Data.SingleOrDefault(x => x.Value == request.Value);
            return new ResolveOptionByValueResponse
            {
                Option = found
            };
        }

        return new ResolveOptionByValueResponse
        {
            Option = null,
        };
    }

    private List<Country> GetCountries()
    {
        var currentType = this.GetType();
        var assembly = currentType.Assembly;
        var resourceName = $"{currentType.Namespace}.all.csv";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            var available = string.Join(',', assembly.GetManifestResourceNames());
            throw new InvalidOperationException($"Failed to read manifest resource stream for '{resourceName}'. Available resource streams are '{available}'.");
        }
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<Country>()
                    .ToList();
        return records;
    }

    public async Task<ResolveOptionsResponse> ResolveOptions(ExecutionContext context, ResolveOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (request.Source.EndsWith("notsupported"))
        {
            await Task.CompletedTask;
            return ResolveOptionsResponse.Empty;
        }
        if (request.Key.StartsWith("outputFormat"))
        {
            return HandleOutputFormat();
        }

        if (request.Key == "country")
        {
            return HandleCountry(request);
        }
        if (request.Key == "state")
        {
            return HandleState(request.CurrentValues);
        }

        return new ResolveOptionsResponse
        {
            Data = Enumerable
                .Range(0, 10)
                .Select(x => new Option(x.ToString(), " value: " + x.ToString()))
                .ToList(),
            Total = 10,
            Page = 0,
            Take = 20,
        };
    }

    private static ResolveOptionsResponse HandleState(IDictionary<string, string> currentValues)
    {
        if (currentValues.TryGetValue("country", out var country))
        {
            switch (country)
            {
                case "au":
                    return new ResolveOptionsResponse
                    {
                        Data = new[]
                        {
                                new Option("brisbane", "Brisbane"),
                                new Option("melbourne", "Melbourne"),
                                new Option("sydney", "Sydney"),
                            },
                        Total = 3,
                        Page = 0,
                        Take = 20,
                    };
                case "my":
                    return new ResolveOptionsResponse
                    {
                        Data = new[]
                        {
                                new Option("kualalumpur", "Kuala Lumpur"),
                                new Option("sabah", "Sabah"),
                                new Option("sarawak", "Sarawak"),
                            },
                        Total = 3,
                        Page = 0,
                        Take = 20,
                    };
                case "ph":
                    return new ResolveOptionsResponse
                    {
                        Data = new[]
                        {
                                new Option("metromanila", "Metro Manila"),
                                new Option("cebu", "Cebu"),
                                new Option("cavite", "Cavite"),
                            },
                        Total = 3,
                        Page = 0,
                        Take = 20,
                    };

            };
        }

        return ResolveOptionsResponse.Empty;
    }

    private ResolveOptionsResponse HandleCountry(ResolveOptionsRequest request)
    {
        var records = GetCountries();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            records = records
                    .Where(x => x.Name
                                .ToLowerInvariant()
                                .Contains(request.SearchTerm.ToLowerInvariant()))
                    .ToList();
        }
        var pageSize = request.Take;
        var pageNumber = request.Page;
        return new ResolveOptionsResponse
        {
            Data = records
                .Select(record => new Option(record.Alpha2.ToLowerInvariant(), record.Name))
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToList(),
            Total = records.Count,
            Page = pageNumber,
            Take = pageSize,
        };
    }

    private static ResolveOptionsResponse HandleOutputFormat()
    {
        return new ResolveOptionsResponse
        {
            Data = new[]
                        {
                    new Option("csv", "CSV"),
                    new Option("parquet", "Parquet"),
                    new Option("json", "JSON"),
                },
            Total = 3,
            Page = 0,
            Take = 20,
        };
    }
}
