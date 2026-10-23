// Pre-deploy smoke test: loads the fresh build's title screen and fails if the UI did not render.
// A broken build (UI shader missing) draws its whole interface magenta.
// Usage: node smoke.mjs <baseUrl> <out.png>
import { chromium } from 'playwright';

const [base, out] = process.argv.slice(2);
const browser = await chromium.launch({
  args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--enable-webgl'],
});
const page = await browser.newPage({ viewport: { width: 932, height: 430 } });
page.on('pageerror', (e) => console.log('pageerror:', e.message));
page.on('console', (m) => { if (m.type() === 'error') console.log('console:', m.text()); });
await page.goto(`${base}/index.html`, { waitUntil: 'load', timeout: 60000 });
await page.waitForFunction(() => window.unityInstance || window.unityError, null, { timeout: 180000 });
const error = await page.evaluate(() => window.unityError || '');
if (error) { console.log('unity error:', error); process.exit(1); }
await page.waitForTimeout(8000);
await page.screenshot({ path: out });
await browser.close();
