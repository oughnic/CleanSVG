using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

class Program
{
    static void Main(string[] args)
    {
        string svgDirectory = Directory.GetCurrentDirectory(); // Default to current directory
        string targetFont = "Cambria";
        bool changeFonts = true;

        // Parse command-line arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--") || args[i].StartsWith("-"))
            {
                string arg = args[i].ToLower();
                
                if (arg == "--font" || arg == "-f")
                {
                    if (i + 1 < args.Length)
                    {
                        targetFont = args[i + 1];
                        i++; // Skip the next argument as it's the font name
                    }
                    else
                    {
                        Console.WriteLine("Error: --font requires a font name");
                        return;
                    }
                }
                else if (arg == "--no-font-change")
                {
                    changeFonts = false;
                }
                else if (arg == "--help" || arg == "-h" || arg == "-?")
                {
                    ShowHelp();
                    return;
                }
                else
                {
                    Console.WriteLine($"Unknown option: {args[i]}");
                    Console.WriteLine("Use --help for usage information");
                    return;
                }
            }
            else
            {
                // First non-option argument is the directory
                svgDirectory = args[i];
            }
        }

        if (!Directory.Exists(svgDirectory))
        {
            Console.WriteLine($"Directory not found: {svgDirectory}");
            Console.WriteLine("\nUsage: CleanSVG.exe [directory] [options]");
            Console.WriteLine("Use --help for more information");
            return;
        }

        Console.WriteLine("CleanSVG - StarUML to Microsoft Word SVG Converter");
        Console.WriteLine("===================================================");
        Console.WriteLine($"Directory: {svgDirectory}");
        if (changeFonts)
        {
            Console.WriteLine($"Target font: {targetFont}");
        }
        else
        {
            Console.WriteLine("Font conversion: Disabled");
        }
        Console.WriteLine();

        var svgFiles = Directory.GetFiles(svgDirectory, "*.svg");
        
        if (svgFiles.Length == 0)
        {
            Console.WriteLine("No SVG files found in the specified directory.");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"Found {svgFiles.Length} SVG files to process...\n");

        int filesProcessed = 0;
        int filesModified = 0;

