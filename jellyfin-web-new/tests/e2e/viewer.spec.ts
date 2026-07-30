import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

import { installSession, mockJellyfin } from './fixtures';

test('signs in without exposing public users', async ({ page }) => {
    await mockJellyfin(page);
    await page.goto('/#/login');

    await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Create an account' })).toBeVisible();
    await expect(page.getByRole('button')).toHaveCount(1);

    await page.getByLabel('Username').fill('master');
    await page.getByLabel('Password').fill('correct horse battery staple');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('heading', { name: 'Who’s watching?' })).toBeVisible();
    const accessibility = await new AxeBuilder({ page }).analyze();
    expect(accessibility.violations.filter(violation => violation.impact === 'critical')).toEqual([]);
});

test('selects an isolated profile and opens a title', async ({ page }) => {
    await installSession(page);
    await mockJellyfin(page);
    await page.goto('/#/profiles');

    await page.getByRole('button', { name: /Master/ }).click();
    await expect(page.getByRole('heading', { name: 'Aurora' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Continue watching' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Top 10' })).toBeVisible();

    await page.getByRole('link', { name: 'More info' }).click();
    await expect(page).toHaveURL(/#\/title\/item-aurora$/);
    await expect(page.getByRole('heading', { name: 'Aurora' })).toBeVisible();

    const accessibility = await new AxeBuilder({ page }).analyze();
    expect(accessibility.violations.filter(violation => violation.impact === 'critical')).toEqual([]);
});
