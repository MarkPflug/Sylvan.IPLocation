# Sylvan.IPLocation

This project implements an optimized lookup for IP2Location binary databases. 
The primary changes over the IP2Location implementation is loading the entire 
database in memory, not using memory mapped files, replacing BigInteger with 
UInt128, and providing non-allocating accessors for data fields.

These benchmark results compare the lookup of 10k random IPv4 addresses from the DB5 database:

|             Method |       Mean |      Error |     StdDev | Ratio |       Gen0 |  Allocated | Alloc Ratio |
|------------------- |-----------:|-----------:|-----------:|------:|-----------:|-----------:|------------:|
|       IP2LocLookup | 676.620 ms | 12.8019 ms | 14.7427 ms | 1.000 | 19000.0000 | 80106568 B |       1.000 |
|    IP2LocMMFLookup |  70.641 ms |  1.3806 ms |  1.6435 ms | 0.105 |  7250.0000 | 30636784 B |       0.382 |
|   SylvanLookupSpan |   1.034 ms |  0.0068 ms |  0.0056 ms | 0.002 |          - |        2 B |       0.000 |
| SylvanLookupString |   1.444 ms |  0.0232 ms |  0.0206 ms | 0.002 |   189.4531 |   795610 B |       0.010 |