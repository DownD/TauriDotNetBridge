# Tauri DotNet Bridge

Enables implementation of Tauri commands in DotNet

[![NuGet](https://img.shields.io/nuget/v/TauriDotNetBridge.svg?label=NuGet)](https://www.nuget.org/packages/TauriDotNetBridge/)
[![crates.io](https://img.shields.io/crates/v/tauri-dotnet-bridge-host.svg?label=crates.io)](https://crates.io/crates/tauri-dotnet-bridge-host)

# Getting Started

The initial setup might seem a bit complicated, but it's actually quite simple.

First, create a new Tauri app as usual:

```bash
pnpm create tauri-app
pnpm i
```

In the root of your project, create a folder called `src-dotnet` alongside `src-tauri`.
Inside `src-dotnet`, create a .NET class library, e.g.:

```bash
cd src-dotnet
dotnet new classlib --name MyApp.TauriPlugIn
cd ..
```

Make sure the name of your class library ends with `.TauriPlugIn`.

Add the following PropertyGroup to the `.csproj` file:

```xml
<PropertyGroup>
  <OutputPath>..\..\src-tauri\target\$(Configuration)\dotnet</OutputPath>
  <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>
```

Add the `TauriDotNetBridge` and `Microsoft.Extensions.DependencyInjection` NuGet packages:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
  <PackageReference Include="TauriDotNetBridge" Version="2.0.0" />
</ItemGroup>
```

Create a plugin to register your controllers:

```csharp
public class PlugIn : IPlugIn
{
    public void Initialize(IServiceCollection services)
    {
        services.AddSingleton<HomeController>();
    }
}
```

Add a sample controller like this:

```csharp
using TauriDotNetBridge.Contracts;

public class LogInInfo
{
    public string? User { get; set; }
    public string? Password { get; set; }
}

public class HomeController
{
    public string Login(LogInInfo loginInfo)
    {
        return RouteResponse.Ok($"User '{loginInfo.User}' logged in successfully");
    }
}
```

Build the .NET project to verify the changes and ensure the C# DLLs are copied to the Tauri target folder.

In `src-tauri/Cargo.toml`, add:

```yaml
[dependencies]
tauri-dotnet-bridge-host = "0.7.0"
```

And then configure Tauri in your `main.rs` or `lib.rs` as follows:

```rust
use tauri_dotnet_bridge_host;

#[tauri::command]
fn dotnet_request(request: &str) -> String {
    tauri_dotnet_bridge_host::process_request(request)
}

fn main() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![dotnet_request])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
```

Run cargo build in `src-tauri` to verify the changes.

To call call your DotNet controllers in TypeScript, you can use the following code snippet:

```typescript
import { invoke } from '@tauri-apps/api/core'

export type PluginRequest = {
  controller: string
  action: string
  data?: object
}

export type RouteResponse<T> = {
  errorMessage?: string
  data?: T
}

export class TauriApi {
  public static async invokePlugin<T>(request: PluginRequest): Promise<T | null> {
    let response = (await invoke('dotnet_request', { request: JSON.stringify(request) })) as string
    let jsonResponse = JSON.parse(response) as RouteResponse<T>

    if (jsonResponse.errorMessage) throw new Error(jsonResponse.errorMessage)

    return jsonResponse.data ?? (null as T | null)
  }
}
```

And then use use it like this:

```typescript
async function login(user: string): Promise<string | null> {
  let userData = { user: user, pass: '<secret>' }

  return await TauriApi.invokePlugin<string>({ controller: 'home', action: 'login', data: userData })
}
```

In plain JavaScript you can call a controller like this:

```javascript
async function login() {
  const response = await invoke('dotnet_request', {
    request: JSON.stringify({ controller: 'home', action: 'login', data: { user: name.value, password: '<secret>' } })
  })
  greetMsg.value = JSON.parse(response).data
}
```

Finally, include the DotNet backend in the build process by changing the ```beforeDevCommand`` and ``beforeBuildCommand`` 
in ``src-tauri\tauri.conf.json`` like this

```json
{
  "build": {
    "beforeDevCommand": "dotnet build src-dotnet/src-dotnet.sln && pnpm dev",
    "beforeBuildCommand": "dotnet build -c Release src-dotnet/src-dotnet.sln && pnpm build",
  }
}
```

and then run the application

```bash
pnpm run tauri dev
```

and enjoy coding a tauri-app with DotNet backend 😊

# Packaging

Simply build the tauri package

```bash
pnpm run tauri build
```

To redistribute the package you can now copy the generated rust executable and the "dotnet" folder.

# Sample project

A sample project can be found here: https://github.com/plainionist/TauriNET

# Credits

Inspired by: https://github.com/RubenPX/TauriNET
