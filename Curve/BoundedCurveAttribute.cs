using UnityEngine;

namespace Lib
{
    public class BoundedCurveAttribute : PropertyAttribute
    {
        public Rect bounds;
        public int height;

        public BoundedCurveAttribute(Rect bounds, int height = 1)
        {
            this.bounds = bounds;
            this.height = height;
        }

        public BoundedCurveAttribute(float xMin, float yMin, float xMax, float yMax, int height = 1)
        {
            this.bounds = new Rect(xMin, yMin, xMax, yMax);
            this.height = height;
        }

        public BoundedCurveAttribute(float xMax, float yMax, int height = 1)
        {
            this.bounds = new Rect(0, 0, xMax, yMax);
            this.height = height;
        }

        public BoundedCurveAttribute(int height = 1)
        {
            this.bounds = new Rect(0, 0, 1, 1);
            this.height = height;
        }
    }
}