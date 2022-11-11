# Sylvan.IPLocation

This project implements an optimized lookup for IP2Location binary databases. 
The primary changes over the IP2Location implementation is loading the entire 
database in memory, not using memory mapped files, replacing BigInteger with 
UInt128, and providing non-allocating accessors for data fields.

These benchmark results compare the lookup of 10k random IPv4 addresses from the DB5 database:

|             Method |         Mean | Ratio |  Allocated | Alloc Ratio |
|------------------- |-------------:|------:|-----------:|------------:|
| IP2LocLookup       | 592,421.2 us | 1.000 | 70394216 B |       1.000 |
| SylvanLookupSpan   |     799.2 us | 0.001 |        1 B |       0.000 |
| SylvanLookupString |   1,382.4 us | 0.002 |   794226 B |       0.011 |