using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace APSIM.Shared.Documentation
{
    /// <summary>A tag which displays an image.</summary>
    /// <remarks>
    /// todo: implement image captions:
    /// - Write tests first
    /// - Add caption property (private setter)
    /// - Add extra constructor
    /// - After appending image, if caption not null/empty,
    ///   bump renderer's figure count, and write figure
    ///   number and caption to new paragraph
    /// </remarks>
    public class Image : ITag
    {
        private SkiaSharp.SKImage raster;
        private string resourceName;

        /// <summary>The image to put into the doc.</summary>
        public System.Drawing.Image image;

        /// <summary>Unique name for image. Used to save image to temp folder.</summary>
        public string name;

        /// <summary>The image to put into the doc.</summary>
        public SkiaSharp.SKImage GetRaster(string relativePath)
        {
            if (raster != null)
                return raster;
            return LoadImage(resourceName, relativePath);
        }

        /// <summary>
        /// Attempt to load an image from the given URI. This can be a file path
        /// or a resource name (or part of a resource name).
        /// </summary>
        /// <param name="uri">Image URI.</param>
        /// <param name="imageSearchPath">The path on which to search for an image with the given filename.</param>
        public static SkiaSharp.SKImage LoadImage(string uri, string imageSearchPath)
        {
            if (string.IsNullOrWhiteSpace(uri))
                throw new InvalidOperationException("Unable to load image: resource name not specified");

            // Check if URI is an absolute path to an image.
            if (File.Exists(uri))
                return LoadFromFile(uri);

            // URI might be a relative path or just a filename without a path.
            // If so, search on the provided search path.
            string absolute = Path.Combine(imageSearchPath ?? "", uri);
            if (File.Exists(absolute))
                return LoadFromFile(absolute);

            // Otherwise try to find a resource file which matches the given URI.
            return LoadFromResource(uri);   
        }

        /// <summary>
        /// Read an image from disk.
        /// </summary>
        /// <param name="fileName">Absolute path to the file on disk.</param>
        public static SkiaSharp.SKImage LoadFromFile(string fileName)
        {
            return SkiaSharp.SKImage.FromEncodedData(fileName);
        }

        /// <summary>
        /// Load an image from the given resource name.
        /// Will attempt to locate the resource in various assemblies.
        /// </summary>
        /// <param name="resourceName">Resource file name.</param>
        public static SkiaSharp.SKImage LoadFromResource(string resourceName)
        {
            using (Stream stream = GetStreamFromResource(resourceName))
                return SkiaSharp.SKImage.FromEncodedData(stream);
        }

        /// <summary>
        /// Loads an image stream from the given resource name
        /// Will attempt to locate the resource in various assemblies.
        /// </summary>
        /// <param name="resourceName">Name of the resource</param>
        /// <returns>The resource as a stream</returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static Stream GetStreamFromResource(string resourceName)
        {
            foreach (Assembly assembly in GetAssemblies())
            {
                Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                    return stream;
                string fullName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.Contains(resourceName));
                if (fullName != null)
                    return assembly.GetManifestResourceStream(fullName);
            }
            throw new FileNotFoundException($"Unable to load image from resource name '{resourceName}': resource not found");
        }

        private static IEnumerable<Assembly> GetAssemblies()
        {
            return new string[]
            {
                "APSIM.Interop",
                "ApsimNG",
                "Models",
                "APSIM.Shared",
            }.Select(GetAssembly)
             .Where(a => a != null);
        }

        private static Assembly GetAssembly(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
        }

        /// <summary>
        /// Create an Image tag for a given image object.
        /// </summary>
        /// <param name="image">The image.</param>
        public Image(SkiaSharp.SKImage image) => raster = image;

        /// <summary>
        /// Create an Image tag from a resource name. The resource name
        /// can be just the file name (e.g. "AIBanner.png") or can be the
        /// full path including the assembly name (e.g.
        /// "ApsimNG.Resources.AIBanner.png).
        /// </summary>
        /// <param name="resource">Name of the resource.</param>
        public Image(string resource)
        {
            resourceName = resource;
        }
    }
}