        foreach (var filePath in svgFiles)
        {
            try
            {
                Console.WriteLine($"Processing: {Path.GetFileName(filePath)}");
                bool modified = CleanSvgFile(filePath, targetFont, changeFonts);

                filesProcessed++;
                if (modified)
                {
                    filesModified++;
                    Console.WriteLine($"  ✓ Modified and saved");
                }
                else
                {
                    Console.WriteLine($"  - No changes needed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error: {ex.Message}");
            }
        }

        Console.WriteLine($"\n=== Summary ===");
        Console.WriteLine($"Files processed: {filesProcessed}");
        Console.WriteLine($"Files modified: {filesModified}");
        Console.WriteLine($"Files unchanged: {filesProcessed - filesModified}");
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static void ShowHelp()
    {
        Console.WriteLine("CleanSVG - StarUML to Microsoft Word SVG Converter");
        Console.WriteLine("===================================================");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  CleanSVG.exe [directory] [options]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  directory              Path to directory containing SVG files");
        Console.WriteLine("                         (default: current directory)");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --font <name>, -f      Target font family for conversion");
        Console.WriteLine("                         (default: Cambria)");
        Console.WriteLine("  --no-font-change       Skip font conversion");
        Console.WriteLine("  --help, -h, -?         Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  CleanSVG.exe");
        Console.WriteLine("  CleanSVG.exe \"C:\\My SVG Files\"");
        Console.WriteLine("  CleanSVG.exe \"C:\\My SVG Files\" --font \"Times New Roman\"");
        Console.WriteLine("  CleanSVG.exe \"C:\\My SVG Files\" -f Calibri");
        Console.WriteLine("  CleanSVG.exe \"C:\\My SVG Files\" --no-font-change");
        Console.WriteLine("  CleanSVG.exe . --font \"Arial\"");
        Console.WriteLine();
        Console.WriteLine("This tool prepares StarUML SVG diagrams for import into Microsoft Word");
        Console.WriteLine("by removing incompatible attributes and optionally converting fonts.");
    }

    static bool CleanSvgFile(string filePath, string targetFont, bool changeFonts)
    {
        var doc = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
        XNamespace svg = "http://www.w3.org/2000/svg";

        bool modified = false;
        int rectRemoved = 0;
        int fontsChanged = 0;
        int emptyElementsRemoved = 0;
        int transformsApplied = 0;
        int attributesCleaned = 0;

        var svgRoot = doc.Root;
        if (svgRoot == null) return false;

        // Step 1: Add viewBox if missing
        if (svgRoot.Attribute("viewBox") == null)
        {
            var width = svgRoot.Attribute("width")?.Value;
            var height = svgRoot.Attribute("height")?.Value;

            if (width != null && height != null)
            {
                // Remove 'px' or other units if present
                width = Regex.Replace(width, @"[^\d\.]", "");
                height = Regex.Replace(height, @"[^\d\.]", "");

                svgRoot.SetAttributeValue("viewBox", $"0 0 {width} {height}");
                modified = true;
                Console.WriteLine($"    Added viewBox: 0 0 {width} {height}");
            }
        }

        // Step 2: Remove debug background if it exists (cleanup from previous runs)
        var debugBackground = svgRoot.Elements(svg + "rect")
            .FirstOrDefault(r => r.Attribute("id")?.Value == "debug-background");
        
        if (debugBackground != null)
        {
            debugBackground.Remove();
            modified = true;
            Console.WriteLine($"    Removed debug background");
        }

        // Step 3: Get the common transform matrix if present
        Matrix transformMatrix = null;
        var mainGroup = svgRoot.Elements(svg + "g").FirstOrDefault();
        if (mainGroup != null)
        {
            var firstChildGroup = mainGroup.Elements(svg + "g").FirstOrDefault();
            if (firstChildGroup != null)
            {
                var transform = firstChildGroup.Attribute("transform")?.Value;
                if (transform != null)
                {
                    transformMatrix = ParseTransformMatrix(transform);
                }
            }
        }

        // Step 4: Process all elements recursively
        ProcessElement(svgRoot, svg, transformMatrix, targetFont, changeFonts, 
                      ref rectRemoved, ref fontsChanged, ref emptyElementsRemoved, 
                      ref transformsApplied, ref attributesCleaned, ref modified);

        // Step 5: Remove empty <g> elements (do this after processing)
        RemoveEmptyGroups(svgRoot, svg, ref emptyElementsRemoved, ref modified);

        // If modified, save with backup
        if (modified)
        {
            // Create backup
            string backupPath = filePath + ".backup";
            if (!File.Exists(backupPath))
            {
                File.Copy(filePath, backupPath);
            }

            // Save cleaned version with proper formatting
            var settings = new System.Xml.XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = false,
                Encoding = System.Text.Encoding.UTF8
            };

            using (var writer = System.Xml.XmlWriter.Create(filePath, settings))
            {
                doc.Save(writer);
            }

            if (rectRemoved > 0)
                Console.WriteLine($"    Rectangles removed: {rectRemoved}");
            if (fontsChanged > 0)
                Console.WriteLine($"    Fonts changed (Arial → {targetFont}): {fontsChanged}");
            if (transformsApplied > 0)
                Console.WriteLine($"    Transforms applied: {transformsApplied}");
            if (attributesCleaned > 0)
                Console.WriteLine($"    Word-incompatible attributes cleaned: {attributesCleaned}");
            if (emptyElementsRemoved > 0)
                Console.WriteLine($"    Empty elements removed: {emptyElementsRemoved}");
        }

        return modified;
    }

