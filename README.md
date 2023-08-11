# CluedIn.Connector.Http

Supports connections to post JSON to HTTP endpoints

### Features
- in memory batching

Eabling `BatchingSupported` flag during configuration allows sending entities in batches instead of one by one.
 and timeout interval in ms (if entities is less) Could be fine tuned by setting environment variables.
`CLUEDIN_APPSETTINGS__Streams_HttpConnector_BatchRecordsThreshold=50` - Number of entities in batch. Tested when equal or close to prefetch messages cound from Core, default value.
`CLUEDIN_APPSETTINGS__Streams_HttpConnector_BatchSyncInterval=60000` - Timeout interval in ms before sending batch, if there are less entities than threshold.

#### Example body structure with batching disabled
``` sh
{
  "TimeStamp": "2023-08-10T23:35:35.9479626+00:00",
  "VersionChangeType": "Added",
  "CorrelationId": "c1cf4432-d340-4305-a21a-330a1d866ffa",
  "Data": {
    "playerorigin": "Sandringham Dragons",
    "playerdob": "22-Jul-1992",
    "playerdisplayName": "Lyons, Jarryd",
    "playerweight": "84",
    "playerplayerId": "2012767797",
    "playerposition": "Midfield",
    "playerheight": "184",
    "Id": "f7520993-ee3c-540c-89e8-fe43d9e40087",
    "PersistHash": "imtif6nhdozuweig06s+oq==",
    "OriginEntityCode": "/Player#CluedInImporter(dataset-FA785663-DF1F-4023-A465-624224D34C01):2012767797",
    "EntityType": "/Player",
    "Codes": [
      "/Player#CluedIn(hash-sha1):9baebc48d9c408ec85aa248dcaa44177aa6c48a1",
      "/Player#CluedInImporter(dataset-FA785663-DF1F-4023-A465-624224D34C01):2012767797",
      "/Player#CluedInImporter(datasource-4):2012767797",
      "/Player#CluedInImporter(datasourcegroup-1):2012767797",
      "/Player#File Data Source:2012767797"
    ],
    "ChangeType": "Added"
  }
}
```

#### Example body structure with batching enabled
``` sh
[
  {
    "TimeStamp": "2023-08-10T17:16:27.0812437+00:00",
    "VersionChangeType": "Added",
    "CorrelationId": "67bb05ed-fe43-44af-a852-52d21be1a73e",
    "Data": {
      "playerposition": "Forward",
      "playerdisplayName": "Walker, Taylor",
      "playerdob": "25-Apr-1990",
      "playerweight": "100",
      "playerheight": "193",
      "playerplayerId": "2009877055",
      "playerorigin": "Broken Hill North",
      "Id": "096963cd-7079-5634-9d93-144cce693b90",
      "PersistHash": "b5d9yzbfehztqobfux2rwq==",
      "OriginEntityCode": "/Player#CluedInImporter(dataset-543C0ED2-F332-4064-A6BB-913D5E972284):2009877055",
      "EntityType": "/Player",
      "Codes": [
        "/Player#CluedIn(hash-sha1):6b9f0859fe1775fec491e63fba2ec1a3f973d69d",
        "/Player#CluedInImporter(dataset-543C0ED2-F332-4064-A6BB-913D5E972284):2009877055",
        "/Player#CluedInImporter(datasource-1002):2009877055",
        "/Player#CluedInImporter(datasourcegroup-1):2009877055",
        "/Player#File Data Source:2009877055"
      ],
      "ChangeType": "Added"
    }
  },
  {
    "TimeStamp": "2023-08-10T17:16:27.1656801+00:00",
    "VersionChangeType": "Added",
    "CorrelationId": "87c06b59-2308-4ddf-8174-7312e5af0ac4",
    "Data": {
      "playerposition": "Defender, Midfield",
      "playerdisplayName": "Mackay, David",
      "playerdob": "25-Jul-1988",
      "playerweight": "78",
      "playerheight": "181",
      "playerplayerId": "2008774238",
      "playerorigin": "Oakleigh Chargers",
      "Id": "4791d093-f8ec-5dfb-8fc3-dc4ebe17bbf5",
      "PersistHash": "qaajctrhthjzzuspsq11va==",
      "OriginEntityCode": "/Player#CluedInImporter(dataset-543C0ED2-F332-4064-A6BB-913D5E972284):2008774238",
      "EntityType": "/Player",
      "Codes": [
        "/Player#CluedIn(hash-sha1):48f09083feca17a523fdd0e7378433ca8c418e6a",
        "/Player#CluedInImporter(dataset-543C0ED2-F332-4064-A6BB-913D5E972284):2008774238",
        "/Player#CluedInImporter(datasource-1002):2008774238",
        "/Player#CluedInImporter(datasourcegroup-1):2008774238",
        "/Player#File Data Source:2008774238"
      ],
      "ChangeType": "Added"
    }
  }
]
```