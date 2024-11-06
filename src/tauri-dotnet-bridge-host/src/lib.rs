use lazy_static::lazy_static;
use netcorehost::{hostfxr::AssemblyDelegateLoader, nethost, pdcstr, pdcstring::PdCString};
use std::env;
use std::ffi::{c_char, CString};

lazy_static! {
    static ref ASM: AssemblyDelegateLoader = {
        let hostfxr = nethost::load_hostfxr().unwrap();

        let exe_path = env::current_exe().expect("Failed to get the executable path");
        let dotnet_dir = exe_path.parent().expect("Failed to get the executable directory").join("dotnet");

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

        let is_debug = cfg!(debug_assertions);
        if is_debug {
            println!("Using TauriDotNetBridge.runtimeconfig.json: {:?}", runtime_config_path.to_string_lossy());
            println!("Using TauriDotNetBridge.dll: {:?}", dll_path.to_string_lossy());
        }

        let context = hostfxr
            .initialize_for_runtime_config(&runtime_config_path)
            .expect("Invalid runtime configuration");

        let instance = context.get_delegate_loader_for_assembly(dll_path).expect("Failed to load DLL");

        let set_debug = instance
            .get_function_with_unmanaged_callers_only::<fn(is_debug: i32)>(
                pdcstr!("TauriDotNetBridge.Bridge, TauriDotNetBridge"),
                pdcstr!("SetDebug"),
            )
            .unwrap();

        set_debug(if is_debug { 1 } else { 0 });

        instance
    };
}

pub fn process_request(request: &str) -> String {
    let instance = &ASM;

    let process_request = instance
        .get_function_with_unmanaged_callers_only::<fn(text_ptr: *const u8, text_length: i32) -> *mut c_char>(
            pdcstr!("TauriDotNetBridge.Bridge, TauriDotNetBridge"),
            pdcstr!("ProcessRequest"),
        )
        .unwrap();

    let response_ptr = process_request(request.as_ptr(), request.len() as i32);

    let response = unsafe { CString::from_raw(response_ptr) };

    format!("{}", response.to_string_lossy())
}
