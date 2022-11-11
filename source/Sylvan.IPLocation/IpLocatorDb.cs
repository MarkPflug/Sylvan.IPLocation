using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static Sylvan.IPLocation.Column;

namespace Sylvan.IPLocation;

public sealed class Database
{
    #region Database Types

    static readonly Column[][] Types;

    static Database()
    {
        Types = new Column[25][];
        Types[1] = new[] { Country };
        Types[2] = new[] { Country, Isp };
        Types[3] = new[] { Country, Region, City };
        Types[4] = new[] { Country, Region, City, Isp };
        Types[5] = new[] { Country, Region, City, Latitude, Longitude };
        Types[6] = new[] { Country, Region, City, Latitude, Longitude, Isp };
        Types[7] = new[] { Country, Region, City, Isp, Domain };
        Types[8] = new[] { Country, Region, City, Latitude, Longitude, Isp, Domain };
        Types[9] = new[] { Country, Region, City, Latitude, Longitude, ZipCode };
        Types[10] = new[] { Country, Region, City, Latitude, Longitude, ZipCode, Isp, Domain };
        Types[11] = new[] { Country, Region, City, Latitude, Longitude, ZipCode, Column.TimeZone };
        Types[12] = new[] { Country, Region, City, Latitude, Longitude, ZipCode, Column.TimeZone, Isp, Domain };
        Types[13] = new[] { Country, Region, City, Latitude, Longitude, Column.TimeZone, NetSpeed };
        Types[14] = new[] { Country, Region, City, Latitude, Longitude, Column.TimeZone, Isp, Domain, NetSpeed };
        Types[15] = new[] { Country, Region, City, Latitude, Longitude, ZipCode, Column.TimeZone, IddCode, AreaCode };
        Types[16] = new[] { Country, Region, City, Latitude, Longitude, ZipCode, Column.TimeZone, Isp, Domain, NetSpeed, IddCode, AreaCode };
        Types[17] = new[] { Country, Region, City, Latitude, Longitude, Column.TimeZone, NetSpeed, WeatherStationCode, WeatherStationName };
        Types[18] = new[] { Country, Region, City, Latitude, Longitude, ZipCode, Column.TimeZone, Isp, Domain, NetSpeed, IddCode, AreaCode, WeatherStationCode, WeatherStationName };
        Types[19] = new[] { Country, Region, City, Latitude, Longitude, Isp, Domain, Mcc, Mnc, MobileBrand };
        Types[20] = new[] { Country, Region, City, Latitude, Longitude, ZipCode, Column.TimeZone, Isp, Domain, NetSpeed, IddCode, AreaCode, WeatherStationCode, WeatherStationName, Mcc, Mnc, MobileBrand };
        Types[21] = new[] { Country, Region, City, Latitude, Longitude, Column.TimeZone, IddCode, AreaCode, Elevation };
        Types[22] = new[] { Country, Region, City, Latitude, Longitude, ZipCode, Column.TimeZone, Isp, Domain, NetSpeed, IddCode, AreaCode, WeatherStationCode, WeatherStationName, Mcc, Mnc, MobileBrand, Elevation };
        Types[23] = new[] { Country, Region, City, Latitude, Longitude, Isp, Domain, Mcc, Mnc, MobileBrand, UsageType };
        Types[24] = new[] { Country, Region, City, Latitude, Longitude, ZipCode, Column.TimeZone, Isp, Domain, NetSpeed, IddCode, AreaCode, WeatherStationCode, WeatherStationName, Mcc, Mnc, MobileBrand, Elevation, UsageType };
    }

    #endregion

    readonly int type;
    readonly int colWidth;
    readonly Column[] cols;
    readonly int[] colMap;

    readonly int ip4Count;
    readonly int ip4Base;
    readonly int ip6Count;
    readonly int ip6Base;

    readonly int[]? ip4Index;
    readonly int[]? ip6Index;

    readonly uint[] ip4Lookup;
    readonly UInt128[] ip6Lookup;

