use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::io::{BufRead, BufReader, Write};
use std::path::PathBuf;
use std::process::{Child, Command, Stdio};
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use tauri::{AppHandle, Emitter};
use tokio::sync::Mutex;

/// 单帧传感器快照
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SensorSnapshot {
    pub t: u64,
    pub s: HashMap<String, f64>,
    pub i: u64,
}

/// Sidecar 进程管理器
pub struct SidecarManager {
    child: Arc<Mutex<Option<Child>>>,
    running: Arc<AtomicBool>,
    max_retries: u32,
    retry_count: u32,
}

impl SidecarManager {
    pub fn new() -> Self {
        Self {
            child: Arc::new(Mutex::new(None)),
            running: Arc::new(AtomicBool::new(false)),
            max_retries: 3,
            retry_count: 0,
        }
    }

    pub async fn start(&mut self, interval_ms: u64) -> Result<(), String> {
        let sidecar_path = get_sidecar_path()?;

        let child = Command::new(&sidecar_path)
            .arg("--interval")
            .arg(interval_ms.to_string())
            .stdout(Stdio::piped())
            .stdin(Stdio::piped())
            .stderr(Stdio::piped())
            .spawn()
            .map_err(|e| format!("Failed to start sidecar: {}", e))?;

        *self.child.lock().await = Some(child);
        self.running.store(true, Ordering::SeqCst);
        self.retry_count = 0;
        Ok(())
    }

    pub async fn start_read_loop(&mut self, app: AppHandle, interval_ms: u64) {
        if !self.running.load(Ordering::SeqCst) {
            if let Err(e) = self.start(interval_ms).await {
                let _ = app.emit("hardware:error", format!("Sidecar start failed: {}", e));
                return;
            }
        }

        let child_lock = self.child.clone();
        let running = self.running.clone();
        let app_clone = app.clone();

        tokio::spawn(async move {
            let mut child_guard = child_lock.lock().await;
            let child = match child_guard.as_mut() {
                Some(c) => c,
                None => return,
            };

            let stdout = child.stdout.take().unwrap();
            let reader = BufReader::new(stdout);

            for line in reader.lines() {
                if !running.load(Ordering::SeqCst) {
                    break;
                }
                match line {
                    Ok(text) if !text.trim().is_empty() => {
                        if let Ok(snapshot) = serde_json::from_str::<SensorSnapshot>(&text) {
                            let _ = app_clone.emit("hardware:sensors", &snapshot);
                        }
                    }
                    Err(e) => {
                        let _ = app_clone.emit("hardware:error", format!("Sidecar read error: {}", e));
                        break;
                    }
                    _ => {}
                }
            }
        });
    }

    pub async fn restart(&mut self, interval_ms: u64) -> Result<(), String> {
        self.stop().await;
        if self.retry_count >= self.max_retries {
            return Err("Max retries exceeded".to_string());
        }
        self.retry_count += 1;
        self.start(interval_ms).await
    }

    pub async fn stop(&mut self) {
        self.running.store(false, Ordering::SeqCst);
        let mut guard = self.child.lock().await;
        if let Some(ref mut child) = guard.as_mut() {
            if let Some(stdin) = child.stdin.as_mut() {
                let _ = writeln!(stdin, "exit");
            }
            let _ = child.wait();
        }
        *guard = None;
    }

    pub async fn is_running(&self) -> bool {
        let mut guard = self.child.lock().await;
        guard.as_mut().map_or(false, |c| c.try_wait().ok().flatten().is_none())
    }
}

fn get_sidecar_path() -> Result<PathBuf, String> {
    let exe_dir = std::env::current_exe()
        .map_err(|e| format!("Cannot get current exe path: {}", e))?
        .parent()
        .ok_or("Cannot get parent dir")?
        .to_path_buf();

    let mut candidates: Vec<PathBuf> = Vec::new();

    // Dev mode: exe is at project/src-tauri/target/debug/app.exe
    if let Some(target_dir) = exe_dir.parent() {
        if let Some(tauri_dir) = target_dir.parent() {
            candidates.push(tauri_dir.join("binaries").join("HardwareSidecar.exe"));
        }
    }

    // Production: Tauri externalBin renames to HardwareSidecar-x86_64-pc-windows-msvc.exe
    candidates.push(exe_dir.join("HardwareSidecar-x86_64-pc-windows-msvc.exe"));
    candidates.push(exe_dir.join("HardwareSidecar.exe"));
    candidates.push(exe_dir.join("binaries").join("HardwareSidecar.exe"));

    // CWD-based: for when run from project root
    if let Ok(cwd) = std::env::current_dir() {
        candidates.push(cwd.join("src-tauri").join("binaries").join("HardwareSidecar.exe"));
        candidates.push(cwd.join("binaries").join("HardwareSidecar.exe"));
    }

    for path in &candidates {
        if path.exists() {
            return Ok(path.clone());
        }
    }

    Err(format!("HardwareSidecar.exe not found (searched {:?})", candidates))
}
