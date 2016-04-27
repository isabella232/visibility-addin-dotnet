﻿// Copyright 2016 Esri 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using VisibilityLibrary.Helpers;
using VisibilityLibrary.ViewModels;
using ProAppVisibilityModule.Models;
using ProAppVisibilityModule.Helpers;

namespace ProAppVisibilityModule.ViewModels
{
    /// <summary>
    /// Base class for all the common properties, commands and events for tab items
    /// </summary>
    public class ProTabBaseViewModel : BaseViewModel
    {
        public ProTabBaseViewModel()
        {
            //commands
            ClearGraphicsCommand = new VisibilityLibrary.Helpers.RelayCommand(OnClearGraphics);
            ActivateToolCommand = new VisibilityLibrary.Helpers.RelayCommand(OnActivateToolCommand);
            EnterKeyCommand = new VisibilityLibrary.Helpers.RelayCommand(OnEnterKeyCommand);
            CancelCommand = new VisibilityLibrary.Helpers.RelayCommand(OnCancelCommand);

            // Mediator
            Mediator.Register(VisibilityLibrary.Constants.NEW_MAP_POINT, OnNewMapPointEvent);
            Mediator.Register(VisibilityLibrary.Constants.MOUSE_MOVE_POINT, OnMouseMoveEvent);
            Mediator.Register(VisibilityLibrary.Constants.TAB_ITEM_SELECTED, OnTabItemSelected);
        }

        #region Properties

        /// <summary>
        /// lists to store GUIDs of graphics, temp feedback and map graphics
        /// </summary>
        private static List<ProGraphic> ProGraphicsList = new List<ProGraphic>();

        /// <summary>
        /// Property used to determine if there are non temp graphics
        /// </summary>
        public bool HasMapGraphics
        {
            get
            {
                return ProGraphicsList.Any(g => g.IsTemp == false);
            }
        }

        private MapPoint point1 = null;
        /// <summary>
        /// Property for the observer MapPoint
        /// </summary>
        public virtual MapPoint Point1
        {
            get
            {
                return point1;
            }
            set
            {
                // do not add anything to the map from here
                point1 = value;
                RaisePropertyChanged(() => Point1);
                RaisePropertyChanged(() => Point1Formatted);
            }
        }

