// Screenshot timeline of a build for review: the title, the level in play, and a scripted explosion.
// The game reads ?shot= (GameController.RunShot). Usage: node shoot.mjs <baseUrl> <outDir>
import { chromium } from 'playwright';
import fs from 'fs';

const [base, out] = process.argv.slice(2);
fs.mkdirSync(out, { recursive: true });
setTimeout(() => { console.log('shoot: hard time limit reached'); process.exit(3); }, 17 * 60 * 1000);
const browser = await chromium.launch({
  args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--enable-webgl'],
});
const plans = {
  title: [8000],
  play: [9000, 12000],
  boom: [8000, 9500, 11000, 13000, 16000, 20000, 26000],
  city: [12000],
  spray: [16000],
};
for (const [shot, times] of Object.entries(plans)) {
  const page = await browser.newPage({ viewport: { width: 1280, height: 590 } });
  const log = [];
  page.on('console', (m) => log.push(`[${m.type()}] ${m.text()}`));
  page.on('pageerror', (e) => log.push(`[pageerror] ${e.message}`));
  await page.goto(`${base}/index.html?shot=${shot}`, { waitUntil: 'load', timeout: 60000 });
  await page.waitForFunction(() => window.unityInstance || window.unityError, null, { timeout: 300000, polling: 1000 });
  const t0 = Date.now();
  for (const t of times) {
    const wait = t - (Date.now() - t0);
    if (wait > 0) await page.waitForTimeout(wait);
    await page.screenshot({ path: `${out}/${shot}-${String(t).padStart(5, '0')}.png`, timeout: 120000 });
    fs.writeFileSync(`${out}/${shot}-console.txt`, log.join('\n'));
  }
  fs.writeFileSync(`${out}/${shot}-console.txt`, log.join('\n'));
  await Promise.race([page.close(), new Promise((r) => setTimeout(r, 5000))]);
}
await Promise.race([browser.close(), new Promise((r) => setTimeout(r, 5000))]);   // a stuck close must not hang the job
process.exit(0);
