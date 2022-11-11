using Sylvan.IPLocation;
using System;
using System.Net;
using System.Numerics;
using Xunit;

namespace IPLocation.Tests
{
    public class IPLocationTests
    {
        readonly string path;
        readonly Database db;

        public IPLocationTests()
        {
            this.path = @"/data/IPDb/IP2LOCATION-LITE-DB11.IPV6.BIN/IP2LOCATION-LITE-DB11.IPV6.BIN";
            this.db = new Database(path);
        }

        [Fact]
        public void Test1()
        {
            var r = db.Lookup("66.220.112.142");
            var c1 = r.GetString(Column.Country);
            var c2 = r.GetString(Column.City);
            var lat = r.GetFloat(Column.Latitude);
            var lon = r.GetFloat(Column.Longitude);
        }

        [Theory]
        [InlineData("0.0.0.0")]
        [InlineData("255.255.255.255")]
        public void Check(string ip)
        {
            var r = db.Lookup(ip);
            var c1 = r.GetString(Column.Country);
            var c2 = r.GetString(Column.City);
            var lat = r.GetFloat(Column.Latitude);
            var lon = r.GetFloat(Column.Longitude);
        }

        [Fact]
        public void Test2()
        {
            var be = BigInteger.Parse("50527214367656204350841161506971713536");
            byte[] buf = new byte[16];
            var beb = be.TryWriteBytes(buf.AsSpan(), out int len, true, true);
            var ip = new IPAddress(buf);
            var str = ip.ToString();

            var r = db.Lookup(ip);
            var c1 = r.GetString(Column.Country);
            var c2 = r.GetString(Column.City);
        }
    }
}
