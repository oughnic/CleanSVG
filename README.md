# CleanSVG - StarUML to Microsoft Word SVG Converter

A specialized utility for converting StarUML-generated SVG diagrams into a format compatible with Microsoft Word, specifically designed for ISO document preparation.

## Purpose

This tool addresses critical compatibility issues when importing StarUML SVG diagrams into Microsoft Word:

- **Removes Word-incompatible SVG attributes** that cause Word's parser to fail (e.g., `paint-order`, `vector-effect`)
- **Preserves UML class borders and structure** (shadows, fills, and path-based borders)
- **Flattens transform matrices** to ensure proper positioning in Word
- **Changes fonts** from Arial to Cambria (or custom font) for ISO document compliance
- **Adds viewBox attributes** for proper scaling
- **Cleans up problematic elements** while maintaining diagram integrity

## Background

StarUML exports SVG files using modern SVG 2.0 features that Microsoft Word doesn't fully support. Without processing, Word will either:
- Stop parsing after the first few elements
- Display diagrams with missing borders
- Fail to import the SVG entirely

This tool bridges the gap, making StarUML diagrams fully compatible with Word's SVG import capabilities.

## Features

✅ **Word Compatibility**
- Removes `paint-order` attributes that break Word's SVG parser
- Removes `vector-effect` and other unsupported attributes
- Cleans up empty `stroke-dasharray` attributes

✅ **Preserves Diagram Structure**
- Maintains UML class borders (rendered as `<path>` elements)
- Preserves shadows (rectangles with opacity)
- Keeps all structural elements intact

✅ **Transform Management**
- Applies and flattens common transform matrices
- Converts matrix transforms to absolute coordinates
- Ensures proper positioning in Word

✅ **Font Conversion**
- Converts Arial to Cambria (default for ISO documents)
- Supports custom target font via command-line parameter
- Maintains text positioning and styling

✅ **Safe Operation**
- Creates `.backup` files before modifying
- Validates XML structure
- Provides detailed processing logs

## Usage

### Basic Usage

Process all SVG files in the default directory:

```bash
CleanSVG.exe
```

### Custom Directory

Process SVG files in a specific directory:

```bash
CleanSVG.exe "C:\Path\To\Your\SVG\Files"
```

### Custom Font Conversion

Convert fonts to a specific typeface:

```bash
CleanSVG.exe "C:\Path\To\SVG\Files" --font "Times New Roman"
```

Or using short form:

```bash
CleanSVG.exe "C:\Path\To\SVG\Files" -f "Calibri"
```

### Skip Font Conversion

Process files without changing fonts:

```bash
CleanSVG.exe "C:\Path\To\SVG\Files" --no-font-change
```

## Command-Line Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `[directory]` | Path to directory containing SVG files | `C:\Users\Nicholas\OneDrive - Oughtibridge Ltd\ContSys\ContSys 2026 FDIS\Large Diagrams\svg` |
| `--font <name>` or `-f <name>` | Target font family for conversion | `Cambria` |
| `--no-font-change` | Skip font conversion entirely | (font change enabled) |

## Processing Details

The tool performs the following operations on each SVG file:

1. **ViewBox Addition**: Adds `viewBox` attribute if missing for proper scaling
2. **Debug Cleanup**: Removes any debug backgrounds from previous runs
3. **Transform Flattening**: Applies and removes transform matrices
4. **Attribute Cleaning**: Removes Word-incompatible attributes
5. **Rectangle Filtering**: Removes only transparent overlays (preserves structural rectangles)
6. **Font Conversion**: Changes font families (default: Arial → Cambria)
7. **Empty Group Removal**: Cleans up empty `<g>` elements
8. **Backup Creation**: Saves original as `filename.svg.backup`

## Output

The tool provides detailed console output:

```
Found 5 SVG files to process...

Processing: ClassDiagram1.svg
    Added viewBox: 0 0 800 600
    Fonts changed (Arial → Cambria): 23
    Transforms applied: 15
    Word-incompatible attributes cleaned: 8
    Empty elements removed: 3
  ✓ Modified and saved

=== Summary ===
Files processed: 5
Files modified: 5
Files unchanged: 0
```

## Requirements

- **.NET 10** or later
- Windows operating system
- Microsoft Word 2016 or later (for importing processed SVGs)

## Typical Workflow

1. **Design** your UML diagrams in StarUML
2. **Export** diagrams as SVG files
3. **Run CleanSVG** on the exported files
4. **Import** the processed SVG files into Microsoft Word
5. **Use** in your ISO documentation

## Technical Notes

### Why Cambria?

Cambria is the default font for ISO documents and technical documentation because:
- It's optimized for on-screen reading
- Includes comprehensive Unicode coverage
- Maintains clarity at small sizes
- Is a standard Microsoft Office font

### StarUML SVG Structure

StarUML generates SVG with the following structure for UML classes:
```xml
<g><!-- Shadow --><rect fill="#C0C0C0" fill-opacity="0.2" .../></g>
<g><!-- Background --><rect fill="#ffffff" stroke="none" .../></g>
<g><!-- Border --><path stroke="#000000" d="M ... Z" /></g>
<g><!-- Text --><text>ClassName</text></g>
```

This tool preserves all these elements while making them Word-compatible.

### Backup Files

Original files are preserved as `filename.svg.backup` (created only once, never overwritten). To restore originals:

```bash
# Windows PowerShell
Get-ChildItem *.backup | ForEach-Object { Copy-Item $_ ($_.Name -replace '\.backup$','') }
```

## Troubleshooting

### Word Still Not Importing

- Verify the SVG opens correctly in a web browser
- Check that the file isn't corrupted
- Ensure Word supports SVG import (Word 2016+)

### Borders Missing

- Check the console output for "Rectangles removed" count
- Verify the source SVG has `<path>` elements with `stroke` attributes
- Run with the latest version of CleanSVG

### Fonts Not Changing

- Verify the `--font` parameter is spelled correctly
- Check console output for "Fonts changed" count
- Ensure source SVG uses Arial or the expected source font

## License

This tool is provided as-is for processing StarUML SVG files for ISO documentation purposes.

## Author

Developed for the ContSys 2026 FDIS documentation project.

## Version History

### v1.0.0
- Initial release
- Word compatibility fixes
- Transform flattening
- Configurable font conversion
- Comprehensive backup system

---

**For ISO Documentation Teams**: This tool is specifically designed to maintain diagram quality and consistency when transitioning from design tools to formal documentation in Microsoft Word.