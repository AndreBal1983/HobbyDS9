"""Package browser-design captures into a gallery, contact sheets and review ZIP.

Composes existing screenshots only. No image generation, engine, model or game API.
"""
from pathlib import Path
import csv
import hashlib
import json
import math
import textwrap
import zipfile
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "artifacts/art/coherent-interface"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def font(size):
    return ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", size)


def sheet(screens, name, title, subtitle, columns=4):
    width, thumb_h, caption_h = 400, 225, 76
    rows = math.ceil(len(screens) / columns)
    canvas = Image.new("RGB", (columns * width + 32, rows * (thumb_h + caption_h + 12) + 110), "#050a10")
    draw = ImageDraw.Draw(canvas)
    draw.text((16, 12), title, fill="#efb16b", font=font(25))
    draw.text((16, 48), subtitle, fill="#a9b8c8", font=font(15))
    for index, item in enumerate(screens):
        x, y = 16 + index % columns * width, 90 + index // columns * (thumb_h + caption_h + 12)
        image = Image.open(OUT / ("standard-" + item["id"] + ".png")).convert("RGB")
        image.thumbnail((width - 12, thumb_h), Image.Resampling.LANCZOS)
        canvas.paste(image, (x, y))
        draw.rectangle((x, y, x + width - 12, y + thumb_h), outline="#45566a", width=1)
        lines = textwrap.wrap(f"{index+1:02d}  {item['title']}", width=44)[:2]
        draw.multiline_text((x + 5, y + thumb_h + 7), "\n".join(lines), fill="#eeeaf2", font=font(16), spacing=2)
        draw.text((x + 5, y + thumb_h + 52), item["statusLabel"], fill="#b9ace6", font=font(12))
    canvas.save(OUT / name, optimize=True)


