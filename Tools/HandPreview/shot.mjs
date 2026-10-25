import { chromium } from 'playwright';
const b = await chromium.launch({ args: ['--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist'] });
const p = await b.newPage({ viewport: { width: 1200, height: 400 } });
p.on('pageerror', e => console.log('pageerror', e.message)); p.on('console', m => console.log(m.text()));
await p.goto(process.argv[2]); await p.waitForFunction(() => window.done, null, { timeout: 30000 });
await p.screenshot({ path: process.argv[3] });
await Promise.race([b.close(), new Promise(r => setTimeout(r, 3000))]); process.exit(0);