    int recordLength;
    readonly byte[] data;
    readonly int strOffset;
    readonly byte[] stringData;

    public Database(string filename)
    {
        using var stream = File.OpenRead(filename);
        var br = new BinaryReader(stream);
        this.type = br.ReadByte();
        if (type < 1 || type >= Types.Length)
        {
            throw new InvalidDataException();
        }
        cols = Types[type];
        this.colWidth = br.ReadByte();

        if (colWidth != cols.Length + 1)
        {
            throw new InvalidDataException();
        }

        colMap = new int[20];

        for (int i = 0; i < colMap.Length; i++)
        {
            colMap[i] = -1;
        }

        for (int i = 0; i < cols.Length; i++)
        {
            var v = (int)cols[i];
            colMap[v] = i;
        }

        var y = br.ReadByte();
        var m = br.ReadByte();
        var d = br.ReadByte();
        this.ip4Count = br.ReadInt32();
        this.ip4Base = br.ReadInt32();
        this.ip6Count = br.ReadInt32();
        this.ip6Base = br.ReadInt32();
        var ip4IndexBase = br.ReadInt32();
        var ip6IndexBase = br.ReadInt32();

        this.recordLength = (colWidth - 1) * 4;

        this.data = new byte[(ip4Count + ip6Count) * recordLength];
        this.ip4Lookup = new uint[ip4Count];
        this.ip6Lookup = new UInt128[ip6Count];
        this.strOffset = ip6Base + (ip6Count * (16 + (colWidth - 1) * 4));
        this.stringData = new byte[stream.Length - strOffset];

        var idx = 0;
        if (ip4IndexBase > 0)
        {
            this.ip4Index = LoadIndex(stream, ip4IndexBase - 1);
        }

        stream.Seek(ip4Base - 1, SeekOrigin.Begin);

        for (int i = 0; i < ip4Count; i++)
        {
            ip4Lookup[i] = br.ReadUInt32();
            stream.Read(data, idx, recordLength);
            idx += recordLength;
        }

        if (ip6IndexBase > 0)
        {
            this.ip6Index = LoadIndex(stream, ip6IndexBase - 1);
        }

        stream.Seek(ip6Base - 1, SeekOrigin.Begin);

        for (int i = 0; i < ip6Count; i++)
        {
            ip6Lookup[i] = Read128(stream);
            stream.Read(data, idx, recordLength);
            idx += recordLength;
        }

        stream.Seek(strOffset, SeekOrigin.Begin);
        stream.Read(stringData, 0, stringData.Length);
    }

    public readonly struct Result
    {
        readonly Database db;
        readonly int offset;

        internal Result(Database db, int offset)
        {
            this.db = db;
            this.offset = offset;
        }

        public bool HasValue(Column col)
        {
            return db.colMap[(int)col] != -1;
        }

        public string GetString(Column col)
        {
            var span = GetUtf8Bytes(col);
            return Encoding.UTF8.GetString(span);
        }

        public ReadOnlySpan<byte> GetUtf8Bytes(Column col)
        {
            if (!col.IsString()) throw new ArgumentException();
            var idx = db.colMap[(int)col];
            if (idx == -1)
            {
                return null;
            }
            var off = this.offset * db.recordLength;
            var strOffset = BitConverter.ToInt32(db.data, off + idx * 4);
            strOffset -= db.strOffset;
            if (strOffset < 0)
                return default;
            var len = db.stringData[strOffset];
            return db.stringData.AsSpan(strOffset + 1, len);
        }

        public float GetFloat(Column col)
        {
            if (!col.IsNumeric()) throw new ArgumentException();
            var idx = db.colMap[(int)col];
            if (idx == -1)
            {
                return 0f;
            }

            var off = this.offset * db.recordLength;
            return BitConverter.ToSingle(db.data, off + idx * 4);
        }
    }

    public Result Lookup(string addr)
    {
        var ip = IPAddress.Parse(addr);
        return Lookup(ip);
    }