def main():
    evidence = json.loads((OUT / "coverage.json").read_text(encoding="utf-8"))
    checks = json.loads((OUT / "interaction-checks.json").read_text(encoding="utf-8"))
    screens = json.loads((ROOT / "tools/interface-mockups/screens.json").read_text(encoding="utf-8"))
    assert checks["mode"] == "FULL" and not checks["errors"]
    assert checks["captures"] == len(screens) * 3 == len(evidence["captures"])
    assert {s["id"] for s in screens} == {c["id"] for c in evidence["captures"]}
    for source in evidence["sourceHashes"]:
        assert sha(ROOT / "tools/interface-mockups" / source["file"]) == source["sha256"]
    for file in ("app.js", "style.css"):
        assert sha(OUT / file) == sha(ROOT / "tools/interface-mockups" / file)
    for capture in evidence["captures"]:
        assert sha(OUT / capture["file"]) == capture["sha256"]
        assert Image.open(OUT / capture["file"]).size == (capture["width"], capture["height"])
    with (OUT / "coverage.csv").open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(["ID", "Screen/state", "Family", "Capability status", "Scope", "1280x720", "1920x1080", "3440x1440"])
        for s in screens:
            writer.writerow([s["id"], s["title"], s["family"], s["statusLabel"], s["scope"], *[n + "-" + s["id"] + ".png" for n in ("narrow", "standard", "wide")]])
    sheet(screens, "contact-sheet.png", "HobbyDS9 / complete interface concepts", f"{len(screens)} distinct screens and states; all at3 sizes. I=implemented scoped capability, A=agreed future, P=proposed. Layouts/data remain concepts.")
    for family in dict.fromkeys(s["family"] for s in screens):
        subset = [s for s in screens if s["family"] == family]
        sheet(subset, "family-" + family.lower() + ".png", "HobbyDS9 / " + family, "Feature-specific layouts and state boundaries; concepts, not Unity screenshots.")
    journey_ids = ["start", "setup-galaxy", "map-overview", "map-sector", "world", "science", "engineering", "production", "queue", "map-route", "strategic-review", "battle-orders", "battle-replay", "battle-result", "battle-aftermath", "battle-fresh"]
    by_id = {s["id"]: s for s in screens}
    journey = [by_id[i] for i in journey_ids]
    sheet(journey, "player-journey.png", "A coherent proposed player journey", "Composite design across scoped/future profiles; no claim of one implemented campaign or continuously simulated fixture.")
    sheet([by_id[i] for i in ["map-overview", "map-hover", "map-sector", "map-route", "strategic-review", "map-travel", "frontier", "map-unavailable"]], "map-storyboard.png", "Map interactions / authority and uncertainty", "Camera, hover and selection are presentation. Route/arrival/knowledge come from authority; surveys remain future design.")
    sheet([by_id[i] for i in ["battle-lobby", "battle-orders", "battle-review", "battle-waiting", "battle-replay", "battle-hidden", "battle-reveal", "battle-result", "battle-aftermath", "battle-fresh", "battle-no-survivors", "victory"]], "battle-storyboard.png", "Fleet orders / cinematic resolution / strategic consequences", "One fleet one command unit; private opponent orders; filtered playback; persistent stocks and explicit once-only aftermath.")
    items = "".join(f'<a href="gallery.html#{s["id"]}"><img src="standard-{s["id"]}.png" alt="{s["title"]}"><b>{i+1:02d} · {s["title"]}</b><small>{s["statusLabel"]}<br>{s["scope"]}</small></a>' for i, s in enumerate(journey))
    (OUT / "journey.html").write_text('<!doctype html><html lang="en"><meta charset="utf-8"><title>HobbyDS9 player journey</title><style>body{background:#050a10;color:#eeeaf2;font:18px Segoe UI;margin:30px}h1{color:#efb16b}p{max-width:1100px;line-height:1.5;color:#a9b8c8}a{color:#9fd8fb}.grid{display:grid;grid-template-columns:repeat(2,1fr);gap:20px}.grid a{border:1px solid #45566a;border-radius:10px;overflow:hidden;text-decoration:none;display:flex;flex-direction:column;background:#101b29}img{width:100%}b,small{padding:10px 16px}small{color:#b9ace6;line-height:1.5}</style><h1>From the frontier to the next decision</h1><p>Proposed composite journey: Start → generated galaxy/map → selected world → Science/Engineering → paid production queue → fleet route → combat planning/3D result projection → aftermath/fresh orders. This crosses separate implemented and future profiles. Generated campaign setup and3D playback are proposals. The production view includes an already-paid queue and an honest insufficient-funds state for a new purchase; no payment is simulated. Click any stage for its three-resolution review and interactive concept.</p><p><a href="gallery.html">Open complete94-view gallery</a> · <a href="contact-sheet.png">Complete contact sheet</a> · <a href="player-journey.png">Journey contact sheet</a></p><div class="grid">' + items + '</div></html>', encoding="utf-8")
    gallery = (OUT / "gallery.html").read_text(encoding="utf-8")
    if 'href="journey.html">Recommended player journey' not in gallery:
        gallery = gallery.replace('<a href="coverage.json">', '<a href="journey.html">Recommended player journey</a><a href="coverage.json">')
    (OUT / "gallery.html").write_text(gallery, encoding="utf-8")
    summary = {"status":"Browser concept review; no Unity/gameplay/native acceptance", "screens":len(screens),"resolutionCaptures":len(evidence["captures"]),"themeComparisons":4,"interactionGroups":len(checks["interactions"]),"sourcesVerified":True,"imageHashesVerified":True,"entry":"gallery.html","journey":"journey.html"}
    (OUT / "delivery.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    zip_path = OUT.parent / "HobbyDS9-Coherent-Interface-Mockups.zip"
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED, compresslevel=4) as archive:
        for file in sorted(OUT.rglob("*")):
            if file.is_file():
                archive.write(file, file.relative_to(OUT))
    summary["zip"] = str(zip_path)
    summary["zipBytes"] = zip_path.stat().st_size
    summary["zipSha256"] = sha(zip_path)
    preview = OUT.parent / "HobbyDS9-Interactive-Interface-Preview.zip"
    with zipfile.ZipFile(preview, "w", zipfile.ZIP_DEFLATED) as archive:
        for name in ("index.html", "app.js", "style.css", "asset-manifest.json", "interaction-checks.json", "gallery-checks.json"):
            if (OUT / name).exists():
                archive.write(OUT / name, name)
        for file in (OUT / "assets").iterdir():
            archive.write(file, "assets/" + file.name)
        archive.writestr("START-HERE.txt", "Open index.html in a browser. Choose any of94screens in the top design-review picker. Resize to1280/1920/3440. All data/layouts are concepts; no campaign API or save access. This lightweight preview omits PNG snapshots; the full gallery bundle includes them.")
    summary["previewZip"] = str(preview)
    summary["previewZipBytes"] = preview.stat().st_size
    summary["previewZipSha256"] = sha(preview)
    (OUT.parent / "coherent-interface-delivery.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary))


if __name__ == "__main__":
    main()
