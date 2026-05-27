## NSB 10.2.x: Convention based message handlers

### Problem: In certain configurations messages are processed twice (dual handler registry?)

I have currently seen four situations / configurations:

#### 1. "old" way -> Message proccessed once:
- Message Handler with `IHandleMessages<>`
- AssemblyScanner = default
- `[Handler]` attribute present, but no registration in `endpointConfiguration.Handlers.XYZAssembly.AddAll()`

#### 2. weird hybrid way -> Message processed once:
- Message Handler with `IHandleMessages<>`
- AssemblyScanner = default
- `[Handler]` attribute present AND registered with `endpointConfiguration.Handlers.XYZAssembly.AddAll()`

#### 3. convention based (default Scanner) -> Message processed twice:
- Message Handler without `IHandleMessages<>`
- AssemblyScanner = default
- `[Handler]` attribute present AND registered `endpointConfiguration.Handlers.XYZAssembly.AddAll()`


#### 4. convention based -> Message processed once:
- Message Handler without `IHandleMessages<>`
- `AssemblyScanner().Disabled = true`
- `[Handler]` attribute present AND registered `endpointConfiguration.Handlers.XYZAssembly.AddAll()`


My expectation would be for Option 3 to work the same way option 4 does. I was also "mildly surprised" option 2 worked correctly.

Reproducible with this repo using NSB 10.2.0

See also what claude has to say about it :) have not checked that, but could be on the right way (what-claude-says.md)
