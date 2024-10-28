using APSIM.Interop.Mapping;
using APSIM.Shared.Utilities;
using Gtk;
using GLib;
using UserInterface.Interfaces;
using Mapsui;
using Mapsui.Layers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Utility;
using ApsimCoordinate = Models.Mapping.Coordinate;
using MapTag = APSIM.Interop.Mapping.MapTag;
using Mapsui.Extensions;

namespace UserInterface.Views
{
    

    /// <remarks>
    /// This view is intended to diplay sites on a map. For the most part, in works, but it has a few flaws
    /// and room for improvement. 
    /// 
    /// Probably the main flaw is that maps are often very slow to render, as the basemap needs
    /// to be downloaded.
    /// 
    /// Another flaw (a problem with Mapsui) is that it doesn't know how to "wrap" the map at the antimeridion 
    /// (International Date Line). This makes it impossible to produce a Pacific-centered map.
    /// 
    /// One enhancement that should be fairly easy to implement would be to allow the user to select the basemap.
    /// Currently it's just using OpenStreetMaps, but Bing maps and several others are readily available. The user
    /// could be presented with a drop-down list of alternative.
    /// 
    /// </remarks>
    public class MapView : ViewBase, IMapView
    {
        /// <summary>
        /// Width of the map as shown in the GUI. I'm setting
        /// this to 718 to match the default page width of the autodocs
        /// documents.
        /// </summary>
        /// <remarks>
        /// todo: should really check the default page size dynamically.
        /// </remarks>
        private int defaultWidth = 718;

        /// <summary>
        /// Height of the map as shown in the GUI. I'm setting
        /// this to 718 to match the default page width of the autodocs
        /// documents.
        /// </summary>
        /// <remarks>
        /// todo: should really check the default page size dynamically.
        /// </remarks>
        private int defaultHeight = 718;

        private Mapsui.Map map;
        private Mapsui.Navigator navigator = new Mapsui.Navigator();
        private Gtk.Image image;
        private Gtk.EventBox container;

        /// <summary>
        /// Is the user dragging the mouse?
        /// </summary>
        private bool isDragging;

        /// <summary>
        /// Position of the mouse when the user starts dragging.
        /// </summary>
        private ApsimCoordinate mouseAtDragStart;

        /// <summary>
        /// Zoom level of the map.
        /// </summary>
        public double Zoom
        {
            get
            {
                if (map == null)
                    return 0;
                return Math.Round(Math.Log((78271.51696401953125 * 512 / defaultWidth) / navigator.Viewport.Resolution, MapRenderer.GetZoomStepFactor()) + 1.0, 2);
             }
            set
            {
                // Refreshing the map is a bit slow, so only do it if
                // the incoming value is different to the old value.
                if (map != null && !MathUtilities.FloatsAreEqual(value, Zoom))
                {
                    double setValue = value - 1.0;
                    if (value >= 60.0 || value < 1.0) // Convert any "old" zoom levels into whole-world maps
                        setValue = 0.0;
                    // The viewport "resolution" is effectively meters per pixel.
                    // The 78271 value is the standard used for 512 pixel square tiles.
                    navigator.ZoomTo((78271.51696401953125 * 512 / defaultWidth)/ Math.Pow(MapRenderer.GetZoomStepFactor(), setValue));
                    RefreshMap();
                }
            }
        }

        /// <summary>
        /// Center of the map.
        /// </summary>
        public ApsimCoordinate Center
        {
            get
            {
                if (map == null)
                    return null;
                var centerLatLon = Mapsui.Projections.SphericalMercator.ToLonLat(navigator.Viewport.CenterX, navigator.Viewport.CenterY);
                return new ApsimCoordinate(Math.Round(centerLatLon.lat, 4), Math.Round(centerLatLon.lon, 4));
            }
            set
            {
                var centerMetric = Mapsui.Projections.SphericalMercator.FromLonLat(value.Longitude, value.Latitude);
                // Refreshing the map is a bit slow, so only do it if
                // the incoming value is different to the old value.
                if (map != null && 
                    (!MathUtilities.FloatsAreEqual(centerMetric.x, navigator.Viewport.CenterX)
                    || !MathUtilities.FloatsAreEqual(centerMetric.y, navigator.Viewport.CenterY)) )
                {
                    navigator.CenterOn(centerMetric.x, centerMetric.y);
                    RefreshMap();
                }
            }
        }

        /// <summary>
        /// GridView widget used to show properties. Could be refactored out.
        /// </summary>
        public IPropertyView PropertiesView { get; private set; }

        /// <summary>
        /// Called when the view is changed by the user.
        /// </summary>
        public event EventHandler ViewChanged;


