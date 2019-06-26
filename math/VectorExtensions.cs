// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace math
{
    /// <summary>
    /// Extensions methods of the vector classes.
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// Return the Y/X components of the vector in the inverse order.
        /// </summary>
        /// <param name="vector">the input vector</param>
        public static Vec2 YX(this Vec2 vector)
        {
            return new Vec2(vector.Y, vector.X);
        }

        /// <summary>
        /// Return the X/Y components of the vector.
        /// </summary>
        /// <param name="vector">the input vector</param>
        public static Vec2 XY(this Vec3 vector)
        {
            return new Vec2(vector.X, vector.Y);
        }

        /// <summary>
        /// Return the X/Z components of the vector.
        /// </summary>
        /// <param name="vector">the input vector</param>
        public static Vec2 XZ(this Vec3 vector)
        {
            return new Vec2(vector.X, vector.Z);
        }

        /// <summary>
        /// Return the Y/Z components of the vector.
        /// </summary>
        /// <param name="vector">the input vector</param>
        public static Vec2 YZ(this Vec3 vector)
        {
            return new Vec2(vector.Y, vector.Z);
        }

        /// <summary>
        /// Return the X/Y components of the vector.
        /// </summary>
        /// <param name="vector">the input vector</param>
        public static Vec2 XY(this Vec4 vector)
        {
            return new Vec2(vector.X, vector.Y);
        }

        /// <summary>
        /// Return the X/Y/Z components of the vector.
        /// </summary>
        /// <param name="vector">the input vector</param>
        public static Vec3 XYZ(this Vec4 vector)
        {
            return new Vec3(vector.X, vector.Y, vector.Z);
        }
    }
}
