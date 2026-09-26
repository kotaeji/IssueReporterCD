"""Renders the app icon (assets/app-icon.svg design) to a multi-size Windows .ico.

Usage: python3 tools/make_icon.py   (requires Pillow)
Draws with Pillow instead of rasterizing the SVG so no native SVG library is needed;
keep the shapes below in sync with assets/app-icon.svg.
"""
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "src" / "IssueReporterCD" / "Assets" / "app.ico"
SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]
SUPERSAMPLE = 1024

NAVY = "#0B2A4A"
WHITE = "#FFFFFF"
CLIP = "#B9C8D8"
RED = "#DC2626"


def render(size: int) -> Image.Image:
    s = SUPERSAMPLE / 64  # design units are a 64x64 grid
    img = Image.new("RGBA", (SUPERSAMPLE, SUPERSAMPLE), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    def rect(x, y, w, h, r, fill):
        d.rounded_rectangle([x * s, y * s, (x + w) * s, (y + h) * s], radius=r * s, fill=fill)

    def circle(cx, cy, r, fill):
        d.ellipse([(cx - r) * s, (cy - r) * s, (cx + r) * s, (cy + r) * s], fill=fill)

    rect(0, 0, 64, 64, 14, NAVY)
    rect(15, 12, 30, 40, 4, WHITE)
    rect(23, 8, 14, 8, 2, CLIP)
    rect(20, 24, 20, 3, 1.5, NAVY)
    rect(20, 31, 16, 3, 1.5, NAVY)
    rect(20, 38, 12, 3, 1.5, NAVY)
    circle(46, 46, 12.5, NAVY)  # badge outline (stroke 3 around r=11)
    circle(46, 46, 9.5, RED)
    rect(44.5, 39, 3, 9, 1.5, WHITE)
    circle(46, 52, 1.8, WHITE)

    return img.resize((size, size), Image.LANCZOS)


def main() -> None:
    images = [render(size) for size in SIZES]
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    largest = images[-1]
    largest.save(OUTPUT, format="ICO", sizes=[(s, s) for s in SIZES], append_images=images[:-1])
    print(f"Wrote {OUTPUT.relative_to(ROOT)} ({', '.join(str(s) for s in SIZES)} px)")


if __name__ == "__main__":
    main()