        /// <summary>
        /// Constructor. Initialises the widget and will show a world
        /// map with no markers until <see cref="ShowMap" /> is called.
        /// </summary>
        /// <param name="owner">Owner view.</param>
        public MapView(ViewBase owner) : base(owner)
        {
            image = new Gtk.Image();

            image.Halign = Align.Start;
            image.Valign = Align.Start;

            container = new Gtk.EventBox();
            container.Add(image);

            Paned box = new Paned(Orientation.Vertical);
            PropertiesView = new PropertyView(this);
            box.Add1(((ViewBase)PropertiesView).MainWidget);

            if ( ((ViewBase)PropertiesView).MainWidget is ScrolledWindow scroller)
                scroller.VscrollbarPolicy = PolicyType.Never;

            box.Add2(container);
            box.AddNotification(OnPanePropertyNotified);
            (owner as ExplorerView).DividerChanged += OnOtherDividersChanged;
            (owner.Owner as MainView).DividerChanged += OnOtherDividersChanged;

            container.AddEvents(
              (int)Gdk.EventMask.ButtonPressMask
            | (int)Gdk.EventMask.ButtonReleaseMask
            | (int)Gdk.EventMask.ScrollMask);
            container.ButtonPressEvent += OnButtonPress;
            container.ButtonReleaseEvent += OnButtonRelease;

            image.Drawn += OnImageExposed;

            container.Destroyed += OnMainWidgetDestroyed;
            container.ScrollEvent += OnMouseScroll;

            mainWidget = box;
            mainWidget.ShowAll();
        }

        /// <summary>
        /// Export the map to an image.
        /// </summary>
        public Gdk.Pixbuf Export()
        {
            Mapsui.Rendering.Skia.MapRenderer renderer = new Mapsui.Rendering.Skia.MapRenderer();
            MemoryStream stream = renderer.RenderToBitmapStream(navigator.Viewport, map.Layers, map.BackColor, 1);
            stream.Seek(0, SeekOrigin.Begin);
            return new Gdk.Pixbuf(stream);
        }

        public void HideZoomControls()
        {
            // Not applicable.
        }

        public void StoreSettings()
        {
            // Not applicable.
        }

        /// <summary>
        /// Show the given markers on the map and set the center/zoom level.
        /// </summary>
        /// <param name="coordinates">Coordinates of the markers.</param>
        /// <param name="locNames">Names of the markers (unused currently).</param>
        /// <param name="zoom">Zoom level of the map.</param>
        /// <param name="center">Location of the center of the map.</param>
        public void ShowMap(List<ApsimCoordinate> coordinates, List<string> locNames, double zoom, ApsimCoordinate center)
        {
            map = new MapTag(center, zoom, coordinates).ToMapsuiMap();
            navigator.SetSize(defaultWidth, defaultHeight);
            if (image.Allocation.Width > 1 && image.Allocation.Height > 1)
                RefreshMap();
        }

        /// <summary>
        /// Refresh the map image shown in the UI.
        /// </summary>
        /// <remarks>
        /// This is fairly slow (often ~200ms), so try not to call it unnecessarily.
        /// </remarks>
        private void RefreshMap()
        {
            if (map != null)
            {
                UpdateMapSize();
                Mapsui.Tiling.Layers.TileLayer osmLayer = map.Layers.FindLayer("OpenStreetMap").FirstOrDefault() as Mapsui.Tiling.Layers.TileLayer; 
                if (osmLayer != null) 
                {
                    osmLayer.Enabled = true;
                    osmLayer.AbortFetch();
                    osmLayer.ClearCache();
                    osmLayer.DataChanged += OsmLayer_DataChanged;
                    MSection mSection = new MSection(navigator.Viewport.ToExtent(), navigator.Viewport.Resolution);
                    osmLayer.RefreshData(new FetchInfo(mSection)); 
                }
            }
        }

        /// <summary>
        /// Set the width and height that the map should be drawn to
        /// </summary>
        private void UpdateMapSize() {
            Rectangle rect = GtkUtilities.GetBorderOfRightHandView(this);
            Point pos = GtkUtilities.GetPositionOfWidget(mainWidget);
            navigator.SetSize(rect.Width, rect.Height - (mainWidget as Paned).Position + pos.Y);
        }

