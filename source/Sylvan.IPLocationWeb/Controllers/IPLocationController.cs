using Sylvan.IPLocation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.Common;
using System.Net;
using Sylvan.Data;


namespace IPLocationWeb.Controllers;

[ApiController]
[Route("[controller]")]
public class IPLocationController : ControllerBase
{
    private readonly ILogger<IPLocationController> logger;
    Database db;
    public IPLocationController(ILogger<IPLocationController> logger, Database db)
    {
        this.logger = logger;
        this.db = db;
    }

    [HttpGet]
    public object Get(string ip)
    {
        
        if (!IPAddress.TryParse(ip, out var addr))
        {
            return BadRequest();
        }
        var r = db.Lookup(addr);
        return new
        {
            City = r.GetString(Column.City),
            State = r.GetString(Column.Region),
        };
    }

    public class IPResponse
    {
        public string IP { get; }
        public string City { get; }
        public string State { get; }

        public IPResponse(string ip, string city, string state)
        {
            this.IP = ip;
            this.City = city;
            this.State = state;
        }
    }

    public class IPInfo
    {
        public string Address { get; private set; }
    }

    [HttpPost("getipdata")]
    public DbDataReader GetIpData(DbDataReader data)
    {
        var addressColumnName = "IPAddress";
        var addressNameHeader = this.HttpContext.Request.Headers["AddressColumnName"];
        if (addressNameHeader.Count > 0)
            addressColumnName = addressNameHeader[0];
        
        string city = string.Empty;
        string region = string.Empty;

        var idx = data.GetOrdinal(addressColumnName);

        var outputData =
            data
            .WithColumns(
                new CustomDataColumn<string>("city",
                r =>
                {

                    var addrStr = r.GetString(idx);
                    if (IPAddress.TryParse(addrStr, out var addr))
                    {
                        var record = db.Lookup(addr);
                        city = record.GetString(Column.City);
                        region = record.GetString(Column.Region);
                    }
                    return city;
                }),
                new CustomDataColumn<string>("region", r => region)

            );
        return outputData;
    }

    [HttpPost("getips")]
    public async IAsyncEnumerable<IPResponse> GetIps(IAsyncEnumerable<IPInfo> ips)
    {
        logger.LogWarning("Enter");
        int i = 0;
        int b = 0;
        await foreach (var ipInfo in ips)
        {
            var ip = ipInfo.Address;
            if (IPAddress.TryParse(ip, out var addr))
            {
                var r = db.Lookup(addr);
                var city = r.GetString(Column.City);
                var state = r.GetString(Column.Region);
                yield return new IPResponse(ip, city, state);
                if (i++ > 10000)
                {
                    logger.LogWarning(b++.ToString());
                    i = 0;
                }
            }
            else
            {
                logger.LogWarning("fail");
            }
        }
        logger.LogWarning("Exit");
    }
}
