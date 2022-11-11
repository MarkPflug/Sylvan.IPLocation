using BenchmarkDotNet.Attributes;
using System;
using System.Net;

namespace Benchmarks;

using static Sylvan.IPLocation.Column;

[MemoryDiagnoser]
[InProcess]
public class LookupBench
{
    const string Path = @"/data/IPDb/IP2LOCATION-LITE-DB11.IPV6.BIN/IP2LOCATION-LITE-DB11.IPV6.BIN";
    //const string Path = @"C:\data\IPDb\IP2LOCATION-LITE-DB5.IPV6.BIN";
    
    IP2Location.Component vb;
    IP2Location.Component vbMMF;
    Sylvan.IPLocation.Database db;
    
    string[] addrStrs;
    IPAddress[] addrs;
    const int count = 10000;

    public LookupBench()
    {
        vb = new IP2Location.Component();
        vb.Open(Path, false);

        vbMMF = new IP2Location.Component();
        vbMMF.Open(Path, true);
        db = new Sylvan.IPLocation.Database(Path);

        byte[] buf = new byte[4];
        Random r = new Random(1);
        this.addrs = new IPAddress[count];
        this.addrStrs = new string[count];
        for (int i = 0; i < count; i++)
        {
            r.NextBytes(buf);
            var addr = new IPAddress(buf);
            addrs[i] = addr;
            addrStrs[i] = addr.ToString();
        }
    }

    [Benchmark(Baseline = true)]
    public void IP2LocLookup()
    {
        foreach(var str in addrStrs)
        {
            var r = vb.IPQuery(str);
            var c = r.City;
            var rg = r.Region;
            var lat = r.Latitude;
            var lon = r.Longitude;
        }
    }

    [Benchmark]
    public void IP2LocMMFLookup()
    {
        foreach (var str in addrStrs)
        {
            var r = vbMMF.IPQuery(str);
            var c = r.City;
            var rg = r.Region;
            var lat = r.Latitude;
            var lon = r.Longitude;
        }
    }

    [Benchmark]
    public void SylvanLookupSpan()
    {
        foreach (var addr in addrs)
        {
            var r = db.Lookup(addr);
            var c = r.GetUtf8Bytes(City);
            var rg = r.GetUtf8Bytes(Region);
            var lat = r.GetFloat(Latitude);
            var lon = r.GetFloat(Longitude);
        }
    }

    [Benchmark]
    public void SylvanLookupString()
    {
        foreach (var addr in addrs)
        {
            var r = db.Lookup(addr);
            var c = r.GetString(City);
            var rg = r.GetString(Region);
            var lat = r.GetFloat(Latitude);
            var lon = r.GetFloat(Longitude);
        }
    }
}
