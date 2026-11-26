// Tiny static server for the built Angular app with SPA fallback and /api proxy to the gateway.
// Used when Docker/nginx is not available:  node scripts/serve-web.mjs  (after `npm run build` in web/)
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../web/dist/web/browser');
const gateway = new URL(process.env.GATEWAY ?? 'http://localhost:8088');
const port = Number(process.env.PORT ?? 4200);
const types = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.ico': 'image/x-icon', '.svg': 'image/svg+xml', '.json': 'application/json' };

http.createServer((req, res) => {
  if (req.url.startsWith('/api/')) {
    const p = http.request({ host: gateway.hostname, port: gateway.port, path: req.url, method: req.method, headers: req.headers },
      r => { res.writeHead(r.statusCode, r.headers); r.pipe(res); });
    p.on('error', () => { res.writeHead(502); res.end('gateway unavailable'); });
    return req.pipe(p);
  }
  let file = path.join(root, decodeURIComponent(req.url.split('?')[0]));
  if (!file.startsWith(root) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) file = path.join(root, 'index.html');
  res.writeHead(200, { 'Content-Type': types[path.extname(file)] ?? 'application/octet-stream', 'Cache-Control': file.endsWith('.html') ? 'no-cache' : 'public, max-age=31536000' });
  fs.createReadStream(file).pipe(res);
}).listen(port, () => console.log(`web on http://localhost:${port}`));
