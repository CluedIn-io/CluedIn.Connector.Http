using CsvHelper.Configuration.Attributes;

namespace CluedIn.Connector.Http;

class Country
{
    [Name("name")]
    public string Name { get; set; }

    [Name("alpha-2")]
    public string Alpha2 { get; set; }
}
