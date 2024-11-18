use tauri::Emitter;

#[tauri::command]
fn dotnet_request(request: &str) -> String {
    tauri_dotnet_bridge_host::process_request(request)
}

fn emit_event(event_name: &str, payload: &str) {
    // let app_handle = tauri::AppHandle::default();
    // app_handle
    //     .emit(event_name, payload)
    //     .expect(&format!("Failed to emit event {}", event_name));
    println!("Emitting event {} with payload {}", event_name, payload);
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .invoke_handler(tauri::generate_handler![dotnet_request])
        // .setup(|app| {
        //     // let app_handle = app.handle().clone();
        //     tauri_dotnet_bridge_host::register_emit(move |event_name, payload| {
        //         println!("Emitting event {} with payload {}", event_name, payload);
        //         // app_handle
        //         //     .emit(event_name, payload)
        //         //     .expect(&format!("Failed to emit event {}", event_name));
        //     });
        //     Ok(())
        // })
        .setup(|app| {
            tauri_dotnet_bridge_host::register_emit(emit_event);

            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
