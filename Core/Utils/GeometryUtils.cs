using Microsoft.Spatial;
namespace ClientApp.Core.Utils
{
    public static class GeometryUtils
    {
        /// <summary>
        /// Calculate either the initial or final bearing (sometimes
        /// referred to as forward azimuth) which, if followed in a straight
        /// line along a great-circle arc, will take you from the start point
        /// to the end point. The final endpoint needs to be reversed.
        /// 
        /// Original source:
        /// https://www.movable-type.co.uk/scripts/latlong.html
        /// 
        /// </summary>
        private static double CalculateBearing2D(double fromLatitude, double fromLongitude, double toLatitude, double toLongitude, bool reverse)
        {
            // Convert latitude and longitude from degrees to radians
            double fromLatitudeRad = fromLatitude * Math.PI / 180;
            double fromLongitudeRad = fromLongitude * Math.PI / 180;
            double toLatitudeRad = toLatitude * Math.PI / 180;
            double toLongitudeRad = toLongitude * Math.PI / 180;

            // Calculate differences
            double longitudeDifference = toLongitudeRad - fromLongitudeRad;

            // Calculate bearing
            double y = Math.Sin(longitudeDifference) * Math.Cos(toLatitudeRad);
            double x = Math.Cos(fromLatitudeRad) * Math.Sin(toLatitudeRad) -
                       Math.Sin(fromLatitudeRad) * Math.Cos(toLatitudeRad) * Math.Cos(longitudeDifference);
            double rawBearingRad = Math.Atan2(y, x);

            // Convert from radians to degrees and normalize
            double bearingDegrees = (rawBearingRad * 180 / Math.PI + 360 + (reverse == true ? 180 : 0)) % 360;
            return bearingDegrees;
        }


        public static double? CalculateInitialBearing2D(GeometryPoint? from, GeometryPoint? to)
        {
            if (from == null || to == null)
                return null;
            return CalculateBearing2D(from.X, from.Y, to.X, to.Y, false);
        }

        public static double? CalculateFinalBearing2D(GeometryPoint? from, GeometryPoint? to)
        {
            if (from == null || to == null)
                return null;
            return CalculateBearing2D(to.X, to.Y, from.X, from.Y, true);
        }
    }
}
