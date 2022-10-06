# BlazePalm Detector
Palm detection from Google MediaPipe in Unity Engine.

## Installing BlazePalm Detector
Add the following items to your Unity project's `Packages/manifest.json`:
```json
{
  "scopedRegistries": [
    {
      "name": "NatML",
      "url": "https://registry.npmjs.com",
      "scopes": ["ai.natml"]
    }
  ],
  "dependencies": {
    "ai.natml.vision.blazepalm.detector": "1.0.0"
  }
}
```

## Detecting Hands in an Image
First, create the predictor:
```csharp
// Fetch the model data from NatML Hub
var modelData = await MLModelData.FromHub("@natml/blazepalm-detector");
// Deserialize the model
var model = modelData.Deserialize();
// Create the BlazePalm detector
var detector = new BlazePalmDetector(model);
```

Then create an image feature:
```csharp
// With an image
Texture2D image = ...;
// Create an image feature
var imageFeature = new MLImageFeature(image);
(imageFeature.mean, imageFeature.std) = modelData.normalization;
imageFeature.aspectMode = modelData.aspectMode;
```

Finally, detect hands in the image:
```csharp
// Detect hands in the image
BlazePalmDetector.Detection[] hands = detector.Predict(imageFeature);
```

___

## Requirements
- Unity 2021.2+

## Quick Tips
- Discover more ML models on [NatML Hub](https://hub.natml.ai).
- See the [NatML documentation](https://docs.natml.ai/unity).
- Join the [NatML community on Discord](https://hub.natml.ai/community).
- Discuss [NatML on Unity Forums](https://forum.unity.com/threads/open-beta-natml-machine-learning-runtime.1109339/).
- Contact us at [hi@natml.ai](mailto:hi@natml.ai).

Thank you very much!