    static void ProcessElement(XElement element, XNamespace svg, Matrix transformMatrix,
                               string targetFont, bool changeFonts,
                               ref int rectRemoved, ref int fontsChanged,
                               ref int emptyElementsRemoved, ref int transformsApplied,
                               ref int attributesCleaned, ref bool modified)
    {
        // Process child groups first
        var childGroups = element.Elements(svg + "g").ToList();
        foreach (var group in childGroups)
        {
            ProcessGroup(group, svg, transformMatrix, targetFont, changeFonts,
                        ref rectRemoved, ref fontsChanged, ref emptyElementsRemoved, 
                        ref transformsApplied, ref attributesCleaned, ref modified);
        }
    }

    static void ProcessGroup(XElement group, XNamespace svg, Matrix transformMatrix,
                            string targetFont, bool changeFonts,
                            ref int rectRemoved, ref int fontsChanged,
                            ref int emptyElementsRemoved, ref int transformsApplied,
                            ref int attributesCleaned, ref bool modified)
    {
        var rects = group.Elements(svg + "rect").ToList();
        var texts = group.Elements(svg + "text").ToList();
        var paths = group.Elements(svg + "path").ToList();
        var nestedTextGroups = group.Elements(svg + "g")
            .Where(g => g.Descendants(svg + "text").Any())
            .ToList();

        // Check if this group has a transform
        var groupTransform = group.Attribute("transform")?.Value;
        Matrix localMatrix = transformMatrix;

        if (groupTransform != null && transformMatrix != null)
        {
            var parsed = ParseTransformMatrix(groupTransform);
            if (parsed != null && parsed.Equals(transformMatrix))
            {
                // This is the common transform - apply it to children and remove
                ApplyTransformToChildren(group, svg, transformMatrix, ref transformsApplied, ref modified);
                group.Attribute("transform")?.Remove();
                modified = true;
            }
        }

        // Clean up Word-incompatible attributes from all elements
        CleanWordIncompatibleAttributes(group, ref attributesCleaned, ref modified);
        foreach (var rect in rects)
            CleanWordIncompatibleAttributes(rect, ref attributesCleaned, ref modified);
        foreach (var path in paths)
            CleanWordIncompatibleAttributes(path, ref attributesCleaned, ref modified);
        foreach (var text in texts)
            CleanWordIncompatibleAttributes(text, ref attributesCleaned, ref modified);

        // RECTANGLE REMOVAL LOGIC - Be extremely conservative to preserve class borders
        // Only remove rectangles that are DEFINITELY problematic overlays
        foreach (var rect in rects)
        {
            var fill = rect.Attribute("fill")?.Value;
            var stroke = rect.Attribute("stroke")?.Value;
            var strokeWidth = rect.Attribute("stroke-width")?.Value;
            var opacity = rect.Attribute("opacity")?.Value;
            var fillOpacity = rect.Attribute("fill-opacity")?.Value;

            bool shouldRemove = false;

            // ONLY remove rectangles in these specific cases:
            
            // Case 1: Explicitly transparent fill (8-digit hex with alpha=00)
            if (fill != null && fill.Length == 9 && fill.EndsWith("00", StringComparison.OrdinalIgnoreCase))
            {
                shouldRemove = true;
            }
            // Case 2: Element-level opacity is 0
            else if (opacity == "0" || fillOpacity == "0")
            {
                shouldRemove = true;
            }
            // DO NOT remove any other rectangles, including:
            // - White rectangles with stroke (class borders)
            // - White rectangles without explicit stroke="none"
            // - Any rectangle that might be structural
            // Note: Class borders in StarUML are drawn as separate <path> elements, not rect strokes

            if (shouldRemove)
            {
                rect.Remove();
                rectRemoved++;
                modified = true;
            }
        }

        // Change fonts if requested
        if (changeFonts)
        {
            var allTextElements = group.Descendants(svg + "text").ToList();
            foreach (var textElement in allTextElements)
            {
                var fontFamilyAttr = textElement.Attribute("font-family");

                if (fontFamilyAttr != null)
                {
                    string currentFont = fontFamilyAttr.Value;

                    if (currentFont.Equals("Arial", StringComparison.OrdinalIgnoreCase))
                    {
                        fontFamilyAttr.Value = targetFont;
                        fontsChanged++;
                        modified = true;
                    }
                }
            }
        }

        // Process nested groups recursively
        var nestedGroups = group.Elements(svg + "g").ToList();
        foreach (var nestedGroup in nestedGroups)
        {
            ProcessGroup(nestedGroup, svg, localMatrix, targetFont, changeFonts,
                        ref rectRemoved, ref fontsChanged, ref emptyElementsRemoved, 
                        ref transformsApplied, ref attributesCleaned, ref modified);
        }
    }

