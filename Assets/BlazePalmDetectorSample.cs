/* 
*   BlazePalm
*   Copyright © 2023 NatML Inc. All Rights Reserved.
*/

namespace NatML.Examples {

    using UnityEngine;
    using NatML.VideoKit;
    using NatML.Vision;
    using NatML.Visualizers;

    public sealed class BlazePalmDetectorSample : MonoBehaviour {

        [Header(@"Camera")]
        public VideoKitCameraManager cameraManager;

        [Header(@"UI")]
        public PalmDetectionVisualizer visualizer;

        private BlazePalmDetector detector;

        private async void Start () {
            // Create the BlazePalm detector
            detector = await BlazePalmDetector.Create();
            // Listen for camera frames
            cameraManager.OnCameraFrame.AddListener(OnCameraFrame);
        }

        private void OnCameraFrame (CameraFrame frame) {
            // Detect palms
            var detections = detector.Predict(frame);
            // Visualize
            visualizer.Render(detections);
        }

        private void OnDisable () {
            // Stop listening for camera frames
            cameraManager.OnCameraFrame.RemoveListener(OnCameraFrame);
            // Dispose the detector
            detector?.Dispose();
        }
    }
}