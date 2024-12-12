using System;
using System.Collections.Generic;
using Ssz.Operator.Core.DsPageTypes;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    public static class Utils
    {
        #region private fields

        private static readonly Random Random = new();

        #endregion

        #region public functions

        public static T[] RandomizeElements<T>(List<T> input)
        {
            //return input.ToArray();

            if (input.Count < 2) return input.ToArray();

            var inputCopy = new List<T>(input);
            var output = new List<T>(input.Count);
            while (inputCopy.Count > 0)
            {
                var index = Random.Next(inputCopy.Count);
                var i = inputCopy[index];
                inputCopy.RemoveAt(index);
                output.Add(i);
            }

            return output.ToArray();
        }


        public static double NormalizeAngle(double angle)
        {
            if (angle > 0)
                return angle % (2 * Math.PI);
            return 2 * Math.PI - -angle % (2 * Math.PI);
        }


        public static double NormalizeAngle2(double angle)
        {
            angle = NormalizeAngle(angle);
            if (angle <= Math.PI)
                return angle;
            return angle - 2 * Math.PI;
        }


        public static double NormalizeAngleInDegrees(double angle)
        {
            if (angle > 0)
                return angle % 360;
            return 360 - -angle % 360;
        }


        public static double NormalizeAngle2InDegrees(double angle)
        {
            angle = NormalizeAngleInDegrees(angle);
            if (angle <= 180)
                return angle;
            return angle - 360;
        }

        public static double ConvertToRadians(double angle)
        {
            return Math.PI / 180 * angle;
        }

        public static double ConvertToDegrees(double angle)
        {
            return 180 / Math.PI * angle;
        }

        public static void CalculateVerticalAngles(PanoramaDsPageType panoramaDsPageType, double drawingWidth,
            double drawingHeight,
            out double verticalImageAngle, out double upAngle, out double downAngle)
        {
            verticalImageAngle = 90;
            switch (panoramaDsPageType.PanoramaType)
            {
                case PanoramaType.Cylindrical:
                {
                    var verticalImageAngleTan = Math.PI * drawingHeight / drawingWidth;
                    var verticalImageAngleRadians = Math.Atan(verticalImageAngleTan) * 2;
                    verticalImageAngle = 180.0 / Math.PI * verticalImageAngleRadians;
                }
                    break;
                case PanoramaType.Spherical:
                {
                    var verticalImageAngleRadians = 2 * Math.PI * drawingHeight / drawingWidth;
                    verticalImageAngle = 180.0 / Math.PI * verticalImageAngleRadians;
                }
                    break;
            }

            if (verticalImageAngle > 180) verticalImageAngle = 180;
            else if (verticalImageAngle < 0) verticalImageAngle = 0;
            upAngle = verticalImageAngle / 2.0 - panoramaDsPageType.HorizonAngle;
            downAngle = -verticalImageAngle / 2.0 - panoramaDsPageType.HorizonAngle;

            switch (panoramaDsPageType.PanoramaType)
            {
                case PanoramaType.Cylindrical:
                    break;
                case PanoramaType.Spherical:
                {
                    if (upAngle >= 90)
                    {
                        upAngle = 90;
                        downAngle = upAngle - verticalImageAngle;
                    }

                    if (downAngle <= -90)
                    {
                        downAngle = -90;
                        upAngle = downAngle + verticalImageAngle;
                    }
                }
                    break;
            }
        }

        #endregion
    }
}