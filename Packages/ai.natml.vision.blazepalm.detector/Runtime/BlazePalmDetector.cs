/* 
*   BlazePalm
*   Copyright © 2023 NatML Inc. All Rights Reserved.
*/

namespace NatML.Vision {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using UnityEngine;
    using NatML.Features;
    using NatML.Internal;
    using NatML.Types;

    /// <summary>
    /// Palm detection predictor.
    /// This predictor accepts an image feature and produces a list of palm rectangles.
    /// Palm rectangles are always specified in normalized coordinates.
    /// </summary>
    public sealed partial class BlazePalmDetector : IMLPredictor<BlazePalmDetector.Detection[]> {

        #region --Client API--
        /// <summary>
        /// Detect palms in an image.
        /// </summary>
        /// <param name="inputs">Input image.</param>
        /// <returns>Detected palms.</returns>
        public Detection[] Predict (params MLFeature[] inputs) {
            // Preprocess
            var input = inputs[0];
            var imageType = MLImageType.FromType(input.type);
            var imageFeature = input as MLImageFeature;
            if (imageFeature != null) {
                (imageFeature.mean, imageFeature.std) = model.normalization;
                imageFeature.aspectMode = model.aspectMode;
            }
            // Predict
            var inputType = model.inputs[0] as MLImageType;
            using var inputFeature = (input as IMLEdgeFeature).Create(inputType);
            using var outputFeatures = model.Predict(inputFeature);
            // Decode
            var (widthInv, heightInv) = (1f / inputType.width, 1f / inputType.height);
            var regressionFeature = new MLArrayFeature<float>(outputFeatures[0]);   // (1,2016,18)
            var scoreFeature = new MLArrayFeature<float>(outputFeatures[1]);        // (1,2016,1)
            candidateBoxes.Clear();
            candidateScores.Clear();
            candidatePoints.Clear();
            for (var i = 0; i < anchors.Length; ++i) {
                // Check score
                var score = 1f / (1f + Mathf.Exp(-scoreFeature[0,i,0]));
                if (score < minScore)
                    continue;
                // Extract box
                var anchor = anchors[i];
                var cx = regressionFeature[0,i,0] * widthInv + anchor.x;
                var cy = regressionFeature[0,i,1] * heightInv + anchor.y;
                var w = regressionFeature[0,i,2] * widthInv;
                var h = regressionFeature[0,i,3] * heightInv;
                var rawBox = new Rect((cx - w / 2), 1f - (cy + h / 2), w, h);
                var box = imageFeature?.TransformRect(rawBox, inputType) ?? rawBox;
                // Extract points
                var points = new Vector2[7];
                for (var j = 0; j < points.Length; ++j) {
                    var point = new Vector2(
                        regressionFeature[0,i,2*j+4] * widthInv + anchor.x,
                        1f - (regressionFeature[0,i,2*j+5] * heightInv + anchor.y)
                    );
                    points[j] = imageFeature?.TransformPoint(point, inputType) ?? point;
                }
                // Add
                candidateBoxes.Add(box);
                candidateScores.Add(score);
                candidatePoints.Add(points);
            }
            var keepIdx = MLImageFeature.NonMaxSuppression(candidateBoxes, candidateScores, maxIoU);
            var result = new List<Detection>();
            for (var i = 0; i < keepIdx.Length; ++i) {
                var idx = keepIdx[i];
                var rect = candidateBoxes[idx];
                var score = candidateScores[idx];
                var points = candidatePoints[idx];
                var detection = new Detection(rect, score, points, imageType);
                result.Add(detection);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Dispose the predictor and release resources.
        /// </summary>
        public void Dispose () => model.Dispose();

        /// <summary>
        /// Create the BlazePalm detection predictor.
        /// </summary>
        /// <param name="minScore">Minimum candidate score.</param>
        /// <param name="maxIoU">Maximum intersection-over-union score for overlap removal.</param>
        /// <param name="configuration">Edge model configuration.</param>
        /// <param name="accessKey">NatML access key.</param>
        public static async Task<BlazePalmDetector> Create (
            float minScore = 0.5f,
            float maxIoU = 0.5f,
            MLEdgeModel.Configuration configuration = null,
            string accessKey = null
        ) {
            var model = await MLEdgeModel.Create("@natml/blazepalm-detector", configuration, accessKey);
            var predictor = new BlazePalmDetector(model, minScore, maxIoU);
            return predictor;
        }
        #endregion


        #region --Operations--
        private readonly MLEdgeModel model;
        private readonly float minScore;
        private readonly float maxIoU;
        private readonly Vector2[] anchors;
        private readonly List<Rect> candidateBoxes;
        private readonly List<float> candidateScores;
        private readonly List<Vector2[]> candidatePoints;

        private BlazePalmDetector (MLModel model, float minScore = 0.5f, float maxIoU = 0.5f) {
            this.model = model as MLEdgeModel;
            this.minScore = minScore;
            this.maxIoU = maxIoU;
            var type = model.inputs[0] as MLImageType;
            var strides = new [] { 8, 16, 16, 16 };
            this.anchors = GenerateAnchors(type, strides, 0.1484375f, 0.75f);
            this.candidateBoxes = new List<Rect>(anchors.Length);
            this.candidateScores = new List<float>(anchors.Length);
            this.candidatePoints = new List<Vector2[]>(anchors.Length);
        }

        private static Vector2[] GenerateAnchors (MLImageType type, int[] strides, float minScale, float maxScale, float aspect = 1f) {
            var result = new List<Vector2>();
            var layerId = 0;
            while (layerId < strides.Length) {
                var scales = new List<float>();
                var lastSameStrideLayer = layerId;
                while (lastSameStrideLayer < strides.Length && strides[lastSameStrideLayer] == strides[layerId]) {
                    var scale = CalculateScale(minScale, maxScale, lastSameStrideLayer, strides.Length);
                    scales.Add(scale);
                    var nextScale = Mathf.Clamp01(CalculateScale(minScale, maxScale, lastSameStrideLayer + 1, strides.Length));
                    scales.Add(Mathf.Sqrt(scale * nextScale));
                    lastSameStrideLayer++;
                }
                var stride = strides[layerId];
                var featureWidth  = Mathf.Ceil((float)type.width / stride);
                var featureHeight = Mathf.Ceil((float)type.height / stride);
                for (var j = 0; j < featureHeight; ++j)
                    for (var i = 0; i < featureWidth; ++i)
                        for (var s = 0; s < scales.Count; ++s) {
                            var anchor = new Vector2(
                                (i + 0.5f) / featureWidth,
                                (j + 0.5f) / featureHeight
                            );
                            result.Add(anchor);
                        }
                layerId = lastSameStrideLayer;
            }
            return result.ToArray();
        }
    
        private static float CalculateScale (float minScale, float maxScale, int strideIdx, int strideCount) {
            if (strideCount == 1)
                return (minScale + maxScale) * 0.5f;
            else
                return minScale + (maxScale - minScale) * strideIdx / (strideCount - 1f);
        }
        #endregion
    }
}