using System;
using System.Collections.Generic;
using System.Linq;
using Models.Climate;
using Models.Core;
using Models.Mapping;

namespace Models
{
    /// <summary>
    /// This component shows a map in the UI.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.MapView")]
    [PresenterName("UserInterface.Presenters.MapPresenter")]
    [ValidParent(DropAnywhere = true)]
    public class Map : Model
    {
        /// <summary>List of coordinates to show on map</summary>
        public List<Coordinate> GetCoordinates(List<string> names = null)
        {
            List<Coordinate> coordinates = new List<Coordinate>();
            if (names != null)
                names.Clear();
            else names = new List<string>();

            foreach (Weather weather in FindAllInScope<Weather>().Where(w => w.Enabled))
            {
                weather.OpenDataFile();
                double latitude = weather.Latitude;
                double longitude = weather.Longitude;
                weather.CloseDataFile();
                if (latitude != 0 && longitude != 0)
                {
                    Coordinate coordinate = new Coordinate(latitude, longitude);
                    coordinates.Add(coordinate);
                    if (names != null)
                        names.Add(System.IO.Path.GetFileName(weather.FileName));
                }
            }

            if (coordinates.Count == 0)
            {
                foreach (var soil in FindAllInScope<Soils.Soil>())
                {
                    double latitude = soil.Latitude;
                    double longitude = soil.Longitude;
                    if (latitude != 0 && longitude != 0)
                    {
                        Coordinate coordinate = new Coordinate(latitude, longitude);
                        coordinates.Add(coordinate);
                        names.Add(soil.Name);
                    }
                }
            }

            return coordinates;
        }

        /// <summary>
        /// Zoom factor for the map
        /// </summary>
        [Description("Zoom level")]
        public Double Zoom
        {
            get
            {
                return _Zoom;
            }
            set { _Zoom = value; }
        }

        /// <summary>
        /// Coordinate of map center
        /// </summary>
        private Coordinate _Center = new Coordinate(0, 0);

        /// <summary>
        /// Coordinate of the center of the map
        /// </summary>
        [Description("Map Center")]
        [Separator("Coordinates for center of map")]
        [Display(Type = DisplayType.SubModel)]
        public Coordinate Center
        {
            get
            {
                return _Center;
            }
            set { _Center = value; }
        }

        /// <summary>
        /// Zoom level
        /// </summary>
        private Double _Zoom = 1.0;
    }
}