    public Result LookupIPv4(ReadOnlySpan<byte> address)
    {
        if (address.Length != 4) throw new ArgumentException();

        var val4 = ReverseEndian(BitConverter.ToUInt32(address));
        return LookupIp4(val4);
    }

    public Result Lookup(IPAddress addr)
    {
        if (addr == null) throw new ArgumentNullException(nameof(addr));
        Span<byte> addrBytes = stackalloc byte[16];
        int len = 0;

        switch (addr.AddressFamily)
        {
            case AddressFamily.InterNetwork:
                if (!addr.TryWriteBytes(addrBytes, out len))
                {
                    throw new ArgumentException();
                }
                var val4 = ReverseEndian(BitConverter.ToUInt32(addrBytes.Slice(0, len)));
                return LookupIp4(val4);
            case AddressFamily.InterNetworkV6:
                if (addr.IsIPv4MappedToIPv6)
                {
                    addr = addr.MapToIPv4();
                    goto case AddressFamily.InterNetwork;
                }

                var bytes = addr.GetAddressBytes();
                var hi = ReverseEndian(BitConverter.ToUInt64(bytes, 0));
                var lo = ReverseEndian(BitConverter.ToUInt64(bytes, 8));
                UInt128 val6 = new UInt128(hi, lo);
                return LookupIp6(val6);
        }

        throw new ArgumentException(nameof(addr));
    }

    static uint ReverseEndian(uint value)
    {
        return BinaryPrimitives.ReverseEndianness(value);
    }

    static ulong ReverseEndian(ulong value)
    {
        return BinaryPrimitives.ReverseEndianness(value);
    }

    Result LookupIp4(uint value)
    {
        int lo = 0;
        int hi = ip4Count;

        if (ip4Index != null)
        {
            var idx = value >> 16;
            lo = ip4Index[idx];
            if (idx + 1 >= ip4Index.Length)
            {
                hi = ip4Lookup.Length - 1;
            }
            else
            {
                hi = ip4Index[idx + 1];
            }
        }

        while (lo <= hi)
        {
            var mid = (lo + hi) / 2;

            var ipFrom = ip4Lookup[mid];
            if (value < ipFrom)
            {
                hi = mid - 1;
                continue;
            }
            if (mid + 1 < ip4Lookup.Length && value >= ip4Lookup[mid + 1])
            {
                lo = mid + 1;
                continue;
            }
            return new Result(this, mid);
        }

        // this should not be reachable unless the 
        // database is corrupt
        throw new InvalidDataException();
    }

    Result LookupIp6(UInt128 value)
    {
        int lo = 0;
        int hi = ip6Count;

        if (ip6Index != null)
        {
            var idx = (int)(value >> (112));
            //var idx = value.Hi >> 48;
            lo = ip6Index[idx];
            //TODO: can this oob if we're in the last bucket?
            hi = ip6Index[idx + 1];
        }

        var colSize = 16 + (colWidth - 1) * 4;

        while (lo <= hi)
        {
            var mid = (lo + hi) / 2;

            var ipFrom = ip6Lookup[mid];
            var ipTo = ip6Lookup[mid + 1];
            if (value < ipFrom)
            {
                hi = mid - 1;
            }
            else if (value >= ipTo)
            {
                lo = mid + 1;
            }
            else
            {
                return new Result(this, ip4Count + mid);
            }
        }

        // this should not be reachable unless the 
        // database is corrupt
        throw new InvalidDataException();
    }

    UInt128 Read128(Stream stream)
    {
        Span<byte> rb = stackalloc byte[16];
        stream.Read(rb);

        var lo = BitConverter.ToUInt64(rb.Slice(0, 8));
        var hi = BitConverter.ToUInt64(rb.Slice(8));
        return new UInt128(hi, lo);
    }

    static int[] LoadIndex(Stream stream, int offset)
    {
        var idx = new int[0x10000];
        stream.Seek(offset, SeekOrigin.Begin);
        var br = new BinaryReader(stream);

        for (int i = 0; i < idx.Length; i++)
        {
            idx[i] = br.ReadInt32();
            br.ReadInt32();
        }
        return idx;
    }
}

