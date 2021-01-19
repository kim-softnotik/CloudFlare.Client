﻿using System.Linq;
using System.Threading.Tasks;
using CloudFlare.Client.Api.Display;
using CloudFlare.Client.Api.Parameters;
using CloudFlare.Client.Api.Parameters.Endpoints;
using CloudFlare.Client.Api.Zones.DnsRecord;
using CloudFlare.Client.Contexts;
using CloudFlare.Client.Enumerators;
using CloudFlare.Client.Test.Helpers;
using CloudFlare.Client.Test.TestData;
using FluentAssertions;
using Force.DeepCloner;
using Newtonsoft.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace CloudFlare.Client.Test.Zones
{
    public class DnsRecordUnitTests
    {
        private readonly WireMockServer _wireMockServer;
        private readonly ConnectionInfo _connectionInfo;

        public DnsRecordUnitTests()
        {
            _wireMockServer = WireMockServer.Start();
            _connectionInfo = new WireMockConnection(_wireMockServer.Urls.First()).ConnectionInfo;
        }

        [Fact]
        public async Task TestCreateDnsRecordAsync()
        {
            var zone = ZoneTestData.Zones.First();
            var dnsRecord = DnsRecordTestData.DnsRecords.First();
            var newZone = new NewDnsRecord
            {
                Name = dnsRecord.Name,
                Content = dnsRecord.Content,
                Priority = dnsRecord.Priority,
                Proxied = dnsRecord.Proxied,
                Ttl = dnsRecord.Ttl,
                Type = dnsRecord.Type
            };

            _wireMockServer
                .Given(Request.Create().WithPath($"/{ZoneEndpoints.Base}/{zone.Id}/{DnsRecordEndpoints.Base}").UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody(WireMockResponseHelper.CreateTestResponse(dnsRecord)));

            using var client = new CloudFlareClient(WireMockConnection.ApiKeyAuthentication, _connectionInfo);

            var created = await client.Zones.DnsRecords.AddAsync(zone.Id, newZone);

            created.Result.Should().BeEquivalentTo(dnsRecord);
        }

        [Fact]
        public async Task TestExportDnsRecordsAsync()
        {
            var zone = ZoneTestData.Zones.First();

            _wireMockServer
                .Given(Request.Create().WithPath($"/{ZoneEndpoints.Base}/{zone.Id}/{DnsRecordEndpoints.Base}/{DnsRecordEndpoints.Export}").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody(WireMockResponseHelper.CreateTestResponse(DnsRecordTestData.Export)));

            using var client = new CloudFlareClient(WireMockConnection.ApiKeyAuthentication, _connectionInfo);

            var export = await client.Zones.DnsRecords.ExportAsync(zone.Id);

            export.Result.Should().BeEquivalentTo(DnsRecordTestData.Export);
        }

        [Fact]
        public async Task TestGetDnsRecordsAsync()
        {
            var displayOptions = new DisplayOptions { Page = 1, PerPage = 20, Order = OrderType.Asc };
            var dnsRecordFilter = new DnsRecordFilter { Content = "127.0.0.1", Match = false, Name = "tothnet.hu", Type = DnsRecordType.A };

            var zone = ZoneTestData.Zones.First();

            _wireMockServer
                .Given(Request.Create()
                    .WithPath($"/{ZoneEndpoints.Base}/{zone.Id}/{DnsRecordEndpoints.Base}/")
                    .WithParam(Filtering.Page)
                    .WithParam(Filtering.PerPage)
                    .WithParam(Filtering.Order)
                    .WithParam(Filtering.Name)
                    .WithParam(Filtering.Content)
                    .WithParam(Filtering.DnsRecordType)
                    .UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody(WireMockResponseHelper.CreateTestResponse(DnsRecordTestData.DnsRecords)));

            using var client = new CloudFlareClient(WireMockConnection.ApiKeyAuthentication, _connectionInfo);

            var records = await client.Zones.DnsRecords.GetAsync(zone.Id, dnsRecordFilter, displayOptions);

            records.Result.Should().BeEquivalentTo(DnsRecordTestData.DnsRecords);
        }

        [Fact]
        public async Task TestGetDnsRecordDetailsAsync()
        {
            var zone = ZoneTestData.Zones.First();
            var record = DnsRecordTestData.DnsRecords.First();

            _wireMockServer
                .Given(Request.Create().WithPath($"/{ZoneEndpoints.Base}/{zone.Id}/{DnsRecordEndpoints.Base}/{record.Id}").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody(WireMockResponseHelper.CreateTestResponse(record)));

            using var client = new CloudFlareClient(WireMockConnection.ApiKeyAuthentication, _connectionInfo);

            var recordDetails = await client.Zones.DnsRecords.GetDetailsAsync(zone.Id, record.Id);

            recordDetails.Result.Should().BeEquivalentTo(record);
        }

        [Fact]
        public async Task TestScanDnsRecordsAsync()
        {
            var zone = ZoneTestData.Zones.First();

            _wireMockServer
                .Given(Request.Create().WithPath($"/{ZoneEndpoints.Base}/{zone.Id}/{DnsRecordEndpoints.Base}/{DnsRecordEndpoints.Scan}").UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody(WireMockResponseHelper.CreateTestResponse(DnsRecordTestData.DnsRecordScans.First())));

            using var client = new CloudFlareClient(WireMockConnection.ApiKeyAuthentication, _connectionInfo);

            var scanZone = await client.Zones.DnsRecords.ScanAsync(zone.Id);

            scanZone.Result.Should().BeEquivalentTo(DnsRecordTestData.DnsRecordScans.First());
        }

        [Fact]
        public async Task TestUpdateDnsRecordAsync()
        {
            var zone = ZoneTestData.Zones.First();
            var record = DnsRecordTestData.DnsRecords.First();
            var modified = new ModifiedDnsRecord
            {
                Name = "new.tothnet.hu",
            };

            _wireMockServer
                .Given(Request.Create().WithPath($"/{ZoneEndpoints.Base}/{zone.Id}/{DnsRecordEndpoints.Base}/{record.Id}").UsingPut())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody(x =>
                    {
                        var body = JsonConvert.DeserializeObject<ModifiedDnsRecord>(x.Body);
                        var response = DnsRecordTestData.DnsRecords.First(y => y.Id == x.PathSegments[3]).DeepClone();
                        response.Name = body.Name;

                        return WireMockResponseHelper.CreateTestResponse(response);
                    }));

            using var client = new CloudFlareClient(WireMockConnection.ApiKeyAuthentication, _connectionInfo);

            var update = await client.Zones.DnsRecords.UpdateAsync(zone.Id, record.Id, modified);

            update.Result.Should().BeEquivalentTo(record, opt => opt.Excluding(x => x.Name));
            update.Result.Name.Should().BeEquivalentTo("new.tothnet.hu");
        }

        [Fact]
        public async Task TestDeleteDnsRecordAsync()
        {
            var zone = ZoneTestData.Zones.First();
            var record = DnsRecordTestData.DnsRecords.First();
            var expected = new DnsRecord() { Id = record.Id };

            _wireMockServer
                .Given(Request.Create().WithPath($"/{ZoneEndpoints.Base}/{zone.Id}/{DnsRecordEndpoints.Base}/{record.Id}").UsingDelete())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody(WireMockResponseHelper.CreateTestResponse(expected)));

            using var client = new CloudFlareClient(WireMockConnection.ApiKeyAuthentication, _connectionInfo);

            var deleteCustomHostname = await client.Zones.DnsRecords.DeleteAsync(zone.Id, record.Id);

            deleteCustomHostname.Result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task TestImportDnsRecordAsync()
        {
            var zone = ZoneTestData.Zones.First();
            var record = DnsRecordTestData.DnsRecords.First();
            var file = FileHelper.CreateTempFile("test.txt");

            _wireMockServer
                .Given(Request.Create().WithPath($"/{ZoneEndpoints.Base}/{zone.Id}/{DnsRecordEndpoints.Base}/{DnsRecordEndpoints.Import}").UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody(WireMockResponseHelper.CreateTestResponse(DnsRecordTestData.DnsRecordImports.First())));

            using var client = new CloudFlareClient(WireMockConnection.ApiKeyAuthentication, _connectionInfo);

            var deleteCustomHostname = await client.Zones.DnsRecords.ImportAsync(zone.Id, file, false);
            file.Delete();

            deleteCustomHostname.Result.Should().BeEquivalentTo(DnsRecordTestData.DnsRecordImports.First());
        }
    }
}