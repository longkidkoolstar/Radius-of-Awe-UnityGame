# Procedural C# Texture & Art Generation Guide for AI Assistants

This guide is written for future AI coding assistants and developers working on **Radius of Awe**. It explains how to generate high-fidelity 2D sprites, textures, particle shapes, and visual UI elements **programmatically inside Unity’s memory at runtime** using pure C# code.

By following this guide, you can create gorgeous pixel art, glowing runic assets, and industrial mechanical blocks without needing to import large PNG files, keeping the project size at zero bytes.

---

## 💡 The Core Workflow

To create a sprite in memory at runtime:
1. **Initialize a canvas** (`Texture2D`) with a specific width, height, filter mode, and wrap mode.
2. **Paint pixels mathematically** using nested loops (`x` and `y`) and geometry formulas.
3. **Commit pixels** to the GPU using `texture.Apply()`.
4. **Wrap the texture into a Unity Sprite** using `Sprite.Create()` so it can be rendered by any `SpriteRenderer`.

---

## 🎨 Geometry Formulas for Pixel Painting

Future AI models should use these formulas to paint shapes inside a `for(int y...)` and `for(int x...)` grid:

### 1. Soft Circular Glows (Radial Fades)
Ideal for magical dust, background glows, and lighting masks.
* **Math:** Calculate the Euclidean distance from the center and apply an exponential curve to the transparency (alpha).
```csharp
float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
float alpha = Mathf.Clamp01(1f - (dist / maxRadius));
alpha = Mathf.Pow(alpha, 2.2f); // Power higher than 1 = softer edges
Color pixelColor = new Color(r, g, b, alpha);
```

### 2. Hollow Circular Rings
Ideal for shockwave ripples, circle boundaries, and circular gauges.
* **Math:** Check if the distance from the center is close to a target radius. The delta controls thickness.
```csharp
float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
float ringThickness = 1.5f;
float val = 1f - Mathf.Abs(dist - targetRadius) / ringThickness;
float alpha = Mathf.Clamp01(val);
```

### 3. Industrial Beveled Steel Plates
Ideal for mechanical crates, walls, and platforms.
* **Math:** Paint highlights on the top/left borders, shadows on the bottom/right borders, and rivets at coordinate offsets.
```csharp
bool isBorder = (x < 4 || x >= width - 4 || y < 4 || y >= height - 4);
bool isTopHighlight = (y >= height - 2 && x >= 4 && x < width - 4);
bool isBottomShadow = (y < 2 && x >= 4 && x < width - 4);

// Corner rivet calculation
bool isRivet = ((x == 12 || x == width - 12) && (y == 6 || y == height - 6));
```

### 4. Glowing circuits and Runes
Ideal for magical/cybertech objects in the Wonder Zone.
* **Math:** Use linear algebra slope equations to draw sharp circuit paths and diagonal carvings.
```csharp
// Diagonal line formula: y = mx + c
float slope = 3.5f;
float c_intercept = width * 0.12f;
bool isDiagonalLine = Mathf.Abs((x - c_intercept) - y * slope) < 1.0f;
```

---

## 🚀 Unity C# Template Code

Use this helper method pattern to safely create and package runtime sprites:

```csharp
private Sprite GenerateProceduralSprite(int width, int height, float pixelsPerUnit)
{
    // 1. Setup texture settings
    Texture2D tex = new Texture2D(width, height);
    tex.filterMode = FilterMode.Point; // Use FilterMode.Point for retro pixel art!
    tex.wrapMode = TextureWrapMode.Clamp;

    // 2. Loop and Paint
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            Color color = CalculatePixelColor(x, y, width, height);
            tex.SetPixel(x, y, color);
        }
    }

    // 3. Upload to GPU
    tex.Apply();

    // 4. Return packaged sprite (centered pivot)
    return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
}
```

---

## 🛠️ Direct File Creation (Alternative)

If you are an AI assistant with access to an **AI Image Generator Tool (`generate_image`)**:
You can bypass procedural code and directly create PNG assets in the filesystem:
1. Generate an image asset with a transparent background matching the target style (e.g. `"2D pixel art metal box crate asset on black transparent background"`).
2. Save it directly to the Unity project folder: `Assets/Textures/<name>.png`.
3. Unity will automatically import it. Use `AssetDatabase.ImportAsset` to load it in the editor.
