import { expect, test } from '@playwright/test';

import { installSession, mockJellyfin } from './fixtures';

test('captures the responsive home contract', async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== 'chromium');
    await installSession(page);
    await mockJellyfin(page);

    for (const viewport of [
        { height: 900, name: 'home-1440', width: 1440 },
        { height: 768, name: 'home-1024', width: 1024 },
        { height: 844, name: 'home-390', width: 390 }
    ]) {
        await page.setViewportSize(viewport);
        await page.goto('/#/home');
        await expect(page.getByRole('heading', { name: 'Aurora' })).toBeVisible();
        await page.screenshot({
            animations: 'disabled',
            fullPage: true,
            path: `output/playwright/${viewport.name}.png`
        });
    }
});
