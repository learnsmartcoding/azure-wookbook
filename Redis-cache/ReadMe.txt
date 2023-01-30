You should install package "StackExchange.Redis"
E.g. <PackageReference Include="StackExchange.Redis" Version="2.6.90" />

Also, you should have your Radis cache and its connection string in config file.

"ConnectionStrings": {
    "DbContext": "{add your connect string here}",
    "CacheConnection": "{add your connect string here}"
  }