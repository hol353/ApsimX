using APSIM.Shared.Utilities;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace APSIM.Interop.Mapping
{
    /// <summary>
    /// This class can render world maps.
    /// </summary>
    public static class MapRenderer
    {
        /// <summary>
        /// Static constructor to perform 1-time initialisation.
        /// </summary>
        static MapRenderer()
        {
        }

        /// <remarks>
        /// The world of mapping and GIS is a rather specialised and complex sub-field. Our needs here
        /// are fairly simple: we just want to be able to plot locations on a base map. But how are locations
        /// specified? Where do we get the map? What projection do we use? These remarks are intended to 
        /// (slightly) clarify what is going on.
        /// 
        /// We are using Mapsui to do the map rendering, which in turn uses BruTile to fetch a suitable base map. The basemap 
        /// tiles use a "projected" coordinate system, specifically EPSG 3857 (also known as Web Mercator); 
        /// the units in this system are (perhaps surpisingly) metres. However, the point data we wish to plot
        /// is expressed as latitude and longitude (using a "geographic" coordinate system, specifically EPSG 4326),
        /// with units of decimal degrees. Note that both are based on WGS84, so they have the same underlying
        /// model of the shape of the earth, but use vastly different units. 

        /// <summary>
        /// Indicates the ratio between steps when zooming.
        /// </summary>
        private const double zoomStepFactor = 1.5;

        /// <summary>
        /// Render the map as a <see cref="SkiaSharp.SKImage"/> of the specified size.
        /// </summary>
        /// <param name="map">Map tag to be rendered.</param>
        /// <param name="width">Width of the map in px.</param>
        public static SkiaSharp.SKImage ToImage(this MapTag map, int width)
        {
            Map exported = map.ToMapsuiMap();
            Navigator navigator = new Navigator();
            var center = SphericalMercator.FromLonLat(map.Center.Longitude, map.Center.Latitude);
            navigator.CenterOn(new MPoint(center.x, center.y));
            navigator.SetSize(width, width);
            // Compat check for old zoom units. Should really have used a converter...
            double zoom = map.Zoom - 1.0;
            if (zoom >= 60.0 || zoom < 0.0) // Convert any "old" zoom levels into whole-world maps
                zoom = 0.0;
            // map.Zoom = map.MaximumZoom / Math.Pow(MapRenderer.GetZoomStepFactor(), setValue);
            double resolution = (78271.51696401953125 * 512 / width) / Math.Pow(MapRenderer.GetZoomStepFactor(), zoom);
            navigator.ZoomTo(resolution);

            SkiaSharp.SKImage result = null;
            TileLayer osmLayer = exported.Layers.FindLayer("OpenStreetMap").FirstOrDefault() as TileLayer;

            Mapsui.Fetcher.DataChangedEventHandler changedHandler = (s, e) =>
            {
                var layer = (TileLayer)s;
                if (((!osmLayer.Busy && e.Error == null) || e.Error != null) && !e.Cancelled)
                {
                    if (e.Error != null)
                    // Try to handle failure to fetch Open Street Map layer
                    // by displaying the country outline layer instead
                    {
                        osmLayer.Enabled = false;
                        Layer countryLayer = exported.Layers.FindLayer("Countries").FirstOrDefault() as Layer;
                        if (countryLayer != null)
                        {
                            countryLayer.Enabled = true;
                            MSection mSection = new MSection(navigator.Viewport.ToExtent(), navigator.Viewport.Resolution);
                            countryLayer.RefreshData(new FetchInfo(mSection));
                            // Give the "country" layer time to be loaded, if necessary.
                            // Seven seconds should be way more than enough...
                            int nSleeps = 0;
                            while (countryLayer.Busy && nSleeps++ < 700)
                                System.Threading.Thread.Sleep(10);
                        }
                    }
                    MemoryStream bitmap = new Mapsui.Rendering.Skia.MapRenderer().RenderToBitmapStream(navigator.Viewport, exported.Layers, exported.BackColor);
                    bitmap.Seek(0, SeekOrigin.Begin);
                    result = SkiaSharp.SKImage.FromEncodedData(bitmap);
                }
            };

            if (osmLayer != null)
            {
                osmLayer.Enabled = true;
                osmLayer.AbortFetch();
                osmLayer.ClearCache();
                osmLayer.DataChanged += changedHandler;
                MSection mSection = new MSection(navigator.Viewport.ToExtent(), navigator.Viewport.Resolution);
                FetchInfo fetchInfo = new FetchInfo(mSection);
                osmLayer.RefreshData(fetchInfo);
            }
            // Allow ourselves up to 30 seconds to get a map.
            int nSleeps = 0;
            while (result == null && nSleeps++ < 3000)
                System.Threading.Thread.Sleep(10);

            if (result == null)
                throw new Exception("Cannot get map after 30 seconds from Open Street Map");
            osmLayer.DataChanged -= changedHandler;
            return result;
        }

        /// <summary>
        /// Create a <see cref="Map"/> representing this map object.
        /// </summary>
        /// <param name="map">A map to be exported/rendered.</param>
        public static Map ToMapsuiMap(this MapTag map)
        {
            Map result = InitMap();

            GenericCollectionLayer<List<IFeature>> markerLayer = new GenericCollectionLayer<List<IFeature>>
            {
                Name = "Markers"
            };
            Stream markerStream = APSIM.Shared.Documentation.Image.GetStreamFromResource("Marker.png");
            int bitmapId = BitmapRegistry.Instance.Register(markerStream);
            markerLayer.Style = new SymbolStyle { BitmapId = bitmapId, SymbolScale = 1.0, SymbolOffset = new Offset(0.0, 0.5, true) };

            foreach (var loc in map.Markers.Select(c => SphericalMercator.FromLonLat(c.Longitude, c.Latitude)))
                markerLayer.Features.Add(new GeometryFeature(new NetTopologySuite.Geometries.Point(loc.x, loc.y)));
            result.Layers.Add(markerLayer);

            return result;
        }


        /// <summary>
        /// Get the zoom step factor. This is the ratio of each zoom increment to
        /// the previous zoom increment.
        /// </summary>
        /// <returns></returns>
        public static double GetZoomStepFactor() => zoomStepFactor;

        /// <summary>
        /// Initialise the map component.
        /// </summary>
        private static Map InitMap()
        {
            Map result = new Map
            {
                CRS = "EPSG:3857",
                BackColor = Mapsui.Styles.Color.FromString("LightBlue")
            };

            Mapsui.Tiling.Layers.TileLayer osmLayer = OpenStreetMap.CreateTileLayer("APSIM Next Generation");
            if (osmLayer.TileSource is BruTile.Web.HttpTileSource)
            {
                BruTile.Cache.FileCache fileCache = new BruTile.Cache.FileCache(Path.Combine(Path.GetTempPath(), "OSM Map Tiles"), "png", new TimeSpan(100, 0, 0, 0));
                (osmLayer.TileSource as BruTile.Web.HttpTileSource).PersistentCache = fileCache;
            }
            result.Layers.Add(osmLayer);

            // This layer is used only as a sort of backup in case the BruTile download times out
            // or is otherwise unavailable.
            // It should normally be invisible, as it will be covered by the BruTile tile layer.
            string apsimx = PathUtilities.GetAbsolutePath("%root%", null);
            string shapeFileName = Path.Combine(apsimx, "ApsimNG", "Resources", "world", "countries.shp");
            if (File.Exists(shapeFileName))
            {
                Layer layWorld = new Layer("Countries");
                Mapsui.Nts.Providers.Shapefile.ShapeFile shapeFile = new Mapsui.Nts.Providers.Shapefile.ShapeFile(shapeFileName, true)
                {
                    CRS = "EPSG:4326"
                };
                ProjectingProvider provider = new ProjectingProvider(shapeFile)
                { 
                    CRS = "EPSG:3857" 
                };
                layWorld.DataSource = provider;
                layWorld.Style = new Mapsui.Styles.VectorStyle
                {
                    Outline = new Mapsui.Styles.Pen { Color = Mapsui.Styles.Color.Black },
                    Fill = new Mapsui.Styles.Brush { Color = Mapsui.Styles.Color.White }
                };
                layWorld.Style.Enabled = true;
                layWorld.FetchingPostponedInMilliseconds = 1;
                layWorld.Enabled = false;
                result.Layers.Insert(0, layWorld);
            }
            return result;
        }
    }
}