    static void CleanWordIncompatibleAttributes(XElement element, ref int attributesCleaned, ref bool modified)
    {
        // MS Word has issues with certain SVG attributes that are valid but cause parsing to fail
        var problematicAttributes = new[]
        {
            "paint-order",           // Not supported by Word
            "stroke-miterlimit",     // Should be "stroke-miterlimit" but sometimes causes issues
            "stroke-dasharray",      // Empty values can cause problems
            "vector-effect"          // Not supported by Word
        };

        foreach (var attrName in problematicAttributes)
        {
            var attr = element.Attribute(attrName);
            if (attr != null)
            {
                // Remove if empty or if it's a known problematic attribute
                if (string.IsNullOrWhiteSpace(attr.Value) || 
                    attrName == "paint-order" || 
                    attrName == "vector-effect")
                {
                    attr.Remove();
                    attributesCleaned++;
                    modified = true;
                }
            }
        }

        // Clean up stroke-dasharray if it's empty
        var dashArray = element.Attribute("stroke-dasharray");
        if (dashArray != null && string.IsNullOrWhiteSpace(dashArray.Value))
        {
            dashArray.Remove();
            attributesCleaned++;
            modified = true;
        }

        // Clean up duplicate Z commands in path data
        if (element.Name.LocalName == "path")
        {
            var d = element.Attribute("d");
            if (d != null && d.Value.Contains("Z Z"))
            {
                d.Value = d.Value.Replace("Z Z", "Z").Trim();
                modified = true;
            }
        }
    }

    static void ApplyTransformToChildren(XElement group, XNamespace svg, Matrix matrix,
                                        ref int transformsApplied, ref bool modified)
    {
        // Apply transform to rect elements
        foreach (var rect in group.Elements(svg + "rect"))
        {
            var x = double.Parse(rect.Attribute("x")?.Value ?? "0", CultureInfo.InvariantCulture);
            var y = double.Parse(rect.Attribute("y")?.Value ?? "0", CultureInfo.InvariantCulture);
            var width = double.Parse(rect.Attribute("width")?.Value ?? "0", CultureInfo.InvariantCulture);
            var height = double.Parse(rect.Attribute("height")?.Value ?? "0", CultureInfo.InvariantCulture);

            var transformed = matrix.Transform(x, y);

            rect.SetAttributeValue("x", transformed.X.ToString("F6", CultureInfo.InvariantCulture));
            rect.SetAttributeValue("y", transformed.Y.ToString("F6", CultureInfo.InvariantCulture));

            // Remove any transform attribute
            rect.Attribute("transform")?.Remove();

            transformsApplied++;
            modified = true;
        }

        // Apply transform to text elements
        foreach (var text in group.Elements(svg + "text"))
        {
            var x = double.Parse(text.Attribute("x")?.Value ?? "0", CultureInfo.InvariantCulture);
            var y = double.Parse(text.Attribute("y")?.Value ?? "0", CultureInfo.InvariantCulture);

            var transformed = matrix.Transform(x, y);

            text.SetAttributeValue("x", transformed.X.ToString("F6", CultureInfo.InvariantCulture));
            text.SetAttributeValue("y", transformed.Y.ToString("F6", CultureInfo.InvariantCulture));

            text.Attribute("transform")?.Remove();

            transformsApplied++;
            modified = true;
        }

        // Apply transform to path elements (including class borders!)
        foreach (var path in group.Elements(svg + "path"))
        {
            var d = path.Attribute("d")?.Value;
            if (d != null)
            {
                var newD = TransformPathData(d, matrix);
                if (newD != d)
                {
                    path.SetAttributeValue("d", newD);
                    transformsApplied++;
                    modified = true;
                }
            }

            path.Attribute("transform")?.Remove();
        }
    }

