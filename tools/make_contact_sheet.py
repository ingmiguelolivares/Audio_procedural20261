from pathlib import Path
from PIL import Image, ImageDraw, ImageOps

folder = Path(r"C:\Users\ASUS\Documents\Unity\Audio_procedural20261\Temp\doc_render_project_word")
images = [Image.open(path).convert("RGB") for path in sorted(folder.glob("page-*.png"))]
thumbs = []

for index, image in enumerate(images, 1):
    thumbnail = ImageOps.contain(image, (360, 470))
    canvas = Image.new("RGB", (380, 510), "white")
    canvas.paste(thumbnail, ((380 - thumbnail.width) // 2, 20))
    draw = ImageDraw.Draw(canvas)
    draw.text((12, 488), f"Pagina {index}", fill="black")
    thumbs.append(canvas)

columns = 2
rows = (len(thumbs) + columns - 1) // columns
sheet = Image.new("RGB", (columns * 380, rows * 510), "white")

for index, thumbnail in enumerate(thumbs):
    sheet.paste(thumbnail, ((index % columns) * 380, (index // columns) * 510))

output = folder / "contact_sheet.png"
sheet.save(output)
print(output)
