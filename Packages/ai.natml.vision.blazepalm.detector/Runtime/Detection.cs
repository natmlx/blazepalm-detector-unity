/* 
*   BlazePalm
*   Copyright © 2023 NatML Inc. All Rights Reserved.
*/

namespace NatML.Vision {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using UnityEngine;
    using NatML.Features;
    using NatML.Internal;
    using NatML.Types;

    public sealed partial class BlazePalmDetector {

        /// <summary>
        /// Detected palm region of interest.
        /// </summary>
        public readonly struct Detection {

            #region --Client API--
            /// <summary>
            /// Palm confidence score.
            /// </summary>
            public readonly float score;

            /// <summary>
            /// Palm bounding box in normalized coordinates.
            /// </summary>
            public readonly Rect rect;

            /// <summary>
            /// Palm clockwise rotation angle from upright in degrees.
            /// </summary>
            public readonly float rotation {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] // hope and pray
                get {
                    var wrist = points[0];
                    var center = points[3];
                    var targetAngle = 0.5f * Mathf.PI;
                    var currentAngle = Mathf.Atan2(midPalm.y - wrist.y, midPalm.x - wrist.x);
                    var rotation = targetAngle - currentAngle;
                    return Mathf.Rad2Deg * rotation;
                }
            }

            /// <summary>
            /// Detected palm region of interest in the source image feature.
            /// This region of interest is large enough to cover the entire detected palm.
            /// This rectangle is specified in pixel coordinates.
            /// </summary>
            public readonly RectInt regionOfInterest {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    var center = Vector2.Scale(midPalm, imageSize);
                    var wrist = Vector2.Scale(points[0], imageSize);
                    var boxSize = 2f * (center - wrist).magnitude;
                    var length = 1.25f * boxSize;
                    var size = Vector2Int.RoundToInt(length * Vector2.one);
                    var minPoint = Vector2Int.RoundToInt(center - 0.5f * (Vector2)size);
                    return new RectInt(minPoint, size);
                }
            }

            /// <summary>
            /// Transformation that maps a normalized 2D point in the region of interest defined by this detection
            /// to a normalized point in the original image.
            /// </summary>
            public readonly Matrix4x4 regionOfInterestToImageMatrix {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    var roi = regionOfInterest;
                    var scale = new Vector3(roi.width / imageSize.x, roi.height / imageSize.y, 1f);
                    var result = Matrix4x4.Translate(midPalm) * 
                        Matrix4x4.Scale(scale) *
                        Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, -rotation)) *
                        Matrix4x4.Translate(new Vector2(-0.5f, -0.5f));
                    return result;
                }
            }
            #endregion


            #region --Operations--
            private readonly Vector2[] points;
            private readonly Vector2 imageSize;
            private readonly Vector2 midPalm => points[2]; // or 3

            internal Detection (Rect rect, float score, Vector2[] points, MLImageType imageType) {
                this.rect = rect;
                this.score = score;
                this.points = points;
                this.imageSize = new Vector2(imageType.width, imageType.height);
            }
            #endregion
        }
    }
}