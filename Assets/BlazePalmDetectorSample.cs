/* 
*   BlazePalm Detector
*   Copyright (c) 2022 NatML Inc. All Rights Reserved.
*/

namespace NatML.Examples {

    using UnityEngine;
    using NatML.Devices;
    using NatML.Devices.Outputs;
    using NatML.Features;
    using NatML.Vision;
    using NatML.Visualizers;

    public sealed class BlazePalmDetectorSample : MonoBehaviour {

        [Header(@"UI")]
        public PalmDetectionVisualizer visualizer;

        private CameraDevice cameraDevice;
        private TextureOutput previewTextureOutput;

        private MLModelData modelData;
        private MLModel model;
        private BlazePalmDetector detector;

        async void Start () {
            // Request camera permissions
            var permissionStatus = await MediaDeviceQuery.RequestPermissions<CameraDevice>();
            if (permissionStatus != PermissionStatus.Authorized) {
                Debug.LogError(@"User did not grant camera permissions");
                return;
            }
            // Get a camera device
            var query = new MediaDeviceQuery(MediaDeviceCriteria.CameraDevice);
            cameraDevice = query.current as CameraDevice;
            // Start the preview
            previewTextureOutput = new TextureOutput();
            cameraDevice.StartRunning(previewTextureOutput);
            // Display the preview
            var previewTexture = await previewTextureOutput;
            visualizer.image = previewTexture;
            // Fetch the model from NatML
            modelData = await MLModelData.FromHub("@natml/blazepalm-detector");
            model = new MLEdgeModel(modelData);
            detector = new BlazePalmDetector(model);
        }

        void Update () {
            // Check that the detector has been loaded
            if (detector == null)
                return;
            // Create an image feature
            var imageFeature = new MLImageFeature(previewTextureOutput.texture);
            (imageFeature.mean, imageFeature.std) = modelData.normalization;
            imageFeature.aspectMode = modelData.aspectMode;
            // Detect palms in the image
            var detections = detector.Predict(imageFeature);
            // Visualize detections
            visualizer.Render(detections);
        }

        void OnDisable () {
            // Dispose the model
            model?.Dispose();
        }
    }
}