const fs = require('fs');
const path = require('path');
const { spawn } = require('child_process');
const net = require('net');

const ROOT = path.resolve(__dirname, '..');
const TAURI_CONF = path.join(ROOT, 'src-tauri', 'tauri.conf.json');

function findFreePort(start = 5180, end = 5299) {
  return new Promise((resolve, reject) => {
    let port = start;
    function tryPort() {
      if (port > end) return reject(new Error(`No free port in ${start}-${end}`));
      const server = net.createServer();
      server.once('error', () => { port++; tryPort(); });
      server.once('listening', () => {
        server.close(() => resolve(port));
      });
      server.listen(port, '127.0.0.1');
    }
    tryPort();
  });
}

function waitForServer(port, timeout = 30000) {
  return new Promise((resolve, reject) => {
    const start = Date.now();
    function check() {
      if (Date.now() - start > timeout) return reject(new Error('Timeout'));
      const req = net.connect(port, '127.0.0.1', () => {
        req.end();
        resolve();
      });
      req.on('error', () => setTimeout(check, 1000));
    }
    check();
  });
}

async function main() {
  const freePort = await findFreePort();
  console.log(`[tauri-dev] Free port: ${freePort}`);

  // Read and modify tauri.conf.json
  const rawConf = fs.readFileSync(TAURI_CONF, 'utf8');
  const conf = JSON.parse(rawConf);
  const originalDevUrl = conf.build.devUrl;
  conf.build.devUrl = `http://127.0.0.1:${freePort}`;

  fs.writeFileSync(TAURI_CONF, JSON.stringify(conf, null, 2));
  console.log(`[tauri-dev] Updated devUrl to ${conf.build.devUrl}`);

  let cleaned = false;
  function cleanup() {
    if (cleaned) return;
    cleaned = true;
    try {
      conf.build.devUrl = originalDevUrl;
      fs.writeFileSync(TAURI_CONF, JSON.stringify(conf, null, 2));
      console.log('[tauri-dev] Restored tauri.conf.json');
    } catch (e) {
      console.error('[tauri-dev] Failed to restore config:', e.message);
    }
  }

  process.on('exit', cleanup);
  process.on('SIGINT', () => { cleanup(); process.exit(1); });
  process.on('SIGTERM', () => { cleanup(); process.exit(1); });

  // Start Vite
  const vite = spawn('npm', ['run', 'dev', '--', '--port', String(freePort), '--strictPort'], {
    stdio: 'inherit',
    cwd: ROOT,
    shell: true,
  });

  try {
    await waitForServer(freePort);
    console.log(`[tauri-dev] Vite ready at http://127.0.0.1:${freePort}`);
  } catch (e) {
    console.error('[tauri-dev] Vite did not start:', e.message);
    vite.kill();
    cleanup();
    process.exit(1);
  }

  // Start Tauri
  const tauri = spawn('npx', ['tauri', 'dev'], {
    stdio: 'inherit',
    cwd: ROOT,
    shell: true,
  });

  tauri.on('exit', (code) => {
    vite.kill();
    cleanup();
    process.exit(code ?? 0);
  });
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
