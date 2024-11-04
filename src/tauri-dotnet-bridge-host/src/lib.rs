use lazy_static::lazy_static;
use netcorehost::{hostfxr::AssemblyDelegateLoader, nethost, pdcstring::PdCString};
use std::env;

lazy_static! {
    static ref ASM: AssemblyDelegateLoader = {
        let hostfxr = nethost::load_hostfxr().unwrap();

        let exe_path = env::current_exe().expect("Failed to get the executable path");
        let dotnet_dir = exe_path
            .parent()
            .expect("Failed to get the executable directory")
            .join("dotnet");

        let runtime_config_path = PdCString::try_from(
            dotnet_dir
                .join("TauriDotNetBridge.runtimeconfig.json")
                .to_str()
                .expect("Failed to convert path to string"),
        )
        .expect("Failed to convert runtime config path to PdCString");

        let dll_path = PdCString::try_from(
            dotnet_dir
                .join("TauriDotNetBridge.dll")
                .to_str()
                .expect("Failed to convert path to string"),
        )
        .expect("Failed to convert DLL path to PdCString");

        println!("Using TauriDotNetBridge.runtimeconfig.json: {:?}", runtime_config_path);
        println!("Using TauriDotNetBridge.dll: {:?}", dll_path);

        let context = hostfxr
            .initialize_for_runtime_config(&runtime_config_path)
            .expect("Invalid runtime configuration");

        context
            .get_delegate_loader_for_assembly(dll_path)
            .expect("Failed to load DLL")
    };
}
