using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aadev.JTF.Editor
{
    public static class Helpers
    {
        public static Point Center(this Rectangle rectangle) => new Point(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Width / 2);
    }
}