        private void OsmLayer_DataChanged(object sender, Mapsui.Fetcher.DataChangedEventArgs e)
        {
            Mapsui.Tiling.Layers.TileLayer osmLayer = (Mapsui.Tiling.Layers.TileLayer)sender;
            if (((!osmLayer.Busy && e.Error == null) || e.Error != null) && !e.Cancelled)
            {
                if (e.Error != null) 
                // Try to handle failure to fetch Open Street Map layer
                // by displaying the country outline layer instead
                {
                    osmLayer.DataChanged -= OsmLayer_DataChanged;
                    osmLayer.AbortFetch();
                    osmLayer.Enabled = false;
                    Layer countryLayer = map.Layers.FindLayer("Countries").FirstOrDefault() as Layer;
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
                osmLayer.DataChanged -= OsmLayer_DataChanged;
                Mapsui.Rendering.Skia.MapRenderer renderer = new Mapsui.Rendering.Skia.MapRenderer();
                MemoryStream stream = renderer.RenderToBitmapStream(navigator.Viewport, map.Layers, map.BackColor, 1);
                stream.Seek(0, SeekOrigin.Begin);
                InvokeOnMainThread(delegate { image.Pixbuf = new Gdk.Pixbuf(stream); });
            }
        }

        /// <summary>
        /// Converts screen x/y coordinates to latitude/longitude on the map.
        /// Note that x/y must be relative to the GtkImage (image)'s GdkWindow.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="lat">Latitude.</param>
        /// <param name="lon">Longitude.</param>
        private void CartesianToGeoCoords(double x, double y, out double lat, out double lon)
        {
            Mapsui.MPoint coord = navigator.Viewport.ScreenToWorld(x, y);
            lat = coord.Y;
            lon = coord.X;
        }
    
        /// <summary>
        /// Traps the Exposed event for the image. This event fires after
        /// size/space allocation has occurred but before it is actually
        /// drawn on the screen. Because the size-allocated signal is emitted
        /// several times, we don't want to refresh the map each time.
        /// Therefore, we refresh the map once, during the expose event.
        /// 
        /// We also disconnect the event handler after refreshing the map so
        /// that we don't refresh it multiple times unnecessarily.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnImageExposed(object sender, DrawnArgs args)
        {
            try
            {
                RefreshMap();

                image.Drawn -= OnImageExposed;

            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the mouse button is pressed down. Records the
        /// mouse position, to be used to move map center when the
        /// mouse button is released.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnButtonPress(object sender, ButtonPressEventArgs args)
        {
            try
            {
                isDragging = true;
                CartesianToGeoCoords(args.Event.X, args.Event.Y, out double lat, out double lon);
                mouseAtDragStart = new ApsimCoordinate(lat, lon);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the mouse button is released.
        /// Handles the map drag logic.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnButtonRelease(object sender, ButtonReleaseEventArgs args)
        {
            try
            {
                if (isDragging)
                {
                    CartesianToGeoCoords(args.Event.X, args.Event.Y, out double lat, out double lon);
                    double dy = lat - mouseAtDragStart.Latitude;
                    double dx = lon - mouseAtDragStart.Longitude;

                    navigator.CenterOn(navigator.Viewport.CenterX - dx, navigator.Viewport.CenterY - dy);
                    RefreshMap();
                    ViewChanged?.Invoke(this, EventArgs.Empty);
                }
                isDragging = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the user scrolls with the mouse.
        /// Handles the zoom in/out logic.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnMouseScroll(object sender, ScrollEventArgs args)
        {
            try
            {
                Mapsui.MRect envelope = navigator.Viewport.ToExtent();
                double mouseLat = args.Event.Y / navigator.Viewport.Height * (envelope.MinY - envelope.MaxY) + envelope.MaxY;
                double mouseLon = args.Event.X / navigator.Viewport.Width * (envelope.MaxX - envelope.MinX) + envelope.MinX;

                if (args.Event.Direction == Gdk.ScrollDirection.Up || args.Event.Direction == Gdk.ScrollDirection.Down)
                {
                    // Adjust zoom level on map.
                    double sign = args.Event.Direction == Gdk.ScrollDirection.Up ? 1 : -1;
                    Zoom = MathUtilities.Bound(Zoom + sign, 1.0, 25.0);

                    // Adjust center of map, so that coordinates at mouse cursor are the same
                    // as previously.
                    double newMouseLat = args.Event.Y / navigator.Viewport.Height * (envelope.MinY - envelope.MaxY) + envelope.MaxY;
                    double newMouseLon = args.Event.X / navigator.Viewport.Width * (envelope.MaxX - envelope.MinX) + envelope.MinX;

                    double dx = newMouseLon - mouseLon;
                    double dy = newMouseLat - mouseLat;
                    navigator.CenterOn(navigator.Viewport.CenterX - dx, navigator.Viewport.CenterY - dy);
                    RefreshMap();
                    ViewChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Refresh the map when the divider changes</summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPanePropertyNotified(object sender, NotifyArgs args)
        {
            if (args.Property == "position")
                RefreshMap();
        }

        /// <summary>Refresh the map when the divider changes</summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnOtherDividersChanged(object sender, EventArgs args)
        {
            RefreshMap();
        }

        /// <summary>
        /// Called when the main widget is destroyed.
        /// Detaches event handlers.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnMainWidgetDestroyed(object sender, EventArgs args)
        {
            try
            {
                var osmLayer = map.Layers.FindLayer("OpenStreetMap").FirstOrDefault();
                if (osmLayer != null)
                   osmLayer.DataChanged -= OsmLayer_DataChanged;
                image.Drawn -= OnImageExposed;
                image.Dispose();
                (PropertiesView as PropertyView).Dispose();
                container.ButtonPressEvent -= OnButtonPress;
                container.ButtonReleaseEvent -= OnButtonRelease;
                container.ScrollEvent -= OnMouseScroll;
                container.Destroyed -= OnMainWidgetDestroyed;
                (owner as ExplorerView).DividerChanged -= OnOtherDividersChanged;
                (owner.Owner as MainView).DividerChanged -= OnOtherDividersChanged;
                mainWidget.RemoveNotification(OnPanePropertyNotified);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}