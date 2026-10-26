"""Rasterize the canonical rectangle-only SVG; preserve its exact geometry."""
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
svg = ET.parse(root / 'assets/ioi-symbol.svg').getroot()
scale = 4096 / 280
im = Image.new('RGBA', (4096, 4096), (0, 0, 0, 0))
draw = ImageDraw.Draw(im)
for node in svg:
    if node.tag.rsplit('}', 1)[-1] != 'rect':
        continue
    x, y, w, h = (float(node.attrib[a]) for a in ('x', 'y', 'width', 'height'))
    stroke = float(node.get('stroke-width', 0))
    if node.get('fill') != 'none':
        draw.rectangle((x*scale, y*scale, (x+w)*scale, (y+h)*scale), fill=node.get('fill'))
    if stroke:
        s = stroke / 2
        draw.rectangle(((x-s)*scale, (y-s)*scale, (x+w+s)*scale, (y+h+s)*scale), fill=node.get('stroke'))
        draw.rectangle(((x+s)*scale, (y+s)*scale, (x+w-s)*scale, (y+h-s)*scale), fill=(0, 0, 0, 0))
im = im.resize((1024, 1024), Image.Resampling.LANCZOS)
im.save(root / 'assets/IOIDesk.png')
im.save(root / 'assets/IOIDesk.ico', sizes=[(s,s) for s in (16,24,32,48,64,128,256)])