        private MapPoint point2 = null;
        /// <summary>
        /// Property for the target MapPoint
        /// Not all tools need a second point
        /// </summary>
        public virtual MapPoint Point2
        {
            get
            {
                return point2;
            }
            set
            {
                point2 = value;
                RaisePropertyChanged(() => Point2);
                RaisePropertyChanged(() => Point2Formatted);
            }
        }
        string point1Formatted = string.Empty;
        /// <summary>
        /// String property for the observer MapPoint
        /// This is used to format the point for the UI and allow string input of different types of coordinates
        /// </summary>
        public string Point1Formatted
        {
            get
            {
                // return a formatted first point depending on how it was entered, manually or via map point tool
                if (string.IsNullOrWhiteSpace(point1Formatted))
                {
                    if (Point1 == null)
                        return string.Empty;

                    // only format if the Point1 data was generated from a mouse click
                    return MapPointHelper.GetMapPointAsDisplayString(Point1);
                }
                else
                {
                    // this was user inputed so just return the inputed string
                    return point1Formatted;
                }
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    point1Formatted = string.Empty;
                    RaisePropertyChanged(() => Point1Formatted);
                    return;
                }
                // try to convert string to a MapPoint
                var point = GetMapPointFromString(value);
                if (point != null)
                {
                    point1Formatted = value;
                    Point1 = point;
                }
                else
                {
                    // invalid coordinate, reset and throw exception
                    Point1 = null;
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEInvalidCoordinate);
                }
            }
        }

        string point2Formatted = string.Empty;
        /// <summary>
        /// String property for the target MapPoint
        /// This is used to format the point for the UI and allow string input of different types of coordinates
        /// Input types like GARS, MGRS, USNG, UTM
        /// </summary>
        public string Point2Formatted
        {
            get
            {
                // return a formatted second point depending on how it was entered, manually or via map point tool
                if (string.IsNullOrWhiteSpace(point2Formatted))
                {
                    if (Point2 == null)
                        return string.Empty;

                    // only format if the Point2 data was generated from a mouse click
                    return MapPointHelper.GetMapPointAsDisplayString(Point2);
                }
                else
                {
                    // this was user inputed so just return the inputed string
                    return point2Formatted;
                }
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    point2Formatted = string.Empty;
                    RaisePropertyChanged(() => Point2Formatted);
                    return;
                }
                // try to convert string to a MapPoint
                var point = GetMapPointFromString(value);
                if (point != null)
                {
                    point2Formatted = value;
                    Point2 = point;
                }
                else
                {
                    // invalid coordinate, reset and throw exception
                    Point2 = null;
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEInvalidCoordinate);
                }
            }
        }

        private bool isActiveTab = false;
        /// <summary>
        /// Property to keep track of which tab/viewmodel is the active item
        /// </summary>
        public bool IsActiveTab
        {
            get
            {
                return isActiveTab;
            }
            set
            {
                Reset(true);
                isActiveTab = value;
                RaisePropertyChanged(() => IsActiveTab);
            }
        }

        /// <summary>
        /// Property used to test if there is enough info to create a map element(s)
        /// </summary>
        public virtual bool CanCreateElement
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Commands

        public VisibilityLibrary.Helpers.RelayCommand ClearGraphicsCommand { get; set; }
        public VisibilityLibrary.Helpers.RelayCommand EnterKeyCommand { get; set; }
        public VisibilityLibrary.Helpers.RelayCommand CancelCommand { get; set; }
        public VisibilityLibrary.Helpers.RelayCommand ActivateToolCommand { get; set; }

        /// <summary>
        /// Clears all the graphics from the maps graphic container
        /// Inlucdes temp and map graphics
        /// Only removes temp and map graphics that were created by this add-in
        /// </summary>
        /// <param name="obj"></param>
        private void OnClearGraphics(object obj)
        {
            if (MapView.Active == null)
                return;

            foreach (var item in ProGraphicsList)
            {
                item.Disposable.Dispose();
            }

            ProGraphicsList.Clear();

            RaisePropertyChanged(() => HasMapGraphics);
        }

        /// <summary>
        /// Handler for the "Enter"key command
        /// Calls CreateMapElement
        /// </summary>
        /// <param name="obj"></param>
        internal virtual void OnEnterKeyCommand(object obj)
        {
            if (!CanCreateElement)
                return;

            CreateMapElement();
        }

        /// <summary>
        /// Handler for the cancel command
        /// </summary>
        /// <param name="obj"></param>
        private void OnCancelCommand(object obj)
        {
            Reset(true);
        }

        /// <summary>
        /// Handler for the activate tool command
        /// Sets the current tool
        /// </summary>
        /// <param name="obj"></param>
        internal virtual void OnActivateToolCommand(object obj)
        {
            FrameworkApplication.SetCurrentToolAsync("ProAppVisibilityModule_MapTool");
        }

        #endregion

        #region Event Methods

        /// <summary>
        /// Handler for the new map point click event
        /// </summary>
        /// <param name="obj">MapPoint</param>
        internal virtual void OnNewMapPointEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            var point = obj as MapPoint;

            if (point == null)
                return;

            // do nothing
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Removes graphics from the map
        /// </summary>
        /// <param name="guidList">list of GUIDs</param>
        internal void RemoveGraphics(List<string> guidList)
        {
            var list = ProGraphicsList.Where(g => guidList.Contains(g.GUID)).ToList();
            foreach (var graphic in list)
            {
                graphic.Disposable.Dispose();
                ProGraphicsList.Remove(graphic);
            }

            RaisePropertyChanged(() => HasMapGraphics);
        }

        /// <summary>
        /// Derived class must override this method in order to create map elements
        /// Clears temp graphics by default
        /// </summary>
        internal virtual void CreateMapElement()
        {
            ClearTempGraphics();
        }

        /// <summary>
        /// Method to clear all temp graphics
        /// </summary>
        internal void ClearTempGraphics()
        {
            var list = ProGraphicsList.Where(g => g.IsTemp == true).ToList();

            foreach (var item in list)
            {
                item.Disposable.Dispose();
                ProGraphicsList.Remove(item);
            }

            RaisePropertyChanged(() => HasMapGraphics);
        }
        
        /// <summary>
        /// Method used to totally reset the tool
        /// reset points, feedback
        /// clear out textboxes
        /// </summary>
        internal virtual void Reset(bool toolReset)
        {
            if (toolReset)
            {
                DeactivateTool("ProAppVisibilityModule_MapTool");
            }

            Point1 = null;
            Point2 = null;
            Point1Formatted = string.Empty;
            Point2Formatted = string.Empty;
        }

        /// <summary>
        /// Method used to convert a string to a known coordinate
        /// Assumes WGS84 for now
        /// </summary>
        /// <param name="coordinate">the coordinate as a string</param>
        /// <returns>MapPoint if successful, null if not</returns>
        internal MapPoint GetMapPointFromString(string coordinate)
        {
            MapPoint point = null;

            // future use if order of GetValues is not acceptable
            //var listOfTypes = new List<GeoCoordinateType>(new GeoCoordinateType[] {
            //    GeoCoordinateType.DD,
            //    GeoCoordinateType.DDM,
            //    GeoCoordinateType.DMS,
            //    GeoCoordinateType.GARS,
            //    GeoCoordinateType.GeoRef,
            //    GeoCoordinateType.MGRS,
            //    GeoCoordinateType.USNG,
            //    GeoCoordinateType.UTM
            //});

            var listOfTypes = Enum.GetValues(typeof(GeoCoordinateType)).Cast<GeoCoordinateType>();

            foreach (var type in listOfTypes)
            {
                try
                {
                    point = QueuedTask.Run(() =>
                    {
                        return MapPointBuilder.FromGeoCoordinateString(coordinate, MapView.Active.Map.SpatialReference, type, FromGeoCoordinateMode.Default);
                    }).Result;
                }
                catch (Exception ex)
                {
                    // do nothing
                }

                if (point != null)
                    return point;
            }

            try
            {
                point = QueuedTask.Run(() =>
                {
                    return MapPointBuilder.FromGeoCoordinateString(coordinate, MapView.Active.Map.SpatialReference, GeoCoordinateType.UTM, FromGeoCoordinateMode.UtmNorthSouth);
                }).Result;
            }
            catch (Exception ex)
            {
                // do nothing
            }

            return point;
        }

        internal async Task<string> AddGraphicToMap(Geometry geom, bool IsTempGraphic = false, double size = 1.0)
        {
            // default color Red
            return await AddGraphicToMap(geom, ColorFactory.Red, IsTempGraphic, size);
        }

        internal async Task<string> AddGraphicToMap(Geometry geom, CIMColor color, bool IsTempGraphic = false, double size = 1.0, string text = "", SimpleMarkerStyle markerStyle = SimpleMarkerStyle.Circle)
        {
            if (geom == null || MapView.Active == null)
                return string.Empty;

            CIMSymbolReference symbol = null;

            if (!string.IsNullOrWhiteSpace(text) && geom.GeometryType == GeometryType.Point)
            {
                await QueuedTask.Run(() =>
                {
                    // TODO add text graphic
                    //var tg = new CIMTextGraphic() { Placement = Anchor.CenterPoint, Text = text};
                });
            }
            else if (geom.GeometryType == GeometryType.Point)
            {
                await QueuedTask.Run(() =>
                {
                    var s = SymbolFactory.ConstructPointSymbol(color, size, markerStyle);
                    symbol = new CIMSymbolReference() { Symbol = s };
                });
            }
            else if (geom.GeometryType == GeometryType.Polyline)
            {
                await QueuedTask.Run(() =>
                {
                    var s = SymbolFactory.ConstructLineSymbol(color, size);
                    symbol = new CIMSymbolReference() { Symbol = s };
                });
            }
            else if (geom.GeometryType == GeometryType.Polygon)
            {
                await QueuedTask.Run(() =>
                {
                    var outline = SymbolFactory.ConstructStroke(ColorFactory.Black, 1.0, SimpleLineStyle.Solid);
                    var s = SymbolFactory.ConstructPolygonSymbol(color, SimpleFillStyle.Solid, outline);
                    symbol = new CIMSymbolReference() { Symbol = s };
                });
            }

            var result = await QueuedTask.Run(() =>
            {
                var disposable = MapView.Active.AddOverlay(geom, symbol);
                var guid = Guid.NewGuid().ToString();
                ProGraphicsList.Add(new ProGraphic(disposable, guid, geom, IsTempGraphic));
                return guid;
            });

            return result;
        }

        /// <summary>
        /// Handler for the mouse move event
        /// When the mouse moves accross the map, MapPoints are returned to aid in updating feedback to user
        /// </summary>
        /// <param name="obj">MapPoint</param>
        internal virtual void OnMouseMoveEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            var point = obj as MapPoint;

            if (point == null)
                return;

            // do nothing
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Handler for the tab item selected event
        /// Helps keep track of which tab item/viewmodel is active
        /// </summary>
        /// <param name="obj">bool if selected or not</param>
        private void OnTabItemSelected(object obj)
        {
            if (obj == null)
                return;

            IsActiveTab = (obj == this);
        }

        /// <summary>
        /// Method used to deactivate tool
        /// </summary>
        private void DeactivateTool(string toolname)
        {
            if (FrameworkApplication.CurrentTool != null &&
                FrameworkApplication.CurrentTool.Equals(toolname))
            {
                FrameworkApplication.SetCurrentToolAsync(String.Empty);
            }
        }

        //TODO update for Pro ??? 
        // TODO remove if found to be not needed
        //internal string AddTextToMap(string text, IGeometry geom, IColor color, bool IsTempGraphic = false, int size = 12)
        //{
        //    if (geom == null || ArcMap.Document == null || ArcMap.Document.FocusMap == null)
        //        return string.Empty;

        //    IElement element = null;

        //    geom.Project(ArcMap.Document.FocusMap.SpatialReference);

        //    if (geom.GeometryType == esriGeometryType.esriGeometryPoint)
        //    {
        //        var te = new TextElementClass() as ITextElement;
        //        te.Text = text;

        //        var ts = new TextSymbolClass();
        //        ts.Size = size;
        //        ts.VerticalAlignment = esriTextVerticalAlignment.esriTVACenter;
        //        ts.HorizontalAlignment = esriTextHorizontalAlignment.esriTHACenter;

        //        te.Symbol = ts;

        //        element = te as IElement;
        //    }

        //    if (element == null)
        //        return string.Empty;

        //    element.Geometry = geom;

        //    var mxdoc = ArcMap.Application.Document as IMxDocument;
        //    var av = mxdoc.FocusMap as IActiveView;
        //    var gc = av as IGraphicsContainer;

        //    // store guid
        //    var eprop = element as IElementProperties;
        //    eprop.Name = Guid.NewGuid().ToString();

        //    if (IsTempGraphic)
        //        TempGraphicsList.Add(eprop.Name);
        //    else
        //        MapGraphicsList.Add(eprop.Name);

        //    gc.AddElement(element, 0);

        //    av.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);

        //    RaisePropertyChanged(() => HasMapGraphics);

        //    return eprop.Name;
        //}



        // TODO remove if found to be not needed
        //internal DistanceTypes GetDistanceType(int linearUnitFactoryCode)
        //{ 
        //    DistanceTypes distanceType = DistanceTypes.Meters;
        //    switch (linearUnitFactoryCode)
        //    {
        //        case (int)esriSRUnitType.esriSRUnit_Foot:
        //            distanceType = DistanceTypes.Feet;
        //            break;
        //        case (int)esriSRUnitType.esriSRUnit_Kilometer:
        //            distanceType = DistanceTypes.Kilometers;
        //            break;
        //        case (int)esriSRUnitType.esriSRUnit_Meter:
        //            distanceType = DistanceTypes.Meters;
        //            break;
        //        case (int)esriSRUnitType.esriSRUnit_NauticalMile:
        //            distanceType = DistanceTypes.NauticalMile;
        //            break;
        //        case (int)esriSRUnitType.esriSRUnit_SurveyFoot:
        //            distanceType = DistanceTypes.SurveyFoot;
        //            break;
        //        default:
        //            distanceType = DistanceTypes.Meters;
        //            break;
        //    }

        //    return distanceType;
        //}

        // TODO remove if found to be not needed
        //internal ISpatialReferenceFactory3 srf3 = null;
        //internal ILinearUnit GetLinearUnit()
        //{
        //    return GetLinearUnit(LineDistanceType);
        //}
        // TODO remove if found to be not needed
        /// <summary>
        /// Gets the linear unit from the esri constants for linear units
        /// </summary>
        /// <returns>ILinearUnit</returns>
        //internal ILinearUnit GetLinearUnit(DistanceTypes distanceType)
        //{
        //    int unitType = (int)esriSRUnitType.esriSRUnit_Meter;
        //    if (srf3 == null)
        //    {
        //        Type srType = Type.GetTypeFromProgID("esriGeometry.SpatialReferenceEnvironment");
        //        srf3 = Activator.CreateInstance(srType) as ISpatialReferenceFactory3;
        //    }

        //    switch (distanceType)
        //    {
        //        case DistanceTypes.Feet:
        //            unitType = (int)esriSRUnitType.esriSRUnit_Foot;
        //            break;
        //        case DistanceTypes.Kilometers:
        //            unitType = (int)esriSRUnitType.esriSRUnit_Kilometer;
        //            break;
        //        case DistanceTypes.Meters:
        //            unitType = (int)esriSRUnitType.esriSRUnit_Meter;
        //            break;
        //        case DistanceTypes.NauticalMile:
        //            unitType = (int)esriSRUnitType.esriSRUnit_NauticalMile;
        //            break;
        //        case DistanceTypes.SurveyFoot:
        //            unitType = (int)esriSRUnitType.esriSRUnit_SurveyFoot;
        //            break;
        //        default:
        //            unitType = (int)esriSRUnitType.esriSRUnit_Meter;
        //            break;
        //    }

        //    return srf3.CreateUnit(unitType) as ILinearUnit;
        //}

        // TODO remove if found to be not needed
        /// <summary>
        /// Ugly method to convert to/from different types of distance units
        /// </summary>
        /// <param name="fromType">DistanceTypes</param>
        /// <param name="toType">DistanceTypes</param>
        //internal double GetDistanceFromTo(DistanceTypes fromType, DistanceTypes toType, double input)
        //{
        //    double length = input;

        //    try
        //    {
        //        if (fromType == DistanceTypes.Meters && toType == DistanceTypes.Kilometers)
        //            length /= 1000.0;
        //        else if (fromType == DistanceTypes.Meters && toType == DistanceTypes.Feet)
        //            length *= 3.28084;
        //        else if (fromType == DistanceTypes.Meters && toType == DistanceTypes.SurveyFoot)
        //            length *= 3.280833333;
        //        else if (fromType == DistanceTypes.Meters && toType == DistanceTypes.NauticalMile)
        //            length *= 0.000539957;
        //        else if (fromType == DistanceTypes.Kilometers && toType == DistanceTypes.Meters)
        //            length *= 1000.0;
        //        else if (fromType == DistanceTypes.Kilometers && toType == DistanceTypes.Feet)
        //            length *= 3280.84;
        //        else if (fromType == DistanceTypes.Kilometers && toType == DistanceTypes.SurveyFoot)
        //            length *= 3280.833333;
        //        else if (fromType == DistanceTypes.Kilometers && toType == DistanceTypes.NauticalMile)
        //            length *= 0.539957;
        //        else if (fromType == DistanceTypes.Feet && toType == DistanceTypes.Kilometers)
        //            length *= 0.0003048;
        //        else if (fromType == DistanceTypes.Feet && toType == DistanceTypes.Meters)
        //            length *= 0.3048;
        //        else if (fromType == DistanceTypes.Feet && toType == DistanceTypes.SurveyFoot)
        //            length *= 0.999998000004;
        //        else if (fromType == DistanceTypes.Feet && toType == DistanceTypes.NauticalMile)
        //            length *= 0.000164579;
        //        else if (fromType == DistanceTypes.SurveyFoot && toType == DistanceTypes.Kilometers)
        //            length *= 0.0003048006096;
        //        else if (fromType == DistanceTypes.SurveyFoot && toType == DistanceTypes.Meters)
        //            length *= 0.3048006096;
        //        else if (fromType == DistanceTypes.SurveyFoot && toType == DistanceTypes.Feet)
        //            length *= 1.000002;
        //        else if (fromType == DistanceTypes.SurveyFoot && toType == DistanceTypes.NauticalMile)
        //            length *= 0.00016457916285097;
        //        else if (fromType == DistanceTypes.NauticalMile && toType == DistanceTypes.Kilometers)
        //            length *= 1.852001376036;
        //        else if (fromType == DistanceTypes.NauticalMile && toType == DistanceTypes.Meters)
        //            length *= 1852.001376036;
        //        else if (fromType == DistanceTypes.NauticalMile && toType == DistanceTypes.Feet)
        //            length *= 6076.1154855643;
        //        else if (fromType == DistanceTypes.NauticalMile && toType == DistanceTypes.SurveyFoot)
        //            length *= 6076.1033333576;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex);
        //    }

        //    return length;
        //}
 
        #endregion Private Methods
    }
}
