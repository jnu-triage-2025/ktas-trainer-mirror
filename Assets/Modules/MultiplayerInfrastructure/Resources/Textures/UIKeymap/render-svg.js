#!/usr/bin/env node
"use strict";

const fs = require("fs").promises;
const path = require("path");
const { pathToFileURL } = require("url");

let puppeteer;
try {
    puppeteer = require("puppeteer");
} catch (error) {
    console.error("puppeteer is required to render SVGs to PNGs");
    console.error("Install it with: npm install puppeteer");
    process.exit(1);
}

async function collectSvgs(dir, outSet) {
    const entries = await fs.readdir(dir, { withFileTypes: true });
    for (const entry of entries) {
        const resolved = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            await collectSvgs(resolved, outSet);
        } else if (entry.isFile() && resolved.toLowerCase().endsWith(".svg")) {
            outSet.add(resolved);
        }
    }
}

async function gatherTargets(rawTargets) {
    const targets = rawTargets.length ? rawTargets : ["."];
    const files = new Set();
    for (const raw of targets) {
        const resolved = path.resolve(raw);
        let stats;
        try {
            stats = await fs.stat(resolved);
        } catch (error) {
            console.warn(`Skipping ${resolved}: ${error.message}`);
            continue;
        }
        if (stats.isDirectory()) {
            await collectSvgs(resolved, files);
        } else if (stats.isFile() && resolved.toLowerCase().endsWith(".svg")) {
            files.add(resolved);
        }
    }
    return Array.from(files).sort();
}

async function renderFile(page, svgPath) {
    const url = pathToFileURL(svgPath).href;
    await page.goto(url, { waitUntil: "networkidle0" });
    const dims = await page.evaluate(() => {
        const svg = document.documentElement;
        const parseLength = (value) => {
            if (!value) {
                return 0;
            }
            const parsed = parseFloat(value);
            return Number.isFinite(parsed) ? parsed : 0;
        };
        const widthAttr = parseLength(svg.getAttribute("width"));
        const heightAttr = parseLength(svg.getAttribute("height"));
        if (widthAttr && heightAttr) {
            return { width: widthAttr, height: heightAttr };
        }
        if (svg.viewBox && svg.viewBox.baseVal) {
            const vb = svg.viewBox.baseVal;
            if (vb.width && vb.height) {
                return { width: vb.width, height: vb.height };
            }
        }
        const rect = svg.getBoundingClientRect();
        return { width: rect.width || 1, height: rect.height || 1 };
    });

    const width = Math.max(1, Math.round(dims.width));
    const height = Math.max(1, Math.round(dims.height));
    await page.setViewport({ width, height });
    const pngPath = path.join(path.dirname(svgPath), path.basename(svgPath, path.extname(svgPath)) + ".png");
    await page.screenshot({
        path: pngPath,
        omitBackground: true,
        clip: { x: 0, y: 0, width, height },
    });
}

(async () => {
    const rawTargets = process.argv.slice(2);
    const files = await gatherTargets(rawTargets);
    if (!files.length) {
        console.log("No SVG files found to render.");
        return;
    }

    const browser = await puppeteer.launch({ args: ["--allow-file-access-from-files"] });
    const page = await browser.newPage();
    try {
        for (const svgPath of files) {
            console.log(`Rendering ${svgPath}`);
            await renderFile(page, svgPath);
        }
    } finally {
        await browser.close();
    }
})().catch((error) => {
    console.error(error);
    process.exit(1);
});