﻿// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Nu
{
    /// <summary>
    /// Represents a ray with an origin and a direction in 3D space.
    /// Copied from - https://github.com/MonoGame/MonoGame/blob/v3.8/MonoGame.Framework/Ray.cs
    /// </summary>
    public struct Ray : IEquatable<Ray>
    {
        /// <summary>
        /// The direction of this <see cref="Ray"/>.
        /// </summary>
        public Vector3 Direction;

        /// <summary>
        /// The origin of this <see cref="Ray"/>.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Create a <see cref="Ray"/>.
        /// </summary>
        /// <param name="position">The origin of the <see cref="Ray"/>.</param>
        /// <param name="direction">The direction of the <see cref="Ray"/>.</param>
        public Ray(Vector3 position, Vector3 direction)
        {
            this.Position = position;
            this.Direction = direction;
        }

        /// <summary>
        /// Check if the specified <see cref="Object"/> is equal to this <see cref="Ray"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to test for equality with this <see cref="Ray"/>.</param>
        /// <returns>
        /// <code>true</code> if the specified <see cref="Object"/> is equal to this <see cref="Ray"/>,
        /// <code>false</code> if it is not.
        /// </returns>
        public override bool Equals(object obj)
        {
            return (obj is Ray) && this.Equals((Ray)obj);
        }

        /// <summary>
        /// Check if the specified <see cref="Ray"/> is equal to this <see cref="Ray"/>.
        /// </summary>
        /// <param name="other">The <see cref="Ray"/> to test for equality with this <see cref="Ray"/>.</param>
        /// <returns>
        /// <code>true</code> if the specified <see cref="Ray"/> is equal to this <see cref="Ray"/>,
        /// <code>false</code> if it is not.
        /// </returns>
        public bool Equals(Ray other)
        {
            return this.Position.Equals(other.Position) && this.Direction.Equals(other.Direction);
        }

        /// <summary>
        /// Get a hash code for this <see cref="Ray"/>.
        /// </summary>
        /// <returns>A hash code for this <see cref="Ray"/>.</returns>
        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Direction.GetHashCode();
        }

        /// <summary>
        /// Transform this <see cref="Ray"/> by a matrix.
        /// </summary>
        public Ray Transform(Matrix4x4 m)
		{
            var a = Vector3.Transform(Position, m);
            var b = Vector3.Transform(Position + Direction, m);
            return new Ray(a, Vector3.Normalize(b - a));
		}

        /// <summary>
        /// Transform this <see cref="Ray"/> by a quaternion.
        /// </summary>
        public Ray Transform(Quaternion q)
		{
            var a = Vector3.Transform(Position, q);
            var b = Vector3.Transform(Position + Direction, q);
            return new Ray(a, Vector3.Normalize(b - a));
		}

        // adapted from http://www.scratchapixel.com/lessons/3d-basic-lessons/lesson-7-intersecting-simple-shapes/ray-box-intersection/
        /// <summary>
        /// Check if this <see cref="Ray"/> intersects the specified <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box">The <see cref="BoundingBox"/> to test for intersection.</param>
        /// <returns>
        /// The distance along the ray of the intersection or <code>null</code> if this
        /// <see cref="Ray"/> does not intersect the <see cref="BoundingBox"/>.
        /// </returns>
        public float? Intersects(Box3 box)
        {
            const float Epsilon = 1e-6f;

            Vector3 min = box.Position, max = box.Position + box.Size;
            float? tMin = null, tMax = null;

            if (Math.Abs(Direction.X) < Epsilon)
            {
                if (Position.X < min.X || Position.X > max.X)
                    return null;
            }
            else
            {
                tMin = (min.X - Position.X) / Direction.X;
                tMax = (max.X - Position.X) / Direction.X;

                if (tMin > tMax)
                {
                    var temp = tMin;
                    tMin = tMax;
                    tMax = temp;
                }
            }

            if (Math.Abs(Direction.Y) < Epsilon)
            {
                if (Position.Y < min.Y || Position.Y > max.Y)
                    return null;
            }
            else
            {
                var tMinY = (min.Y - Position.Y) / Direction.Y;
                var tMaxY = (max.Y - Position.Y) / Direction.Y;

                if (tMinY > tMaxY)
                {
                    var temp = tMinY;
                    tMinY = tMaxY;
                    tMaxY = temp;
                }

                if ((tMin.HasValue && tMin > tMaxY) || (tMax.HasValue && tMinY > tMax))
                    return null;

                if (!tMin.HasValue || tMinY > tMin) tMin = tMinY;
                if (!tMax.HasValue || tMaxY < tMax) tMax = tMaxY;
            }

            if (Math.Abs(Direction.Z) < Epsilon)
            {
                if (Position.Z < min.Z || Position.Z > max.Z)
                    return null;
            }
            else
            {
                var tMinZ = (min.Z - Position.Z) / Direction.Z;
                var tMaxZ = (max.Z - Position.Z) / Direction.Z;

                if (tMinZ > tMaxZ)
                {
                    var temp = tMinZ;
                    tMinZ = tMaxZ;
                    tMaxZ = temp;
                }

                if ((tMin.HasValue && tMin > tMaxZ) || (tMax.HasValue && tMinZ > tMax))
                    return null;

                if (!tMin.HasValue || tMinZ > tMin) tMin = tMinZ;
                if (!tMax.HasValue || tMaxZ < tMax) tMax = tMaxZ;
            }

            // having a positive tMax and a negative tMin means the ray is inside the box
            // we expect the intesection distance to be 0 in that case
            if ((tMin.HasValue && tMin < 0) && tMax > 0) return 0;

            // a negative tMin means that the intersection point is behind the ray's origin
            // we discard these as not hitting the AABB
            if (tMin < 0) return null;

            return tMin;
        }

        /// <summary>
        /// Check if this <see cref="Ray"/> intersects the specified <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box">The <see cref="BoundingBox"/> to test for intersection.</param>
        /// <param name="result">
        /// The distance along the ray of the intersection or <code>null</code> if this
        /// <see cref="Ray"/> does not intersect the <see cref="BoundingBox"/>.
        /// </param>
        public void Intersects(in Box3 box, out float? result)
        {
            result = Intersects(box);
        }

        public float? Intersects(Frustum frustum)
        {
            if (frustum == null)
			{
				throw new ArgumentNullException("frustum");
			}
			
			return frustum.Intersects(this);			
        }

        /// <summary>
        /// Check if this <see cref="Ray"/> intersects the specified <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere">The <see cref="Box"/> to test for intersection.</param>
        /// <returns>
        /// The distance along the ray of the intersection or <code>null</code> if this
        /// <see cref="Ray"/> does not intersect the <see cref="Sphere"/>.
        /// </returns>
        public float? Intersects(Sphere sphere)
        {
            float? result;
            Intersects(in sphere, out result);
            return result;
        }

        /// <summary>
        /// Check if this <see cref="Ray"/> intersects the specified <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">The <see cref="Plane"/> to test for intersection.</param>
        /// <returns>
        /// The distance along the ray of the intersection or <code>null</code> if this
        /// <see cref="Ray"/> does not intersect the <see cref="Plane"/>.
        /// </returns>
        public float? Intersects(Plane plane)
        {
            float? result;
            Intersects(in plane, out result);
            return result;
        }

        /// <summary>
        /// Check if this <see cref="Ray"/> intersects the specified <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">The <see cref="Plane"/> to test for intersection.</param>
        /// <param name="result">
        /// The distance along the ray of the intersection or <code>null</code> if this
        /// <see cref="Ray"/> does not intersect the <see cref="Plane"/>.
        /// </param>
        public void Intersects(in Plane plane, out float? result)
        {
            var den = Vector3.Dot(Direction, plane.Normal);
            if (Math.Abs(den) < 0.00001f)
            {
                result = null;
                return;
            }

            result = (-plane.D - Vector3.Dot(plane.Normal, Position)) / den;

            if (result < 0.0f)
            {
                if (result < -0.00001f)
                {
                    result = null;
                    return;
                }

                result = 0.0f;
            }
        }

        /// <summary>
        /// Check if this <see cref="Ray"/> intersects the specified <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere">The <see cref="Box3"/> to test for intersection.</param>
        /// <param name="result">
        /// The distance along the ray of the intersection or <code>null</code> if this
        /// <see cref="Ray"/> does not intersect the <see cref="Sphere"/>.
        /// </param>
        public void Intersects(in Sphere sphere, out float? result)
        {
            // Find the vector between where the ray starts the the sphere's centre
            Vector3 difference = sphere.Center - this.Position;

            float differenceLengthSquared = difference.LengthSquared();
            float sphereRadiusSquared = sphere.Radius * sphere.Radius;

            // If the distance between the ray start and the sphere's centre is less than
            // the radius of the sphere, it means we've intersected. N.B. checking the LengthSquared is faster.
            if (differenceLengthSquared < sphereRadiusSquared)
            {
                result = 0.0f;
                return;
            }

            float distanceAlongRay = Vector3.Dot(this.Direction, difference);
            // If the ray is pointing away from the sphere then we don't ever intersect
            if (distanceAlongRay < 0)
            {
                result = null;
                return;
            }

            // Next we kinda use Pythagoras to check if we are within the bounds of the sphere
            // if x = radius of sphere
            // if y = distance between ray position and sphere centre
            // if z = the distance we've travelled along the ray
            // if x^2 + z^2 - y^2 < 0, we do not intersect
            float dist = sphereRadiusSquared + distanceAlongRay * distanceAlongRay - differenceLengthSquared;

            result = (dist < 0) ? null : distanceAlongRay - (float?)Math.Sqrt(dist);
        }

        /// <summary>
        /// Get all of the ray intersections of a triangle.
        /// </summary>
        public IEnumerable<(int, float)> GetIntersections(int[] indices, Vector3[] vertices)
        {
            var faceCount = indices.Length / 3;
            for (var i = 0; i < faceCount; ++i)
            {
                // Retrieve vertex.
                Vector3 a = vertices[indices[i * 3]];
                Vector3 b = vertices[indices[i * 3 + 1]];
                Vector3 c = vertices[indices[i * 3 + 2]];
                
                // Compute vectors along two edges of the triangle.
                Vector3 edge1 = b - a;
                Vector3 edge2 = c - a;

                // Compute the determinant.
                Vector3 directionCrossEdge2 = Vector3.Cross(Direction, edge2);
                float determinant = Vector3.Dot(edge1, directionCrossEdge2);

                // If the ray is parallel to the triangle plane, there is no collision.
                if (determinant > -float.Epsilon && determinant < float.Epsilon)
                    continue;

                // Calculate the U parameter of the intersection point.
                float inverseDeterminant = 1.0f / determinant;
                Vector3 distanceVector = Position - a;
                float triangleU = Vector3.Dot(distanceVector, directionCrossEdge2);
                triangleU *= inverseDeterminant;

                // Make sure it is inside the triangle.
                if (triangleU < 0 || triangleU > 1)
                    continue;

                // Calculate the V parameter of the intersection point.
                Vector3 distanceCrossEdge1 = Vector3.Cross(distanceVector, edge1);
                float triangleV = Vector3.Dot(Direction, distanceCrossEdge1);
                triangleV *= inverseDeterminant;

                // Make sure it is inside the triangle.
                if (triangleV < 0 || triangleU + triangleV > 1)
                    continue;

                // Compute the distance along the ray to the triangle.
                float rayDistance = Vector3.Dot(edge2, distanceCrossEdge1);
                rayDistance *= inverseDeterminant;

                // Is the triangle behind the ray origin?
                if (rayDistance >= 0)
                    yield return (i, rayDistance);
            }

            //const float epsilon = 0.000001f;
            //var faceCount = indices.Length / 3;
            //for (var i = 0; i < faceCount; i += 3)
            //{
            //    var a = vertices[indices[i * 3]];
            //    var b = vertices[indices[i * 3 + 1]];
            //    var c = vertices[indices[i * 3 + 2]];
            //    var edgeA = b - a;
            //    var edgeB = c - a;
            //    var p = Vector3.Cross(Direction, edgeB);
            //    var det = Vector3.Dot(edgeA, p);
            //    if (det <= -epsilon || det >= epsilon)
            //    {
            //        var detInv = 1.0f / det;
            //        var tvec = Position - a;
            //        var u = Vector3.Dot(tvec, p) * detInv;
            //        if (u >= 0f && u <= 1f)
            //        {
            //            var qvec = Vector3.Cross(tvec, edgeA);
            //            var v = Vector3.Dot(Direction, qvec) * detInv;
            //            if (v >= 0 && u + v <= 1f)
            //            {
            //                var t = Vector3.Dot(c, qvec) * detInv;
            //                yield return (i, t);
            //            }
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Attempt to get the first found intersection from an array of triangle vertices.
        /// </summary>
        public bool Intersects(int[] indices, Vector3[] vertices, out float? result)
		{
            var enr = GetIntersections(indices, vertices).GetEnumerator();
            if (enr.MoveNext())
            {
                var (_, t) = enr.Current;
                result = t;
                return true;
            }
            result = null;
            return false;
		}

        /// <summary>
        /// Check if ray intersects any of the given triangle vertices.
        /// </summary>
        public bool Intersects(int[] indices, Vector3[] vertices)
		{
            Intersects(indices, vertices, out var resultOpt);
            return resultOpt.HasValue;
		}

        /// <summary>
        /// Check if two rays are not equal.
        /// </summary>
        /// <param name="a">A ray to check for inequality.</param>
        /// <param name="b">A ray to check for inequality.</param>
        /// <returns><code>true</code> if the two rays are not equal, <code>false</code> if they are.</returns>
        public static bool operator !=(Ray a, Ray b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Check if two rays are equal.
        /// </summary>
        /// <param name="a">A ray to check for equality.</param>
        /// <param name="b">A ray to check for equality.</param>
        /// <returns><code>true</code> if the two rays are equals, <code>false</code> if they are not.</returns>
        public static bool operator ==(Ray a, Ray b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Get a <see cref="String"/> representation of this <see cref="Ray"/>.
        /// </summary>
        /// <returns>A <see cref="String"/> representation of this <see cref="Ray"/>.</returns>
        public override string ToString()
        {
            return "{{Position:" + Position.ToString() + " Direction:" + Direction.ToString() + "}}";
        }

        /// <summary>
        /// Deconstruction method for <see cref="Ray"/>.
        /// </summary>
        /// <param name="position">Receives the start position of the ray.</param>
        /// <param name="direction">Receives the direction of the ray.</param>
        public void Deconstruct(out Vector3 position, out Vector3 direction)
        {
            position = Position;
            direction = Direction;
        }
    }
}