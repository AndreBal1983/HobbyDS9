// Use an already installed Playwright module; no installation or network required.
// node capture_asset_gallery_01.cjs <gallery.html> <contact-sheet.png> [absolute-playwright-module]
const path = require('node:path');
const {pathToFileURL} = require('node:url');
const {chromium} = require(process.argv[4] || 'playwright');
(async () => {
  const browser = await chromium.launch({channel: 'chrome', headless: true, args: ['--disable-gpu']});
  try {
    const page = await browser.newPage({viewport: {width: 1440, height: 1080}, deviceScaleFactor: 1});
    await page.goto(pathToFileURL(path.resolve(process.argv[2])).href);
    await page.locator('img').evaluateAll(images => Promise.all(images.map(i => i.decode())));
    await page.screenshot({path: path.resolve(process.argv[3]), fullPage: true});
  } finally {
    await browser.close();
  }
})().catch(error => {process.stderr.write(String(error)); process.exitCode = 1;});