    static string TransformPathData(string pathData, Matrix matrix)
    {
        // Enhanced path transform - handles M, L, Z commands
        // This is critical for class border paths like "M x1 y1 L x2 y2 L x3 y3 L x4 y4 L x1 y1 Z"
        var commands = Regex.Matches(pathData, @"[MLZ]\s*[\d\.\s-]+|Z");
        var result = new System.Text.StringBuilder();

        foreach (Match cmd in commands)
        {
            var cmdStr = cmd.Value.Trim();
            if (cmdStr == "Z" || cmdStr.Length == 1)
            {
                result.Append(" Z");
                continue;
            }

            var parts = cmdStr.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            result.Append($" {parts[0]}");

            for (int i = 1; i < parts.Length; i += 2)
            {
                if (i + 1 < parts.Length)
                {
                    if (double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                        double.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                    {
                        var transformed = matrix.Transform(x, y);
                        result.Append($" {transformed.X.ToString("F6", CultureInfo.InvariantCulture)} {transformed.Y.ToString("F6", CultureInfo.InvariantCulture)}");
                    }
                }
            }
        }

        return result.ToString().Trim();
    }

    static void RemoveEmptyGroups(XElement element, XNamespace svg,
                                  ref int emptyElementsRemoved, ref bool modified)
    {
        var groups = element.Descendants(svg + "g").ToList();

        foreach (var group in groups)
        {
            // Be more careful - only remove groups that are truly empty
            // Don't remove groups that contain rectangles or paths (they might be class containers or borders)
            bool hasRectangles = group.Elements(svg + "rect").Any();
            bool hasPaths = group.Elements(svg + "path").Any();
            bool hasContent = group.HasElements && group.Elements().Any(e => !string.IsNullOrWhiteSpace(e.Value) || e.HasElements);
            
            // Only remove if truly empty AND has no structural elements
            if (!hasContent && !hasRectangles && !hasPaths)
            {
                group.Remove();
                emptyElementsRemoved++;
                modified = true;
            }
        }
    }

    static Matrix ParseTransformMatrix(string transform)
    {
        // Parse matrix(a, b, c, d, e, f) format
        var match = Regex.Match(transform, @"matrix\(([-\d\.]+)\s+([-\d\.]+)\s+([-\d\.]+)\s+([-\d\.]+)\s+([-\d\.]+)\s+([-\d\.]+)\)");

        if (match.Success)
        {
            return new Matrix(
                double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                double.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
                double.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture),
                double.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture)
            );
        }

        return null;
    }

    class Matrix
    {
        public double A { get; set; }
        public double B { get; set; }
        public double C { get; set; }
        public double D { get; set; }
        public double E { get; set; }
        public double F { get; set; }

        public Matrix(double a, double b, double c, double d, double e, double f)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
            F = f;
        }

        public (double X, double Y) Transform(double x, double y)
        {
            return (
                A * x + C * y + E,
                B * x + D * y + F
            );
        }

        public bool Equals(Matrix other)
        {
            if (other == null) return false;
            return Math.Abs(A - other.A) < 0.0001 &&
                   Math.Abs(B - other.B) < 0.0001 &&
                   Math.Abs(C - other.C) < 0.0001 &&
                   Math.Abs(D - other.D) < 0.0001 &&
                   Math.Abs(E - other.E) < 0.0001 &&
                   Math.Abs(F - other.F) < 0.0001;
        }
    }
}