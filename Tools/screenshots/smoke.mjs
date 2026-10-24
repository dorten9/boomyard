// Pre-deploy smoke test: loads the fresh build's title screen and fails if the UI did not render.
// A broken build (UI shader missing) draws its whole interface magenta. Prints the page console as it goes,
// and gives up after a hard time limit instead of hanging the job.
// Usage: node smoke.mjs <baseUrl> <out.png>
import { chromium } from 'playwright';

const [base, out] = process.argv.slice(2);
const t0 = Date.now();
const stamp = () => ((Date.now() - t0) / 1000).toFixed(1).padStart(6) + 's';
setTimeout(() => { console.log(stamp(), 'smoke: hard time limit reached'); process.exit(3); }, 9 * 60 * 1000);

const browser = await chromium.launch({
  args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--enable-webgl'],
});
const page = await browser.newPage({ viewport: { width: 932, height: 430 } });
page.on('pageerror', (e) => console.log(stamp(), 'pageerror:', e.message));
page.on('console', (m) => console.log(stamp(), `[${m.type()}]`, m.text().slice(0, 300)));
await page.goto(`${base}/index.html`, { waitUntil: 'load', timeout: 60000 });
console.log(stamp(), 'page loaded');
await page.waitForFunction(() => window.unityInstance || window.unityError, null, { timeout: 300000, polling: 1000 });
const error = await page.evaluate(() => window.unityError || '');
if (error) { console.log(stamp(), 'unity error:', error); process.exit(1); }
console.log(stamp(), 'unity instance ready');
await page.waitForTimeout(10000);
await page.screenshot({ path: out, timeout: 120000 });
console.log(stamp(), 'screenshot taken');
await browser.close();
process.exit(0);
